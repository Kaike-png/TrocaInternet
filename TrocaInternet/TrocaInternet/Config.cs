using System.Text.Json;

namespace TrocaInternet.TrocaInternet;

public static class Config
{
    private const string ConfigFileName = "config.json";
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

    public static string PrimaryGateway { get; set; } = "1";
    public static string SecondaryGateway { get; set; } = "254";
    public static List<string> PingHosts { get; set; } = new List<string>
    {
        "azure.com",
        "google.com",
        "github.com",
        "8.8.8.8"
    };
    public static int PingTimeout { get; set; } = 2000;
    public static int CheckInterval { get; set; } = 10000;

    public static void Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                string json = File.ReadAllText(ConfigPath);
                var configData = JsonSerializer.Deserialize<ConfigData>(json);
                if (configData != null)
                {
                    PrimaryGateway = configData.PrimaryGateway;
                    SecondaryGateway = configData.SecondaryGateway;
                    PingHosts = configData.PingHosts;
                    PingTimeout = configData.PingTimeout;
                    CheckInterval = configData.CheckInterval;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Erro ao carregar configurações: {ex.Message}");
            }
        }
    }

    public static void Save()
    {
        try
        {
            var configData = new ConfigData
            {
                PrimaryGateway = PrimaryGateway,
                SecondaryGateway = SecondaryGateway,
                PingHosts = PingHosts,
                PingTimeout = PingTimeout,
                CheckInterval = CheckInterval
            };

            string json = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao salvar configurações: {ex.Message}");
        }
    }

    // Classe interna para serialização/desserialização
    private class ConfigData
    {
        public string PrimaryGateway { get; set; } = "1";
        public string SecondaryGateway { get; set; } = "254";
        public List<string> PingHosts { get; set; } = new List<string>
        {
            "azure.com",
            "google.com",
            "github.com",
            "8.8.8.8"
        };
        public int PingTimeout { get; set; } = 2000;
        public int CheckInterval { get; set; } = 5000;
    }
}