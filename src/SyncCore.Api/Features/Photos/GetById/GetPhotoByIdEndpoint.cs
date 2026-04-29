using MediatR;
using SyncCore.Api.Infrastructure.Auth;

namespace SyncCore.Api.Features.Photos.GetById;

public static class GetPhotoByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetPhotoByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/photos/{publicId}", async (
            string publicId,
            IMediator mediator,
            System.Security.Claims.ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = CurrentUser.GetUserId(user);
            var result = await mediator.Send(new GetPhotoByIdQuery(publicId, userId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Photos");

        return app;
    }
}
