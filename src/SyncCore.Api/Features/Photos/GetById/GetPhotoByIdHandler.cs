using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SyncCore.Api.Common.Options;
using SyncCore.Api.Infrastructure.BlobStorage;
using SyncCore.Api.Infrastructure.Data;

namespace SyncCore.Api.Features.Photos.GetById;

public class GetPhotoByIdHandler(
    IPhotoDbContext db,
    IBlobStorageService blobStorage,
    IOptions<BlobStorageOptions> opts) : IRequestHandler<GetPhotoByIdQuery, PhotoDetailDto?>
{
    public async Task<PhotoDetailDto?> Handle(GetPhotoByIdQuery request, CancellationToken cancellationToken)
    {
        var photo = await db.Photos
            .Include(p => p.AlbumPhotos)
            .FirstOrDefaultAsync(p => p.PublicId == request.PublicId
                                   && p.UserId == request.UserId
                                   && !p.IsDeleted, cancellationToken);

        if (photo is null) return null;

        var previewUrl = photo.PreviewPath != null
            ? blobStorage.GenerateSasUri(opts.Value.ThumbnailContainer, photo.PreviewPath, TimeSpan.FromMinutes(15)).ToString()
            : string.Empty;

        var originalUrl = blobStorage.GenerateSasUri(opts.Value.OriginalContainer, photo.BlobPath, TimeSpan.FromMinutes(5)).ToString();

        var albumIds = photo.AlbumPhotos
            .Select(ap => ap.AlbumId.ToString())
            .ToList();

        return new PhotoDetailDto(
            photo.PublicId, photo.FileName, photo.TakenAt, photo.UploadedAt,
            photo.FileSizeBytes, photo.ContentType, previewUrl, originalUrl,
            photo.IsFavourite, albumIds);
    }
}
