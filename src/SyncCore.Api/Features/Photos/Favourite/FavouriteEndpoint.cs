using MediatR;
using SyncCore.Api.Infrastructure.Auth;

namespace SyncCore.Api.Features.Photos.Favourite;

public static class FavouriteEndpoint
{
    public static IEndpointRouteBuilder MapFavouriteEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/photos/{publicId}/favourite", async (
            string publicId,
            IMediator mediator,
            System.Security.Claims.ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = CurrentUser.GetUserId(user);
            var result = await mediator.Send(new ToggleFavouriteCommand(publicId, userId), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Photos");

        return app;
    }
}
