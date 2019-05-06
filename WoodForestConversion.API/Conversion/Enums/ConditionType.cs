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
        OnDay = 0,
        DayOfWeek = 1,
        DayOfMonth = 2,
        Month = 3,
        MonthAndDay = 5,
        EndOfDayMonthDay = 6,
        StartOfMonthDay = 7
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

    public enum ExecutionFrequency
    {
        Once = 0,
        ContinueUntil = 1,
        AlwaysExecuting = 2,
        ExecuteWithDependencies = 3,
    }
}