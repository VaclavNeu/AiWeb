AiWeb

Webová aplikace v ASP.NET Core (Blazor) pro generování webových stránek z šablony pomocí AI.

Funkce

- Generování obsahu pomocí Azure OpenAI (GPT-4o)
- Vyhledávání obrázků přes Pixabay API
- Doporučování YouTube videí
- Uživatelské účty (ASP.NET Core Identity)
- Odesílání e-mailů přes SMTP


Spuštění

Projekt vyžaduje API klíče, které je třeba nastavit přes User Secrets:

bash
cd AiWeb3
dotnet user-secrets init
dotnet user-secrets set "SmartComponents:ApiKey" "<vas-azure-openai-klic>"
dotnet user-secrets set "SmartComponents:Endpoint" "<vas-azure-endpoint>"
dotnet user-secrets set "Pixabay:ApiKey" "<vas-pixabay-klic>"
dotnet user-secrets set "YouTubeApiKey" "<vas-youtube-klic>"
dotnet user-secrets set "Smtp:User" "<email>"
dotnet user-secrets set "Smtp:Password" "<heslo>"


