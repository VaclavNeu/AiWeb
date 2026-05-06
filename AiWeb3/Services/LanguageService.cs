using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

public class LanguageService
{
    private readonly IHttpContextAccessor _http;

    public LanguageService(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Task SetLanguageAsync(string culture)
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return Task.CompletedTask;

        // Zapiš cookie, kterou čte RequestLocalization middleware
        ctx.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Path = "/",                      // velmi důležité
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,              // kvůli GDPR bannerům
                HttpOnly = false,                // ať je vidět v devtools
                Secure = ctx.Request.IsHttps
            });

        return Task.CompletedTask;
    }
}
