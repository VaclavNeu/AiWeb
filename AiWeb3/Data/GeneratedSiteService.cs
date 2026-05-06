using AiWeb3.Data;
using Microsoft.EntityFrameworkCore;

namespace AiWeb3.Services;

public record CreatedSiteResult(int SiteId, int? HtmlAssetId, string? Url);

public class GeneratedSiteService
{
    private readonly AppDbContext _db;
    public GeneratedSiteService(AppDbContext db) => _db = db;

    // --- EXISTUJÍCÍ: vytvoření webu s HTML ---
    public async Task<CreatedSiteResult> CreateAsync(string userId, string title, string prompt, string html)
    {
        var site = new GeneratedSite { UserId = userId, Title = title, Prompt = prompt };

        var htmlAsset = new GeneratedAsset { Kind = "html", Path = "", Content = html };
        site.Assets.Add(htmlAsset);

        _db.GeneratedSites.Add(site);
        await _db.SaveChangesAsync();

        htmlAsset.Path = $"/sites/db/asset/{htmlAsset.Id}";
        await _db.SaveChangesAsync();

        return new CreatedSiteResult(site.Id, htmlAsset.Id, htmlAsset.Path);
    }

    // --- EXISTUJÍCÍ overload ---
    public async Task<int> CreateAsync(
        string userId, string title, string prompt,
        IEnumerable<(string path, string kind, string? content)> assets)
    {
        var site = new GeneratedSite { UserId = userId, Title = title, Prompt = prompt };

        foreach (var a in assets)
            site.Assets.Add(new GeneratedAsset { Path = a.path ?? "", Kind = a.kind, Content = a.content });

        _db.GeneratedSites.Add(site);
        await _db.SaveChangesAsync();

        var htmlAsset = site.Assets.FirstOrDefault(a => a.Kind == "html");
        if (htmlAsset is not null)
        {
            htmlAsset.Path = $"/sites/db/asset/{htmlAsset.Id}";
            await _db.SaveChangesAsync();
        }

        return site.Id;
    }

    // --- NOVÉ: přidej obrázek jako asset a vrať URL ---
    public async Task<string> AddImageAssetAsync(int siteId, byte[] data)
    {
        var site = await _db.GeneratedSites.Include(s => s.Assets)
                                           .FirstOrDefaultAsync(s => s.Id == siteId)
                   ?? throw new InvalidOperationException("Site nenalezen.");

        var asset = new GeneratedAsset
        {
            Kind = "image",
            Path = "",                           // doplníme po SaveChanges
            Content = Convert.ToBase64String(data),
            
        };

        site.Assets.Add(asset);
        await _db.SaveChangesAsync();

        asset.Path = $"/sites/db/asset/{asset.Id}";
        await _db.SaveChangesAsync();

        return asset.Path!;
    }

    // --- NOVÉ: přepiš HTML asset (po dosazení lokálních src) ---
    public async Task UpdateHtmlAsync(int siteId, string newHtml)
    {
        var htmlAsset = await _db.GeneratedAssets
            .Include(a => a.GeneratedSite)
            .FirstOrDefaultAsync(a => a.GeneratedSiteId == siteId && a.Kind == "html")
            ?? throw new InvalidOperationException("HTML asset nenalezen.");

        htmlAsset.Content = newHtml;
        await _db.SaveChangesAsync();
    }

    // --- EXISTUJÍCÍ LIST/GET/DELETE ---
    public Task<List<GeneratedSite>> ListMineAsync(string userId) =>
        _db.GeneratedSites.Where(s => s.UserId == userId)
           .OrderByDescending(s => s.CreatedAt)
           .Include(s => s.Assets)
           .ToListAsync();

    public Task<GeneratedSite?> GetMineAsync(string userId, int id) =>
        _db.GeneratedSites.Include(s => s.Assets)
           .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

    public async Task<bool> DeleteMineAsync(string userId, int id)
    {
        var site = await _db.GeneratedSites.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (site is null) return false;
        _db.GeneratedSites.Remove(site);
        await _db.SaveChangesAsync();
        return true;
    }
}
