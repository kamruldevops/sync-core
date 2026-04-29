using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SyncCore.Api.Common.Options;
using SyncCore.Api.Infrastructure.BlobStorage;
using SyncCore.Api.Infrastructure.Data;
using SyncCore.Api.Infrastructure.Queue;

namespace SyncCore.Api.Features.Photos.Delete;

// --- Soft delete ---
public record DeletePhotoCommand(string PublicId, string UserId) : IRequest;

public class DeletePhotoHandler(IPhotoDbContext db, ITrashQueueService trashQueue) : IRequestHandler<DeletePhotoCommand>
{
    public async Task Handle(DeletePhotoCommand request, CancellationToken cancellationToken)
    {
        var photo = await db.Photos.FirstOrDefaultAsync(
            p => p.PublicId == request.PublicId && p.UserId == request.UserId && !p.IsDeleted,
            cancellationToken);

        if (photo is null) throw new KeyNotFoundException($"Photo {request.PublicId} not found.");

        photo.IsDeleted = true;
        photo.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        // Enqueue deferred blob deletion — worker will permanently delete after 30 days
        await trashQueue.EnqueueAsync(request.PublicId, request.UserId, cancellationToken);
    }
}

// --- Restore ---
public record RestorePhotoCommand(string PublicId, string UserId) : IRequest;

public class RestorePhotoHandler(IPhotoDbContext db) : IRequestHandler<RestorePhotoCommand>
{
    public async Task Handle(RestorePhotoCommand request, CancellationToken cancellationToken)
    {
        var photo = await db.Photos.FirstOrDefaultAsync(
            p => p.PublicId == request.PublicId && p.UserId == request.UserId && p.IsDeleted,
            cancellationToken);

        if (photo is null) throw new KeyNotFoundException($"Photo {request.PublicId} not found in trash.");

        photo.IsDeleted = false;
        photo.DeletedAt = null;
        await db.SaveChangesAsync(cancellationToken);
    }
}

// --- Permanent delete ---
public record PermanentDeletePhotoCommand(string PublicId, string UserId) : IRequest;

public class PermanentDeletePhotoHandler(
    IPhotoDbContext db,
    IBlobStorageService blobStorage,
    IOptions<BlobStorageOptions> opts) : IRequestHandler<PermanentDeletePhotoCommand>
{
    public async Task Handle(PermanentDeletePhotoCommand request, CancellationToken cancellationToken)
    {
        var photo = await db.Photos
            .Include(p => p.AlbumPhotos)
            .FirstOrDefaultAsync(
                p => p.PublicId == request.PublicId && p.UserId == request.UserId && p.IsDeleted,
                cancellationToken);

        if (photo is null) throw new KeyNotFoundException($"Photo {request.PublicId} not found in trash.");

        await blobStorage.DeleteAsync(opts.Value.OriginalContainer, photo.BlobPath, cancellationToken);
        if (photo.ThumbnailPath is not null)
            await blobStorage.DeleteAsync(opts.Value.ThumbnailContainer, photo.ThumbnailPath, cancellationToken);
        if (photo.PreviewPath is not null)
            await blobStorage.DeleteAsync(opts.Value.ThumbnailContainer, photo.PreviewPath, cancellationToken);

        db.AlbumPhotos.RemoveRange(photo.AlbumPhotos);
        db.Photos.Remove(photo);
        await db.SaveChangesAsync(cancellationToken);
    }
}
