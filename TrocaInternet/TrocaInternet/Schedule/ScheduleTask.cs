
using Microsoft.Win32;
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
    public static void AddToStartup()
    {
        string taskName = "TrocaInternet";
        string exePath = Application.ExecutablePath;
        string workingDir = Path.GetDirectoryName(exePath);

        // Caminho do .bat temporário
        string batPath = Path.Combine(workingDir, "StartTrocaInternet.bat");
        string batContent = $@"@echo off
                                cd /d ""{workingDir}""
                                ""{exePath}""";
        File.WriteAllText(batPath, batContent);

        // Caminho do .vbs que vai chamar o .bat invisível
        string vbsPath = Path.Combine(workingDir, "StartTrocaInternet.vbs");
        string vbsContent = $@"Set WshShell = CreateObject(""WScript.Shell"")
                                WshShell.Run chr(34) & ""{batPath}"" & Chr(34), 0";

        File.WriteAllText(vbsPath, vbsContent);

        // Verifica se a tarefa já existe
        if (TaskExists(taskName))
        {
            //MessageBox.Show("A tarefa já existe.", "Info");
            return;
        }

        // Cria a tarefa apontando para o VBS (janela invisível)
        string args = $"/create /f /rl HIGHEST /sc onlogon /tn \"{taskName}\" /tr \"wscript.exe \"\"{vbsPath}\"\"\" /it";

        RunProcess("schtasks", args, showMessage: true);
    }


    public static void RemoveFromStartup()
    {
        string taskName = "TrocaInternet";

        if (!TaskExists(taskName))
        {
            MessageBox.Show("A tarefa não existe.", "Info");
            return;
        }

        string args = $"/delete /f /tn \"{taskName}\"";
        RunProcess("schtasks", args);
    }

    private static bool TaskExists(string taskName)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = $"/query /tn \"{taskName}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (Process proc = Process.Start(psi))
        {
            proc.WaitForExit();
            return proc.ExitCode == 0; // 0 = tarefa encontrada, 1 = não encontrada
        }
    }

    private static void RunProcess(string fileName, string arguments, bool showMessage = false)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (Process proc = Process.Start(psi))
        {
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (showMessage)
                MessageBox.Show($"Saída:\n{output}\n\nErro:\n{error}", "Debug");
        }
    }
}
