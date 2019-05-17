using MVPSI.JAMS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Archon.Tasks;
using MVPSI.JAMSSequence;
using WoodForestConversion.API.Conversion.Base;
using WoodForestConversion.API.Conversion.DTOs;
using WoodForestConversion.API.Conversion.Enums;
using WoodForestConversion.Data;
using Job = MVPSI.JAMS.Job;

namespace WoodForestConversion.API.Conversion.Jobs
{
    public class JobConversion : IEntityToConvert<Job>
    {
        private readonly TextWriter _log;
        private readonly Dictionary<Data.Job, JobFreqDto> _jobConditionsDictionary = new Dictionary<Data.Job, JobFreqDto>();
        public static Dictionary<Guid, Data.Job> ArchonJobDictionary;
        public static Dictionary<Guid, string> JobFolderName;
        public ARCHONEntities ArchonEntities { get; set; }

        public JobConversion(TextWriter log)
        {
            _log = log;
        }
        public ICollection<Job> Convert()
        {
            using (ArchonEntities = new ARCHONEntities())
            {
                PopulateJobConditionsDictionary();
                //JobConversionHelper.ObjectToJson(@"c:\deps.json", _jobConditionsDictionary);

                // duplicates
                var duplicates = ArchonEntities.Jobs
                    .GroupBy(x => x.JobName)
                    .Where(g => g.Count() > 1)
                    .Select(y => y.Key)
                    .ToList();

                JobFolderName = ArchonEntities.Jobs
                    .ToDictionary(j => j.JobUID, job =>
                        ArchonEntities.Categories.FirstOrDefault(cat => cat.CategoryUID == job.Category.Value)
                            ?.CategoryName ?? "");

                Dictionary<string, List<Job>> convertedJobs = new Dictionary<string, List<Job>>();
                foreach (var jobConditions in _jobConditionsDictionary)
                {
                    Job jamsJob = new Job();

                    if (JobConversionHelper.CheckNonConvertible(jobConditions.Key.JobName)) continue;
                    if (JobConversionHelper.GenerateExceptions(jobConditions.Key, convertedJobs)) continue;

                    Task[] tasks = new Task[3];
                    tasks[0] = ConvertJobDetailsAsync(jobConditions.Key, jamsJob, duplicates);
                    tasks[1] = ConvertJobConditionsAsync(jobConditions.Value, jamsJob);
                    tasks[2] = AddJobStepsAsync(jobConditions.Key, jamsJob);
                    Task.WaitAll(tasks);

                    if (convertedJobs.TryGetValue(JobFolderName[jobConditions.Key.JobUID], out var jobForFolder))
                    {
                        jobForFolder.Add(jamsJob);
                    }
                    else
                    {
                        convertedJobs.Add(JobFolderName[jobConditions.Key.JobUID], new List<Job> {jamsJob});
                    }
                }

                //JobConversionHelper.ObjectToJson(@"c:\jobs.json", convertedJobs);
                foreach (var convertedJob in convertedJobs)
                {
                    JAMSXmlSerializer.WriteXml(convertedJob.Value, $@"c:\{convertedJob.Key}.xml");
                }

                return null;
            }
        }

        private void PopulateJobConditionsDictionary()
        {
            ArchonJobDictionary = ArchonEntities.Jobs
                .Where(j => j.IsLive && !j.IsDeleted)
                .ToDictionary(j => j.JobUID);

            foreach (var jobKeyValue in ArchonJobDictionary)
            {
                JobFreqDto jobFreq = new JobFreqDto(jobKeyValue.Value);

                _jobConditionsDictionary.Add(jobKeyValue.Value, jobFreq);
            }
        }

        public async Task ConvertJobDetailsAsync(Data.Job sourceJob, Job targetJob, List<string> duplicates)
        {
            string jobName;
            if (duplicates.Contains(sourceJob.JobName))
            {
                jobName = sourceJob.JobName + "2";
                duplicates.Remove(sourceJob.JobName);
            }
            else
            {
                jobName = sourceJob.JobName;
            }

            await Task.Run(() =>
            {
                targetJob.JobName = jobName;
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
