---
applyTo: "**"
---

# Feature 05 — Photo Gallery View

## Goal
Display the authenticated user's photos in a responsive masonry-style grid sorted by `TakenAt` descending. Photos are grouped by date (e.g. "April 2026"). Thumbnails are loaded lazily. Supports cursor-based pagination so the page does not need to load all photos at once.

---

## Backend

### Feature slice — `Features/Photos/List/`

**`GetPhotosQuery.cs`**
```csharp
public record GetPhotosQuery(
    string UserId,
    string? Cursor,   // opaque cursor — encodes last seen TakenAt + PublicId
    int PageSize = 50
) : IRequest<GetPhotosResult>;

public record GetPhotosResult(IReadOnlyList<PhotoDto> Photos, string? NextCursor);
```

**`GetPhotosHandler.cs`**
1. Query `photos` table for current user, `IsDeleted = false`
2. Decode cursor (base64 of `{takenAt}|{publicId}`) to filter: `TakenAt < cursorTakenAt OR (TakenAt = cursorTakenAt AND PublicId < cursorPublicId)`
3. Order by `TakenAt DESC`, then `PublicId DESC`
4. Take `PageSize + 1`; if count > PageSize, set `NextCursor` from the last item, return only PageSize items
5. Map to `PhotoDto` — generate 15-min SAS URLs via `IBlobStorageService.GenerateSasUri`

**`GetPhotosEndpoint.cs`**
```csharp
app.MapGet("/photos", async (
    [FromQuery] string? cursor,
    [FromQuery] int pageSize,
    IMediator mediator,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    var userId = CurrentUser.GetUserId(user);
    var result = await mediator.Send(new GetPhotosQuery(userId, cursor, pageSize), ct);
    return Results.Ok(result);
})
.RequireAuthorization()
.WithTags("Photos");
```

---

## Frontend

### Feature folder — `src/features/gallery/`

**`types.ts`**
```ts
export interface Photo {
  publicId: string;
  fileName: string;
  takenAt: string;   // ISO 8601
  thumbUrl: string;  // 15-min SAS URL
  previewUrl: string;
}

export interface PhotoPage {
  photos: Photo[];
  nextCursor: string | null;
}
```

**`api.ts`** — infinite query
```ts
export function usePhotos() {
  return useInfiniteQuery<PhotoPage>({
    queryKey: ["photos"],
    queryFn: ({ pageParam }) =>
      apiClient
        .get<PhotoPage>("/photos", { params: { cursor: pageParam, pageSize: 50 } })
        .then((r) => r.data),
    getNextPageParam: (last) => last.nextCursor ?? undefined,
    initialPageParam: undefined,
  });
}
```

**`GalleryPage.tsx`** — main page component
- Call `usePhotos()` — flatten `pages[].photos` into a single array
- Group photos by date label: `format(new Date(photo.takenAt), "MMMM yyyy")` (use `date-fns`)
- Render each group as a `<section>` with a sticky date heading
- Inside each group render a CSS grid: `grid grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-1`
- At the bottom of the list render a sentinel `<div ref={sentinelRef} />` — use `IntersectionObserver` to call `fetchNextPage()` when it enters the viewport
- Show a spinner while `isFetchingNextPage`

**`PhotoThumbnail.tsx`** — single grid cell
```tsx
// Props: photo: Photo, onClick: () => void
<div className="aspect-square overflow-hidden cursor-pointer" onClick={onClick}>
  <img
    src={photo.thumbUrl}
    alt={photo.fileName}
    loading="lazy"
    className="w-full h-full object-cover hover:opacity-90 transition-opacity"
  />
</div>
```

**`SelectionBar.tsx`** (optional for MVP)
- Appears at the top when one or more photos are selected (checkbox on hover)
- Exposes "Add to album", "Favourite", "Delete" actions (wired in later features)

### Route
```tsx
{ path: "/", element: <RequireAuth><GalleryPage /></RequireAuth> }
```

### npm additions
```bash
npm install date-fns
```

---

## Acceptance Criteria
- [ ] `GET /photos` returns paginated results with SAS thumbnail URLs
- [ ] Gallery renders a grid of thumbnails grouped by month/year
- [ ] Scrolling to the bottom of the page loads the next page automatically
- [ ] Thumbnails load lazily (browser only requests visible images)
- [ ] Empty state shows a friendly "No photos yet — upload some!" message with upload button
- [ ] Clicking a thumbnail navigates to the photo detail view (feature 06)
- [ ] Layout is responsive across mobile (2 cols), tablet (3 cols), and desktop (5 cols)
