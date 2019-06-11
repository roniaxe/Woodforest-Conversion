using Archon.Tasks;
using MVPSI.JAMS;
using MVPSI.JAMSSequence;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WoodForestConversion.API.Conversion.Base;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.DTOs;
using WoodForestConversion.API.Conversion.Enums;
using WoodForestConversion.Data;
using Job = MVPSI.JAMS.Job;

namespace WoodForestConversion.API.Conversion.Jobs
{
    public class JobConversion : IEntityToConvert
    {
        private readonly TextWriter _log;
        private readonly ConcurrentDictionary<Data.Job, JobFreqDto> _concurrentDictionary = new ConcurrentDictionary<Data.Job, JobFreqDto>();
        public Dictionary<Guid, IGrouping<Guid, ServiceModuleDto>> ServiceModuleDictionary { get; set; }
        public ARCHONEntities ArchonEntities { get; set; }
        private static readonly Random Rnd = new Random();

        public JobConversion(TextWriter log)
        {
            _log = log;
            ArchonEntities = new ARCHONEntities();
        }
        public void Convert()
        {
            ServiceModuleDictionary = CreateServiceModuleDtoAsync();
            InitializeMembers();
            PopulateJobConditionsDictionary();

            Dictionary<string, List<Job>> convertedJobs = new Dictionary<string, List<Job>>();

            foreach (var jobConditions in _concurrentDictionary)
            {
                try
                {
                    Job jamsJob = new Job();
                    Console.WriteLine($"Converting {jobConditions.Key.JobName}");
                    ConvertJobDetailsAsync(jobConditions.Key, jamsJob);
                    ConvertJobConditionsAsync(jobConditions.Value, jamsJob);
                    AddJobStepsAsync(jobConditions.Key, jamsJob);

                    if (JobConversionHelper.GenerateExceptions(jamsJob, convertedJobs, jobConditions.Key.JobUID)) continue;

                    var jobCategoryName = JobConversionHelper.JobFolderName[jobConditions.Key.JobUID]?.CategoryName;
                    if (convertedJobs.TryGetValue(jobCategoryName ?? "", out var jobForFolder))
                    {
                        jobForFolder.Add(jamsJob);
                    }
                    else
                    {
                        convertedJobs.Add(jobCategoryName ?? "", new List<Job> { jamsJob });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

            foreach (var convertedJob in convertedJobs)
            {
                JobConversionHelper.CreateDummyJobExportXml(convertedJob);

                JAMSXmlSerializer.WriteXml(convertedJob.Value, $@"{ConversionBaseHelper.XmlOutputLocation}\Jobs\{convertedJob.Key}\2_run_second-{convertedJob.Key}.xml");
            }
        }

        private void InitializeMembers()
        {
            JobConversionHelper.JobFolderName =
                (from job in ArchonEntities.Jobs
                 join category in ArchonEntities.Categories
                     on job.Category equals category.CategoryUID into ps
                 from category in ps.DefaultIfEmpty()
                 select new { job.JobUID, category.CategoryName })
                .ToDictionary(j => j.JobUID, j => new JobCategoryDto(j.JobUID, j.CategoryName));

            JobConversionHelper.ArchonJobDictionary = ArchonEntities.Jobs
                .Where(j => j.IsLive && !j.IsDeleted)
                .ToDictionary(j => j.JobUID);

        }

        private Dictionary<Guid, IGrouping<Guid, ServiceModuleDto>> CreateServiceModuleDtoAsync()
        {
            return (
                from serviceModule in ArchonEntities.ServiceModules
                join executionModule in ArchonEntities.ExecutionModules on serviceModule.ModuleUID equals executionModule.ModuleUID
                join jobService in ArchonEntities.JobServices on serviceModule.ServiceUID equals jobService.ServiceUID
                where jobService.Available && jobService.Capacity > 0
                select new ServiceModuleDto
                {
                    ModuleName = executionModule.ModuleName,
                    ModuleUid = executionModule.ModuleUID,
                    ServiceUID = jobService.ServiceUID,
                    ServiceName = jobService.ServiceName
                })
                .GroupBy(arg => arg.ModuleUid)
                //.ToList()
                .ToDictionary(dtos => dtos.Key);
        }

        private void PopulateJobConditionsDictionary()
        {
            // Get the partitioner.
            OrderablePartitioner<KeyValuePair<Guid, Data.Job>> partitioner = Partitioner.Create(JobConversionHelper.ArchonJobDictionary);

            // creation strategies.
            IList<IEnumerator<KeyValuePair<Guid, Data.Job>>> partitions = partitioner.GetPartitions(Environment.ProcessorCount / 2);

            // Create a task for each partition.
            Task[] tasks = partitions.Select(p => Task.Run(() =>
            {
                // Create the context.
                using (var ctx = new ARCHONEntities())
                // Remember, the IEnumerator<T> implementation
                // might implement IDisposable.
                using (p)
                    // While there are items in p.
                    while (p.MoveNext())
                    {
                        // Get the current item.
                        var current = p.Current;
                        JobFreqDto jobFreq = new JobFreqDto(current.Value, ctx);

                        _concurrentDictionary.AddOrUpdate(current.Value, jobFreq, (job1, dto) => dto);
                    }
            })).
                // ToArray is needed (or something to materialize the list) to
                // avoid deferred execution.
                ToArray();

            Task.WaitAll(tasks);
        }

        private void ConvertJobDetailsAsync(Data.Job sourceJob, Job targetJob)
        {
            sourceJob.JobName = JobConversionHelper.FixJobName(sourceJob.JobName);

            targetJob.JobName = sourceJob.JobName;
            targetJob.Description = sourceJob.Note;
            targetJob.MethodName = "Sequence";
        }

        private void ConvertJobConditionsAsync(JobFreqDto conditions, Job targetJob)
        {
            conditions.PopulateScheduleTriggers(conditions.ConditionsTree);
            conditions.PopulateFileDependencies();
            conditions.PopulateJobDependencies(targetJob.JobName);

            if (!conditions.Elements.Any())
            {
                switch (conditions.ExecutionFrequency)
                {
                    //
                    // Manual jobs - Do nothing for now
                    //
                    case ExecutionFrequency.Once:
                        break;
                    //
                    // Always executing, with intervals (Everyday + Resubmit)
                    // 
                    case ExecutionFrequency.AlwaysExecuting:
                        targetJob.Elements.Add(new ScheduleTrigger("Daily", new TimeOfDay(new DateTime())));
                        targetJob.Elements.Add(new Resubmit(new DeltaTime(conditions.Interval * 60),
                            new TimeOfDay(DateTime.Today - TimeSpan.FromMinutes(conditions.Interval))));
                        break;
                }
            }
            else
            {
                foreach (var jobConditionsElement in conditions.Elements)
                {
                    targetJob.Elements.Add(jobConditionsElement);
                }
            }
        }

        private void AddJobStepsAsync(Data.Job sourceJob, Job targetJob)
        {
            SequenceTask sequenceTask = new SequenceTask();
            sequenceTask.Properties.SetValue("ParentTaskID", Guid.Empty);
            targetJob.SourceElements.Add(sequenceTask);

            var archonSteps = ArchonEntities.JobSteps
                .Where(js => js.JobUID == sourceJob.JobUID && !js.IsDeleted && js.IsLive)
                .OrderBy(js => js.DisplayID).Select(js => new ArchonStepDto
                {
                    ArchonStepName = js.StepName,
                    ArchonConfiguration = js.ConfigurationFile,
                    ParentTaskID = sequenceTask.ElementUid,
                    DisplayTitle = js.StepName,
                    ExecutionModule =
                        ArchonEntities.ExecutionModules.FirstOrDefault(em => em.ModuleUID == js.ModuleUID)
                });

            SelectAgent(archonSteps, targetJob);

            foreach (var archonStep in archonSteps)
            {
                var archonTask = Element.Create("ArchonStep");
                archonTask.Properties.SetValue("ArchonStepName", archonStep.ArchonStepName);
                archonTask.Properties.SetValue("ArchonModuleName", archonStep.ExecutionModule.ModuleName);
                archonTask.Properties.SetValue("ArchonConfiguration", archonStep.ArchonConfiguration);
                archonTask.Properties.SetValue("ArchonModuleAssembly", ModuleAssemblyConverter.FromString(archonStep.ExecutionModule.ModuleAssembly));
                archonTask.Properties.SetValue("ArchonModuleObject", ModuleObjectConverter.FromString(archonStep.ExecutionModule.ModuleObject));
                archonTask.Properties.SetValue("ParentTaskID", archonStep.ParentTaskID);
                archonTask.Properties.SetValue("DisplayTitle", archonStep.DisplayTitle);
                targetJob.SourceElements.Add(archonTask);
            }
        }

        private void SelectAgent(IQueryable<ArchonStepDto> archonSteps, Job targetJob)
        {
            HashSet<Guid> jobModules = new HashSet<Guid>(archonSteps.Select(step => step.ExecutionModule.ModuleUID));
            if (!jobModules.Any()) return;

            try
            {
                IEnumerable<string> mergedList = null;
                foreach (var jobModule in jobModules)
                {
                    IEnumerable<string> serviceList;

                    if (ServiceModuleDictionary.ContainsKey(jobModule))
                    {
                        serviceList = ServiceModuleDictionary[jobModule].Select(dto => dto.ServiceName);
                    }
                    else
                    {
                        Console.WriteLine($"No service found to run module {jobModule} - Job: {targetJob.JobName}");
                        continue;
                    }

                    if (mergedList == null)
                    {
                        mergedList = serviceList;
                    }
                    else
                    {
                        mergedList = mergedList.Join(serviceList,
                            s => s, s => s, (s, s1) => s);
                    }
                }

                if (mergedList == null)
                {
                    Console.WriteLine($"Job {targetJob.JobName} has no agent to run on. No agent will be assigned.");
                    return;
                }

                int r = Rnd.Next(mergedList.Count());

                targetJob.AgentName = mergedList.ElementAt(r);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
