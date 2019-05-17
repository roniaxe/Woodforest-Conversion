using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WoodForestConversion.API.Conversion.Enums
{
    public enum ConditionType
    {
        JobDependency = 0,
        FileDependency = 1,
        RunOn = 2,
        RunOnTimeWindow = 3
    }

    public enum DateInterval
    {
        Daily = 0,
        OnWeekday = 1,
        DayDuringMonth = 2,
        DuringMonth = 3,
        WeekDuringMonth = 4,
        OnDate = 5,
        BeforeOrOnDate = 6,
        AfterOrOnDate = 7
    }


    public enum DayOfTheWeek
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6
    }

    public enum Month
    {
        January = 1,
        February = 2,
        Match = 3,
        April = 4,
        May = 5,
        June = 6,
        July = 7,
        August = 8,
        September = 9,
        October = 10,
        November = 11,
        December = 12
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ExecutionFrequency
    {
        Once = 0,
        ContinueUntil = 1,
        AlwaysExecuting = 2,
        ExecuteWithDependencies = 3,
    }

    public enum ConditionMatch
    {
        All,
        Any,
    }
}