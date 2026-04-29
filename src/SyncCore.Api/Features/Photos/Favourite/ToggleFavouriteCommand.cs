using MediatR;
using Microsoft.EntityFrameworkCore;
using SyncCore.Api.Infrastructure.Data;

namespace SyncCore.Api.Features.Photos.Favourite;

public record ToggleFavouriteCommand(string PublicId, string UserId) : IRequest<ToggleFavouriteResult>;
public record ToggleFavouriteResult(bool IsFavourite);

public class ToggleFavouriteHandler(IPhotoDbContext db) : IRequestHandler<ToggleFavouriteCommand, ToggleFavouriteResult>
{
    public async Task<ToggleFavouriteResult> Handle(ToggleFavouriteCommand request, CancellationToken cancellationToken)
    {
        var photo = await db.Photos.FirstOrDefaultAsync(
            p => p.PublicId == request.PublicId && p.UserId == request.UserId && !p.IsDeleted,
            cancellationToken);

        if (photo is null) throw new KeyNotFoundException($"Photo {request.PublicId} not found.");

        photo.IsFavourite = !photo.IsFavourite;
        await db.SaveChangesAsync(cancellationToken);
        return new ToggleFavouriteResult(photo.IsFavourite);
    }
}
