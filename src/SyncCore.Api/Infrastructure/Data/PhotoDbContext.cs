using Microsoft.EntityFrameworkCore;
using SyncCore.Api.Infrastructure.Data.Entities;

namespace SyncCore.Api.Infrastructure.Data;

public class PhotoDbContext(DbContextOptions<PhotoDbContext> options) : DbContext(options), IPhotoDbContext
{
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<AlbumPhoto> AlbumPhotos => Set<AlbumPhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Photo>(b =>
        {
            b.ToTable("photos");
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.PublicId).IsUnique();
            b.HasIndex(p => p.UserId);
            b.HasIndex(p => p.IsDeleted).HasFilter("\"IsDeleted\" = false");
            b.HasIndex(p => new { p.UserId, p.DeletedAt }).HasFilter("\"IsDeleted\" = true");
        });

        modelBuilder.Entity<Album>(b =>
        {
            b.ToTable("albums");
            b.HasKey(a => a.Id);
            b.HasIndex(a => a.PublicId).IsUnique();
            b.HasIndex(a => a.UserId);
        });

        modelBuilder.Entity<AlbumPhoto>(b =>
        {
            b.ToTable("album_photos");
            b.HasKey(ap => new { ap.AlbumId, ap.PhotoId });
            b.HasOne(ap => ap.Album).WithMany(a => a.AlbumPhotos).HasForeignKey(ap => ap.AlbumId);
            b.HasOne(ap => ap.Photo).WithMany(p => p.AlbumPhotos).HasForeignKey(ap => ap.PhotoId);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);
}
