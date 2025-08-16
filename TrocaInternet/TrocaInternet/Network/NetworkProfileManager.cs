using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using TrocaInternet.TrocaInternet;
using TrocaInternet.TrocaInternet.Network;

public static class NetworkProfileManager
{
    private static readonly string ProfilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
    private static readonly string ProfilesFile = Path.Combine(ProfilesPath, "profiles.json");

    public static List<NetworkProfile> Profiles { get; private set; } = new List<NetworkProfile>();

    static NetworkProfileManager()
    {
        Directory.CreateDirectory(ProfilesPath);
        LoadProfiles();
    }

    public static void LoadProfiles()
    {
        if (File.Exists(ProfilesFile))
        {
            try
            {
                string json = File.ReadAllText(ProfilesFile);
                Profiles = JsonSerializer.Deserialize<List<NetworkProfile>>(json) ?? new List<NetworkProfile>();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Erro ao carregar perfis: {ex.Message}");
            }
        }
    }

    public static void SaveProfiles()
    {
        try
        {
            string json = JsonSerializer.Serialize(Profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ProfilesFile, json);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao salvar perfis: {ex.Message}");
        }
    }

    public static void CreateProfile()
    {
        Console.Clear();
        Console.WriteLine("\n   Criar novo perfil de rede");
        Console.Write("   Nome do perfil: ");
        string name = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("   Nome inválido.");
            Program.Pause();
            return;
        }

        var profile = new NetworkProfile
        {
            Name = name,
            IpAddress = NetworkHelper.GetCurrentIpAddress(),
            Gateway = NetworkHelper.GetCurrentGateway(),
            DnsServers = GetDnsServers(),
            CreatedDate = DateTime.Now
        };

        Profiles.Add(profile);
        SaveProfiles();

        Console.WriteLine($"\n   Perfil '{name}' criado com sucesso!");
        Program.Pause();
    }

    public static void ApplyProfile()
    {
        Console.Clear();
        Console.WriteLine("\n   Perfis disponíveis:");

        for (int i = 0; i < Profiles.Count; i++)
        {
            Console.WriteLine($"   {i + 1}. {Profiles[i].Name}");
        }

        Console.Write("\n   Selecione um perfil (0 para cancelar): ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > Profiles.Count)
        {
            Console.WriteLine("   Operação cancelada.");
            Program.Pause();
            return;
        }

        var profile = Profiles[choice - 1];
        ApplyNetworkSettings(profile);

        Console.WriteLine($"\n   Perfil '{profile.Name}' aplicado com sucesso!");
        Program.Pause();
    }

    private static void ApplyNetworkSettings(NetworkProfile profile)
    {
        // Aplicar configurações de IP
        var processInfo = new ProcessStartInfo("netsh",
            $"interface ip set address \"{NetworkHelper.ActiveNetworkInterface}\" static {profile.IpAddress} 255.255.255.0 {profile.Gateway}")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            process.WaitForExit();
        }

        // Aplicar DNS
        if (profile.DnsServers.Count > 0)
        {
            string dnsArgs = string.Join(" ", profile.DnsServers);
            var dnsProcess = new ProcessStartInfo("netsh",
                $"interface ip set dns \"{NetworkHelper.ActiveNetworkInterface}\" static {dnsArgs}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(dnsProcess))
            {
                process.WaitForExit();
            }
        }

        Logger.LogInfo($"Perfil '{profile.Name}' aplicado");
    }

    private static List<string> GetDnsServers()
    {
        var dnsServers = new List<string>();

        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.Name == NetworkHelper.ActiveNetworkInterface && ni.OperationalStatus == OperationalStatus.Up)
            {
                var ipProperties = ni.GetIPProperties();
                foreach (var dns in ipProperties.DnsAddresses)
                {
                    dnsServers.Add(dns.ToString());
                }
            }
        }

        return dnsServers;
    }
    public static async Task ManageNetworkProfiles()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("\n   GERENCIAMENTO DE PERFIS DE REDE");
            Console.WriteLine("   1. Criar novo perfil");
            Console.WriteLine("   2. Aplicar perfil existente");
            Console.WriteLine("   3. Listar perfis");
            Console.WriteLine("   4. Voltar");
            Console.Write("\n   Escolha uma opção: ");

            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    NetworkProfileManager.CreateProfile();
                    break;
                case "2":
                    NetworkProfileManager.ApplyProfile();
                    break;
                case "3":
                    NetworkProfile.ListNetworkProfiles();
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("   Opção inválida.");
                    await Task.Delay(1000);
                    break;
            }
        }
    }
}

