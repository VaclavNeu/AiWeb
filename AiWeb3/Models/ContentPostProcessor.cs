using System.Text.RegularExpressions;
using AiWeb3.Data;

namespace AiWeb3.Services;

public record SaveAndLocalizeResult(int SiteId, string? Url, string LocalizedHtml);

public class ContentPostProcessor
{
    private readonly PixabayService _pixabay;
    private readonly YoutubeService _youtube;
    private readonly GeneratedSiteService _sites;

    public ContentPostProcessor(PixabayService pixabay, YoutubeService youtube, GeneratedSiteService sites)
    {
        _pixabay = pixabay;
        _youtube = youtube;
        _sites = sites;
    }

    public async Task<string> ReplacePixabayImagesAsync(string html)
    {
        var matches = Regex.Matches(html, "<img[^>]*pixabay=\"([^\"]+)\"[^>]*>");
        foreach (Match match in matches)
        {
            var originalTag = match.Value;
            var keyword = match.Groups[1].Value;

            string? imageUrl = await _pixabay.GetImageUrlAsync(keyword);

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                foreach (var fb in new[] { "technology", "abstract", "background", "design", "pattern", "business", "modern" })
                {
                    imageUrl = await _pixabay.GetImageUrlAsync(fb);
                    if (!string.IsNullOrWhiteSpace(imageUrl)) break;
                }
            }

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                string attributes = Regex.Replace(originalTag, @"\s?pixabay=""[^""]+""", "");
                var imgTag = Regex.Replace(attributes, @"<img", $"<img src=\"{imageUrl}\" alt=\"{keyword}\"");
                html = html.Replace(originalTag, imgTag);
            }
            else
            {
                var fallbackDiv = "<div style=\"background:#eee;border:1px solid #ccc;padding:20px;text-align:center;height:200px;display:flex;align-items:center;justify-content:center;\">[Obrázek nebyl nalezen]</div>";
                html = html.Replace(originalTag, fallbackDiv);
            }
        }
        return html;
    }

    public async Task<string> LocalizePixabayImagesAsync(int siteId, string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return html;

        var pxMatches = Regex.Matches(html, "<img[^>]*pixabay=\"([^\"]+)\"[^>]*>", RegexOptions.IgnoreCase);
        foreach (Match m in pxMatches)
        {
            var originalTag = m.Value;
            var keyword = m.Groups[1].Value;

            byte[]? bytes = await _pixabay.DownloadImageAsync(keyword);

            if (bytes is null)
            {
                foreach (var fb in new[] { "technology", "abstract", "background", "design", "business", "modern" })
                {
                    bytes = await _pixabay.DownloadImageAsync(fb);
                    if (bytes != null) break;
                }
            }

            if (bytes is null)
            {
                var fallbackDiv =
                    "<div style=\"background:#eee;border:1px solid #ccc;padding:20px;text-align:center;height:200px;display:flex;align-items:center;justify-content:center;\">[Obrázek nedostupný]</div>";
                html = html.Replace(originalTag, fallbackDiv);
                continue;
            }

            var localUrl = await _sites.AddImageAssetAsync(siteId, bytes);

            string attrs = Regex.Replace(originalTag, @"\s?pixabay=""[^""]+""", "", RegexOptions.IgnoreCase);
            string newTag = Regex.Replace(attrs, @"<img", $"<img src=\"{localUrl}\" alt=\"{keyword}\"", RegexOptions.IgnoreCase);

            html = html.Replace(originalTag, newTag);
        }

        var srcMatches = Regex.Matches(html, "<img[^>]*src=\"([^\"]+)\"[^>]*>", RegexOptions.IgnoreCase);
        foreach (Match m in srcMatches)
        {
            var originalTag = m.Value;
            var src = m.Groups[1].Value;

            if (!src.Contains("pixabay.com", StringComparison.OrdinalIgnoreCase))
                continue;

            byte[]? bytes = await _pixabay.DownloadFromUrlAsync(src);
            if (bytes is null) continue;

            var localUrl = await _sites.AddImageAssetAsync(siteId, bytes);

            string newTag = Regex.Replace(originalTag,
                "src=\"([^\"]+)\"",
                $"src=\"{localUrl}\"",
                RegexOptions.IgnoreCase);

            html = html.Replace(originalTag, newTag);
        }

        return html;
    }

    public async Task<string> ReplaceYoutubeEmbedsAsync(string html)
    {
        var matches = Regex.Matches(html, "<iframe[^>]*youtube=\"([^\"]+)\"[^>]*/?>", RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var keyword = match.Groups[1].Value;
            var embedUrl = await _youtube.GetYoutubeEmbedUrlAsync(keyword);

            if (!string.IsNullOrWhiteSpace(embedUrl))
            {
                string iframe = $"<iframe width=\"100%\" height=\"315\" src=\"{embedUrl}\" frameborder=\"0\" allowfullscreen></iframe>";
                html = html.Replace(match.Value, iframe);
            }
        }

        return html;
    }

    public async Task<SaveAndLocalizeResult> SaveAndLocalizeAsync(string userId, string title, string prompt, string html)
    {
        var created = await _sites.CreateAsync(userId, title, prompt, html);
        var localizedHtml = await LocalizePixabayImagesAsync(created.SiteId, html);
        await _sites.UpdateHtmlAsync(created.SiteId, localizedHtml);
        return new SaveAndLocalizeResult(created.SiteId, created.Url, localizedHtml);
    }
}
