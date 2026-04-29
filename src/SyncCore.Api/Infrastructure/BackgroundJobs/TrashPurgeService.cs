using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using MediatR;
using Microsoft.Extensions.Options;
using SyncCore.Api.Common.Options;
using SyncCore.Api.Features.Photos.Delete;
using SyncCore.Api.Infrastructure.Data;
using SyncCore.Api.Infrastructure.Queue;
using Microsoft.EntityFrameworkCore;

namespace SyncCore.Api.Infrastructure.BackgroundJobs;

/// <summary>
/// Polls the Azure Storage Queue for photos due for permanent deletion.
/// Azure Storage Queue max visibility timeout = 7 days, so messages are re-hidden
/// in 7-day increments until the 30-day retention period expires.
/// </summary>
public class TrashPurgeService(
    IServiceScopeFactory scopeFactory,
    IOptions<BlobStorageOptions> opts,
    ILogger<TrashPurgeService> logger) : BackgroundService
{
    // Azure Storage Queue hard limit on visibility timeout
    private static readonly TimeSpan MaxVisibility = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueClient = new QueueClient(opts.Value.ConnectionString, opts.Value.TrashQueueName);
        await queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        logger.LogInformation("TrashPurgeService started, polling queue '{Queue}'", opts.Value.TrashQueueName);

        // Poll every 2 minutes
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessMessagesAsync(queueClient, stoppingToken);
        }
    }

    private async Task ProcessMessagesAsync(QueueClient queueClient, CancellationToken ct)
    {
        try
        {
            // Dequeue up to 32 messages per cycle (Azure max per call)
            QueueMessage[] messages = await queueClient.ReceiveMessagesAsync(maxMessages: 32, cancellationToken: ct);
            if (messages.Length == 0) return;

            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<IPhotoDbContext>();

            foreach (var msg in messages)
            {
                await ProcessMessageAsync(queueClient, mediator, db, msg, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing trash queue messages");
        }
    }

    private async Task ProcessMessageAsync(
        QueueClient queueClient,
        IMediator mediator,
        IPhotoDbContext db,
        QueueMessage msg,
        CancellationToken ct)
    {
        TrashQueueMessage? message;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(msg.Body.ToString()));
            message = JsonSerializer.Deserialize<TrashQueueMessage>(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize trash queue message {MessageId} — deleting poison message", msg.MessageId);
            await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, ct);
            return;
        }

        if (message is null)
        {
            await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, ct);
            return;
        }

        var now = DateTimeOffset.UtcNow;

        if (now < message.DeletionDueAt)
        {
            // Not due yet — re-hide for min(remaining, 7 days) so it re-appears when due
            var remaining = message.DeletionDueAt - now;
            var rehide = remaining < MaxVisibility ? remaining : MaxVisibility;

            await queueClient.UpdateMessageAsync(msg.MessageId, msg.PopReceipt,
                visibilityTimeout: rehide, cancellationToken: ct);

            logger.LogDebug("Photo {PublicId} not due yet, re-hidden for {Hours:F1}h", message.PublicId, rehide.TotalHours);
            return;
        }

        // 30 days have passed — check if photo was restored (IsDeleted = false)
        var stillDeleted = await db.Photos
            .AnyAsync(p => p.PublicId == message.PublicId && p.IsDeleted, ct);

        if (!stillDeleted)
        {
            // Photo was restored by the user — just remove the queue message
            logger.LogInformation("Photo {PublicId} was restored, skipping permanent deletion", message.PublicId);
            await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, ct);
            return;
        }

        // Permanently delete blobs + DB row
        try
        {
            await mediator.Send(new PermanentDeletePhotoCommand(message.PublicId, message.UserId), ct);
            logger.LogInformation("Permanently deleted photo {PublicId} after 30-day retention", message.PublicId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to permanently delete photo {PublicId}", message.PublicId);
            // Leave message in queue — it will re-appear after default visibility expires and retry
            return;
        }

        await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, ct);
    }
}

