using MediatR;

namespace SyncCore.Api.Features.Photos.List;

public record GetPhotosQuery(
    string UserId,
    string? Cursor,
    int PageSize = 50,
    string? SearchTerm = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    bool FavouritesOnly = false,
    bool IncludeTrashed = false
) : IRequest<GetPhotosResult>;

public record GetPhotosResult(IReadOnlyList<PhotoDto> Items, string? NextCursor);

public record PhotoDto(
    string PublicId,
    string FileName,
    DateTimeOffset TakenAt,
    string ThumbUrl,
    string PreviewUrl,
    bool IsFavourite,
    DateTimeOffset? DeletedAt
);
