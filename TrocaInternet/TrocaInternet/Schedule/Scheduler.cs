using TrocaInternet.TrocaInternet;
using TrocaInternet.TrocaInternet.Network;
using TrocaInternet.TrocaInternet.Schedule;
public static class Scheduler
{
    public static List<ScheduledTask> _tasks = new List<ScheduledTask>();

    public static void AddTask(ScheduledTask task)
    {
        _tasks.Add(task);
        Logger.LogInfo($"Tarefa agendada: {task.Name} - {task.Schedule}");
    }

    public static void StartScheduler()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                foreach (var task in _tasks)
                {
                    if (task.ShouldRun())
                    {
                        try
                        {
                            await task.ExecuteAsync();
                            task.LastRun = DateTime.Now;
                            Logger.LogInfo($"Tarefa executada: {task.Name}");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Erro ao executar tarefa {task.Name}: {ex.Message}");
                        }
                    }
                }

                await Task.Delay(60000); // Verifica a cada minuto
            }
        });
    }

    public static void CreateGatewaySwitchSchedule()
    {
        Console.Clear();
        Console.WriteLine("\n   Agendar troca de gateway");
        Console.Write("   Nome da tarefa: ");
        string name = Console.ReadLine();

        Console.Write("   Horário (HH:mm): ");
        if (!TimeSpan.TryParse(Console.ReadLine(), out TimeSpan time))
        {
            Console.WriteLine("   Horário inválido.");
            Pause();
            return;
        }

        Console.Write("   Dias da semana (ex: 1,2,3,4,5 para seg-sex): ");
        string daysInput = Console.ReadLine();
        var days = daysInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(d => int.Parse(d.Trim()))
                          .ToList();

        var task = ScheduledTask.Create(
            name,
            new DailySchedule { Time = time, DaysOfWeek = days },
            async () => await NetworkHelper.ChangeGatewayAsync()
        );

        AddTask(task);
        Console.WriteLine($"\n   Tarefa '{name}' agendada com sucesso!");
        Pause();
    }

    private static void Pause()
    {
        Console.WriteLine("\n   Pressione qualquer tecla para continuar...");
        Console.ReadKey();
        Console.Clear();
    }

    public static void ListTasks()
    {
        Console.Clear();
        Console.WriteLine("\n   TAREFAS AGENDADAS:");

        if (_tasks.Count == 0)
        {
            Console.WriteLine("   Nenhuma tarefa agendada.");
        }
        else
        {
            foreach (var task in _tasks)
            {
                Console.WriteLine($"\n   Nome: {task.Name}");
                Console.WriteLine($"   Agendamento: {task.Schedule}");
                Console.WriteLine($"   Última execução: {(task.LastRun == default ? "Nunca" : task.LastRun.ToString("dd/MM/yyyy HH:mm"))}");
            }
        }

        Pause();
    }
    public static async Task ManageScheduledTasks()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("\n   GERENCIAMENTO DE TAREFAS AGENDADAS");
            Console.WriteLine("   1. Agendar troca de gateway");
            Console.WriteLine("   2. Listar tarefas");
            Console.WriteLine("   3. Voltar");
            Console.Write("\n   Escolha uma opção: ");

            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    CreateGatewaySwitchSchedule();
                    break;
                case "2":
                    ScheduledTask.ListScheduledTasks();
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("   Opção inválida.");
                    await Task.Delay(1000);
                    break;
            }
        }
    }
}