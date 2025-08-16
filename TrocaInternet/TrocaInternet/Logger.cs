
namespace TrocaInternet.TrocaInternet;

public static class Logger
{
    private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
    private static readonly string LogPath = Path.Combine(LogDirectory, "log.txt");
    private const long MaxLogFileSize = 1024 * 1024; // 1MB
    private const int MaxLogFiles = 5;

    static Logger()
    {
        // Garante que o diretório de logs exista
        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }
    }

    public static void LogInfo(string message)
    {
        Log("INFO", message);
    }

    public static void LogError(string message)
    {
        Log("ERROR", message);
    }

    public static void LogWarning(string message)
    {
        Log("WARNING", message);
    }

    private static void Log(string level, string message)
    {
        try
        {
            // Verifica se precisa fazer backup do arquivo atual
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxLogFileSize)
            {
                RotateLogs();
            }

            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            File.AppendAllText(LogPath, logMessage + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Se não conseguir escrever no log, tenta escrever no console se visível
            if (Program.IsConsoleVisible())
            {
                Console.WriteLine($"Erro ao escrever no log: {ex.Message}");
            }
        }
    }

    private static void RotateLogs()
    {
        try
        {
            // Exclui o arquivo de log mais antigo se existirem muitos
            var logFiles = Directory.GetFiles(LogDirectory, "log*.txt")
                .OrderBy(f => f)
                .ToList();

            while (logFiles.Count >= MaxLogFiles)
            {
                File.Delete(logFiles[0]);
                logFiles.RemoveAt(0);
            }

            // Renomeia o arquivo atual com timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(LogDirectory, $"log_{timestamp}.txt");
            File.Move(LogPath, backupPath);
        }
        catch (Exception ex)
        {
            if (Program.IsConsoleVisible())
            {
                Console.WriteLine($"Erro ao rotacionar logs: {ex.Message}");
            }
        }
    }
    public static void ShowLogs()
    {
        Console.Clear();
        string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        if (!Directory.Exists(logDirectory))
        {
            Console.WriteLine("   Diretório de logs não encontrado.");
            Program.Pause();
            return;
        }

        // Lista todos os arquivos de log
        var logFiles = Directory.GetFiles(logDirectory, "log*.txt")
            .OrderByDescending(f => f)
            .ToList();

        if (logFiles.Count == 0)
        {
            Console.WriteLine("   Nenhum arquivo de log encontrado.");
            Program.Pause();
            return;
        }

        Console.WriteLine("   Arquivos de log disponíveis:");
        for (int i = 0; i < logFiles.Count; i++)
        {
            FileInfo fi = new FileInfo(logFiles[i]);
            Console.WriteLine($"   {i + 1}. {Path.GetFileName(logFiles[i])} ({fi.Length / 1024} KB)");
        }

        Console.Write("\n   Digite o número do arquivo para visualizar (0 para cancelar): ");
        string input = Console.ReadLine();

        if (int.TryParse(input, out int choice) && choice > 0 && choice <= logFiles.Count)
        {
            string selectedFile = logFiles[choice - 1];
            Console.Clear();
            Console.WriteLine($"   Visualizando: {Path.GetFileName(selectedFile)}");
            Console.WriteLine("   " + new string('-', 60));

            try
            {
                // Lê as últimas 50 linhas para não sobrecarregar o console
                string[] lines = File.ReadAllLines(selectedFile);
                int linesToShow = Math.Min(50, lines.Length);

                for (int i = lines.Length - linesToShow; i < lines.Length; i++)
                {
                    Console.WriteLine($"   {lines[i]}");
                }

                Console.WriteLine($"\n   Total de entradas: {lines.Length}");
                Console.WriteLine($"   Arquivo: {selectedFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Erro ao ler o arquivo: {ex.Message}");
            }
        }
        Program.Pause();
    }
}