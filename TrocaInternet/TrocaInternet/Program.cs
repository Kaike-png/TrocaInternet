using System.Diagnostics;
using System.Runtime.InteropServices;
using TrocaInternet.TrocaInternet.Network;
using TrocaInternet.TrocaInternet.Schedule;

namespace TrocaInternet.TrocaInternet;

internal static class Program
{
    public static readonly string StoreConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "store_config.txt");
    public static NotifyIcon _notifyIcon;
    private const int CtrlCloseEvent = 2;

    private delegate bool ConsoleEventDelegate(int eventType);
    private static ConsoleEventDelegate _handler;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    private const int SwHide = 0;

    [STAThread]
    static async Task Main(string[] args)
    {
        // Verifica se o aplicativo foi iniciado após uma atualização
        string updateFlagFile = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!, ".updated");
        if (File.Exists(updateFlagFile))
        {
            try
            {
                File.Delete(updateFlagFile);

                // Mostra uma notificação de que a atualização foi concluída
                MessageBox.Show(
                    "O TrocaInternet foi atualizado com sucesso!",
                    "Atualização Concluída",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Erro ao processar arquivo de atualização: {ex.Message}");
            }
        }
        // Carrega configurações
        Config.Load();

        // Cria uma tarefa agendada para iniciar com o Logon do Usuário 
        ScheduledTask.AddToStartup();
        // Configura o manipulador de eventos do console
        _handler = ConsoleEventCallback;
        SetConsoleCtrlHandler(_handler, true);

        // Inicia monitoramento em uma thread separada
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await NetworkHelper.ChangeGatewayAsync();
                await Task.Delay(Config.CheckInterval);
            }
        });
        // Inicia sistema de notificações
        _ = Task.Run(async () =>
        {
            await NotificationManager.MonitorAndNotifyAsync();
        });

        // Inicia agendador de tarefas
        Scheduler.StartScheduler();

        // Esconde a janela do console
        IntPtr handle = GetConsoleWindow();
        ShowWindow(handle, SwHide);

        // Inicializa o NotifyIcon na bandeja do sistema
        _notifyIcon = new NotifyIcon
        {
            Icon = new Icon("Assets/Chama.ico"),
            Visible = true,
            Text = "Troca Internet",
        };

        // Cria o ContextMenuStrip
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Abrir Console", null, (s, e) => ShowConsoleWindow());
        contextMenu.Items.Add("Testar Conectividade", null, async (s, e) =>
        {
            ShowConsoleWindow();
            await NetworkHelper.TestConnectivityAsync();
        });
        contextMenu.Items.Add("Restaurar DHCP", null, (s, e) =>
        {
            ShowConsoleWindow();
            NetworkHelper.RestoreDhcp();
        });
        contextMenu.Items.Add("Aplicar Último Perfil", null, (s, e) =>
        {
            ShowConsoleWindow();
            NetworkProfileManager.ApplyLastProfile();
        });
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Sair", null, (s, e) =>
        {
            _notifyIcon.Dispose();
            Application.Exit();
        });

        _notifyIcon.ContextMenuStrip = contextMenu;

        // Thread para monitoramento contínuo do startUp 
        Thread monitoringThread = new Thread(MonitorStartUp)
        {
            IsBackground = true
        };
        monitoringThread.Start();

        // Verifica atualizações ao iniciar
        UpdateManager.CheckForUpdatesAsync();

        // Mantém o ícone na bandeja ativo
        Application.Run();
              
    }

    public static string SafeReadLine()
    {
        if (!IsConsoleVisible())
        {
            return string.Empty;
        }
        try
        {
            return Console.ReadLine()!;
        }
        catch (IOException)
        {
            return string.Empty;
        }
    }

    private static async Task StartUp()
    {
        while (IsConsoleVisible())
        {
            Console.Clear();
            Console.WriteLine(@"       ");
            Console.WriteLine("\n   1. Verificar conexão atual");
            Console.WriteLine("   2. Alterar final do gateway");
            Console.WriteLine("   3. Configurar número da loja");
            Console.WriteLine("   4. Testar conectividade");
            Console.WriteLine("   5. Restaurar configuração DHCP");
            Console.WriteLine("   6. Visualizar logs");
            Console.WriteLine("   7. Teste de velocidade (servidores padrão)");
            Console.WriteLine("   8. Teste de velocidade (servidor personalizado)");
            Console.WriteLine("   9. Traceroute");
            Console.WriteLine("   10. Gerenciar perfis de rede");
            Console.WriteLine("   11. Agendar tarefas");
            Console.WriteLine("   12. Testar notificação");
            Console.WriteLine("   13. Verificar atualizações");
            Console.WriteLine("   14. Sair");
            Console.Write("\n   Escolha uma opção: ");

            string option = SafeReadLine();
            if (string.IsNullOrEmpty(option))
            {
                if (IsConsoleVisible()) { Console.WriteLine("   Opção inválida. Tente novamente."); }                
                break;
            }
            switch (option)
            {
                case "1":
                    await NetworkHelper.ShowNetworkInformation();
                    break;
                case "2":
                    NetworkHelper.ChangeGatewayOctet();
                    break;
                case "3":
                    StoreSettings.ConfigureStoreNumber();
                    break;
                case "4":
                    await NetworkHelper.TestConnectivityAsync();
                    break;
                case "5":
                    NetworkHelper.RestoreDhcp();
                    break;
                case "6":
                    Logger.ShowLogs();
                    break;
                case "7":
                    await NetworkHelper.TestConnectionSpeedAsync();
                    break;
                case "8":
                    await NetworkHelper.TestConnectionSpeedWithCustomServerAsync();
                    break;
                case "9":
                    Console.Write("   Digite o destino (padrão: google.com): ");
                    string? target = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(target)) target = "google.com";
                    await NetworkHelper.PerformTracerouteAsync(target);
                    break;
                case "10":
                    await NetworkProfileManager.ManageNetworkProfiles();
                    break;
                case "11":
                    await Scheduler.ManageScheduledTasks();
                    break;
                case "12":
                    NotificationManager.TestNotification();
                    break;
                case "13":
                    await UpdateManager.CheckForUpdatesAsync();
                    break;
                case "14":
                    ConsoleEventCallback(2);
                    break;
                default:
                    Console.WriteLine("   Opção inválida. Tente novamente.");
                    await Task.Delay(2000);
                    break;
            }
        }
    }      

    private static void ShowConsoleWindow()
    {
        FreeConsole();
        AllocConsole();

        Console.OutputEncoding = System.Text.Encoding.UTF8;
        StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        StreamReader standardInput = new StreamReader(Console.OpenStandardInput());
        StreamWriter standardError = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };

        Console.SetOut(standardOutput);
        Console.SetIn(standardInput);
        Console.SetError(standardError);

        Console.Title = "Troca Internet";
        Console.Clear();
    }

    public static bool IsConsoleVisible()
    {
        IntPtr handle = GetConsoleWindow();
        return handle != IntPtr.Zero;
    }

    private static async void MonitorStartUp()
    {
        while (true)
        {
            await StartUp();
            await Task.Delay(2000);
        }
    }

    private static bool ConsoleEventCallback(int eventType)
    {
        if (eventType == CtrlCloseEvent)
        {
            Console.WriteLine("   O console foi fechado, mas o aplicativo continuará na bandeja.");
            Pause();
            IntPtr handle = GetConsoleWindow();
            ShowWindow(handle, SwHide);
        }
        return false;
    }      

    public static void Pause()
    {
        if (IsConsoleVisible())
        {
            Console.WriteLine("\n   Pressione qualquer tecla para continuar...");
            Console.ReadKey();
            Console.Clear();
        }
    }
   
}