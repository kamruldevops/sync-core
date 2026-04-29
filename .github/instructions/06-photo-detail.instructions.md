---
applyTo: "**"
---

# Feature 06 — Photo Detail View

## Goal
Display a single photo in a full-screen lightbox with its preview image, EXIF metadata, and action buttons (favourite, add to album, delete). Users can navigate to the previous/next photo with keyboard arrows or on-screen buttons.

---

## Backend

### Feature slice — `Features/Photos/GetById/`

**`GetPhotoByIdQuery.cs`**
```csharp
public record GetPhotoByIdQuery(string PublicId, string UserId) : IRequest<PhotoDetailDto?>;

public record PhotoDetailDto(
    string PublicId,
    string FileName,
    DateTimeOffset TakenAt,
    DateTimeOffset UploadedAt,
    long FileSizeBytes,
    string ContentType,
    string PreviewUrl,    // 15-min SAS URL for preview_1200
    string OriginalUrl,   // 15-min SAS URL for original (download)
    bool IsFavourite,     // wired in feature 08
    IReadOnlyList<string> AlbumIds   // wired in feature 07
);
```

**`GetPhotoByIdHandler.cs`**
1. Fetch `Photo` by `PublicId` and `UserId` (ensures ownership)
2. Return `null` if not found or deleted — endpoint returns 404
3. Generate SAS URLs for `PreviewPath` (15 min) and `BlobPath` original (5 min, download disposition)

**`GetPhotoByIdEndpoint.cs`**
```csharp
app.MapGet("/photos/{publicId}", async (
    string publicId,
    IMediator mediator,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    var userId = CurrentUser.GetUserId(user);
    var result = await mediator.Send(new GetPhotoByIdQuery(publicId, userId), ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
})
.RequireAuthorization()
.WithTags("Photos");
```

---

## Frontend

### Feature folder — `src/features/photo-detail/`

**`api.ts`**
```ts
export function usePhoto(publicId: string) {
  return useQuery({
    queryKey: ["photos", publicId],
    queryFn: () =>
      apiClient.get<PhotoDetailDto>(`/photos/${publicId}`).then((r) => r.data),
    enabled: !!publicId,
    staleTime: 10 * 60 * 1000,  // 10 min — slightly less than SAS validity
  });
}
```

**`PhotoDetailPage.tsx`**
Layout:
```
┌─────────────────────────────────────────────┐
│  ← Back          [♡ Fav] [+ Album] [🗑 Del] │  ← top bar
│                                             │
│         [ ← ]  <preview image>  [ → ]      │  ← centred preview + nav arrows
│                                             │
│  ┌── Info panel ────────────────────────┐   │
│  │  File name, date taken, size, type  │   │
│  └─────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```
- Preview image: `<img src={photo.previewUrl} className="max-h-[80vh] object-contain" />`
- Back button navigates to `/` (gallery)
- Keyboard listener: `ArrowLeft` / `ArrowRight` navigate to adjacent photos in the cached pages from `usePhotos`
- "Download original" link: `<a href={photo.originalUrl} download={photo.fileName}>Download</a>`

**`PhotoMetaPanel.tsx`**
Display in a small panel below or beside the image:
- File name
- Date taken (formatted: "29 April 2026 at 14:32")
- File size (formatted: "3.2 MB")
- Content type
- Dimensions (optional — store `width`/`height` in the DB when uploading)

### Route
```tsx
{ path: "/photo/:publicId", element: <RequireAuth><PhotoDetailPage /></RequireAuth> }
```

### Navigation helper
In `GalleryPage.tsx`, pass an `onClick` to each `PhotoThumbnail` that navigates to `/photo/{publicId}`.

---

## Acceptance Criteria
- [ ] `GET /photos/{publicId}` returns 200 with detail DTO including SAS URLs
- [ ] `GET /photos/{publicId}` for another user's photo returns 404
- [ ] Clicking a thumbnail in the gallery opens the detail page
- [ ] Preview image loads from the SAS URL without CORS errors
- [ ] Metadata panel shows correct file name, date, and size
- [ ] Left/right arrow keys navigate between photos
- [ ] "Download" link triggers a browser file download of the original
- [ ] Back button returns to the gallery without a full page reload
