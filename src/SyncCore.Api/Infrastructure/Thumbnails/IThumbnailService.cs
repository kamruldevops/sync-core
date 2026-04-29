namespace SyncCore.Api.Infrastructure.Thumbnails;

public interface IThumbnailService
{
    Task<ThumbnailResult> GenerateAndUploadAsync(
        Stream originalStream,
        string userId,
        string publicId,
        CancellationToken ct);
}

public record ThumbnailResult(string ThumbPath, string PreviewPath);
