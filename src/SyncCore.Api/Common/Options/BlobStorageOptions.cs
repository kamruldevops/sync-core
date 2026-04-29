namespace SyncCore.Api.Common.Options;

public class BlobStorageOptions
{
    public const string SectionName = "AzureBlobStorage";
    public string ConnectionString { get; set; } = default!;
    public string OriginalContainer { get; set; } = "photos-original";
    public string ThumbnailContainer { get; set; } = "photos-thumbnails";
}
