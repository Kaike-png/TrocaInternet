using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using TrocaInternet.TrocaInternet.GitHubAPI;

namespace TrocaInternet.TrocaInternet
{
    public static class UpdateManager
    {
        public static readonly string LatestReleaseUrl = "https://api.github.com/repos/Kaike-png/TrocaInternet/releases/latest";
        private static string TempZipPath = Path.Combine(Path.GetTempPath(), "TrocaInternet_Update.zip");
        private static readonly string TempExtractPath = Path.Combine(Path.GetTempPath(), "TrocaInternet_Update");
        private static readonly string VersionFileName = "version.txt";
        private static readonly string ExecutableFileName = "TrocaInternet.exe";
        private static readonly string UpdateSubfolder = "TrocaInternet"; // Subpasta onde estão os arquivos
        public static async Task CheckForUpdatesAsync()
        {
            try
            {                
                if (Program.IsConsoleVisible()) { Console.Clear(); }
                if (Program.IsConsoleVisible()) {Console.WriteLine("   Verificando atualizações...");}              
                Logger.LogInfo("Iniciando verificação de atualizações...");

                // Obtém a versão atual do aplicativo
                Version currentVersion = GetCurrentVersion();
                if (Program.IsConsoleVisible()) { Console.WriteLine($"   Versão atual: {currentVersion}"); }                
                Logger.LogInfo($"Versão atual: {currentVersion}");

                // Obtém informações do último release
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "TrocaInternet-Updater");

                    // Obtém informações do release
                    var releaseInfo = await GitHubRelease.GetLatestReleaseInfo(httpClient).ConfigureAwait(false); 
                    if (releaseInfo == null)
                    {
                        if (Program.IsConsoleVisible()) { Console.WriteLine("   Não foi possível obter informações do último release."); }                        
                        Logger.LogError("Não foi possível obter informações do último release.");
                        return;
                    }

                    // Obtém a versão do release
                    Version newVersion;
                    try
                    {
                        newVersion = ExtractVersionFromTag(releaseInfo.Name);
                        if (Program.IsConsoleVisible()) { Console.WriteLine($"   Versão disponível: {newVersion}"); }                        
                        Logger.LogInfo($"Versão disponível: {newVersion}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Erro ao parsear a versão do release: {ex.Message}");
                        Logger.LogError($"Tag do release: {releaseInfo.TagName}");

                        if (Program.IsConsoleVisible())
                        {
                            Console.WriteLine("\n   Erro ao interpretar a versão do release.");
                            Console.WriteLine($"   Tag original: {releaseInfo.TagName}");
                            Console.WriteLine($"   Erro: {ex.Message}");
                            Console.WriteLine("   Pressione qualquer tecla para continuar...");
                            Console.ReadKey();
                        }

                        return;
                    }

                    if (newVersion > currentVersion)
                    {
                        if (Program.IsConsoleVisible()) { Console.WriteLine("   Nova versão disponível!"); }
                        Logger.LogInfo("Nova versão disponível!");

                        // Encontra o asset (arquivo) correto
                        var asset = GitHubAsset.FindAsset(releaseInfo);
                        if (asset == null)
                        {
                            if (Program.IsConsoleVisible()) { Console.WriteLine("   Não foi possível encontrar o arquivo de atualização no release."); }
                            Logger.LogError("Não foi possível encontrar o arquivo de atualização no release.");
                            return;
                        }

                        // Baixa o arquivo
                        byte[] zipData = await DownloadFileAsync(httpClient, asset.BrowserDownloadUrl);

                        // Verifica se o download foi bem-sucedido
                        if (zipData == null || zipData.Length == 0)
                        {
                            if (Program.IsConsoleVisible()) { Console.WriteLine("   Falha ao baixar o arquivo de atualização."); }
                            Logger.LogError("Falha ao baixar o arquivo de atualização.");
                            return;
                        }

                        // Tenta salvar o ZIP temporariamente
                        if (!await SaveTempFileAsync(zipData))
                        {
                            if (Program.IsConsoleVisible()) { Console.WriteLine("   Não foi possível salvar o arquivo temporário."); }
                            Logger.LogError("Não foi possível salvar o arquivo temporário.");
                            return;
                        }

                        // Extrai o ZIP
                        ExtractZip(TempZipPath, TempExtractPath);

                        // Lê o arquivo de versão
                        string versionFilePath = Path.Combine(TempExtractPath, UpdateSubfolder, VersionFileName);
                        if (!File.Exists(versionFilePath))
                        {
                            if (Program.IsConsoleVisible()) { Console.WriteLine("   Arquivo de versão não encontrado no pacote de atualização."); }
                            Logger.LogError("Arquivo de versão não encontrado no pacote de atualização.");
                            CleanTempFiles();
                            return;
                        }

                        string versionInfo = File.ReadAllText(versionFilePath).Trim();
                        Version fileVersion;
                        try
                        {
                            fileVersion = Version.Parse(versionInfo);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Erro ao parsear a versão do arquivo: {ex.Message}");
                            Logger.LogError($"Conteúdo do arquivo: {versionInfo}");

                            if (Program.IsConsoleVisible())
                            {
                                Console.WriteLine("\n   Erro ao interpretar a versão do arquivo.");
                                Console.WriteLine("   Pressione qualquer tecla para continuar...");
                                Console.ReadKey();
                            }

                            CleanTempFiles();
                            return;
                        }

                        // Verifica se a versão do arquivo corresponde à do release
                        if (fileVersion != newVersion)
                        {
                            if (Program.IsConsoleVisible()) { Console.WriteLine($"   Incompatibilidade de versão: Release={newVersion}, Arquivo={fileVersion}"); }
                            Logger.LogError($"Incompatibilidade de versão: Release={newVersion}, Arquivo={fileVersion}");
                            CleanTempFiles();
                            return;
                        }

                        await DownloadAndInstallUpdateAsync();
                    }
                    else
                    {
                        Logger.LogInfo("O aplicativo está atualizado.");
                        if (Program.IsConsoleVisible()) { Console.WriteLine("   O aplicativo está atualizado."); }                        
                        Program.Pause();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Erro ao verificar atualizações: {ex.Message}");
                CleanTempFiles();
            }
        }
        private static Version GetCurrentVersion()
        {
            try
            {
                // Obtém o caminho do executável atual
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string exeDir = Path.GetDirectoryName(exePath);
                string versionFilePath = Path.Combine(exeDir, VersionFileName);

                // Verifica se o arquivo de versão existe
                if (!File.Exists(versionFilePath))
                {
                    Logger.LogWarning($"Arquivo de versão não encontrado em: {versionFilePath}");
                    Logger.LogInfo("Usando versão do assembly como fallback");
                    return Assembly.GetExecutingAssembly().GetName().Version;
                }

                // Lê o conteúdo do arquivo
                string versionContent = File.ReadAllText(versionFilePath).Trim();

                // Tenta parsear a versão
                try
                {
                    Version version = Version.Parse(versionContent);
                    Logger.LogInfo($"Versão lida do arquivo: {version}");
                    return version;
                }
                catch (FormatException ex)
                {
                    Logger.LogError($"Formato de versão inválido no arquivo: {versionContent}");
                    Logger.LogError($"Erro: {ex.Message}");
                    Logger.LogInfo("Usando versão do assembly como fallback");
                    return Assembly.GetExecutingAssembly().GetName().Version;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Erro ao ler versão atual: {ex.Message}");
                if (Program.IsConsoleVisible()) { Console.WriteLine($"   Erro ao ler versão atual: {ex.Message}"); }
                Logger.LogInfo("Usando versão do assembly como fallback");
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }
        private static Version ExtractVersionFromTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException("Tag não pode ser nula ou vazia");
            }

            // Remove o prefixo 'v' se existir
            string versionString = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? tag.Substring(1)
                : tag;

            // Remove sufixos como -beta, -rc, -alpha, etc.
            int dashIndex = versionString.IndexOf('-');
            if (dashIndex > 0)
            {
                versionString = versionString.Substring(0, dashIndex);
            }

            // Remove sufixos como +build.metadata
            int plusIndex = versionString.IndexOf('+');
            if (plusIndex > 0)
            {
                versionString = versionString.Substring(0, plusIndex);
            }

            // Valida o formato da versão
            if (!Regex.IsMatch(versionString, @"^(\d+\.)*\d+$"))
            {
                throw new FormatException($"Formato de versão inválido: {versionString}");
            }

            // Garante que a versão tenha pelo menos 2 partes (major.minor)
            string[] parts = versionString.Split('.');
            if (parts.Length < 2)
            {
                throw new FormatException($"Versão deve ter pelo menos 2 partes: {versionString}");
            }

            // Se tiver mais de 4 partes, ignora as extras
            if (parts.Length > 4)
            {
                versionString = string.Join(".", parts, 0, 4);
            }

            return Version.Parse(versionString);
        }

        private static async Task<bool> SaveTempFileAsync(byte[] data)
        {
            try
            {
                // Limpa arquivos temporários existentes
                CleanTempFiles();

                // Tenta salvar o arquivo com tratamento de erros
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        // Garante que o diretório temporário existe
                        string tempDir = Path.GetDirectoryName(TempZipPath);
                        if (!Directory.Exists(tempDir))
                        {
                            Directory.CreateDirectory(tempDir);
                        }

                        // Se o arquivo já existir, tenta excluí-lo
                        if (File.Exists(TempZipPath))
                        {
                            File.SetAttributes(TempZipPath, FileAttributes.Normal); // Remove atributos de somente leitura
                            File.Delete(TempZipPath);
                            // Aguarda um pouco para garantir que o arquivo foi liberado
                            await Task.Delay(200);
                        }

                        // Salva o arquivo
                        File.WriteAllBytes(TempZipPath, data);

                        // Verifica se o arquivo foi salvo corretamente
                        if (File.Exists(TempZipPath) && new FileInfo(TempZipPath).Length == data.Length)
                        {
                            Logger.LogInfo($"Arquivo temporário salvo com sucesso em: {TempZipPath}");
                            return true;
                        }

                        Logger.LogError($"Falha ao verificar o arquivo salvo (tentativa {attempt})");
                    }
                    catch (IOException ioEx)
                    {
                        Logger.LogError($"Erro de E/S ao salvar arquivo (tentativa {attempt}): {ioEx.Message}");

                        if (attempt == 3)
                        {
                            // Última tentativa: tenta com um nome de arquivo alternativo
                            try
                            {
                                string altPath = Path.Combine(Path.GetTempPath(), $"TrocaInternet_Update_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                                File.WriteAllBytes(altPath, data);
                                TempZipPath = altPath; // Atualiza o caminho para o arquivo alternativo
                                Logger.LogInfo($"Arquivo temporário salvo com nome alternativo: {altPath}");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"Falha ao salvar arquivo com nome alternativo: {ex.Message}");
                            }
                        }

                        // Aguarda antes da próxima tentativa
                        await Task.Delay(500 * attempt);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Erro ao salvar arquivo (tentativa {attempt}): {ex.Message}");
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Erro ao preparar salvamento do arquivo: {ex.Message}");
                return false;
            }
        }

        private static void ExtractZip(string zipPath, string extractPath)
        {
            try
            {
                // Verifica se o arquivo existe
                if (!File.Exists(zipPath))
                {
                    Logger.LogError($"Arquivo ZIP não encontrado: {zipPath}");
                    throw new FileNotFoundException("Arquivo ZIP não encontrado.", zipPath);
                }

                // Verifica o tamanho do arquivo
                var fileInfo = new FileInfo(zipPath);
                if (fileInfo.Length == 0)
                {
                    Logger.LogError("O arquivo ZIP está vazio.");
                    throw new InvalidDataException("O arquivo ZIP está vazio.");
                }

                // Verifica os bytes iniciais para confirmar que é um ZIP
                using (var fileStream = File.OpenRead(zipPath))
                {
                    byte[] header = new byte[4];
                    fileStream.Read(header, 0, 4);

                    if (!(header[0] == 0x50 && header[1] == 0x4B &&
                          (header[2] == 0x03 && header[3] == 0x04 ||
                           header[2] == 0x05 && header[3] == 0x06 ||
                           header[2] == 0x07 && header[3] == 0x08)))
                    {
                        Logger.LogError("O arquivo não é um arquivo ZIP válido.");
                        throw new InvalidDataException("O arquivo não é um arquivo ZIP válido.");
                    }
                }

                // Limpa a pasta de extração se já existir
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }

                Directory.CreateDirectory(extractPath);

                // Extrai o ZIP
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                Logger.LogInfo($"ZIP extraído com sucesso para: {extractPath}");
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
                    File.SetAttributes(TempZipPath, FileAttributes.Normal);
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
                string executablePath = Path.Combine(TempExtractPath, UpdateSubfolder, ExecutableFileName);
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
                string currentDir = Path.GetDirectoryName(currentExePath);

                // O caminho da pasta de extração completa (incluindo a subpasta TrocaInternet)
                string sourceDir = Path.GetDirectoryName(newExecutablePath);

                // Cria um arquivo de lote para realizar a atualização
                string batchFilePath = Path.Combine(Path.GetTempPath(), $"update_{DateTime.Now:yyyyMMdd_HHmmss}.bat");

                // Conteúdo do arquivo de lote
                string batchContent = $@"
                @echo off
                echo ================================================================
                echo         Iniciando processo de atualizacao do TrocaInternet
                echo ================================================================
                echo.
                echo Aguardando o processo atual fechar...
                timeout /t 3 /nobreak > nul
                echo.
                echo Encerrando o processo atual...
                taskkill /f /im {Path.GetFileName(currentExePath)} > nul 2>&1
                echo.
                echo Copiando novos arquivos...
                echo Origem: {sourceDir}
                echo Destino: {currentDir}
                echo.
                xcopy /s /e /y /i /h /r ""{sourceDir}"" ""{currentDir}"" > nul
                if %errorlevel% neq 0 (
                    echo ERRO: Falha ao copiar arquivos.
                    pause
                    exit /b 1
                )
                echo.
                echo Arquivos copiados com sucesso!
                echo.
                echo Criando arquivo de sinalizacao...
                echo. > ""{currentDir}\.updated""
                echo.
                echo Iniciando nova versao...
                cd /d ""{currentDir}""
                start /B """" ""{Path.GetFileName(currentExePath)}""
                echo.
                echo Limpando arquivos temporais...
                del ""{batchFilePath}""
                del ""{TempZipPath}""
                if exist ""{TempExtractPath}"" rd /s /q ""{TempExtractPath}""
                echo.
                echo ================================================================
                echo                Atualizacao concluida com sucesso!
                echo ================================================================
                timeout /t 3 /nobreak > nul
                ";

                // Salva o arquivo de lote
                File.WriteAllText(batchFilePath, batchContent);

                // Inicia o processo de atualização
                var startInfo = new ProcessStartInfo
                {
                    FileName = batchFilePath,
                    UseShellExecute = true,
                    Verb = "runas", // Solicita privilégios de administrador
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(startInfo);

                // Fecha o aplicativo atual
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Erro ao iniciar processo de atualização: {ex.Message}");
            }
        }
        private static async Task<byte[]> DownloadFileAsync(HttpClient httpClient, string url)
        {
            try
            {
                Logger.LogInfo($"Baixando arquivo de: {url}");

                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                // Verifica se o conteúdo parece ser um ZIP
                if (fileBytes.Length >= 4 &&
                    fileBytes[0] == 0x50 && fileBytes[1] == 0x4B &&
                    (fileBytes[2] == 0x03 && fileBytes[3] == 0x04 ||
                     fileBytes[2] == 0x05 && fileBytes[3] == 0x06 ||
                     fileBytes[2] == 0x07 && fileBytes[3] == 0x08))
                {
                    Logger.LogInfo("Arquivo ZIP válido detectado");
                    return fileBytes;
                }

                Logger.LogError("O arquivo baixado não é um arquivo ZIP válido.");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Erro ao baixar arquivo: {ex.Message}");
                return null;
            }
        }
    }
       
    
}