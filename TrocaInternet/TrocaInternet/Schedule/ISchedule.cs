namespace TrocaInternet.TrocaInternet.Schedule;

public interface ISchedule
{
    bool ShouldRun(DateTime lastRun);
}
