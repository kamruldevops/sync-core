using MediatR;

namespace SyncCore.Api.Features.Photos.GetById;

public record GetPhotoByIdQuery(string PublicId, string UserId) : IRequest<PhotoDetailDto?>;

public record PhotoDetailDto(
    string PublicId,
    string FileName,
    DateTimeOffset TakenAt,
    DateTimeOffset UploadedAt,
    long FileSizeBytes,
    string ContentType,
    string PreviewUrl,
    string OriginalUrl,
    bool IsFavourite,
    IReadOnlyList<string> AlbumIds
);
