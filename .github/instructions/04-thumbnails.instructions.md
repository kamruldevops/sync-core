---
applyTo: "**"
---

# Feature 04 — Thumbnail Generation

## Goal
After a photo is uploaded, generate two derivative images using **SixLabors.ImageSharp** and store them in the `photos-thumbnails` Azure Blob container. Update the `Photo` record with the thumbnail and preview paths. This keeps the original safe and reduces data-transfer costs by serving small thumbnails in the gallery.

---

## Derivative sizes

| Name | Size | Mode | Format | Use |
|---|---|---|---|---|
| `thumb_300` | 300 × 300 px | Square crop (centre) | WebP | Gallery grid thumbnails |
| `preview_1200` | 1200 × ∞ px | Max width, aspect-preserved | WebP | Photo detail lightbox |

Blob paths (in `photos-thumbnails` container):
- `{userId}/{publicId}/thumb_300.webp`
- `{userId}/{publicId}/preview_1200.webp`

---

## Backend

### NuGet (already added in setup)
```
SixLabors.ImageSharp
```

### `IThumbnailService` — `Infrastructure/Thumbnails/IThumbnailService.cs`
```csharp
public interface IThumbnailService
{
    /// <summary>Generates thumb_300 and preview_1200, uploads both to blob, returns their paths.</summary>
    Task<ThumbnailResult> GenerateAndUploadAsync(
        Stream originalStream,
        string userId,
        string publicId,
        CancellationToken ct);
}

public record ThumbnailResult(string ThumbPath, string PreviewPath);
```

### `ThumbnailService` — `Infrastructure/Thumbnails/ThumbnailService.cs`

```csharp
public class ThumbnailService(IBlobStorageService blobStorage, IOptions<BlobStorageOptions> opts)
    : IThumbnailService
{
    public async Task<ThumbnailResult> GenerateAndUploadAsync(
        Stream originalStream, string userId, string publicId, CancellationToken ct)
    {
        // Load image once; clone for each derivative
        using var image = await Image.LoadAsync(originalStream, ct);

        var thumbPath   = $"{userId}/{publicId}/thumb_300.webp";
        var previewPath = $"{userId}/{publicId}/preview_1200.webp";

        // --- Thumb 300x300 ---
        using var thumb = image.Clone(ctx => ctx
            .AutoOrient()
            .Resize(new ResizeOptions { Size = new Size(300, 300), Mode = ResizeMode.Crop }));
        using var thumbStream = new MemoryStream();
        await thumb.SaveAsWebpAsync(thumbStream, ct);
        thumbStream.Position = 0;
        await blobStorage.UploadAsync(opts.Value.ThumbnailContainer, thumbPath, thumbStream, "image/webp", ct);

        // --- Preview 1200px max width ---
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
```

### Wire up after upload — `Features/Photos/Upload/UploadPhotoHandler.cs`

After the blob upload, re-open the blob stream (or buffer the original) and call:
```csharp
var originalStream = await blobStorage.OpenReadAsync(opts.Value.OriginalContainer, blobPath, ct);
var thumbnails = await thumbnailService.GenerateAndUploadAsync(originalStream, userId, publicId, ct);

photo.ThumbnailPath = thumbnails.ThumbPath;
photo.PreviewPath   = thumbnails.PreviewPath;
await db.SaveChangesAsync(ct);
```

> **Note:** For large photos this adds latency. Consider running thumbnail generation in an `IHostedService` background queue if upload response time becomes a concern. For the MVP, inline generation is acceptable.

### `IBlobStorageService` additions
```csharp
Task<Stream> OpenReadAsync(string containerName, string blobPath, CancellationToken ct);
```

### `BlobStorageService` implementation note
- `UploadAsync`: use `BlobClient.UploadAsync(stream, overwrite: true)` with the provided content type set via `BlobHttpHeaders`
- `OpenReadAsync`: use `BlobClient.OpenReadAsync()`

---

## SAS Token Generation

Thumbnails and originals are in **private** containers. The API generates short-lived SAS tokens so the frontend can fetch images directly from Blob Storage without proxying bytes through the API.

### Add to `IBlobStorageService`
```csharp
Uri GenerateSasUri(string containerName, string blobPath, TimeSpan validity);
```

### Implementation
```csharp
public Uri GenerateSasUri(string containerName, string blobPath, TimeSpan validity)
{
    var client = _serviceClient.GetBlobContainerClient(containerName).GetBlobClient(blobPath);
    return client.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(validity));
}
```

> SAS tokens require the Storage Account to use **account key** credentials (not managed identity) during local dev. On Azure, use managed identity with a user-delegation SAS.

---

## Serving thumbnail URLs

When the API returns photo data to the frontend it should include pre-signed thumbnail URLs, not raw blob paths.

In the `PhotoDto` response object:
```csharp
public record PhotoDto(
    string PublicId,
    string FileName,
    DateTimeOffset TakenAt,
    string ThumbUrl,    // 15-min SAS URL for thumb_300
    string PreviewUrl   // 15-min SAS URL for preview_1200
);
```

Generate SAS URLs in the query handler (feature 05) when building the `PhotoDto`.

---

## Acceptance Criteria
- [ ] After upload, `thumb_300.webp` and `preview_1200.webp` exist in the `photos-thumbnails` container
- [ ] `Photo.ThumbnailPath` and `Photo.PreviewPath` are set in the database
- [ ] `thumb_300` is exactly 300×300 px
- [ ] `preview_1200` is at most 1200 px wide and has correct aspect ratio
- [ ] EXIF orientation is respected (portrait photos are not rotated incorrectly)
- [ ] The API returns SAS thumbnail URLs that are directly loadable by the browser
- [ ] SAS URLs expire after 15 minutes
