using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace TrocaInternet.TrocaInternet;

public static class UpdateManager
{
    // Link direto para download do arquivo ZIP
    private static readonly string UpdateZipUrl = "https://takeout-download-drive.usercontent.google.com/download/TrocaInternet-20250816T003840Z-1-001.zip?j=d67d9e6b-f852-4f0c-a861-e8db81d1ee31&i=0&user=253434638370&authuser=0";
    private static readonly string TempZipPath = Path.Combine(Path.GetTempPath(), "TrocaInternet_Update.zip");
    private static readonly string TempExtractPath = Path.Combine(Path.GetTempPath(), "TrocaInternet_Update");
    private static readonly string VersionFileName = "version.txt";
    private static readonly string ExecutableFileName = "TrocaInternet.exe"; // Nome do executável na pasta

    public static async Task CheckForUpdatesAsync()
    {
        try
        {
            Logger.LogInfo("Iniciando verificação de atualizações...");

            // Obtém a versão atual do aplicativo
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            Logger.LogInfo($"Versão atual: {currentVersion}");

            // Baixa o arquivo ZIP
            using (var httpClient = new HttpClient())
            {
                byte[] zipData = await DownloadFileWithRedirectHandlingAsync(httpClient, UpdateZipUrl);

                // Verifica se o download foi bem-sucedido
                if (zipData == null || zipData.Length == 0)
                {
                    Logger.LogError("Falha ao baixar o arquivo de atualização.");
                    return;
                }

                // Salva o ZIP temporariamente
                File.WriteAllBytes(TempZipPath, zipData);

                // Extrai o ZIP
                ExtractZip(TempZipPath, TempExtractPath);

                // Lê o arquivo de versão
                string versionFilePath = Path.Combine(TempExtractPath, VersionFileName);
                if (!File.Exists(versionFilePath))
                {
                    Logger.LogError("Arquivo de versão não encontrado no pacote de atualização.");
                    CleanTempFiles();
                    return;
                }

                string versionInfo = File.ReadAllText(versionFilePath).Trim();
                Version newVersion = Version.Parse(versionInfo);

                Logger.LogInfo($"Versão disponível: {newVersion}");

                if (newVersion > currentVersion)
                {
                    Logger.LogInfo("Nova versão disponível!");
                    await ShowUpdateNotificationAsync(currentVersion, newVersion);
                }
                else
                {
                    Logger.LogInfo("O aplicativo está atualizado.");
                    CleanTempFiles(); // Limpa os arquivos temporários
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao verificar atualizações: {ex.Message}");
            CleanTempFiles();
        }
    }

    private static async Task<byte[]> DownloadFileWithRedirectHandlingAsync(HttpClient httpClient, string url)
    {
        try
        {
            var response = await httpClient.GetAsync(url);

            // Verifica se houve redirecionamento
            if (response.StatusCode == System.Net.HttpStatusCode.Found)
            {
                var redirectUrl = response.Headers.Location;
                response = await httpClient.GetAsync(redirectUrl);
            }

            // Se o conteúdo for HTML (página de aviso do Google Drive), precisamos extrair o link real
            string content = await response.Content.ReadAsStringAsync();
            if (content.Contains("<title>Google Drive - Virus scan warning</title>"))
            {
                // Extrai o link de download real da página de aviso
                int start = content.IndexOf("href=\"https://drive.google.com/uc?export=download&amp;id=") + 6;
                int end = content.IndexOf("\"", start);
                string realUrl = content.Substring(start, end - start).Replace("&amp;", "&");

                response = await httpClient.GetAsync(realUrl);
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao baixar arquivo: {ex.Message}");
            throw;
        }
    }

    private static void ExtractZip(string zipPath, string extractPath)
    {
        try
        {
            // Limpa a pasta de extração se já existir
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            Directory.CreateDirectory(extractPath);

            // Extrai o ZIP
            ZipFile.ExtractToDirectory(zipPath, extractPath);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao extrair ZIP: {ex.Message}");
            throw;
        }
    }

    private static void CleanTempFiles()
    {
        try
        {
            if (File.Exists(TempZipPath))
            {
                File.Delete(TempZipPath);
            }

            if (Directory.Exists(TempExtractPath))
            {
                Directory.Delete(TempExtractPath, true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao limpar arquivos temporários: {ex.Message}");
        }
    }

    private static async Task ShowUpdateNotificationAsync(Version currentVersion, Version newVersion)
    {
        if (Program.IsConsoleVisible())
        {
            Console.WriteLine($"\n   Nova versão disponível!");
            Console.WriteLine($"   Versão atual: {currentVersion}");
            Console.WriteLine($"   Nova versão: {newVersion}");
            Console.Write("   Deseja atualizar agora? (S/N): ");

            string response = Console.ReadLine()?.ToUpper();
            if (response == "S")
            {
                await DownloadAndInstallUpdateAsync();
            }
            else
            {
                CleanTempFiles(); // Limpa os arquivos temporários se o usuário cancelar
            }
        }
        else
        {
            var result = MessageBox.Show(
                $"Uma nova versão do TrocaInternet está disponível!\n\nVersão atual: {currentVersion}\nNova versão: {newVersion}\n\nDeseja atualizar agora?",
                "Atualização Disponível",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification);

            if (result == DialogResult.Yes)
            {
                await DownloadAndInstallUpdateAsync();
            }
            else
            {
                CleanTempFiles(); // Limpa os arquivos temporários se o usuário cancelar
            }
        }
    }

    private static async Task DownloadAndInstallUpdateAsync()
    {
        try
        {
            Logger.LogInfo("Iniciando instalação da atualização...");

            // O arquivo ZIP já foi baixado e extraído na verificação inicial
            string executablePath = Path.Combine(TempExtractPath, ExecutableFileName);
            if (!File.Exists(executablePath))
            {
                Logger.LogError("Executável não encontrado no pacote de atualização.");
                CleanTempFiles();
                return;
            }

            // Inicia o processo de atualização
            StartUpdateProcess(executablePath);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao instalar atualização: {ex.Message}");

            if (Program.IsConsoleVisible())
            {
                Console.WriteLine($"\n   Erro ao instalar atualização: {ex.Message}");
                Console.WriteLine("   Pressione qualquer tecla para continuar...");
                Console.ReadKey();
            }

            CleanTempFiles();
        }
    }

    private static void StartUpdateProcess(string newExecutablePath)
    {
        try
        {
            // Obtém o caminho do executável atual
            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;

            // Prepara o processo de atualização
            var startInfo = new ProcessStartInfo
            {
                FileName = newExecutablePath,
                Arguments = $"\"{currentExePath}\" \"{Process.GetCurrentProcess().Id}\"",
                UseShellExecute = true,
                Verb = "runas" // Solicita privilégios de administrador
            };

            // Inicia o processo de atualização
            Process.Start(startInfo);

            // Fecha o aplicativo atual
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao iniciar processo de atualização: {ex.Message}");
        }
    }

    public static void PerformUpdate(string targetPath, string currentProcessId)
    {
        try
        {
            // Espera o processo principal fechar
            int processId = int.Parse(currentProcessId);
            var currentProcess = Process.GetProcessById(processId);

            if (!currentProcess.WaitForExit(5000))
            {
                currentProcess.Kill();
            }

            // Aguarda um pouco para garantir que o arquivo foi liberado
            System.Threading.Thread.Sleep(1000);

            // Faz backup do arquivo atual
            string backupPath = targetPath + ".bak";
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            if (File.Exists(targetPath))
            {
                File.Move(targetPath, backupPath);
            }

            // Copia o novo arquivo
            string newExecutablePath = Path.Combine(TempExtractPath, ExecutableFileName);
            File.Copy(newExecutablePath, targetPath);

            // Inicia o aplicativo atualizado
            Process.Start(new ProcessStartInfo
            {
                FileName = targetPath,
                UseShellExecute = true
            });

            // Limpa os arquivos temporários
            CleanTempFiles();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao realizar atualização: {ex.Message}");

            // Tenta restaurar o backup
            string backupPath = targetPath + ".bak";
            if (File.Exists(backupPath))
            {
                try
                {
                    if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                    }
                    File.Move(backupPath, targetPath);

                    MessageBox.Show(
                        "Ocorreu um erro durante a atualização. O aplicativo foi restaurado para a versão anterior.",
                        "Erro na Atualização",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch { }
            }

            // Inicia o aplicativo original
            Process.Start(new ProcessStartInfo
            {
                FileName = targetPath,
                UseShellExecute = true
            });

            CleanTempFiles();
        }
    }
}