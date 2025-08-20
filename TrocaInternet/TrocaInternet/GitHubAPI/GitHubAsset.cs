using Newtonsoft.Json;

namespace TrocaInternet.TrocaInternet.GitHubAPI;

public class GitHubAsset
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("browser_download_url")]
    public string BrowserDownloadUrl { get; set; }

    public static GitHubAsset FindAsset(GitHubRelease release)
    {
        // Procura por um arquivo ZIP
        var zipAsset = release.Assets.FirstOrDefault(a =>
            a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
            a.Name.Contains("TrocaInternet"));

        if (zipAsset != null)
        {
            Logger.LogInfo($"Asset encontrado: {zipAsset.Name}");
            return zipAsset;
        }

        // Se não encontrar um ZIP, procura por qualquer arquivo que contenha "TrocaInternet"
        var anyAsset = release.Assets.FirstOrDefault(a =>
            a.Name.Contains("TrocaInternet"));

        if (anyAsset != null)
        {
            Logger.LogInfo($"Asset alternativo encontrado: {anyAsset.Name}");
            return anyAsset;
        }

        return null;
    }
}
