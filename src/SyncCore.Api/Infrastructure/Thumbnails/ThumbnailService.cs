using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SyncCore.Api.Infrastructure.BlobStorage;
using SyncCore.Api.Common.Options;

namespace SyncCore.Api.Infrastructure.Thumbnails;

public class ThumbnailService(IBlobStorageService blobStorage, IOptions<BlobStorageOptions> opts) : IThumbnailService
{
    public async Task<ThumbnailResult> GenerateAndUploadAsync(
        Stream originalStream, string userId, string publicId, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(originalStream, ct);

        var thumbPath = $"{userId}/{publicId}/thumb_300.webp";
        var previewPath = $"{userId}/{publicId}/preview_1200.webp";

        // Thumb 300x300 square crop
        using var thumb = image.Clone(ctx => ctx
            .AutoOrient()
            .Resize(new ResizeOptions { Size = new Size(300, 300), Mode = ResizeMode.Crop }));
        using var thumbStream = new MemoryStream();
        await thumb.SaveAsWebpAsync(thumbStream, ct);
        thumbStream.Position = 0;
        await blobStorage.UploadAsync(opts.Value.ThumbnailContainer, thumbPath, thumbStream, "image/webp", ct);

        // Preview 1200px max width, aspect-preserved
        using var preview = image.Clone(ctx => ctx
            .AutoOrient()
            .Resize(new ResizeOptions { Size = new Size(1200, 0), Mode = ResizeMode.Max }));
        using var previewStream = new MemoryStream();
        await preview.SaveAsWebpAsync(previewStream, ct);
        previewStream.Position = 0;
        await blobStorage.UploadAsync(opts.Value.ThumbnailContainer, previewPath, previewStream, "image/webp", ct);

        return new ThumbnailResult(thumbPath, previewPath);
    }
}
