using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using SyncCore.Api.Common.Options;

namespace SyncCore.Api.Infrastructure.Queue;

public class TrashQueueService(IOptions<BlobStorageOptions> opts, ILogger<TrashQueueService> logger)
    : ITrashQueueService
{
    // Azure Storage Queue max visibility timeout is 7 days (604800 seconds)
    private static readonly TimeSpan MaxVisibility = TimeSpan.FromDays(7);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    private QueueClient? _client;

    private async Task<QueueClient> GetClientAsync(CancellationToken ct)
    {
        if (_client is not null) return _client;

        var client = new QueueClient(opts.Value.ConnectionString, opts.Value.TrashQueueName);
        await client.CreateIfNotExistsAsync(cancellationToken: ct);
        _client = client;
        return _client;
    }

    public async Task EnqueueAsync(string publicId, string userId, CancellationToken ct)
    {
        var deletionDueAt = DateTimeOffset.UtcNow.Add(RetentionPeriod);

        var message = new TrashQueueMessage(publicId, userId, deletionDueAt);
        var json = JsonSerializer.Serialize(message);
        // Base64-encode as Azure Storage Queue requires base64 for binary-safe transport
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        // Initial visibility = min(30 days, 7 days) = 7 days (Azure max per send)
        var initialVisibility = TimeSpan.FromDays(Math.Min(RetentionPeriod.TotalDays, MaxVisibility.TotalDays));

        // TTL must be strictly greater than visibilityTimeout.
        // Azure SDK 12.x (API 2020-10-02+) supports TTL beyond 7 days — use 35 days.
        var timeToLive = TimeSpan.FromDays(35);

        var client = await GetClientAsync(ct);
        await client.SendMessageAsync(encoded, visibilityTimeout: initialVisibility, timeToLive: timeToLive, cancellationToken: ct);

        logger.LogInformation("Enqueued trash deletion for photo {PublicId}, due at {DeletionDueAt}", publicId, deletionDueAt);
    }
}

public record TrashQueueMessage(string PublicId, string UserId, DateTimeOffset DeletionDueAt);
