using System;
using System.Collections.Generic;
using System.Text;
using MVPSI.JAMS;
using WoodForestConversion.API.Conversion.Enums;
using Condition = WoodForestConversion.Data.Condition;

namespace WoodForestConversion.API.Conversion.DTOs
{
    public class SetFreqDto
    {
        public SetFreqDto(){}
        public void ProcessCondition(Condition condition)
        {
            IsNegative = condition.IsNegative;
            ConditionType = (ConditionType)condition.ConditionType;
            switch (ConditionType)
            {
                case Enums.ConditionType.JobDependency:
                    JobDependencies.Add(condition.ReferenceUID);
                    break;
                case Enums.ConditionType.FileDependency:
                    FileDependencies.Add($@"{condition.Directory.TrimEnd('\\')}\{condition.FileExists}");
                    break;
                case Enums.ConditionType.RunOn:
                    SetLiteralDate(condition);
                    break;
                case Enums.ConditionType.RunOnTimeWindow:
                    AfterTimeUTC = condition.AfterTimeUtc;
                    BeforeTimeUTC = condition.BeforeTimeUtc;
                    break;
            }
        }

        private void SetLiteralDate(Condition condition)
        {
            if (condition.IsNegative)
            {
                switch ((DateInterval) condition.DateInterval)
                {
                    case Enums.DateInterval.DayOfWeek:
                        NotDayOfWeek.Add((DayOfTheWeek) condition.PrimaryFrequency);
                        break;
                    case Enums.DateInterval.DayOfMonth:
                        NotDayOfTheMonth.Add(condition.PrimaryFrequency);
                        break;
                    case Enums.DateInterval.Month:
                        NotMonths.Add((Month)condition.PrimaryFrequency);
                        break;
                    case Enums.DateInterval.MonthAndDay:
                        NotStringDates.Add($"{condition.SecondaryFrequency}-{(Month)condition.PrimaryFrequency}");
                        break;
                }
            }
            else
            {
                switch ((DateInterval)condition.DateInterval)
                {
                    case Enums.DateInterval.DayOfWeek:
                        DayOfTheWeek.Add((DayOfTheWeek)condition.PrimaryFrequency);
                        break;
                    case Enums.DateInterval.DayOfMonth:
                        DayOfTheMonth.Add(condition.PrimaryFrequency);
                        break;
                    case Enums.DateInterval.Month:
                        Months.Add((Month)condition.PrimaryFrequency);
                        break;
                    case Enums.DateInterval.MonthAndDay:
                        StringDates.Add($"{condition.SecondaryFrequency}-{(Month)condition.PrimaryFrequency}");
                        break;
                }
            }
        }
        public ConditionType? ConditionType { get; set; }
        public DateInterval? DateInterval { get; set; }
        public List<string> StringDates { get; set; } = new List<string>();
        public List<string> NotStringDates { get; set; } = new List<string>();
        public List<DayOfTheWeek> DayOfTheWeek { get; set; } = new List<DayOfTheWeek>();
        public List<DayOfTheWeek> NotDayOfWeek { get; set; } = new List<DayOfTheWeek>();
        public List<byte?> DayOfTheMonth { get; set; } = new List<byte?>();
        public List<byte?> NotDayOfTheMonth { get; set; } = new List<byte?>();
        public List<Month> Months { get; set; } = new List<Month>();
        public List<Month> NotMonths { get; set; } = new List<Month>();
        public DateTime? AfterTimeUTC { get; set; }
        public DateTime? BeforeTimeUTC { get; set; }
        public bool IsNegative { get; set; }
        public List<Guid?> JobDependencies { get; set; } = new List<Guid?>();
        public List<string> FileDependencies { get; set; } = new List<string>();
    }
}
