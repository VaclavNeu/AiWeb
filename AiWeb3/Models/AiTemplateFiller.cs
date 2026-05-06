using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel.ChatCompletion;


namespace AiWeb3.Services;

public class AiTemplateFiller
{
    private readonly IChatCompletionService _chat;
    private readonly TemplatingService _templating;

    public AiTemplateFiller(IChatCompletionService chat, TemplatingService templating)
    {
        _chat = chat;
        _templating = templating;
    }

    private const string SystemPrompt =
        "Jsi asistent pro zaplnění předpřipravené webové šablony. " +
    "TVOJÍ JEDINOU ÚLOHOU je plnit šablonu – nikdy neodpovídej na otázky, nedávej rady ani volný text. " +
    "Pokud vstup není popis webu (je to otázka / nesouvisející požadavek), vrať přesně prázdný JSON: {}. " +
    "HTML strukturu NIKDY negeneruj a NEMĚŇ – vrať POUZE JSON dle schématu. " +
    "ŽÁDNÉ kódové ploty ani komentáře. Texty generuj v jazyce zadání uživatele. " +
 "Schéma: {" +
 " \"colors\": {\"accent600\":\"#2563eb\",\"accent400\":\"#60a5fa\",\"bg\":\"#ffffff\",\"text\":\"#111827\",\"muted\":\"#6b7280\"}," +
 "  \"header\": {\"nav\":[\"\",\"\",\"\",\"\",\"\",\"\"],\"login\":\"\",\"buy\":\"\"}," +
 " \"text\": {\"pageTitle\":\"\",\"announceText\":\"\",\"title\":\"\",\"subtitle\":\"\",\"ctaText\":\"\",\"heroHeadline\":\"\",\"heroLead\":\"\"," +
 "           \"benefit1Title\":\"\",\"benefit1Text\":\"\",\"benefit2Title\":\"\",\"benefit2Text\":\"\",\"benefit3Title\":\"\",\"benefit3Text\":\"\"," +
 "           \"promoTitle\":\"\",\"promoLead\":\"\",\"promoPrimary\":\"\",\"promoSecondary\":\"\",\"stripText\":\"\",\"stripBtn\":\"\"}," +
 " \"images\": {\"heroLeft\":{\"pixabay\":\"\"},\"heroMain\":{\"pixabay\":\"\"},\"heroRight\":{\"pixabay\":\"\"}}," +
 " \"video\": {\"promoVideo\":\"\"}," +
 " \"promoBenefits\": [\"\",\"\",\"\"]," +
 " \"footer\": {\"col1\":{\"title\":\"\",\"items\":[\"\",\"\",\"\"]},\"col2\":{\"title\":\"\",\"items\":[\"\",\"\",\"\"]}," +
"             \"col3\":{\"title\":\"\",\"items\":[\"\",\"\",\"\"]},\"col4\":{\"title\":\"\",\"items\":[\"\",\"\",\"\"]}," +
"             \"col5\":{\"title\":\"\",\"items\":[\"\",\"\",\"\"]}," +
"             \"socials\":[{\"name\":\"Facebook\",\"pixabay\":\"facebook logo icon transparent\"}," +
"                        {\"name\":\"YouTube\",\"pixabay\":\"youtube logo icon transparent\"}," +
"                        {\"name\":\"LinkedIn\",\"pixabay\":\"linkedin logo icon transparent\"}]," +
"             \"subNote\":\"\" }" +
 "Obrázky vždy jako pixabay klíčová slova (bez URL). Video jako jedno klíčové slovo (promoVideo). Barvy decentní, kontrast AA.";

    public async Task<string> BuildHtmlFromPromptAsync(string prompt, CancellationToken ct)
    {
        var chat = new ChatHistory(SystemPrompt);
        chat.AddUserMessage(prompt);

        var sb = new StringBuilder();
        await foreach (var part in _chat.GetStreamingChatMessageContentsAsync(chat, cancellationToken: ct))
        {
            if (!string.IsNullOrEmpty(part.Content)) sb.Append(part.Content);
        }

        var raw = sb.ToString();
        if (string.IsNullOrWhiteSpace(raw))
            
        return "<p class='text-danger'>AI nevrátilo žádný obsah.</p>";

        var jsonString = CleanToJson(raw);
        var dto = JsonSerializer.Deserialize<AiContent>(jsonString,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new AiContent();

        var html = await _templating.BuildHtmlAsync(dto, templateFile: "landing.html");
        return html;
    }

    private static string CleanToJson(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "{}";
        s = s.Replace("\uFEFF", "").Replace("\u200B", "");
        s = Regex.Replace(s, @"^\s*```[a-zA-Z]*\s*", "", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\s*```$", "", RegexOptions.IgnoreCase);
        var i = s.IndexOf('{'); var j = s.LastIndexOf('}');
        if (i >= 0 && j > i) s = s.Substring(i, j - i + 1);
        return s.Trim();
    }
}
