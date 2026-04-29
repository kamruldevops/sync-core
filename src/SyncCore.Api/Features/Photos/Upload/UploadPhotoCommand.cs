using MediatR;

namespace SyncCore.Api.Features.Photos.Upload;

public record UploadPhotoCommand(IFormFile File, string UserId) : IRequest<UploadPhotoResult>;
public record UploadPhotoResult(string PublicId, string FileName, DateTimeOffset TakenAt);
