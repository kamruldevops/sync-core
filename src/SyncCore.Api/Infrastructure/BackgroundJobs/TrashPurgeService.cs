using MediatR;
using Microsoft.EntityFrameworkCore;
using SyncCore.Api.Features.Photos.Delete;
using SyncCore.Api.Infrastructure.Data;

namespace SyncCore.Api.Infrastructure.BackgroundJobs;

public class TrashPurgeService(IServiceScopeFactory scopeFactory, ILogger<TrashPurgeService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PurgeExpiredTrashAsync(stoppingToken);
        }
    }

    private async Task PurgeExpiredTrashAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IPhotoDbContext>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
            var expired = await db.Photos
                .Where(p => p.IsDeleted && p.DeletedAt < cutoff)
                .Select(p => new { p.PublicId, p.UserId })
                .ToListAsync(ct);

            foreach (var photo in expired)
            {
                try
                {
                    await mediator.Send(new PermanentDeletePhotoCommand(photo.PublicId, photo.UserId), ct);
                    logger.LogInformation("Purged expired photo {PublicId}", photo.PublicId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to purge photo {PublicId}", photo.PublicId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trash purge job failed");
        }
    }
}
