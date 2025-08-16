
using System.Diagnostics;

namespace TrocaInternet.TrocaInternet.Schedule;

public class ScheduledTask
{
    public string Name { get; set; }
    public ISchedule Schedule { get; set; }
    public Func<Task> Action { get; set; }
    public DateTime LastRun { get; set; }

    public bool ShouldRun()
    {
        return Schedule.ShouldRun(LastRun);
    }

    public async Task ExecuteAsync()
    {
        await Action();
    }

    // Método estático para criar tarefas facilmente
    public static ScheduledTask Create(string name, ISchedule schedule, Func<Task> action)
    {
        return new ScheduledTask
        {
            Name = name,
            Schedule = schedule,
            Action = action
        };
    }

    public static void ListScheduledTasks()
    {
        Console.Clear();
        Console.WriteLine("\n   TAREFAS AGENDADAS:");

        if (Scheduler._tasks.Count == 0)
        {
            Console.WriteLine("   Nenhuma tarefa agendada.");
        }
        else
        {
            foreach (var task in Scheduler._tasks)
            {
                Console.WriteLine($"\n   Nome: {task.Name}");
                Console.WriteLine($"   Agendamento: {task.Schedule}");
                Console.WriteLine($"   Última execução: {(task.LastRun == default ? "Nunca" : task.LastRun.ToString("dd/MM/yyyy HH:mm"))}");
            }
        }

        Program.Pause();
    }

    public static void CreateScheduledTask()
    {
        string taskName = "TrocaInternet";
        string exePath = Process.GetCurrentProcess().MainModule.FileName;

        Process.Start(new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = $"/create /tn \"{taskName}\" /tr \"{exePath}\" /sc onlogon /rl highest /f",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });
    }
}
