namespace SyncCore.Api.Infrastructure.Data.Entities;

public class Album
{
    public int Id { get; set; }
    public string PublicId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<AlbumPhoto> AlbumPhotos { get; set; } = [];
}
