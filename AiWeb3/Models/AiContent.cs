public sealed class AiContent
{
    public Colors colors { get; set; } = new();
    public Text text { get; set; } = new();
    public Images images { get; set; } = new();
    public Video video { get; set; } = new();
    public List<string> promoBenefits { get; set; } = new();
    public Header header { get; set; } = new();
    public Footer footer { get; set; } = new();
}
public sealed class Colors { public string accent600 { get; set; } = "#2563eb"; public string accent400 { get; set; } = "#60a5fa"; public string bg { get; set; } = "#ffffff"; public string text { get; set; } = "#111827"; public string muted { get; set; } = "#6b7280"; }

public sealed class SocialIcon
{
    public string name { get; set; } = ""; // např. "Facebook" (ALT bude v cílovém jazyce)
    public string pixabay { get; set; } = ""; // např. "facebook logo icon transparent"
}

public sealed class Header
{
    // Přesně 7 položek v pořadí: Home, Features, Templates, Clients, Pricing, Blog, Contact
    public List<string> nav { get; set; } = new();

    // Text tlačítek vpravo
    public string login { get; set; } = "";
    public string buy { get; set; } = "";
}
public sealed class Text
{
    public string pageTitle { get; set; } = "";
    public string announceText { get; set; } = "";
    public string title { get; set; } = ""; public string subtitle { get; set; } = ""; public string ctaText { get; set; } = "";
    public string heroHeadline { get; set; } = ""; public string heroLead { get; set; } = "";
    public string benefit1Title { get; set; } = ""; public string benefit1Text { get; set; } = "";
    public string benefit2Title { get; set; } = ""; public string benefit2Text { get; set; } = "";
    public string benefit3Title { get; set; } = ""; public string benefit3Text { get; set; } = "";
    public string promoTitle { get; set; } = ""; public string promoLead { get; set; } = "";
    public string promoPrimary { get; set; } = ""; public string promoSecondary { get; set; } = "";
    public string stripText { get; set; } = ""; public string stripBtn { get; set; } = "";
}
public sealed class Images { public PixabayKey heroLeft { get; set; } = new(); public PixabayKey heroMain { get; set; } = new(); public PixabayKey heroRight { get; set; } = new(); }
public sealed class PixabayKey { public string pixabay { get; set; } = ""; }
public sealed class Video { public string promoVideo { get; set; } = ""; }
public sealed class FooterCol { public string title { get; set; } = ""; public List<string> items { get; set; } = new(); }
public sealed class Footer { public FooterCol col1 { get; set; } = new(); public FooterCol col2 { get; set; } = new(); public FooterCol col3 { get; set; } = new(); public FooterCol col4 { get; set; } = new(); public FooterCol col5 { get; set; } = new(); public List<SocialIcon> socials { get; set; } = new(); public string subNote { get; set; } = ""; }
