namespace SyncCore.Api.Infrastructure.Data.Entities;

public class Photo
{
    public int Id { get; set; }
    public string PublicId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public string BlobPath { get; set; } = default!;
    public string? ThumbnailPath { get; set; }
    public string? PreviewPath { get; set; }
    public DateTimeOffset TakenAt { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsFavourite { get; set; }
    public ICollection<AlbumPhoto> AlbumPhotos { get; set; } = [];
}
