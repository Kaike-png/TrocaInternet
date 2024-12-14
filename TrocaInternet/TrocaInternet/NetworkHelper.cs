using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;
using System.IO;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TrocaInternet.TrocaInternet
{
    internal class NetworkHelper
    {
        public static readonly string ActiveNetworkInterface = GetActiveNetworkInterfaceName();
        public static string CurrentIpAddress = string.Empty;
        public static string PrimaryGateway { get; private set; } = "1";
        public static string SecondaryGateway { get; private set; } = "254";

        public static string GetActiveNetworkInterfaceName()
        {
            try
            {
                var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

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

                return "   Nenhuma interface ativa encontrada.";
            }
            catch (Exception ex)
            {
                return $"   Erro ao obter a interface de rede: {ex.Message}";
            }
        }

        public static string GetCurrentIpAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.Name == ActiveNetworkInterface && ni.OperationalStatus == OperationalStatus.Up)
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

        public static string GetCurrentGateway()
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

        public static bool PingAzure()
        {
            try
            {
                
                string host = "azure.com";
                IPAddress[] addresses = Dns.GetHostAddresses(host);

                if (addresses.Length == 0)
                    return false;

                using (var ping = new Ping())
                {
                    PingReply reply = ping.Send(addresses[0], 2000); 
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetGateway(string gateway)
        {
            var processInfo = new ProcessStartInfo("netsh", $"interface ip set address \"{ActiveNetworkInterface}\" static {CurrentIpAddress} 255.255.255.0 {gateway}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (Program.IsConsoleVisible())
            {
                process.WaitForExit();
                Console.WriteLine($"\n   Gateway alterado para: {gateway}");
                Console.WriteLine("   Pressione qualquer tecla para continuar...");
                Program.SafeReadLine();
            }
        }


        public static string ReplaceLastOctet(string ip, string newLastOctet)
        {
            var octets = ip.Split('.');
            octets[3] = newLastOctet;
            return string.Join(".", octets);
        }

        public static void ChangeGatewayOctet()
        {
            string currentGateway = GetCurrentGateway();

            if (currentGateway == null)
            {
                Console.WriteLine("   Nenhum gateway encontrado para alterar.");
            }
            else
            {
                Console.WriteLine($"\n   Gateway atual: {currentGateway}");
                Console.WriteLine($"   Digite o novo octeto do gateway primário atual: {PrimaryGateway}");
                string newPrimaryOctet = Console.ReadLine();
                Console.WriteLine($"   Digite o final do gateway secundário atual: {SecondaryGateway}");
                string newSecondaryOctet = Console.ReadLine();
                Console.WriteLine($"   O final do gateway primário e secundário serão, respectivamente: {newPrimaryOctet} e {newSecondaryOctet} ");
                Console.Write("   Confirmar alteração dos gateways? (S/N): ");
                string confirm = Console.ReadLine()?.ToUpper();

                if (confirm == "S")
                {
                    if (newPrimaryOctet != null) PrimaryGateway = newPrimaryOctet;
                    if (newSecondaryOctet != null) SecondaryGateway = newSecondaryOctet;
                    Console.WriteLine("   Gateway alterado com sucesso!");
                }
                else
                {
                    Console.WriteLine("   Alteração cancelada.");
                }
            }
        }

        public static void ChangeGateway()
        {
            CurrentIpAddress = GetCurrentIpAddress();

            if (string.IsNullOrEmpty(CurrentIpAddress))
            {
                if (Program.IsConsoleVisible())
                {
                    Console.WriteLine("   IP nulo ou inválido\n");
                    Console.WriteLine("   Pressione qualquer tecla para continuar...");
                    Console.ReadKey();
                    Console.Clear();
                }
            }
            else
            {
                string currentGateway = GetCurrentGateway();
                if (currentGateway == null)
                {
                    if (Program.IsConsoleVisible())
                    {
                        Console.WriteLine("   Gateway nulo ou inválido\n");
                        Console.WriteLine("   Pressione qualquer tecla para continuar...");
                        Console.ReadKey();
                        Console.Clear();
                    }
                    
                }
                else
                {
                    string newGateway = currentGateway.EndsWith(PrimaryGateway)
                        ? ReplaceLastOctet(currentGateway, SecondaryGateway)
                        : ReplaceLastOctet(currentGateway, PrimaryGateway);

                    if (!PingAzure())
                    {
                        SetGateway(newGateway);
                    }
                }
            }

        }
    }

}


