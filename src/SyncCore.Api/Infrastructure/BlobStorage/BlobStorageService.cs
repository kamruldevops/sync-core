using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace SyncCore.Api.Infrastructure.BlobStorage;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _serviceClient;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureBlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureBlobStorage:ConnectionString is not configured.");
        _serviceClient = new BlobServiceClient(connectionString);
    }

    public async Task UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken ct)
    {
        var client = _serviceClient.GetBlobContainerClient(containerName).GetBlobClient(blobPath);
        await client.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);
    }

    public async Task<Stream> OpenReadAsync(string containerName, string blobPath, CancellationToken ct)
    {
        var client = _serviceClient.GetBlobContainerClient(containerName).GetBlobClient(blobPath);
        return await client.OpenReadAsync(cancellationToken: ct);
    }

    public async Task DeleteAsync(string containerName, string blobPath, CancellationToken ct)
    {
        var client = _serviceClient.GetBlobContainerClient(containerName).GetBlobClient(blobPath);
        await client.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public Uri GenerateSasUri(string containerName, string blobPath, TimeSpan validity)
    {
        var client = _serviceClient.GetBlobContainerClient(containerName).GetBlobClient(blobPath);
        return client.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(validity));
    }
}
