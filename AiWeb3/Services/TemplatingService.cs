using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

public class TemplatingService
{
    private readonly IWebHostEnvironment _env;
    private readonly PixabayService _pixabay;
    
    static IHtmlHeadElement EnsureHead(IDocument doc)
    {
        var head = doc.Head;
        if (head == null) 
        {
            head = (IHtmlHeadElement)doc.CreateElement("head");

            var html = doc.DocumentElement ?? (IElement)doc.AppendChild(doc.CreateElement("html"));
            if(doc.Body is null)
            html.AppendChild(doc.CreateElement("body"));

            html.Prepend((INode)head);


        }
        return head;
    }
    public TemplatingService(IWebHostEnvironment env, PixabayService pixabay)
    {
        _env = env; _pixabay = pixabay;
    }
    private static void FillFooterColumn(IDocument doc, int colIndex, FooterCol col)
    {
        var titleEl = doc.QuerySelector($"#f-col{colIndex}-title");
        var listEl = doc.QuerySelector($"#f-col{colIndex}-list");

        if (titleEl is not null) titleEl.TextContent = col?.title ?? "";

        if (listEl is not null)
        {
            listEl.InnerHtml = "";
            foreach (var item in col?.items ?? Enumerable.Empty<string>())
            {
                var li = doc.CreateElement("li");
                var span = doc.CreateElement("span"); // vizuální odkaz, bez href
                span.TextContent = item;
                li.AppendChild(span);
                listEl.AppendChild(li);
            }
        }
    }

    private static void FillFooterSocials(IDocument doc, IEnumerable<SocialIcon> socials)
    {
        var box = doc.QuerySelector("#footer-socials");
        if (box is null) return;

        box.InnerHtml = "";
        foreach (var s in socials ?? Enumerable.Empty<SocialIcon>())
        {
            var img = doc.CreateElement("img");
            img.SetAttribute("pixabay", s.pixabay ?? "");
            img.SetAttribute("alt", string.IsNullOrWhiteSpace(s.name) ? "social icon" : s.name);
            box.AppendChild(img);
        }
    }

    private static void FillHeader(IDocument doc, Header header)
    {
        if (header == null) return;

        // MENU
        var nav = doc.QuerySelector(".main-nav");
        if (nav != null && header.nav != null && header.nav.Count > 0)
        {
            nav.InnerHtml = "";
            foreach (var title in header.nav)
            {
                var span = doc.CreateElement("span");
                span.TextContent = title ?? "";
                nav.AppendChild(span);
            }
        }

        // TLAČÍTKA
        var loginBtn = doc.QuerySelector(".actions .btn:not(.solid)");
        if (loginBtn != null && !string.IsNullOrWhiteSpace(header.login))
            loginBtn.TextContent = header.login;

        var buyBtn = doc.QuerySelector(".actions .btn.solid");
        if (buyBtn != null && !string.IsNullOrWhiteSpace(header.buy))
            buyBtn.TextContent = header.buy;
    }

    public async Task<string> BuildHtmlAsync(AiContent dto, string templateFile = "landing.html")
    {
        var templatePath = Path.Combine(_env.ContentRootPath, "Templates", templateFile);
        var html = await File.ReadAllTextAsync(templatePath);

        var context = BrowsingContext.New(Configuration.Default);
        var doc = await context.OpenAsync(req => req.Content(html));

        // helpery
        void SetText(string sel, string v) { var el = doc.QuerySelector(sel); if (el != null) el.TextContent = v ?? ""; }
        void SetList(string sel, IEnumerable<string> items)
        {
            var host = doc.QuerySelector(sel); if (host == null) return;
            host.InnerHtml = "";
            foreach (var s in items ?? Enumerable.Empty<string>())
            {
                var li = doc.CreateElement("li"); li.TextContent = s; host.AppendChild(li);
            }
        }
        Task SetPixabayImgAsync(string sel, string? keyword)
        {
            var img = doc.QuerySelector(sel) as IHtmlImageElement;
            if (img == null || string.IsNullOrWhiteSpace(keyword))
                return Task.CompletedTask;

            // Označíme – skutečná náhrada proběhne později v ReplacePixabayImagesAsync
            img.SetAttribute("pixabay", keyword);
            img.SetAttribute("alt", keyword);
            return Task.CompletedTask;
        }

        // Barvy → přidáme <style> s proměnnými
        var style = doc.CreateElement("style");
        style.TextContent = $@":root{{--accent-600:{dto.colors.accent600};--accent-400:{dto.colors.accent400};--bg:{dto.colors.bg};--text:{dto.colors.text};--muted:{dto.colors.muted};}}";

        var headEl = EnsureHead(doc);
        headEl.AppendChild(style);


        // Texty
        SetText("[data-ai='pageTitle']", dto.text.pageTitle);
        SetText("[data-ai='announceText']", dto.text.announceText);
        SetText("[data-ai='title']", dto.text.title);
        SetText("[data-ai='subtitle']", dto.text.subtitle);
        SetText("[data-ai='ctaText']", dto.text.ctaText);
        SetText("[data-ai='heroHeadline']", dto.text.heroHeadline);
        SetText("[data-ai='heroLead']", dto.text.heroLead);
        SetText("[data-ai='benefit1Title']", dto.text.benefit1Title);
        SetText("[data-ai='benefit1Text']", dto.text.benefit1Text);
        SetText("[data-ai='benefit2Title']", dto.text.benefit2Title);
        SetText("[data-ai='benefit2Text']", dto.text.benefit2Text);
        SetText("[data-ai='benefit3Title']", dto.text.benefit3Title);
        SetText("[data-ai='benefit3Text']", dto.text.benefit3Text);
        SetText("[data-ai='promoTitle']", dto.text.promoTitle);
        SetText("[data-ai='promoLead']", dto.text.promoLead);
        SetText("[data-ai='promoPrimary']", dto.text.promoPrimary);
        SetText("[data-ai='promoSecondary']", dto.text.promoSecondary);
        SetText("[data-ai='stripText']", dto.text.stripText);
        SetText("[data-ai='stripBtn']", dto.text.stripBtn);

        // Seznamy
        SetList("[data-ai-list='promoBenefits']", dto.promoBenefits);

        // Obrázky (zatím jen pixabay klíče; url doplní tvoje existující ReplacePixabayImagesAsync)
        await SetPixabayImgAsync("[data-ai-img='heroLeft']", dto.images.heroLeft?.pixabay);
        await SetPixabayImgAsync("[data-ai-img='heroMain']", dto.images.heroMain?.pixabay);
        await SetPixabayImgAsync("[data-ai-img='heroRight']", dto.images.heroRight?.pixabay);

        // YouTube – nastav klíč do atributu "youtube" (aby tvůj ReplaceYoutubeEmbedsAsync našel)
        var ifr = doc.QuerySelector("iframe[data-ai-youtube]") as IHtmlElement;
        if (ifr != null)
        {
            ifr.SetAttribute("youtube", dto.video.promoVideo ?? "");
            ifr.RemoveAttribute("data-ai-youtube");
        }
        FillHeader(doc, dto.header);

        FillFooterColumn(doc, 1, dto.footer.col1);
        FillFooterColumn(doc, 2, dto.footer.col2);
        FillFooterColumn(doc, 3, dto.footer.col3);
        FillFooterColumn(doc, 4, dto.footer.col4);
        FillFooterColumn(doc, 5, dto.footer.col5);

        // Social ikony
        FillFooterSocials(doc, dto.footer.socials);

        // Subfooter poznámka
        var note = doc.QuerySelector("#subnote");
        if (note != null) note.TextContent = dto.footer.subNote ?? "";

        return doc.DocumentElement.OuterHtml;
    }


}
