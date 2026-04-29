namespace SyncCore.Api.Infrastructure.BlobStorage;

public interface IBlobStorageService
{
    Task UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken ct);
    Task<Stream> OpenReadAsync(string containerName, string blobPath, CancellationToken ct);
    Task DeleteAsync(string containerName, string blobPath, CancellationToken ct);
    Uri GenerateSasUri(string containerName, string blobPath, TimeSpan validity);
}
