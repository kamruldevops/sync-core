using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SyncCore.Api.Common.Options;
using SyncCore.Api.Infrastructure.Auth;
using SyncCore.Api.Infrastructure.BlobStorage;
using SyncCore.Api.Infrastructure.Data;
using System.Security.Claims;

namespace SyncCore.Api.Features.Photos.ImageProxy;

public static class ImageProxyEndpoints
{
    public static void MapImageProxyEndpoints(this WebApplication app)
    {
        app.MapGet("/photos/{publicId}/thumb", ServeThumb)
           .RequireAuthorization()
           .WithTags("Photos");

        app.MapGet("/photos/{publicId}/preview", ServePreview)
           .RequireAuthorization()
           .WithTags("Photos");
    }

    private static async Task<IResult> ServeThumb(
        string publicId,
        IPhotoDbContext db,
        IBlobStorageService blobStorage,
        IOptions<BlobStorageOptions> opts,
        ClaimsPrincipal user,
        HttpRequest request,
        HttpResponse response,
        CancellationToken ct)
    {
        var userId = CurrentUser.GetUserId(user);
        var blobPath = await db.Photos
            .Where(p => p.PublicId == publicId && p.UserId == userId && !p.IsDeleted && p.ThumbnailPath != null)
            .Select(p => p.ThumbnailPath)
            .FirstOrDefaultAsync(ct);

        if (blobPath is null) return Results.NotFound();

        // Use publicId as ETag — thumbnails are immutable once generated
        var etag = $"\"{publicId}-thumb\"";
        if (request.Headers.IfNoneMatch == etag)
        {
            response.Headers.ETag = etag;
            return Results.StatusCode(304);
        }

        // private: only the user's browser may cache (not CDNs/proxies) — matches Google Photos behaviour
        response.Headers.CacheControl = "private, max-age=86400";
        response.Headers.ETag = etag;

        var stream = await blobStorage.OpenReadAsync(opts.Value.ThumbnailContainer, blobPath, ct);
        return Results.Stream(stream, "image/webp");
    }

    private static async Task<IResult> ServePreview(
        string publicId,
        IPhotoDbContext db,
        IBlobStorageService blobStorage,
        IOptions<BlobStorageOptions> opts,
        ClaimsPrincipal user,
        HttpRequest request,
        HttpResponse response,
        CancellationToken ct)
    {
        var userId = CurrentUser.GetUserId(user);
        var blobPath = await db.Photos
            .Where(p => p.PublicId == publicId && p.UserId == userId && !p.IsDeleted && p.PreviewPath != null)
            .Select(p => p.PreviewPath)
            .FirstOrDefaultAsync(ct);

        if (blobPath is null) return Results.NotFound();

        var etag = $"\"{publicId}-preview\"";
        if (request.Headers.IfNoneMatch == etag)
        {
            response.Headers.ETag = etag;
            return Results.StatusCode(304);
        }

        response.Headers.CacheControl = "private, max-age=86400";
        response.Headers.ETag = etag;

        var stream = await blobStorage.OpenReadAsync(opts.Value.ThumbnailContainer, blobPath, ct);
        return Results.Stream(stream, "image/webp");
    }
}
