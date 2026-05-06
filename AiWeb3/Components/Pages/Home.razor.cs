using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using AiWeb3.Data;
using AiWeb3.Services;
using System.Security.Claims;

namespace AiWeb3.Components.Pages;

public partial class Home : ComponentBase, IDisposable
{
    // DI
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private AuthenticationStateProvider Auth { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private GeneratedSiteService Sites { get; set; } = default!;
    [Inject] private AiTemplateFiller TemplateFiller { get; set; } = default!;
    [Inject] private ContentPostProcessor Post { get; set; } = default!;

    // UI stav
    private string? message;
    private string selectedResponse = string.Empty;
    private bool isGenerating = false;
    private int generationProgress;
    private CancellationTokenSource? progressCts;

    private string? _lastSavedUrl;
    private string? _SiteName;
    private int _lastSiteId;
    private List<GeneratedSite> _mySites = new();
    private string? _selectedPrompt;
    private bool _loadingSites;
    private bool _deleteMode;
    private int? _siteToDeleteId;
    private string? _deleteError;

    protected override async Task OnInitializedAsync()
    {
        Auth.AuthenticationStateChanged += OnAuthChanged;
        await LoadMySitesAsync();
    }

    public void Dispose()
    {
        Auth.AuthenticationStateChanged -= OnAuthChanged;
    }

    private void OnAuthChanged(Task<AuthenticationState> authTask)
    {
        _ = InvokeAsync(async () =>
        {
            await LoadMySitesAsync();
            StateHasChanged();
        });
    }

    private async Task LoadMySitesAsync()
    {
        try
        {
            _loadingSites = true;
            var auth = await Auth.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _mySites = string.IsNullOrEmpty(userId) ? new() : await Sites.ListMineAsync(userId);
        }
        finally { _loadingSites = false; }
    }

    private void ToggleDeleteMode()
    {
        _deleteError = null;
        _deleteMode = !_deleteMode;
        if (_deleteMode && _mySites?.Any() == true)
            _siteToDeleteId = _mySites.First().Id;
    }

    private async Task DeleteSelectedSiteAsync()
    {
        _deleteError = null;
        if (_siteToDeleteId is null) return;

        var ok = await JS.InvokeAsync<bool>("confirm", "Opravdu chceš tento web odstranit? Akce je nevratná.");
        if (!ok) return;

        var auth = await Auth.GetAuthenticationStateAsync();
        var userId = auth.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) { _deleteError = "Nejsi přihlášen."; return; }

        var success = await Sites.DeleteMineAsync(userId, _siteToDeleteId.Value);
        if (!success) { _deleteError = "Smazání se nepodařilo (záznam nenalezen nebo není tvůj)."; return; }

        await LoadMySitesAsync();
        _siteToDeleteId = _mySites.FirstOrDefault()?.Id;
        _deleteMode = false;
        StateHasChanged();
    }

    private async Task SaveHtmlAsync()
    {
        try
        {
            var auth = await Auth.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                await JS.InvokeVoidAsync("alert", "Pro uložení se prosím přihlaste.");
                return;
            }

            var title = MakeTitleFromPrompt(_selectedPrompt ?? message ?? "Můj web");
            var result = await Post.SaveAndLocalizeAsync(
                userId,
                title,
                _selectedPrompt ?? message ?? string.Empty,
                selectedResponse
            );

            _lastSiteId = result.SiteId;
            _lastSavedUrl = result.Url;
            _SiteName = title;
            selectedResponse = result.LocalizedHtml;

            await LoadMySitesAsync();
        }
        catch (Exception ex)
        {
            selectedResponse = $"<pre style='white-space:pre-wrap'>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
            await JS.InvokeVoidAsync("bootstrapInterop.showModal", "previewModal");
        }
    }

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            selectedResponse = "<p>Zadej prosím popis webu.</p>";
            await JS.InvokeVoidAsync("bootstrapInterop.showModal", "previewModal");
            return;
        }

        // progress animace
        generationProgress = 0;
        progressCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                while (!progressCts.IsCancellationRequested)
                {
                    if (generationProgress < 95)
                    {
                        generationProgress++;
                        await InvokeAsync(StateHasChanged);
                    }
                    await Task.Delay(100, progressCts.Token);
                }
            }
            catch (TaskCanceledException) { }
        });

        try
        {
            isGenerating = true;
            StateHasChanged();

            using var aiCts = new CancellationTokenSource(TimeSpan.FromSeconds(40));
            var html = await TemplateFiller.BuildHtmlFromPromptAsync(message!, aiCts.Token);

            // post-processing (Pixabay / YouTube)
            html = await Post.ReplacePixabayImagesAsync(html);
            html = await Post.ReplaceYoutubeEmbedsAsync(html);

            selectedResponse = html;
            _selectedPrompt = message;

            await InvokeAsync(StateHasChanged);
            await JS.InvokeVoidAsync("bootstrapInterop.showModal", "previewModal");
        }
        catch (Exception ex)
        {
            Console.WriteLine("SEND() ERROR: " + ex);
            selectedResponse = $"<p class='text-danger'>Chyba: {System.Net.WebUtility.HtmlEncode(ex.Message)}</p>";
            await JS.InvokeVoidAsync("bootstrapInterop.showModal", "previewModal");
        }
        finally
        {
            progressCts?.Cancel();
            generationProgress = 100;
            isGenerating = false;
            StateHasChanged();
        }
    }

    private static string MakeTitleFromPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return "Můj web";
        var t = Regex.Replace(prompt, @"\s+", " ").Trim();
        return t.Length > 60 ? t[..60] + "…" : t;
    }

    private string GetLocalReturnUrl()
    {
        var rel = Nav.ToBaseRelativePath(Nav.Uri);
        if (string.IsNullOrWhiteSpace(rel)) return "/";
        return "/" + rel;
    }
}

