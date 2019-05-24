using System;
using System.Collections.Generic;
using System.Linq;
using MVPSI.JAMS;
using WoodForestConversion.API.Conversion.ConditionsTree;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.Enums;
using WoodForestConversion.API.Conversion.Jobs;
using WoodForestConversion.Data;
using Condition = WoodForestConversion.Data.Condition;
using Job = WoodForestConversion.Data.Job;

namespace WoodForestConversion.API.Conversion.DTOs
{
    [Serializable]
    public class JobFreqDto
    {
        private TimeSpan? _stopsAtUtc;

        #region Properties
        public TreeNode ConditionsTree { get; set; }
        public ElementCollection Elements { get; set; } = new ElementCollection();
        public ExecutionFrequency ExecutionFrequency { get;}
        public int Interval { get; }
        public TimeSpan? StopsAtUTC
        {
            get => _stopsAtUtc;
            private set
            {
                if (value.HasValue)
                {
                    if (value.Value == default)
                    {
                        _stopsAtUtc = null;
                    }
                    else
                    {
                        _stopsAtUtc = value.Value;
                    }
                }
            }
        }
        public HashSet<Guid?> JobDependencies { get; set; } = new HashSet<Guid?>();
        public HashSet<string> FileDependencies { get; set; } = new HashSet<string>();
        #endregion

        public JobFreqDto(Job job)
        {
            ExecutionFrequency = (ExecutionFrequency)job.Frequency;
            StopsAtUTC = job.StopAtUtc.TimeOfDay;
            Interval = job.Interval;

            BuildConditionTree(job);
            BuildElements(ConditionsTree);
        }

        private void BuildConditionTree(Job job)
        {
            using (var context = new ARCHONEntities())
            {
                var conditionSets = context.ConditionSets.Where(cs => cs.EntityUID == job.JobUID).ToList();
                var root = conditionSets.FirstOrDefault(cs => cs.ParentSet == Guid.Empty);
                if (root == null) throw new Exception($"Job {job.JobName} is missing condition hierarchy");
                var conditions = context.Conditions
                    .Where(c => c.EntityUID == job.JobUID && c.IsLive);
                ConditionsTree = TreeNode.BuildTree(root, conditionSets, conditions);
            }
        }

        private void BuildElements(TreeNode node)
        {
            if (node.Conditions.Any())
            {
                List<Condition> otherConditions = new List<Condition>();

                foreach (var nodeCondition in node.Conditions)
                {
                    if (nodeCondition.ConditionType == (byte) ConditionType.RunOnTimeWindow)
                    {
                        node.NodeScheduleTrigger.Add(new SetFreqDto(nodeCondition));
                    }
                    else
                    {
                        otherConditions.Add(nodeCondition);
                    }
                }

                foreach (var otherCondition in otherConditions)
                {
                    switch ((ConditionType)otherCondition.ConditionType)
                    {
                        case ConditionType.RunOn:
                            UpdateScheduleTrigger(node, otherCondition);
                            break;
                        case ConditionType.JobDependency:
                            JobDependencies.Add(otherCondition.ReferenceUID);
                            break;
                        case ConditionType.FileDependency:
                            FileDependencies.Add($@"{otherCondition.Directory.TrimEnd('\\')}\{otherCondition.FileExists}");
                            break;
                    }
                }
            }
            
            if (node.ChildrenCount > 0)
            {
                foreach (var nodeChild in node.Children)
                {
                    BuildElements(nodeChild);
                }
            }
        }

        private void UpdateScheduleTrigger(TreeNode node, Condition otherCondition)
        {
            if (node.NodeScheduleTrigger.Any())
            {
                foreach (var setFreqDto in node.NodeScheduleTrigger)
                {
                    setFreqDto.SetLiteralDate(otherCondition);
                }
            }
            else
            {
                if (node.Parent == null)
                {
                    node.NodeScheduleTrigger = new List<SetFreqDto>
                    {
                        new SetFreqDto(otherCondition)
                    };
                    return;
                }
                UpdateScheduleTrigger(node.Parent, otherCondition);
            }
        }

        public void PopulateScheduleTriggers(TreeNode node)
        {
            TimeOfDay? CalculateEndTime(SetFreqDto nodeScheduleTrigger)
            {
                TimeOfDay? eTime = null;

                TimeSpan delta = default;
                if (Interval > 1)
                {
                    delta = new TimeSpan(0, 0, -Interval, 0);
                }

                if (nodeScheduleTrigger.BeforeTimeUtc != null)
                {
                    eTime = new TimeOfDay(nodeScheduleTrigger.BeforeTimeUtc.Value.Add(delta));
                }
                else if (StopsAtUTC != null)
                {
                    eTime = new TimeOfDay((int)((int)StopsAtUTC.Value.TotalSeconds - delta.TotalSeconds));
                }
                else if (delta != default)
                {
                    eTime = new TimeOfDay(DateTime.Today.Add(delta));
                }

                return eTime;
            }

            foreach (var nodeScheduleTrigger in node.NodeScheduleTrigger)
            {
                TimeOfDay? endTime = CalculateEndTime(nodeScheduleTrigger);
                TimeOfDay startTime = nodeScheduleTrigger.AfterTimeUtc.HasValue ?
                    new TimeOfDay(nodeScheduleTrigger.AfterTimeUtc.Value) :
                    new TimeOfDay(new DateTime());

                var dates = string.Join(",", nodeScheduleTrigger.StringDates);
                var exDates = string.Join(",", nodeScheduleTrigger.ExceptDates);

                var scheduleTrigger = new ScheduleTrigger(string.IsNullOrWhiteSpace(dates) ? "Daily" : dates, startTime);
                if (!string.IsNullOrWhiteSpace(exDates))
                {
                    scheduleTrigger.ExceptForDate = exDates;
                }

                Elements.Add(scheduleTrigger);

                if (ExecutionFrequency == ExecutionFrequency.Once)
                {
                    #region Runaway
                    if (endTime.HasValue)
                    {
                        var deltaTime = new DeltaTime(Math.Abs(endTime.Value.TotalSeconds - startTime.TotalSeconds));
                        Elements.Add(new RunawayEvent(deltaTime));
                    }
                    #endregion
                }
                else
                {
                    #region Resubmit
                    var end = endTime ?? default;
                    var delta = end.TotalSeconds - startTime.TotalSeconds;
                    if (delta < Interval * 60) continue;

                    Elements.Add(new Resubmit(new DeltaTime(Interval * 60), endTime ?? default));
                    #endregion
                }
            }

            if (node.Children.Any())
            {
                foreach (var conditionsTreeChild in node.Children)
                {
                    PopulateScheduleTriggers(conditionsTreeChild);
                }
            }
        }

        public void PopulateJobDependencies()
        {
            foreach (var jobDependencyId in JobDependencies)
            {
                if (JobConversion.ArchonJobDictionary.TryGetValue(jobDependencyId.Value, out var job))
                {
                    if (JobConversionHelper.HandleATMCreateJob(job.JobName, Elements)) continue;

                    job.JobName = JobConversionHelper.FixJobName(job.JobName);
                    var folder = JobConversion.JobFolderName[jobDependencyId.Value].CategoryName;
                    var destination = string.IsNullOrWhiteSpace(folder) 
                        ? $@"\{ConversionBaseHelper.JamsArchonRootFolder}\{job.JobName}" 
                        : $@"\{ConversionBaseHelper.JamsArchonRootFolder}\{folder}\{job.JobName}";
                    
                    Elements.Add(new JobDependency(destination));
                }
                else
                {
                    Console.WriteLine($"Job {jobDependencyId.Value} was not found");
                }
            }
        }

        public void PopulateFileDependencies()
        {
            foreach (var fileDependency in FileDependencies)
            {
                Elements.Add(new FileDependency(fileDependency));
            }
        }
    }
}