using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SyncCore.Api.Infrastructure.Data;

namespace SyncCore.Api.Features.Photos.List;

public class GetPhotosHandler(
    IPhotoDbContext db) : IRequestHandler<GetPhotosQuery, GetPhotosResult>
{

    public async Task<GetPhotosResult> Handle(GetPhotosQuery request, CancellationToken cancellationToken)
    {
        var q = db.Photos.Where(p => p.UserId == request.UserId);

        if (request.IncludeTrashed)
            q = q.Where(p => p.IsDeleted);
        else
            q = q.Where(p => !p.IsDeleted);

        if (request.FavouritesOnly)
            q = q.Where(p => p.IsFavourite);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            q = q.Where(p => EF.Functions.ILike(p.FileName, $"%{request.SearchTerm}%"));

        if (request.DateFrom.HasValue)
            q = q.Where(p => p.TakenAt >= request.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        if (request.DateTo.HasValue)
            q = q.Where(p => p.TakenAt <= request.DateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

        // Cursor-based pagination
        if (!string.IsNullOrEmpty(request.Cursor))
        {
            var (cursorTakenAt, cursorPublicId) = DecodeCursor(request.Cursor);
            q = q.Where(p => p.TakenAt < cursorTakenAt ||
                              (p.TakenAt == cursorTakenAt && string.Compare(p.PublicId, cursorPublicId) < 0));
        }

        q = q.OrderByDescending(p => p.TakenAt).ThenByDescending(p => p.PublicId);

        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var photos = await q.Take(pageSize + 1).ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (photos.Count > pageSize)
        {
            photos.RemoveAt(photos.Count - 1);
            var last = photos[^1];
            nextCursor = EncodeCursor(last.TakenAt, last.PublicId);
        }

        var dtos = photos.Select(p => new PhotoDto(
            p.PublicId,
            p.FileName,
            p.TakenAt,
            p.ThumbnailPath != null ? $"/photos/{p.PublicId}/thumb" : string.Empty,
            p.PreviewPath   != null ? $"/photos/{p.PublicId}/preview" : string.Empty,
            p.IsFavourite,
            p.DeletedAt
        )).ToList();

        return new GetPhotosResult(dtos, nextCursor);
    }

    private static string EncodeCursor(DateTimeOffset takenAt, string publicId)
    {
        var raw = $"{takenAt:O}|{publicId}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static (DateTimeOffset TakenAt, string PublicId) DecodeCursor(string cursor)
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = raw.Split('|', 2);
        return (DateTimeOffset.Parse(parts[0]), parts[1]);
    }
}
