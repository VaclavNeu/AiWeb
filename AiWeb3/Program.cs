using AiWeb3;
using AiWeb3.Components;
using AiWeb3.Data;
using AiWeb3.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();           
builder.Logging.AddDebug();


// Razor Components (.NET 8)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(o => o.DetailedErrors = true);

// Kernel, tvoje služby, lokalizace 
builder.Services.AddKernel().AddAzureOpenAIChatCompletion(
    deploymentName: builder.Configuration["SmartComponents:DeploymentName"]!,
    endpoint: builder.Configuration["SmartComponents:Endpoint"]!,
    apiKey: builder.Configuration["SmartComponents:ApiKey"]!);

builder.Services.AddScoped(sp => KernelPluginFactory.CreateFromType<ThemePlugin>(serviceProvider: sp));
builder.Services.AddHttpClient<PixabayService>();
builder.Services.AddHttpClient<YoutubeService>();
builder.Services.AddScoped<TemplatingService>();
builder.Services.AddScoped<AiTemplateFiller>();
builder.Services.AddScoped<ContentPostProcessor>();

builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LanguageService>();

// >>> IDENTITY – DŮLEŽITÉ <<<
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireNonAlphanumeric = false; // méně přísné požadavky na heslo (pro vývoj)
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();



builder.Services.AddRazorPages();                 // pro /Identity/Account/...
builder.Services.AddAuthorization();              
builder.Services.AddCascadingAuthenticationState();


builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();


// Tvoje aplikační DB
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<GeneratedSiteService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
    catch(Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex + "Migrace pri startu selhala");
    }
}

// lokalizace 
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/account/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(IdentityConstants.ApplicationScheme);
    await ctx.SignOutAsync(IdentityConstants.ExternalScheme);
    return Results.Redirect("/");
})
.AllowAnonymous();
// endpoint pro čtení HTML z DB 
app.MapGet("/sites/db/asset/{id:int}", async (int id, AppDbContext db, ClaimsPrincipal user) =>
{
    var uid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    var asset = await db.GeneratedAssets
        .Include(a => a.GeneratedSite)
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.Id == id);

    if (asset is null) return Results.NotFound();
    if (asset.GeneratedSite?.UserId != uid) return Results.Forbid();

    if (string.Equals(asset.Kind, "image", StringComparison.OrdinalIgnoreCase))
    {
        
        var bytes = Convert.FromBase64String(asset.Content ?? "");
        return Results.File(bytes, "image/jpeg");
    }

    // default: HTML
    return Results.Content(asset.Content ?? "", "text/html; charset=utf-8");
})
.RequireAuthorization();
app.MapRazorPages();



app.UseAntiforgery(); // ← před Razor Components

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// auto migrace 
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.Run();