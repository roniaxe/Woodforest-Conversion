using Archon.Tasks;
using MVPSI.JAMS;
using MVPSI.JAMSSequence;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WoodForestConversion.API.Conversion.Base;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.DTOs;
using WoodForestConversion.API.Conversion.Enums;
using WoodForestConversion.Data;
using Condition = WoodForestConversion.Data.Condition;
using Job = MVPSI.JAMS.Job;

namespace WoodForestConversion.API.Conversion.Jobs
{
    public class JobConversion : IEntityToConvert
    {
        private readonly TextWriter _log;
        private readonly Dictionary<Data.Job, JobFreqDto> _jobConditionsDictionary = new Dictionary<Data.Job, JobFreqDto>();
        public static Dictionary<Guid, Data.Job> ArchonJobDictionary;
        public static Dictionary<Guid, JobCategoryDto> JobFolderName;
        public ARCHONEntities ArchonEntities { get; set; }

        public JobConversion(TextWriter log)
        {
            _log = log;
        }
        public void Convert()
        {
            InitializeMembers();
            PopulateJobConditionsDictionary();

            using (ArchonEntities = new ARCHONEntities())
            {
                Dictionary<string, List<Job>> convertedJobs = new Dictionary<string, List<Job>>();

                foreach (var jobConditions in _jobConditionsDictionary)
                {
                    try
                    {
                        Job jamsJob = new Job();

                        Task[] tasks = new Task[3];
                        tasks[0] = ConvertJobDetailsAsync(jobConditions.Key, jamsJob);
                        tasks[1] = ConvertJobConditionsAsync(jobConditions.Value, jamsJob);
                        tasks[2] = AddJobStepsAsync(jobConditions.Key, jamsJob);
                        Task.WaitAll(tasks);

                        if (JobConversionHelper.GenerateExceptions(jamsJob, convertedJobs, jobConditions.Key.JobUID)) continue;

                        if (convertedJobs.TryGetValue(JobFolderName[jobConditions.Key.JobUID]?.CategoryName ?? "", out var jobForFolder))
                        {
                            jobForFolder.Add(jamsJob);
                        }
                        else
                        {
                            convertedJobs.Add(JobFolderName[jobConditions.Key.JobUID]?.CategoryName ?? "", new List<Job> { jamsJob });
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
        }

        private void InitializeMembers()
        {
            using (var context = new ARCHONEntities())
            {
                JobFolderName =
                    (from job in context.Jobs
                    join category in context.Categories
                        on job.Category equals category.CategoryUID into ps
                        from category in ps.DefaultIfEmpty()
                    select new {job.JobUID, category.CategoryName})
                    .ToDictionary(j => j.JobUID, j => new JobCategoryDto(j.JobUID, j.CategoryName));

                ArchonJobDictionary = context.Jobs
                    .Where(j => j.IsLive && !j.IsDeleted)
                    .ToDictionary(j => j.JobUID);
            }
        }

        private void PopulateJobConditionsDictionary()
        {
            foreach (var jobKeyValue in ArchonJobDictionary)
            {
                JobFreqDto jobFreq = new JobFreqDto(jobKeyValue.Value);

                _jobConditionsDictionary.Add(jobKeyValue.Value, jobFreq);
            }
        }

        public async Task ConvertJobDetailsAsync(Data.Job sourceJob, Job targetJob)
        {
            sourceJob.JobName = JobConversionHelper.FixJobName(sourceJob.JobName);

            await Task.Run(() =>
            {
                targetJob.JobName = sourceJob.JobName;
                targetJob.Description = sourceJob.Note;
                targetJob.MethodName = "Sequence";
            });
        }

        public async Task ConvertJobConditionsAsync(object conditions, Job targetJob)
        {
            await Task.Run(() =>
            {
                var jobConditions = (JobFreqDto)conditions;
                jobConditions.PopulateScheduleTriggers(jobConditions.ConditionsTree);
                jobConditions.PopulateFileDependencies();
                jobConditions.PopulateJobDependencies();

                if (!jobConditions.Elements.Any())
                {
                    switch (jobConditions.ExecutionFrequency)
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
                            targetJob.Elements.Add(new Resubmit(new DeltaTime(jobConditions.Interval * 60),
                                new TimeOfDay(DateTime.Today - TimeSpan.FromMinutes(jobConditions.Interval))));
                            break;
                    }
                }
                else
                {
                    foreach (var jobConditionsElement in jobConditions.Elements)
                    {
                        targetJob.Elements.Add(jobConditionsElement);
                    }
                }
            });
        }

        private async Task AddJobStepsAsync(Data.Job sourceJob, Job targetJob)
        {
            await Task.Run(() =>
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
            });
        }
    }
}
