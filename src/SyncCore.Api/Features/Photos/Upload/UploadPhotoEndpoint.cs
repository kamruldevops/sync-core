using MediatR;
using SyncCore.Api.Infrastructure.Auth;

namespace SyncCore.Api.Features.Photos.Upload;

public static class UploadPhotoEndpoint
{
    public static IEndpointRouteBuilder MapUploadPhotoEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/photos", async (
            IFormFile file,
            IMediator mediator,
            System.Security.Claims.ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = CurrentUser.GetUserId(user);
            var result = await mediator.Send(new UploadPhotoCommand(file, userId), ct);
            return Results.Created($"/photos/{result.PublicId}", result);
        })
        .RequireAuthorization()
        .DisableAntiforgery()
        .WithTags("Photos");

        return app;
    }
}
