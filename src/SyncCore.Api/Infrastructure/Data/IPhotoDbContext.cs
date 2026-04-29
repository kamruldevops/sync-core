using Microsoft.EntityFrameworkCore;
using SyncCore.Api.Infrastructure.Data.Entities;

namespace SyncCore.Api.Infrastructure.Data;

public interface IPhotoDbContext
{
    DbSet<Photo> Photos { get; }
    DbSet<Album> Albums { get; }
    DbSet<AlbumPhoto> AlbumPhotos { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
