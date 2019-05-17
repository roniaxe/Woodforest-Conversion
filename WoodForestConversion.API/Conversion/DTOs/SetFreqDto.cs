using System;
using System.Collections.Generic;
using WoodForestConversion.API.Conversion.Enums;
using Condition = WoodForestConversion.Data.Condition;

namespace WoodForestConversion.API.Conversion.DTOs
{
    public class SetFreqDto
    {
        public SetFreqDto(Condition condition)
        {
            ProcessCondition(condition);
        }

        #region Properties
        public HashSet<string> StringDates { get; set; } = new HashSet<string>();
        public HashSet<string> ExceptDates { get; set; } = new HashSet<string>();
        public DateTime? AfterTimeUtc { get; set; }
        public DateTime? BeforeTimeUtc { get; set; }
        private string _runOnSpecific;
        #endregion

        public void ProcessCondition(Condition condition)
        {
            switch ((ConditionType)condition.ConditionType)
            {
                case ConditionType.RunOn:
                    SetLiteralDate(condition);
                    break;
                case ConditionType.RunOnTimeWindow:
                    AfterTimeUtc = condition.IsNegative ? condition.BeforeTimeUtc : condition.AfterTimeUtc;
                    BeforeTimeUtc = condition.IsNegative ? condition.AfterTimeUtc : condition.BeforeTimeUtc;
                    break;
            }
        }

        public void SetLiteralDate(Condition condition)
        {
            void AddDateCondition(string strCondition)
            {
                if (_runOnSpecific != null)
                {
                    StringDates.Remove("Daily");
                }

                if (!condition.IsNegative)
                    StringDates.Add(strCondition);
                else
                    ExceptDates.Add(strCondition);
            }

            string dateLiteral;
            switch ((DateInterval?)condition.DateInterval)
            {
                case DateInterval.Daily:
                    StringDates.Add(condition.PrimaryFrequency == 0
                        ? "Daily"
                        : $"Every {condition.PrimaryFrequency} days");
                    break;
                case DateInterval.OnWeekday:
                    if (condition.SecondaryFrequency == 0)
                    {
                        _runOnSpecific = $"{(DayOfTheWeek) condition.PrimaryFrequency}";
                        AddDateCondition(_runOnSpecific);
                    }
                    else
                    {
                        if (condition.SecondaryFrequency == 5)
                        {
                            _runOnSpecific = "last";
                            dateLiteral = $"{_runOnSpecific} {(DayOfTheWeek)condition.PrimaryFrequency} of month";
                        }
                        else
                        {
                            _runOnSpecific = condition.SecondaryFrequency.ToString();
                            dateLiteral = $"{_runOnSpecific} {(DayOfTheWeek)condition.PrimaryFrequency} of month";
                        }

                        AddDateCondition(dateLiteral);
                    }

                    break;
                case DateInterval.DayDuringMonth:
                    if (condition.PrimaryFrequency < 1 || condition.PrimaryFrequency > 31)
                    {
                        _runOnSpecific = "last";
                    }
                    else
                    {
                        _runOnSpecific = condition.PrimaryFrequency.ToString();
                    }
                    AddDateCondition($"{_runOnSpecific} day of month");
                    break;
                case DateInterval.DuringMonth:
                    StringDates.RemoveWhere(str => str.Contains("day of month"));
                    dateLiteral = $"{_runOnSpecific} day of {(Month) condition.PrimaryFrequency}";
                    AddDateCondition(dateLiteral);
                    break;
                case DateInterval.OnDate:
                case DateInterval.AfterOrOnDate:
                case DateInterval.BeforeOrOnDate:
                    _runOnSpecific = $"{condition.SecondaryFrequency}";
                    AddDateCondition($@"{_runOnSpecific}-{(Month)condition.PrimaryFrequency}");
                    break;
                case DateInterval.WeekDuringMonth:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}