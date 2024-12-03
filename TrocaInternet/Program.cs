using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;
using System.IO;
using Microsoft.Win32;

internal class Program
{
    private static string networkInterface = "redelocal";
    private static string currentIpAddress = string.Empty;

    private static void Main()
    {
        // Adiciona o programa ao registro para iniciar com o Windows
        AddToStartup();
        // Cria uma tarefa agendada para iniciar com o Logon do Usuário 
        CreateScheduledTask(); 

        while (true)
        {
            currentIpAddress = GetCurrentIpAddress();

            if (string.IsNullOrEmpty(currentIpAddress))
            {
                Console.WriteLine("Nenhum IP encontrado na interface.");
            }
            else
            {
                Console.WriteLine($"IP atual da interface {networkInterface}: {currentIpAddress}");

                string currentGateway = GetCurrentGateway();
                if (currentGateway == null)
                {
                    Console.WriteLine("Nenhum gateway atual encontrado.");
                }
                else
                {
                    Console.WriteLine($"Gateway atual: {currentGateway}");

                    string newGateway = currentGateway.EndsWith("1") ? ReplaceLastOctet(currentGateway, "254") : ReplaceLastOctet(currentGateway, "1");

                    if (!PingGateway(currentGateway))
                    {
                        Console.WriteLine($"Conexão com {currentGateway} falhou. Alterando para {newGateway}...");
                        SetGateway(newGateway);
                    }
                }
            }

            Thread.Sleep(5000); // Intervalo de verificação a cada 5 segundos
        }
    }

    private static void AddToStartup()
    {
        var appName = "TrocaInternet"; 
        string exePath = Process.GetCurrentProcess().MainModule.FileName; // Caminho do executável atual

        // A chave do Registro para a inicialização do programa
        RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        ;
        if (registryKey.GetValue(appName) == null)
        {
            registryKey.SetValue(appName, exePath); // Adiciona o aplicativo ao registro para inicializar
            Console.WriteLine("Aplicativo adicionado à inicialização do Windows.");
        }
        else
        {
            Console.WriteLine("Aplicativo já está configurado para iniciar com o Windows.");
        }
    }

    private static string GetCurrentIpAddress()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.Name == networkInterface && ni.OperationalStatus == OperationalStatus.Up)
            {
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString();
                    }
                }
            }
        }
        return null;
    }

    private static string GetCurrentGateway()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.Name == networkInterface && ni.OperationalStatus == OperationalStatus.Up)
            {
                foreach (GatewayIPAddressInformation gateway in ni.GetIPProperties().GatewayAddresses)
                {
                    return gateway.Address.ToString();
                }
            }
        }
        return null;
    }

    private static bool PingGateway(string gateway)
    {
        try
        {
            using (var ping = new Ping())
            {
                PingReply reply = ping.Send(gateway, 2000); // Timeout de 2 segundos
                return reply.Status == IPStatus.Success;
            }
        }
        catch
        {
            return false;
        }
    }

    private static void SetGateway(string gateway)
    {
        var processInfo = new ProcessStartInfo("netsh", $"interface ip set address \"{networkInterface}\" static {currentIpAddress} 255.255.255.0 {gateway}")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            process.WaitForExit();
            Console.WriteLine($"Gateway alterado para: {gateway}");
        }
    }

    private static string ReplaceLastOctet(string ip, string newLastOctet)
    {
        var octets = ip.Split('.');
        octets[3] = newLastOctet;
        return string.Join(".", octets);
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

}
