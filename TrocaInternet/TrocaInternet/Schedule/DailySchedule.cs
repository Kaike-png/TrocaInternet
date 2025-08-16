namespace TrocaInternet.TrocaInternet.Schedule;

public class DailySchedule : ISchedule
{
    public TimeSpan Time { get; set; }
    public List<int> DaysOfWeek { get; set; } = new List<int>();

    public bool ShouldRun(DateTime lastRun)
    {
        var now = DateTime.Now;

        if (DaysOfWeek.Count > 0 && !DaysOfWeek.Contains((int)now.DayOfWeek))
            return false;

        var scheduledTime = new DateTime(now.Year, now.Month, now.Day, Time.Hours, Time.Minutes, Time.Seconds);

        return now >= scheduledTime && lastRun < scheduledTime;
    }

}
