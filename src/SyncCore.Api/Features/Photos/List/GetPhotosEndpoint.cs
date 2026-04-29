using MediatR;
using SyncCore.Api.Infrastructure.Auth;

namespace SyncCore.Api.Features.Photos.List;

public static class GetPhotosEndpoint
{
    public static IEndpointRouteBuilder MapGetPhotosEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/photos", async (
            string? cursor,
            int pageSize = 50,
            string? q = null,
            DateOnly? dateFrom = null,
            DateOnly? dateTo = null,
            bool favourites = false,
            IMediator mediator = default!,
            System.Security.Claims.ClaimsPrincipal user = default!,
            CancellationToken ct = default) =>
        {
            var userId = CurrentUser.GetUserId(user);
            var result = await mediator.Send(
                new GetPhotosQuery(userId, cursor, pageSize, q, dateFrom, dateTo, favourites), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Photos");

        app.MapGet("/photos/trash", async (
            string? cursor,
            int pageSize = 50,
            IMediator mediator = default!,
            System.Security.Claims.ClaimsPrincipal user = default!,
            CancellationToken ct = default) =>
        {
            var userId = CurrentUser.GetUserId(user);
            var result = await mediator.Send(
                new GetPhotosQuery(userId, cursor, pageSize, IncludeTrashed: true), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Photos");

        return app;
    }
}
