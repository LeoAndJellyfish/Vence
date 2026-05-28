using Microsoft.EntityFrameworkCore;
using Vence.Storage.Entities;

namespace Vence.Storage;

public sealed class VenceDbContext : DbContext
{
    public VenceDbContext(DbContextOptions<VenceDbContext> options)
        : base(options)
    {
    }

    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var document = modelBuilder.Entity<DocumentEntity>();

        document.ToTable("documents");
        document.HasKey(item => item.Id);
        document.HasIndex(item => item.Path).IsUnique();
        document.Property(item => item.Path).HasMaxLength(1024);
        document.Property(item => item.Title).HasMaxLength(256);
        document.Property(item => item.Checksum).HasMaxLength(64);
    }
}
