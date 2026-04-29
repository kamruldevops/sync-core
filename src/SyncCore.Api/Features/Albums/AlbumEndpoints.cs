using MediatR;
using SyncCore.Api.Features.Photos.List;
using SyncCore.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace SyncCore.Api.Features.Albums;

public static class AlbumEndpoints
{
    public static IEndpointRouteBuilder MapAlbumEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/albums", async (
            CreateAlbumRequest body, IMediator mediator,
            System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = CurrentUser.GetUserId(user);
            var result = await mediator.Send(new CreateAlbumCommand(body.Name, userId), ct);
            return Results.Created($"/albums/{result.PublicId}", result);
        })
        .RequireAuthorization()
        .WithTags("Albums");

        app.MapGet("/albums", async (
            IMediator mediator, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = CurrentUser.GetUserId(user);
            var result = await mediator.Send(new ListAlbumsQuery(userId), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Albums");

        app.MapGet("/albums/{albumId}/photos", async (
            string albumId, string? cursor, int pageSize = 50,
            IMediator mediator = default!, System.Security.Claims.ClaimsPrincipal user = default!, CancellationToken ct = default) =>
        {
            var userId = CurrentUser.GetUserId(user);
            var result = await mediator.Send(new GetAlbumPhotosQuery(albumId, userId, cursor, pageSize), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Albums");

        app.MapPost("/albums/{albumId}/photos", async (
            string albumId, AddPhotoToAlbumRequest body, IMediator mediator,
            System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = CurrentUser.GetUserId(user);
            await mediator.Send(new AddPhotoToAlbumCommand(albumId, body.PhotoPublicId, userId), ct);
            return Results.Ok();
        })
        .RequireAuthorization()
        .WithTags("Albums");

        app.MapDelete("/albums/{albumId}/photos/{photoPublicId}", async (
            string albumId, string photoPublicId, IMediator mediator,
            System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = CurrentUser.GetUserId(user);
            await mediator.Send(new RemovePhotoFromAlbumCommand(albumId, photoPublicId, userId), ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Albums");

        app.MapDelete("/albums/{albumId}", async (
            string albumId, IMediator mediator,
            System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = CurrentUser.GetUserId(user);
            await mediator.Send(new DeleteAlbumCommand(albumId, userId), ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Albums");

        return app;
    }
}

public record CreateAlbumRequest(string Name);
public record AddPhotoToAlbumRequest(string PhotoPublicId);
