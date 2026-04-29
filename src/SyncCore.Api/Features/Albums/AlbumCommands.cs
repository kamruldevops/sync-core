using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SyncCore.Api.Common.Options;
using SyncCore.Api.Infrastructure.BlobStorage;
using SyncCore.Api.Infrastructure.Data;
using SyncCore.Api.Infrastructure.Data.Entities;

namespace SyncCore.Api.Features.Albums;

public record AlbumDto(string PublicId, string Name, int PhotoCount, string? CoverThumbUrl);

// --- Create ---
public record CreateAlbumCommand(string Name, string UserId) : IRequest<AlbumDto>;

public class CreateAlbumHandler(IPhotoDbContext db) : IRequestHandler<CreateAlbumCommand, AlbumDto>
{
    public async Task<AlbumDto> Handle(CreateAlbumCommand request, CancellationToken cancellationToken)
    {
        var album = new Album
        {
            PublicId = Ulid.NewUlid().ToString(),
            UserId = request.UserId,
            Name = request.Name,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Albums.Add(album);
        await db.SaveChangesAsync(cancellationToken);
        return new AlbumDto(album.PublicId, album.Name, 0, null);
    }
}

// --- List ---
public record ListAlbumsQuery(string UserId) : IRequest<IReadOnlyList<AlbumDto>>;

public class ListAlbumsHandler(
    IPhotoDbContext db,
    IBlobStorageService blobStorage,
    IOptions<BlobStorageOptions> opts) : IRequestHandler<ListAlbumsQuery, IReadOnlyList<AlbumDto>>
{
    public async Task<IReadOnlyList<AlbumDto>> Handle(ListAlbumsQuery request, CancellationToken cancellationToken)
    {
        var albums = await db.Albums
            .Where(a => a.UserId == request.UserId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.PublicId, a.Name,
                PhotoCount = a.AlbumPhotos.Count(ap => !ap.Photo.IsDeleted),
                Cover = a.AlbumPhotos
                    .Where(ap => !ap.Photo.IsDeleted && ap.Photo.ThumbnailPath != null)
                    .OrderByDescending(ap => ap.AddedAt)
                    .Select(ap => ap.Photo.ThumbnailPath)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return albums.Select(a => new AlbumDto(
            a.PublicId, a.Name, a.PhotoCount,
            a.Cover != null
                ? blobStorage.GenerateSasUri(opts.Value.ThumbnailContainer, a.Cover, TimeSpan.FromMinutes(15)).ToString()
                : null
        )).ToList();
    }
}

// --- Add photo to album ---
public record AddPhotoToAlbumCommand(string AlbumPublicId, string PhotoPublicId, string UserId) : IRequest;

public class AddPhotoToAlbumHandler(IPhotoDbContext db) : IRequestHandler<AddPhotoToAlbumCommand>
{
    public async Task Handle(AddPhotoToAlbumCommand request, CancellationToken cancellationToken)
    {
        var album = await db.Albums.FirstOrDefaultAsync(
            a => a.PublicId == request.AlbumPublicId && a.UserId == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Album {request.AlbumPublicId} not found.");

        var photo = await db.Photos.FirstOrDefaultAsync(
            p => p.PublicId == request.PhotoPublicId && p.UserId == request.UserId && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Photo {request.PhotoPublicId} not found.");

        var existing = await db.AlbumPhotos.FindAsync([album.Id, photo.Id], cancellationToken);
        if (existing is null)
        {
            db.AlbumPhotos.Add(new AlbumPhoto { AlbumId = album.Id, PhotoId = photo.Id, AddedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

// --- Remove photo from album ---
public record RemovePhotoFromAlbumCommand(string AlbumPublicId, string PhotoPublicId, string UserId) : IRequest;

public class RemovePhotoFromAlbumHandler(IPhotoDbContext db) : IRequestHandler<RemovePhotoFromAlbumCommand>
{
    public async Task Handle(RemovePhotoFromAlbumCommand request, CancellationToken cancellationToken)
    {
        var ap = await db.AlbumPhotos
            .Include(x => x.Album)
            .Include(x => x.Photo)
            .FirstOrDefaultAsync(x =>
                x.Album.PublicId == request.AlbumPublicId &&
                x.Photo.PublicId == request.PhotoPublicId &&
                x.Album.UserId == request.UserId, cancellationToken);

        if (ap is not null)
        {
            db.AlbumPhotos.Remove(ap);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

// --- Delete album ---
public record DeleteAlbumCommand(string AlbumPublicId, string UserId) : IRequest;

public class DeleteAlbumHandler(IPhotoDbContext db) : IRequestHandler<DeleteAlbumCommand>
{
    public async Task Handle(DeleteAlbumCommand request, CancellationToken cancellationToken)
    {
        var album = await db.Albums
            .Include(a => a.AlbumPhotos)
            .FirstOrDefaultAsync(a => a.PublicId == request.AlbumPublicId && a.UserId == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Album {request.AlbumPublicId} not found.");

        db.AlbumPhotos.RemoveRange(album.AlbumPhotos);
        db.Albums.Remove(album);
        await db.SaveChangesAsync(cancellationToken);
    }
}
