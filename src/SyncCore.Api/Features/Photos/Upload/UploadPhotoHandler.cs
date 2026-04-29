using MediatR;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.Extensions.Options;
using SyncCore.Api.Common.Options;
using SyncCore.Api.Infrastructure.BlobStorage;
using SyncCore.Api.Infrastructure.Data;
using SyncCore.Api.Infrastructure.Data.Entities;
using SyncCore.Api.Infrastructure.Thumbnails;

namespace SyncCore.Api.Features.Photos.Upload;

public class UploadPhotoHandler(
    IPhotoDbContext db,
    IBlobStorageService blobStorage,
    IThumbnailService thumbnailService,
    IOptions<BlobStorageOptions> opts) : IRequestHandler<UploadPhotoCommand, UploadPhotoResult>
{
    public async Task<UploadPhotoResult> Handle(UploadPhotoCommand request, CancellationToken cancellationToken)
    {
        var publicId = Ulid.NewUlid().ToString();
        var ext = GetExtension(request.File.ContentType);
        var blobPath = $"{request.UserId}/{publicId}/original{ext}";

        // Upload original to blob
        await using var stream = request.File.OpenReadStream();
        await blobStorage.UploadAsync(opts.Value.OriginalContainer, blobPath, stream, request.File.ContentType, cancellationToken);

        // Extract EXIF date
        var takenAt = await ExtractTakenAtAsync(request.File);

        var photo = new Photo
        {
            PublicId = publicId,
            UserId = request.UserId,
            FileName = request.File.FileName,
            ContentType = request.File.ContentType,
            FileSizeBytes = request.File.Length,
            BlobPath = blobPath,
            TakenAt = takenAt,
            UploadedAt = DateTimeOffset.UtcNow,
        };

        db.Photos.Add(photo);
        await db.SaveChangesAsync(cancellationToken);

        // Generate thumbnails inline (re-open stream from blob)
        var originalStream = await blobStorage.OpenReadAsync(opts.Value.OriginalContainer, blobPath, cancellationToken);
        var thumbnails = await thumbnailService.GenerateAndUploadAsync(originalStream, request.UserId, publicId, cancellationToken);

        photo.ThumbnailPath = thumbnails.ThumbPath;
        photo.PreviewPath = thumbnails.PreviewPath;
        await db.SaveChangesAsync(cancellationToken);

        return new UploadPhotoResult(photo.PublicId, photo.FileName, photo.TakenAt);
    }

    private static string GetExtension(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/heic" => ".heic",
        _ => ".bin"
    };

    private static async Task<DateTimeOffset> ExtractTakenAtAsync(IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var directories = ImageMetadataReader.ReadMetadata(stream);
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfd is not null && subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dt))
                return new DateTimeOffset(dt, TimeSpan.Zero);
        }
        catch
        {
            // Fall through to default
        }
        return DateTimeOffset.UtcNow;
    }
}
