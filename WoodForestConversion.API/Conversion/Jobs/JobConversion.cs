using System;
using MVPSI.JAMS;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WoodForestConversion.API.Conversion.Base;
using WoodForestConversion.API.Conversion.DTOs;
using WoodForestConversion.API.Conversion.Enums;
using WoodForestConversion.Data;
using Job = MVPSI.JAMS.Job;

namespace WoodForestConversion.API.Conversion.Jobs
{
    public class JobConversion : IJobConverter<Data.Job, Job>
    {
        private readonly TextWriter _log;
        private readonly Dictionary<Data.Job, JobFreqDto> _jobConditionsDictionary = new Dictionary<Data.Job, JobFreqDto>();
        private Dictionary<Guid, Data.Job> _jobDictionary;
        public ARCHONEntities ArchonEntities { get; set; }

        public JobConversion(TextWriter log)
        {
            _log = log;
        }
        public ICollection<Job> Convert()
        {
            using (ArchonEntities = new ARCHONEntities())
            {
                _log.WriteLine("Starting Job Conversion");
                _log.WriteLine("-----------------------");
                _log.WriteLine();

                _jobDictionary = ArchonEntities.Jobs.ToDictionary(j => j.JobUID);
                PopulateJobConditionsDictionary();
                
                List<Job> convertedJobs = new List<Job>();
                foreach (var jobConditions in _jobConditionsDictionary)
                {
                    _log.WriteLine($"Converting Job: {jobConditions.Key.JobName}");
                    Job jamsJob = new Job();

                    ConvertJobDetails(jobConditions.Key, jamsJob);
                    ConvertJobConditions(jobConditions.Value, jamsJob);


                    convertedJobs.Add(jamsJob);
                }

                return convertedJobs;
            }
        }

        private void PopulateJobConditionsDictionary()
        {
            var jobs = ArchonEntities.Jobs.Where(j => !j.IsDeleted && j.IsLive);

            foreach (var job in jobs)
            {
                var jobConditions = ArchonEntities.Conditions
                    .Where(c => c.IsLive && c.EntityUID == job.JobUID);
                JobFreqDto jobFreq = new JobFreqDto(job);
                foreach (var jobCondition in jobConditions)
                {
                    jobFreq.ProcessCondition(jobCondition);
                }
                _jobConditionsDictionary.Add(job, jobFreq);
            }
        }

        public void ConvertJobDetails(Data.Job sourceJob, Job targetJob)
        {
            var sJob = sourceJob;
            var tJob = targetJob;
            _log.WriteLine($"Converting Job {sJob.JobName} General Details");

            tJob.JobName = sJob.JobName;
            tJob.Description = sJob.Note;
        }

        public void ConvertJobConditions(object conditions, Job targetJob)
        {
            var c = (JobFreqDto)conditions;
            var j = targetJob;
            _log.WriteLine("Converting job dependencies and triggers");

            if (!c.ConditionSetDictionary.Any())
            {
                switch (c.ExecutionFrequency)
                {
                    //
                    // Setting a daily jobs, resubmitting with interval. 
                    // These jobs has no conditions, so the schedule rules are from Job.Frequency, Job.Interval
                    //
                    case ExecutionFrequency.AlwaysExecuting:
                        targetJob.Elements.Add(new ScheduleTrigger("Daily", new TimeOfDay(new DateTime())));
                        targetJob.Elements.Add(new Resubmit(new DeltaTime(c.Interval * 60),
                                new TimeOfDay(DateTime.Today - TimeSpan.FromMinutes(c.Interval))));
                        break;
                }
            }
            else
            {
                foreach (var setConditionRules in c.ConditionSetDictionary)
                {
                    AddJobDependency(j, setConditionRules.Value.JobDependencies);
                    AddFileDependency(j, setConditionRules.Value.FileDependencies);
                    AddScheduleTrigger(j, setConditionRules.Value, c.Interval, c.TimeSpan);
                }
            }
        }

        private void AddScheduleTrigger(Job targetJob, SetFreqDto setConditionRules, int jobInterval, TimeSpan? stopTime)
        {
            switch (setConditionRules.DateInterval)
            {
                case DateInterval.OnDay:
                    TimeOfDay endTime = CalculateEndTime(setConditionRules, jobInterval, stopTime);
                    TimeOfDay startTime = setConditionRules.AfterTimeUTC.HasValue ?
                        new TimeOfDay(setConditionRules.AfterTimeUTC.Value) :
                        new TimeOfDay(new DateTime());

                    targetJob.Elements.Add(new ScheduleTrigger("Daily", startTime));
                    if (jobInterval > 1)
                    {
                        targetJob.Elements.Add(new Resubmit(new DeltaTime(jobInterval * 60), endTime));
                    }
                    break;
            }
        }

        private static TimeOfDay CalculateEndTime(SetFreqDto setConditionRules, int jobInterval, TimeSpan? stopTime)
        {
            TimeOfDay endTime;
            if (setConditionRules.BeforeTimeUTC != null)
            {
                endTime = new TimeOfDay(setConditionRules.BeforeTimeUTC.Value);
            }
            else if (stopTime != null)
            {
                endTime = new TimeOfDay((DateTime) (new DateTime() + stopTime));
            }
            else
            {
                endTime = new TimeOfDay(DateTime.Today - TimeSpan.FromMinutes(jobInterval));
            }

            return endTime;
        }

        private void AddFileDependency(Job targetJob, List<string> setFileDependencies)
        {
            foreach (var fileDependency in setFileDependencies)
            {
                targetJob.Elements.Add(new FileDependency(fileDependency));
            }
        }

        private void AddJobDependency(Job targetJob, List<Guid?> setJobDependencies)
        {
            foreach (var jobDependencyId in setJobDependencies)
            {
                var jobName = _jobDictionary[jobDependencyId.Value];
                targetJob.Elements.Add(new JobDependency($@"\{jobName}"));
            }

            //Element jobDependency = Element.Create("JobDependency");
            //jobDependency.Properties.SetValue("DependsOnJob", new JobReference($@"\{dependentJob.JobName}", false));
            //targetJob.Elements.Add(jobDependency);
        }
    }
}
