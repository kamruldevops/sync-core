using FluentValidation;

namespace SyncCore.Api.Features.Photos.Upload;

public class UploadPhotoValidator : AbstractValidator<UploadPhotoCommand>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/heic"
    ];

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    public UploadPhotoValidator()
    {
        RuleFor(x => x.File).NotNull().WithMessage("File is required.");
        RuleFor(x => x.File.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("File must be a JPEG, PNG, WebP, or HEIC image.");
        RuleFor(x => x.File.Length)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("File size must not exceed 50 MB.");
    }
}
