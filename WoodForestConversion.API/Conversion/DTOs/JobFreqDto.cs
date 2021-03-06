﻿using MVPSI.JAMS;
using System;
using System.Collections.Generic;
using System.Linq;
using WoodForestConversion.API.Conversion.ConditionsTree;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.Enums;
using WoodForestConversion.API.Conversion.JobsHelpers;
using WoodForestConversion.Data;
using Condition = WoodForestConversion.Data.Condition;
using Job = WoodForestConversion.Data.Job;

namespace WoodForestConversion.API.Conversion.DTOs
{
    public class JobFreqDto
    {
        #region Properties
        private readonly ARCHONEntities _ctx;
        private TimeSpan? _stopsAtUtc;
        private TimeSpan? StopsAtUTC
        {
            get => _stopsAtUtc;
            set
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
        private HashSet<Guid?> JobDependencies { get; } = new HashSet<Guid?>();
        private HashSet<string> FileDependencies { get; } = new HashSet<string>();
        public TreeNode ConditionsTree { get; private set; }
        public ElementCollection Elements { get; } = new ElementCollection();
        public ExecutionFrequency ExecutionFrequency { get; }
        public int Interval { get; }
        #endregion

        public JobFreqDto(Job job, ARCHONEntities ctx)
        {
            _ctx = ctx;
            ExecutionFrequency = (ExecutionFrequency)job.Frequency;
            StopsAtUTC = job.StopAtUtc.TimeOfDay;
            Interval = job.Interval;

            BuildConditionTree(job);
            BuildElements(ConditionsTree);
        }

        private void BuildConditionTree(Job job)
        {
            var conditionSets = _ctx.ConditionSets.Where(cs => cs.EntityUID == job.JobUID);
            var root = conditionSets.FirstOrDefault(cs => cs.ParentSet == Guid.Empty);
            if (root == null) throw new Exception($"Job {job.JobName} is missing condition hierarchy");
            var conditions = _ctx.Conditions.Where(c => c.EntityUID == job.JobUID && c.IsLive);
            ConditionsTree = TreeNode.GetConditionTree(root, conditionSets, conditions);
        }

        private void BuildElements(TreeNode node)
        {
            if (node.Conditions.Any())
            {
                List<Condition> otherConditions = new List<Condition>();

                foreach (var nodeCondition in node.Conditions)
                {
                    if (nodeCondition.ConditionType == (byte)ConditionType.RunOnTimeWindow)
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

        public void PopulateScheduleTriggers(TreeNode node, byte style)
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

                if (style == (int) JobStyle.ManualTask) scheduleTrigger.Properties.SetValue("TriggerSubmitOnHold", true);

                if (ExecutionFrequency == ExecutionFrequency.Once)
                {
                    #region Runaway
                    if (endTime.HasValue)
                    {
                        var deltaTime = new DeltaTime(Math.Abs(endTime.Value.TotalSeconds - startTime.TotalSeconds));
                        if (!Elements.Any(e => e is RunawayEvent))
                        {
                            Elements.Add(new RunawayEvent(deltaTime));
                        }
                    }
                    #endregion
                }
                else
                {
                    #region Resubmit
                    var end = endTime ?? default;
                    var delta = end.TotalSeconds - startTime.TotalSeconds;
                    if (delta < Interval * 60) continue;

                    if (!Elements.Any(e => e is Resubmit))
                    {
                        Elements.Add(new Resubmit(new DeltaTime(Interval * 60), endTime ?? default));
                    }
                    #endregion
                }
            }

            if (node.Children.Any())
            {
                foreach (var conditionsTreeChild in node.Children)
                {
                    PopulateScheduleTriggers(conditionsTreeChild, style);
                }
            }
        }

        public void PopulateJobDependencies(string targetJobName, byte style)
        {
            if (JobConversionHelper.HandleATMCreateJob(targetJobName, Elements)) return;
            foreach (var jobDependencyId in JobDependencies)
            {
                if (JobConversionHelper.ArchonJobDictionary.TryGetValue(jobDependencyId.Value, out var job))
                {
                    job.JobName = JobConversionHelper.FixJobName(job.JobName);
                    var folder = JobConversionHelper.JobFolderName[jobDependencyId.Value].CategoryName;
                    var destination = string.IsNullOrWhiteSpace(folder)
                        ? $@"\{ConversionBaseHelper.JamsArchonRootFolder}\{job.JobName}"
                        : $@"\{ConversionBaseHelper.JamsArchonRootFolder}\{folder}\{job.JobName}";

                    Element trigger;

                    if (style == (int) JobStyle.ManualTask)
                    {
                        trigger = new JobCompletionTrigger(destination);
                        trigger.Properties.SetValue("TriggerSubmitOnHold", true);
                    }
                    else
                    {
                        trigger = new JobDependency(destination);
                    }

                    Elements.Add(trigger);
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