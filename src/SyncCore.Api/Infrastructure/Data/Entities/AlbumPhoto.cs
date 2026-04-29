namespace SyncCore.Api.Infrastructure.Data.Entities;

public class AlbumPhoto
{
    public int AlbumId { get; set; }
    public Album Album { get; set; } = default!;
    public int PhotoId { get; set; }
    public Photo Photo { get; set; } = default!;
    public DateTimeOffset AddedAt { get; set; }
}
