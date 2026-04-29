using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using SyncCore.Api.Common.Extensions;
using SyncCore.Api.Common.Options;
using SyncCore.Api.Features.Albums;
using SyncCore.Api.Features.Photos.Delete;
using SyncCore.Api.Features.Photos.Favourite;
using SyncCore.Api.Features.Photos.GetById;
using SyncCore.Api.Features.Photos.List;
using SyncCore.Api.Features.Photos.Upload;
using SyncCore.Api.Infrastructure.BackgroundJobs;
using SyncCore.Api.Infrastructure.BlobStorage;
using SyncCore.Api.Infrastructure.Data;
using SyncCore.Api.Infrastructure.Thumbnails;

var builder = WebApplication.CreateBuilder(args);

// --- Options ---
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(BlobStorageOptions.SectionName));

// --- Database ---
builder.Services.AddDbContext<PhotoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddScoped<IPhotoDbContext>(sp => sp.GetRequiredService<PhotoDbContext>());

// --- Infrastructure services ---
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IThumbnailService, ThumbnailService>();

// --- MediatR ---
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// --- FluentValidation ---
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

// --- Authentication: disabled until B2C is configured ---
// Use a permissive anonymous scheme so .RequireAuthorization() on endpoints
// doesn't throw when B2C is not yet wired up.
builder.Services.AddAuthentication("NoAuth")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
               SyncCore.Api.Infrastructure.Auth.NoAuthHandler>("NoAuth", _ => { });
builder.Services.AddAuthorization();

// --- CORS: allow all localhost origins (dev) ---
builder.Services.AddCors(options =>
    options.AddPolicy("ViteDev", policy =>
        policy.SetIsOriginAllowed(origin =>
                   new Uri(origin).Host == "localhost")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Background services ---
builder.Services.AddHostedService<TrashPurgeService>();

// --- Problem details (RFC 7807) ---
builder.Services.AddProblemDetails();

var app = builder.Build();

// --- Middleware pipeline ---
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ViteDev");
app.UseAuthentication();
app.UseAuthorization();

// --- Endpoints ---
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
   .AllowAnonymous()
   .WithTags("Health");

app.MapUploadPhotoEndpoint();
app.MapGetPhotosEndpoint();
app.MapGetPhotoByIdEndpoint();
app.MapFavouriteEndpoint();
app.MapDeletePhotoEndpoints();
app.MapAlbumEndpoints();

// --- Auto-migrate on startup in development ---
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PhotoDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
