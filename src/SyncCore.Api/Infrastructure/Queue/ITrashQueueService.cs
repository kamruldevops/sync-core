namespace SyncCore.Api.Infrastructure.Queue;

public interface ITrashQueueService
{
    /// <summary>
    /// Enqueues a deferred permanent-delete message.
    /// The message becomes processable after 30 days (via re-hide cycles since Azure Queue max visibility = 7 days).
    /// </summary>
    Task EnqueueAsync(string publicId, string userId, CancellationToken ct);
}
