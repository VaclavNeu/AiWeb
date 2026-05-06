public class YoutubeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public YoutubeService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["YouTubeApiKey"] ?? throw new ArgumentNullException("YouTubeApiKey");
    }

    public async Task<string?> GetYoutubeEmbedUrlAsync(string keyword)
    {
        var apiUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&maxResults=1&q={Uri.EscapeDataString(keyword)}&key={_apiKey}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<YouTubeSearchResponse>(apiUrl);
            var videoId = response?.items?.FirstOrDefault()?.id?.videoId;

            if (!string.IsNullOrWhiteSpace(videoId))
            {
                var embedUrl = $"https://www.youtube.com/embed/{videoId}";
                Console.WriteLine($"DEBUG: Query={keyword}, VideoId={videoId}, URL={embedUrl}");
                return $"https://www.youtube-nocookie.com/embed/{videoId}?rel=0&modestbranding=1&autoplay=0";
                ;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"YouTube API error: {ex.Message}");
        }

        return "https://www.youtube.com/embed/Yq0QkCxoTHM";  //fallback video
    }

    private class YouTubeSearchResponse
    {
        public List<Item>? items { get; set; }
    }

    private class Item
    {
        public Id? id { get; set; }
    }

    private class Id
    {
        public string? videoId { get; set; }
    }
}
