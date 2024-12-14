using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TrocaInternet.TrocaInternet
{
    internal class StoreSettings
    {
        public static string StoreNumber = LoadStoreNumber();


        public static string LoadStoreNumber()
        {
            if (File.Exists(Program.StoreConfigPath))
            {
                return File.ReadAllText(Program.StoreConfigPath);
            }
            return "Não configurado";
        }

        public static void SaveStoreNumber(string storeNumber)
        {
            File.WriteAllText(Program.StoreConfigPath, storeNumber);
        }

        public static void ConfigureStoreNumber()
        {
            Console.Write("\n   Digite o número da loja: ");
            StoreNumber = Console.ReadLine();
            SaveStoreNumber(StoreNumber);
            Console.WriteLine($"   Número da loja configurado como: {StoreNumber}");
            Console.WriteLine("   Pressione qualquer tecla para continuar...");
            Console.ReadKey();
            Console.Clear();
        }
    }
}
