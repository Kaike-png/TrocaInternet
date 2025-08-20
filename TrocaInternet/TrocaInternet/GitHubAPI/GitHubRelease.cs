using Newtonsoft.Json;

namespace TrocaInternet.TrocaInternet.GitHubAPI;

public class GitHubRelease
{
    [JsonProperty("tag_name")]
    public string TagName { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("body")]
    public string Body { get; set; }

    [JsonProperty("assets")]
    public List<GitHubAsset> Assets { get; set; }

    public static async Task<GitHubRelease> GetLatestReleaseInfo(HttpClient httpClient)
    {
        try
        {
            Logger.LogInfo($"Consultando URL: {UpdateManager.LatestReleaseUrl}");

            var response = await httpClient.GetAsync(UpdateManager.LatestReleaseUrl).ConfigureAwait(false);
            Logger.LogInfo($"Status da resposta: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Logger.LogError($"Erro na requisição: {response.StatusCode} - {response.ReasonPhrase}");
                Logger.LogError($"Conteúdo da resposta: {content}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Logger.LogError("O repositório não foi encontrado ou não possui releases públicos.");
                    Logger.LogError("Verifique se o repositório 'Kaike-png/TrocaInternet' existe e é público.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Logger.LogError("Acesso negado. Verifique se o repositório é público.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Logger.LogError("Não autorizado. Verifique se o User-Agent está configurado corretamente.");
                }

                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            Logger.LogInfo("Resposta da API recebida com sucesso");

            var release = JsonConvert.DeserializeObject<GitHubRelease>(json);
            if (release == null)
            {
                Logger.LogError("Não foi possível desserializar a resposta da API");
                return null;
            }

            Logger.LogInfo($"Release encontrado: {release.Name} ({release.TagName})");
            Logger.LogInfo($"Assets encontrados: {release.Assets?.Count ?? 0}");

            return release;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao obter informações do release: {ex.Message}");
            return null;
        }
    }
}