
namespace TrocaInternet.TrocaInternet;

internal static class StoreSettings
{
    public static string StoreNumber { get; private set; } = LoadStoreNumber();

    private static string LoadStoreNumber()
    {
        try
        {
            if (File.Exists(Program.StoreConfigPath))
            {
                return File.ReadAllText(Program.StoreConfigPath);
            }
            return "Não configurado";
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao carregar número da loja: {ex.Message}");
            return "Não configurado";
        }
    }

    public static void SaveStoreNumber(string storeNumber)
    {
        try
        {
            File.WriteAllText(Program.StoreConfigPath, storeNumber);
            StoreNumber = storeNumber;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao salvar número da loja: {ex.Message}");
        }
    }

    public static void ConfigureStoreNumber()
    {
        try
        {
            Console.Write("\n   Digite o número da loja: ");
            string input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input))
            {
                SaveStoreNumber(input);
                Console.WriteLine($"   Número da loja configurado como: {input}");
            }
            else
            {
                Console.WriteLine("   Número da loja inválido.");
            }
            Console.WriteLine("   Pressione qualquer tecla para continuar...");
            Console.ReadKey();
            Console.Clear();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao configurar número da loja: {ex.Message}");
        }
    }
}