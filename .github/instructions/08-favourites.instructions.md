---
applyTo: "**"
---

# Feature 08 — Favourites

## Goal
Users can mark any photo as a favourite (star/heart). A dedicated "Favourites" view shows only starred photos. The favourite state is toggled with a single action and reflected immediately in the UI via optimistic updates.

---

## Data Model

Add a column to the existing `photos` table:

```csharp
// In Photo entity (Infrastructure/Data/Entities/Photo.cs)
public bool IsFavourite { get; set; }
```

### Migration
```bash
dotnet ef migrations add AddIsFavourite -p src/SyncCore.Api
dotnet ef database update -p src/SyncCore.Api
```

---

## Backend

### Feature slice — `Features/Photos/Favourite/`

**`ToggleFavouriteCommand.cs`**
```csharp
public record ToggleFavouriteCommand(string PublicId, string UserId) : IRequest<ToggleFavouriteResult>;
public record ToggleFavouriteResult(bool IsFavourite);
```

**`ToggleFavouriteHandler.cs`**
1. Fetch `Photo` by `PublicId` + `UserId` (return 404 if not found or deleted)
2. Flip `photo.IsFavourite`
3. `SaveChangesAsync`
4. Return new `IsFavourite` value

**`ToggleFavouriteEndpoint.cs`**
```csharp
app.MapPost("/photos/{publicId}/favourite", async (
    string publicId,
    IMediator mediator,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    var userId = CurrentUser.GetUserId(user);
    var result = await mediator.Send(new ToggleFavouriteCommand(publicId, userId), ct);
    return Results.Ok(result);
})
.RequireAuthorization()
.WithTags("Photos");
```

### Favourites list — `GET /photos?favourites=true`

Extend `GetPhotosQuery` with an optional `bool FavouritesOnly` filter parameter. When true, add `.Where(p => p.IsFavourite)` to the query. The endpoint receives this via `[FromQuery] bool favourites = false`.

---

## Frontend

### `api.ts` addition in `src/features/gallery/api.ts`
```ts
export function useToggleFavourite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (publicId: string) =>
      apiClient.post<{ isFavourite: boolean }>(`/photos/${publicId}/favourite`).then((r) => r.data),
    // Optimistic update
    onMutate: async (publicId) => {
      await queryClient.cancelQueries({ queryKey: ["photos"] });
      const previous = queryClient.getQueryData(["photos"]);
      queryClient.setQueryData<InfiniteData<PhotoPage>>(["photos"], (old) => {
        if (!old) return old;
        return {
          ...old,
          pages: old.pages.map((page) => ({
            ...page,
            photos: page.photos.map((p) =>
              p.publicId === publicId ? { ...p, isFavourite: !p.isFavourite } : p
            ),
          })),
        };
      });
      return { previous };
    },
    onError: (_err, _id, context) => {
      queryClient.setQueryData(["photos"], context?.previous);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: ["photos"] }),
  });
}

export function useFavouritePhotos() {
  return useInfiniteQuery<PhotoPage>({
    queryKey: ["photos", "favourites"],
    queryFn: ({ pageParam }) =>
      apiClient
        .get<PhotoPage>("/photos", { params: { favourites: true, cursor: pageParam, pageSize: 50 } })
        .then((r) => r.data),
    getNextPageParam: (last) => last.nextCursor ?? undefined,
    initialPageParam: undefined,
  });
}
```

### Add `isFavourite` to `Photo` type in `types.ts`
```ts
export interface Photo {
  // ...existing fields
  isFavourite: boolean;
}
```

### `FavouriteButton.tsx` — `src/features/gallery/`
```tsx
// Props: publicId: string, isFavourite: boolean
const { mutate } = useToggleFavourite();
<button
  onClick={(e) => { e.stopPropagation(); mutate(publicId); }}
  className="..."
  aria-label={isFavourite ? "Remove from favourites" : "Add to favourites"}
>
  {isFavourite ? "♥" : "♡"}
</button>
```

Render this button:
- As an overlay on thumbnail hover in the gallery grid
- In the action bar of the photo detail page

### `FavouritesPage.tsx`
- Same layout as `GalleryPage` but uses `useFavouritePhotos`
- Empty state: "No favourites yet — tap the heart on a photo"

### Routes
```tsx
{ path: "/favourites", element: <RequireAuth><FavouritesPage /></RequireAuth> }
```

### Sidebar / nav link
Add "Favourites ♥" link to the app navigation sidebar.

---

## Acceptance Criteria
- [ ] `POST /photos/{publicId}/favourite` toggles `IsFavourite` and returns the new state
- [ ] Calling the endpoint twice returns the original state (toggle is idempotent per call pair)
- [ ] `GET /photos?favourites=true` returns only favourited photos for the current user
- [ ] Heart icon on thumbnail reflects current favourite state without a page reload
- [ ] Optimistic update makes the toggle feel instant; rolls back on API error
- [ ] Favourites page shows only starred photos with the same pagination as the gallery
