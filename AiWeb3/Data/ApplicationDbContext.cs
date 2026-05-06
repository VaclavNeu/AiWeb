using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NuGet.ContentModel;

namespace AiWeb3.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<GeneratedSite> GeneratedSites => Set<GeneratedSite>();
    public DbSet<GeneratedAsset> GeneratedAssets => Set<GeneratedAsset>();


}
