using MediatR;
using SyncCore.Api.Infrastructure.Auth;

namespace SyncCore.Api.Features.Photos.Delete;

public static class DeletePhotoEndpoints
{
    public static IEndpointRouteBuilder MapDeletePhotoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/photos/{publicId}", async (
            string publicId, IMediator mediator,
            System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = CurrentUser.GetUserId(user);
            await mediator.Send(new DeletePhotoCommand(publicId, userId), ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Photos");

        app.MapPost("/photos/{publicId}/restore", async (
            string publicId, IMediator mediator,
            System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = CurrentUser.GetUserId(user);
            await mediator.Send(new RestorePhotoCommand(publicId, userId), ct);
            return Results.Ok();
        })
        .RequireAuthorization()
        .WithTags("Photos");

        app.MapDelete("/photos/{publicId}/permanent", async (
            string publicId, IMediator mediator,
            System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = CurrentUser.GetUserId(user);
            await mediator.Send(new PermanentDeletePhotoCommand(publicId, userId), ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Photos");

        return app;
    }
}
