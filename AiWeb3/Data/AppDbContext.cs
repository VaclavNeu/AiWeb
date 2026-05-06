using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace AiWeb3.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<GeneratedSite> GeneratedSites => Set<GeneratedSite>();
    public DbSet<GeneratedAsset> GeneratedAssets => Set<GeneratedAsset>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<IdentityUser>()
        .ToTable("AspNetUsers", tb => tb.ExcludeFromMigrations());

        b.Entity<GeneratedSite>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.UserId).IsRequired();
            e.HasIndex(x => new { x.UserId, x.CreatedAt }); 
            e.HasMany(x => x.Assets)
             .WithOne(x => x.GeneratedSite!)
             .HasForeignKey(x => x.GeneratedSiteId)
             .OnDelete(DeleteBehavior.Cascade);

         
            
        });

        b.Entity<GeneratedSite>()
        .HasOne<IdentityUser>()              
        .WithMany()
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.Cascade);

        b.Entity<GeneratedAsset>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasMaxLength(30);
            e.Property(x => x.Path).HasMaxLength(500).IsRequired();
        });
    }
}

public class GeneratedSite
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Prompt { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GeneratedAsset> Assets { get; set; } = new List<GeneratedAsset>();
}

public class GeneratedAsset
{
    public int Id { get; set; }
    public int GeneratedSiteId { get; set; }
    public GeneratedSite? GeneratedSite { get; set; }

    public string Path { get; set; } = default!;   // např. wwwroot/sites/{id}/index.html
    public string Kind { get; set; } = "html";     // html/css/js/image…
    public string? Content { get; set; }           // pro textové assety
}
