using System;
using System.Collections.Generic;
using WoodForestConversion.API.Conversion.Enums;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.DTOs
{
    public class JobFreqDto
    {
        public JobFreqDto() { }

        public JobFreqDto(Job job)
        {
            ExecutionFrequency = (ExecutionFrequency)job.Frequency;
            TimeSpan = job.StopAtUtc.TimeOfDay;
            Interval = job.Interval;
            ConditionSetDictionary = new Dictionary<Guid, SetFreqDto>();
        }

        public void ProcessCondition(Condition condition)
        {
            if (ConditionSetDictionary.TryGetValue(condition.SetUID, out var conditionSet))
            {
                conditionSet.ProcessCondition(condition);
            }
            else
            {
                SetFreqDto newConditionSet = new SetFreqDto();
                newConditionSet.ProcessCondition(condition);
                ConditionSetDictionary.Add(condition.SetUID, newConditionSet);
            }
        }
        public ExecutionFrequency ExecutionFrequency { get; }
        public int Interval { get; }
        public TimeSpan? TimeSpan { get; }
        public Dictionary<Guid, SetFreqDto> ConditionSetDictionary { get; }
    }
}
