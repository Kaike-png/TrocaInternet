using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TrocaInternet.TrocaInternet;

internal class Program
{
    public static readonly string StoreConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "store_config.txt"); //Caminho para ler e salvar a configuração da loja
    private static NotifyIcon _notifyIcon;
    private const int CtrlCloseEvent = 2; // Valor correspondente ao evento de fechamento do console

    // Delegate para o manipulador de eventos do console
    private delegate bool ConsoleEventDelegate(int eventType);

    // Variável global para manter o delegate em escopo
    private static ConsoleEventDelegate _handler;

    // Importação do método para configurar o manipulador de eventos do console
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);


    [DllImport("kernel32.dll")]
    static extern nint GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    static extern bool FreeConsole();

    const int SwHide = 0;
    static async Task Main(string[] args)
    {
        // Cria uma tarefa agendada para iniciar com o Logon do Usuário 
        CreateScheduledTask();

        // Configura o manipulador de eventos do console
        _handler = new ConsoleEventDelegate(ConsoleEventCallback);
        SetConsoleCtrlHandler(_handler, true);

        //Inicia monitoramento em uma thread separada
        _ = Task.Run(async () =>
        {
            while (true)
            {
                NetworkHelper.ChangeGateway(); // Executa periodicamente
                await Task.Delay(5000); // Espera 5 segundos entre as execuções
            }
        });

        // Esconde a janela do console
        nint handle = GetConsoleWindow();
        ShowWindow(handle, SwHide);

        // Inicializa o NotifyIcon na bandeja do sistema
        _notifyIcon = new NotifyIcon
        {
            Icon = new System.Drawing.Icon("Assets/Chama.ico"), // Ícone
            Visible = true,
            Text = "Troca Internet", // Tooltip ao passar o mouse no ícone
        };

        // Cria o ContextMenuStrip (menu de contexto moderno)
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Abrir Console", null, (s, e) =>
        {
            // Mostra o console
            ShowConsoleWindow();
        });
        contextMenu.Items.Add("Sair", null, (s, e) =>
        {
            // Fecha o programa
            _notifyIcon.Dispose();
            Application.Exit();
        });

        _notifyIcon.ContextMenuStrip = contextMenu; // Configura o menu de contexto

        // Thread para monitoramento contínuo do startUp 
        Thread monitoringThread = new Thread(MonitorStartUp);
        monitoringThread.IsBackground = true;
        monitoringThread.Start();

        // Mantém o ícone na bandeja ativo
        Application.Run();
    }
    //Evita o erro do readLine no starUp
    public static string SafeReadLine()
    {
        if (!IsConsoleVisible())
        {
            return string.Empty;
        }
        try
        {
            return Console.ReadLine();
        }
        catch (IOException ex)
        {

            return string.Empty;
        }
    }

    private static void StartUp()
    {
        while (IsConsoleVisible())
        {
            Console.Clear();
            Console.WriteLine(@"       
   ████████╗██████╗░░█████╗░░█████╗░░█████╗░  ██╗███╗░░██╗████████╗███████╗██████╗░███╗░░██╗███████╗████████╗
   ╚══██╔══╝██╔══██╗██╔══██╗██╔══██╗██╔══██╗  ██║████╗░██║╚══██╔══╝██╔════╝██╔══██╗████╗░██║██╔════╝╚══██╔══╝
   ░░░██║░░░██████╔╝██║░░██║██║░░╚═╝███████║  ██║██╔██╗██║░░░██║░░░█████╗░░██████╔╝██╔██╗██║█████╗░░░░░██║░░░
   ░░░██║░░░██╔══██╗██║░░██║██║░░██╗██╔══██║  ██║██║╚████║░░░██║░░░██╔══╝░░██╔══██╗██║╚████║██╔══╝░░░░░██║░░░
   ░░░██║░░░██║░░██║╚█████╔╝╚█████╔╝██║░░██║  ██║██║░╚███║░░░██║░░░███████╗██║░░██║██║░╚███║███████╗░░░██║░░░
   ░░░╚═╝░░░╚═╝░░╚═╝░╚════╝░░╚════╝░╚═╝░░╚═╝  ╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚══════╝╚═╝░░╚═╝╚═╝░░╚══╝╚══════╝░░░╚═╝░░░");
            Console.WriteLine("\n   1. Verificar conexão atual");
            Console.WriteLine("   2. Alterar final do gateway");
            Console.WriteLine("   3. Configurar número da loja");
            Console.WriteLine("   4. Sair");
            Console.Write("\n   Escolha uma opção: ");

            string option = SafeReadLine();
            if (string.IsNullOrEmpty(option))
            {
                Thread.Sleep(2000);
                Console.WriteLine("   Pressione qualquer tecla para continuar...");
                break;
            }

            switch (option)
            {
                case "1":
                    ShowNetworkInformation();
                    break;
                case "2":
                    NetworkHelper.ChangeGatewayOctet();
                    break;
                case "3":
                    StoreSettings.ConfigureStoreNumber();
                    break;
                case "4":
                    ConsoleEventCallback(2);
                    break;
                case "":
                    Console.WriteLine("   Opção inválida. Tente novamente.");
                    Thread.Sleep(2000);
                    break;
                default:
                    Console.WriteLine("   Opção inválida. Tente novamente.");
                    Thread.Sleep(2000);
                    break;
            }
        }
    }
    private static void ShowConsoleWindow()
    {
        // Libera o console existente (se houver)
        FreeConsole();

        // Reanexa o console ao processo atual
        AllocConsole();

        // Redireciona a entrada, saída e erro padrão para o novo console
        Console.OutputEncoding = System.Text.Encoding.UTF8; //Força a codificação para UTF-8
        StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        StreamReader standardInput = new StreamReader(Console.OpenStandardInput());
        StreamWriter standardError = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };

        Console.SetOut(standardOutput);
        Console.SetIn(standardInput);
        Console.SetError(standardError);

        // Configura o título do console
        Console.Title = "Troca Internet";
        Console.Clear();
    }

    public static bool IsConsoleVisible()
    {
        nint handle = GetConsoleWindow();
        return handle != nint.Zero; // Retorna true se o console existir
    }

    private static void MonitorStartUp()
    {
        while (true)
        {
            StartUp();
            Thread.Sleep(2000);
        }
    }

    private static bool ConsoleEventCallback(int eventType)
    {
        if (eventType == CtrlCloseEvent)
        {
            Console.WriteLine("   O console foi fechado, mas o aplicativo continuará na bandeja.");
            Pause();
            nint handle = GetConsoleWindow();
            ShowWindow(handle, SwHide); // Apenas oculta o console
        }
        return false; // Impede que o processo principal seja encerrado
    }

    private static void ShowNetworkInformation()
    {
        Console.Clear();
        NetworkHelper.CurrentIpAddress = NetworkHelper.GetCurrentIpAddress();

        Console.WriteLine($"\n   Interface ativa: {NetworkHelper.ActiveNetworkInterface}");
        Console.WriteLine($"   Número da loja: {StoreSettings.StoreNumber}");
        Console.WriteLine($"   IP atual: {NetworkHelper.CurrentIpAddress ?? "Nenhum IP encontrado"}");

        string currentGateway = NetworkHelper.GetCurrentGateway();
        Console.WriteLine($"   Gateway atual: {currentGateway ?? "Nenhum gateway encontrado"}");

        if (currentGateway != null && NetworkHelper.PingAzure())
        {
            Console.WriteLine("   Conexão com o gateway estável.");
        }
        else
        {
            Console.WriteLine("   Conexão com o gateway falhou.");
        }

        Pause();
    }

    private static void CreateScheduledTask()
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