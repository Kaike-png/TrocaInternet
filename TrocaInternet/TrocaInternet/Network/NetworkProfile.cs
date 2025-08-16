namespace TrocaInternet.TrocaInternet.Network;

public class NetworkProfile
{
    public string Name { get; set; }
    public string IpAddress { get; set; }
    public string Gateway { get; set; }
    public List<string> DnsServers { get; set; } = new List<string>();
    public DateTime CreatedDate { get; set; }
    public DateTime LastUsed { get; set; }

    public static void ListNetworkProfiles()
    {
        Console.Clear();
        Console.WriteLine("\n   PERFIS DE REDE CADASTRADOS:");

        if (NetworkProfileManager.Profiles.Count == 0)
        {
            Console.WriteLine("   Nenhum perfil cadastrado.");
        }
        else
        {
            foreach (var profile in NetworkProfileManager.Profiles)
            {
                Console.WriteLine($"\n   Nome: {profile.Name}");
                Console.WriteLine($"   IP: {profile.IpAddress}");
                Console.WriteLine($"   Gateway: {profile.Gateway}");
                Console.WriteLine($"   DNS: {string.Join(", ", profile.DnsServers)}");
                Console.WriteLine($"   Criado em: {profile.CreatedDate:dd/MM/yyyy HH:mm}");
                if (profile.LastUsed != default)
                    Console.WriteLine($"   Último uso: {profile.LastUsed:dd/MM/yyyy HH:mm}");
            }
        }

        Program.Pause();
    }
}