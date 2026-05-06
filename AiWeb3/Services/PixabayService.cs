using System.Net.Http.Json;
using System.Text.Json;

public class PixabayService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public PixabayService(HttpClient http, IConfiguration config)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _apiKey = config["Pixabay:ApiKey"]
                  ?? throw new InvalidOperationException("Missing config key 'Pixabay:ApiKey'.");
    }

    public async Task<string?> GetImageUrlAsync(string query)
    {
        var url = $"https://pixabay.com/api/?key={_apiKey}&q={Uri.EscapeDataString(query)}&image_type=photo&per_page=3";
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("hits", out var hits) || hits.GetArrayLength() == 0)
            return null;

         return hits[0].GetProperty("webformatURL").GetString();
    }

    // NOVÉ: stáhni skutečná data obrázku
    public async Task<byte[]?> DownloadImageAsync(string query)
    {
        var url = await GetImageUrlAsync(query);
        if (string.IsNullOrWhiteSpace(url)) return null;

        using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (!resp.IsSuccessStatusCode) return null;

        return await resp.Content.ReadAsByteArrayAsync();
    }
    public async Task<byte[]?> DownloadFromUrlAsync(string absoluteUrl)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out _)) return null;

        using var resp = await _http.GetAsync(absoluteUrl, HttpCompletionOption.ResponseHeadersRead);
        if (!resp.IsSuccessStatusCode) return null;

        return await resp.Content.ReadAsByteArrayAsync();
    }
}
