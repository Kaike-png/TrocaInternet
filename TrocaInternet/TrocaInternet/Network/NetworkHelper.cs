using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TrocaInternet.TrocaInternet.Network;

internal static class NetworkHelper
{
    public static string ActiveNetworkInterface { get; } = GetActiveNetworkInterfaceName();
    public static string CurrentIpAddress { get; set; } = string.Empty;

    public static string GetActiveNetworkInterfaceName()
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    var ipProperties = networkInterface.GetIPProperties();

                    // Verifica se a interface tem um gateway padrão configurado
                    if (ipProperties.GatewayAddresses.Any())
                    {
                        return networkInterface.Name; // Nome da interface de rede ativa
                    }
                }
            }

            return "Nenhuma interface ativa encontrada.";
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao obter a interface de rede: {ex.Message}");
            return $"Erro ao obter a interface de rede: {ex.Message}";
        }
    }
    public static string GetCurrentIpAddress()
    {
        try
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.Name == ActiveNetworkInterface && ni.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao obter o IP atual: {ex.Message}");
            return null;
        }
    }

    public static string GetCurrentGateway()
    {
        try
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.Name == ActiveNetworkInterface && ni.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (GatewayIPAddressInformation gateway in ni.GetIPProperties().GatewayAddresses)
                    {
                        return gateway.Address.ToString();
                    }
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao obter o gateway atual: {ex.Message}");
            return null;
        }
    }

    public static async Task<bool> CheckConnectivityAsync()
    {
        try
        {
            var tasks = Config.PingHosts.Select(host => PingHostAsync(host));
            var results = await Task.WhenAll(tasks);

            // Loga os resultados para depuração
            for (int i = 0; i < Config.PingHosts.Count; i++)
            {
                Logger.LogInfo($"Ping para {Config.PingHosts[i]}: {(results[i] ? "Sucesso" : "Falha")}");
            }

            return results.Any(success => success);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro na verificação de conectividade: {ex.Message}");
            return false;
        }
    }
    private static async Task<bool> PingHostAsync(string host)
    {
        try
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host);
            if (addresses.Length == 0)
            {
                Logger.LogError($"Nenhum endereço encontrado para {host}");
                return false;
            }

            using (var ping = new Ping())
            {
                PingReply reply = await ping.SendPingAsync(addresses[0], Config.PingTimeout);
                return reply.Status == IPStatus.Success;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao pingar {host}: {ex.Message}");
            return false;
        }
    }

    public static void SetGateway(string gateway)
    {
        try
        {
            CurrentIpAddress = GetCurrentIpAddress();
            if (string.IsNullOrEmpty(CurrentIpAddress))
            {
                Logger.LogError("IP nulo ou inválido");
                return;
            }

            var processInfo = new ProcessStartInfo("netsh", $"interface ip set address \"{ActiveNetworkInterface}\" static {CurrentIpAddress} 255.255.255.0 {gateway}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            process?.WaitForExit();

            Logger.LogInfo($"Gateway alterado para: {gateway}");
                      
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao setar gateway: {ex.Message}");
        }
    }

    public static string ReplaceLastOctet(string ip, string newLastOctet)
    {
        var octets = ip.Split('.');
        if (octets.Length == 4)
        {
            octets[3] = newLastOctet;
            return string.Join(".", octets);
        }
        return ip;
    }

    public static void ChangeGatewayOctet()
    {
        try
        {
            string currentGateway = GetCurrentGateway();

            if (currentGateway == null)
            {
                Console.WriteLine("   Nenhum gateway encontrado para alterar.");
                return;
            }

            Console.WriteLine($"\n   Gateway atual: {currentGateway}");
            Console.WriteLine($"   Digite o novo octeto do gateway primário atual: {Config.PrimaryGateway}");
            string newPrimaryOctet = Console.ReadLine();
            Console.WriteLine($"   Digite o final do gateway secundário atual: {Config.SecondaryGateway}");
            string newSecondaryOctet = Console.ReadLine();
            Console.WriteLine($"   O final do gateway primário e secundário serão, respectivamente: {newPrimaryOctet} e {newSecondaryOctet} ");
            Console.Write("   Confirmar alteração dos gateways? (S/N): ");
            string confirm = Console.ReadLine()?.ToUpper();

            if (confirm == "S")
            {
                if (!string.IsNullOrEmpty(newPrimaryOctet)) Config.PrimaryGateway = newPrimaryOctet;
                if (!string.IsNullOrEmpty(newSecondaryOctet)) Config.SecondaryGateway = newSecondaryOctet;
                Config.Save();
                Console.WriteLine("   Gateway alterado com sucesso!");
            }
            else
            {
                Console.WriteLine("   Alteração cancelada.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao alterar octeto do gateway: {ex.Message}");
        }
    }

    public static async Task ChangeGatewayAsync()
    {
        try
        {
            CurrentIpAddress = GetCurrentIpAddress();

            if (string.IsNullOrEmpty(CurrentIpAddress))
            {
                Logger.LogError("IP nulo ou inválido.");
                return;
            }

            string currentGateway = GetCurrentGateway();
            if (currentGateway == null)
            {
                Logger.LogError("Gateway nulo ou inválido.");                
                return;
            }

            bool isConnected = await CheckConnectivityAsync();
            if (!isConnected)
            {
                string newGateway = currentGateway.EndsWith(Config.PrimaryGateway)
                    ? ReplaceLastOctet(currentGateway, Config.SecondaryGateway)
                    : ReplaceLastOctet(currentGateway, Config.PrimaryGateway);

                SetGateway(newGateway);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao alterar gateway: {ex.Message}");
        }
    }

    // Nova funcionalidade: Testar conectividade com hosts específicos
    public static async Task TestConnectivityAsync(List<string> customHosts = null)
    {
        var hosts = customHosts ?? Config.PingHosts;
        Console.WriteLine("\n   Testando conectividade com os hosts:");

        foreach (var host in hosts)
        {
            bool success = await PingHostAsync(host);
            Console.WriteLine($"   {host}: {(success ? "✅ Sucesso" : "❌ Falha")}");
        }

        Console.WriteLine("\n   Pressione qualquer tecla para continuar...");
        Console.ReadKey();
    }

    // Nova funcionalidade: Restaurar configurações de rede para DHCP
    public static void RestoreDhcp()
    {
        try
        {
            var processInfo = new ProcessStartInfo("netsh", $"interface ip set address \"{ActiveNetworkInterface}\" dhcp")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            process?.WaitForExit();

            Logger.LogInfo("Configuração de rede restaurada para DHCP");

            if (Program.IsConsoleVisible())
            {
                Console.WriteLine("\n   Configuração de rede restaurada para DHCP");
                Console.WriteLine("   Pressione qualquer tecla para continuar...");
                Program.SafeReadLine();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao restaurar DHCP: {ex.Message}");
        }
    }

    public static async Task PerformTracerouteAsync(string target = "google.com")
    {
        Console.WriteLine($"\n   Realizando traceroute para {target}...");

        try
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(target);
            if (addresses.Length == 0)
            {
                Console.WriteLine("   Endereço não encontrado.");
                return;
            }

            IPAddress targetAddress = addresses[0];
            Console.WriteLine($"   Destino: {target} ({targetAddress})\n");

            for (int ttl = 1; ttl <= 30; ttl++)
            {
                using (var ping = new Ping())
                {
                    var options = new PingOptions(ttl, true);
                    var reply = await ping.SendPingAsync(targetAddress, 5000, new byte[32], options);

                    if (reply.Status == IPStatus.TtlExpired)
                    {
                        string hostname = await TryResolveHostnameAsync(reply.Address);
                        Console.WriteLine($"   {ttl:D2}  {reply.Address,-15} {hostname,-30} {reply.RoundtripTime,4} ms");
                    }
                    else if (reply.Status == IPStatus.Success)
                    {
                        string hostname = await TryResolveHostnameAsync(reply.Address);
                        Console.WriteLine($"   {ttl:D2}  {reply.Address,-15} {hostname,-30} {reply.RoundtripTime,4} ms  < Destino");
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"   {ttl:D2}  *                *                            *    Timeout");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro no traceroute: {ex.Message}");
            Console.WriteLine("   Não foi possível completar o traceroute.");
        }

        Program.Pause();
    }

    private static async Task<string> TryResolveHostnameAsync(IPAddress address)
    {
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(address);
            return hostEntry.HostName;
        }
        catch
        {
            return address.ToString();
        }
    }
    public static async Task TestConnectionSpeedAsync()
    {
        try
        {
            Console.WriteLine("\n   Iniciando teste de velocidade...");

            // Lista de servidores de teste com fallback
            var testServers = new List<string>
        {
            "http://speedtest.tele2.net/10MB.zip",
            "http://proof.ovh.net/files/100Mb.dat",
            "http://www.ovh.net/files/100Mb.dat",
            "http://speedtest.wdc01.softlayer.com/downloads/test10.zip"
        };

            byte[] data = null;
            string usedServer = null;

            // Tenta cada servidor até conseguir baixar
            foreach (var server in testServers)
            {
                try
                {
                    Console.WriteLine($"   Tentando servidor: {server}");
                    using (var client = new WebClient())
                    {
                        data = await client.DownloadDataTaskAsync(server);
                        usedServer = server;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Falha ao baixar de {server}: {ex.Message}");
                    Console.WriteLine($"   Falha ao baixar de {server}: {ex.Message}");
                    continue;
                }
            }

            if (data == null)
            {
                Console.WriteLine("   Não foi possível baixar o arquivo de teste de nenhum servidor.");
                Program.Pause();
                return;
            }

            // Calcula a velocidade
            var stopwatch = Stopwatch.StartNew();
            stopwatch.Stop();

            double speed = data.Length / 1024.0 / 1024.0 / stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"\n   Velocidade de download: {speed:F2} MB/s");
            Console.WriteLine($"   Tamanho do arquivo: {data.Length / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"   Servidor utilizado: {usedServer}");

            // Teste de ping médio
            Console.WriteLine("\n   Realizando teste de ping...");
            double avgPing = await TestAveragePingAsync();
            Console.WriteLine($"   Ping médio: {avgPing:F0} ms");

            // Teste de jitter
            Console.WriteLine("\n   Calculando jitter...");
            double jitter = await TestJitterAsync();
            Console.WriteLine($"   Jitter: {jitter:F0} ms");

            // Avaliação da conexão
            Console.WriteLine("\n   Avaliação da conexão:");
            if (speed > 10)
                Console.WriteLine("   ✅ Excelente velocidade de download");
            else if (speed > 5)
                Console.WriteLine("   ✅ Boa velocidade de download");
            else if (speed > 1)
                Console.WriteLine("   ⚠️ Velocidade de download regular");
            else
                Console.WriteLine("   ❌ Velocidade de download baixa");

            if (avgPing < 50)
                Console.WriteLine("   ✅ Excelente ping");
            else if (avgPing < 100)
                Console.WriteLine("   ✅ Bom ping");
            else if (avgPing < 200)
                Console.WriteLine("   ⚠️ Ping regular");
            else
                Console.WriteLine("   ❌ Ping alto");

            if (jitter < 10)
                Console.WriteLine("   ✅ Jitter baixo (conexão estável)");
            else if (jitter < 30)
                Console.WriteLine("   ⚠️ Jitter moderado");
            else
                Console.WriteLine("   ❌ Jitter alto (conexão instável)");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro no teste de velocidade: {ex.Message}");
            Console.WriteLine("   Não foi possível completar o teste de velocidade.");
            Console.WriteLine($"   Erro: {ex.Message}");
        }

        Program.Pause();
    }

    private static async Task<double> TestAveragePingAsync()
    {
        const int testCount = 10;
        long totalMs = 0;
        int successCount = 0;

        using (var ping = new Ping())
        {
            for (int i = 0; i < testCount; i++)
            {
                try
                {
                    var reply = await ping.SendPingAsync("8.8.8.8");
                    if (reply.Status == IPStatus.Success)
                    {
                        totalMs += reply.RoundtripTime;
                        successCount++;
                    }
                }
                catch { }

                await Task.Delay(100);
            }
        }

        return successCount > 0 ? totalMs / (double)successCount : 0;
    }

    private static async Task<double> TestJitterAsync()
    {
        const int testCount = 10;
        var pings = new List<long>();

        using (var ping = new Ping())
        {
            for (int i = 0; i < testCount; i++)
            {
                try
                {
                    var reply = await ping.SendPingAsync("8.8.8.8");
                    if (reply.Status == IPStatus.Success)
                    {
                        pings.Add(reply.RoundtripTime);
                    }
                }
                catch { }

                await Task.Delay(100);
            }
        }

        if (pings.Count < 2) return 0;

        // Calcular jitter como desvio padrão
        double avg = pings.Average();
        double sumOfSquares = pings.Sum(ping => Math.Pow(ping - avg, 2));
        return Math.Sqrt(sumOfSquares / pings.Count);
    }
    public static async Task TestConnectionSpeedWithCustomServerAsync()
    {
        try
        {
            Console.Clear();
            Console.WriteLine("\n   TESTE DE VELOCIDADE COM SERVIDOR PERSONALIZADO");
            Console.Write("   Digite a URL do arquivo de teste (ou deixe em branco para usar servidores padrão): ");
            string customUrl = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(customUrl))
            {
                Console.WriteLine("\n   Iniciando teste de velocidade...");
                Console.WriteLine($"   Servidor: {customUrl}");

                using (var client = new WebClient())
                {
                    var stopwatch = Stopwatch.StartNew();
                    byte[] data = await client.DownloadDataTaskAsync(customUrl);
                    stopwatch.Stop();

                    double speed = data.Length / 1024.0 / 1024.0 / stopwatch.Elapsed.TotalSeconds;
                    Console.WriteLine($"\n   Velocidade de download: {speed:F2} MB/s");
                    Console.WriteLine($"   Tamanho do arquivo: {data.Length / 1024.0 / 1024.0:F2} MB");
                }
            }
            else
            {
                await TestConnectionSpeedAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro no teste de velocidade personalizado: {ex.Message}");
            Console.WriteLine("   Não foi possível completar o teste de velocidade.");
            Console.WriteLine($"   Erro: {ex.Message}");
        }

        Program.Pause();
    }

    public static async Task ShowNetworkInformation()
    {
        Console.Clear();
        CurrentIpAddress = GetCurrentIpAddress();

        Console.WriteLine($"\n   Interface ativa: {ActiveNetworkInterface}");
        Console.WriteLine($"   Número da loja: {StoreSettings.StoreNumber}");
        Console.WriteLine($"   IP atual: {CurrentIpAddress ?? "Nenhum IP encontrado"}");

        string currentGateway = GetCurrentGateway();
        Console.WriteLine($"   Gateway atual: {currentGateway ?? "Nenhum gateway encontrado"}");

        if (currentGateway != null)
        {
            Console.Write("   Verificando conectividade... ");
            bool isConnected = await CheckConnectivityAsync();
            Console.WriteLine(isConnected ? "✅ Estável" : "❌ Falhou");
        }
        else
        {
            Console.WriteLine("   Não foi possível verificar a conectividade (gateway não encontrado)");
        }

        Program.Pause();
    }
}