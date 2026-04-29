---
applyTo: "**"
---

# Feature 07 — Albums

## Goal
Users can create named albums, add photos to them, remove photos from them, and delete albums. An album view shows only the photos in that album. Albums are user-scoped (no sharing in MVP).

---

## Data Model

### `Album` entity — `Infrastructure/Data/Entities/Album.cs`
```csharp
public class Album
{
    public int Id { get; set; }
    public string PublicId { get; set; } = default!;  // ULID
    public string UserId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<AlbumPhoto> AlbumPhotos { get; set; } = [];
}
```

### `AlbumPhoto` join entity — `Infrastructure/Data/Entities/AlbumPhoto.cs`
```csharp
public class AlbumPhoto
{
    public int AlbumId { get; set; }
    public Album Album { get; set; } = default!;
    public int PhotoId { get; set; }
    public Photo Photo { get; set; } = default!;
    public DateTimeOffset AddedAt { get; set; }
}
```

EF composite PK: `(AlbumId, PhotoId)`

### Migration
```bash
dotnet ef migrations add AddAlbums -p src/SyncCore.Api
dotnet ef database update -p src/SyncCore.Api
```

---

## Backend — Feature slices under `Features/Albums/`

### 1. Create album — `POST /albums`
- Command: `CreateAlbumCommand(string Name, string UserId)`
- Validator: `Name` required, max 100 chars
- Returns: `AlbumDto(PublicId, Name, PhotoCount, CoverThumbUrl)`

### 2. List albums — `GET /albums`
- Query: `ListAlbumsQuery(string UserId)`
- Returns: list of `AlbumDto` ordered by `CreatedAt DESC`
- `CoverThumbUrl`: SAS URL of the thumb of the most recently added photo in the album (or null)

### 3. Get album photos — `GET /albums/{albumId}/photos`
- Same pagination pattern as feature 05 (`cursor`, `pageSize`)
- Only returns photos belonging to this album AND this user, not deleted

### 4. Add photo to album — `POST /albums/{albumId}/photos`
- Body: `{ "photoPublicId": "..." }`
- Validates both album and photo belong to the current user
- Idempotent — no error if already added

### 5. Remove photo from album — `DELETE /albums/{albumId}/photos/{photoPublicId}`
- Validates ownership
- Deletes the `AlbumPhoto` join row only (does not delete the photo)

### 6. Delete album — `DELETE /albums/{albumId}`
- Validates ownership
- Deletes `AlbumPhoto` rows then the `Album` row (or use cascade delete)
- Does not delete photos themselves

---

## Frontend

### Feature folder — `src/features/albums/`

**`types.ts`**
```ts
export interface Album {
  publicId: string;
  name: string;
  photoCount: number;
  coverThumbUrl: string | null;
}
```

**`api.ts`**
```ts
export function useAlbums() { /* useQuery GET /albums */ }
export function useCreateAlbum() { /* useMutation POST /albums */ }
export function useDeleteAlbum() { /* useMutation DELETE /albums/:id */ }
export function useAlbumPhotos(albumId: string) { /* useInfiniteQuery GET /albums/:id/photos */ }
export function useAddPhotoToAlbum() { /* useMutation POST /albums/:id/photos */ }
export function useRemovePhotoFromAlbum() { /* useMutation DELETE /albums/:id/photos/:photoId */ }
```

**`AlbumsPage.tsx`**
- Grid of album cards (cover thumbnail, name, photo count)
- "New album" button opens an inline input or modal to enter a name
- Clicking an album navigates to `/albums/{publicId}`

**`AlbumDetailPage.tsx`**
- Same responsive grid as `GalleryPage` but sourced from `useAlbumPhotos`
- Album name editable inline (rename — optional MVP stretch)
- "Delete album" button with confirmation

**`AddToAlbumModal.tsx`**
- Rendered from the photo detail page action bar
- Lists user's albums; tap one to add the photo
- "New album" option creates and immediately adds

### Routes
```tsx
{ path: "/albums", element: <RequireAuth><AlbumsPage /></RequireAuth> }
{ path: "/albums/:albumId", element: <RequireAuth><AlbumDetailPage /></RequireAuth> }
```

---

## Acceptance Criteria
- [ ] `POST /albums` creates an album and returns the new `AlbumDto`
- [ ] `GET /albums` returns only the current user's albums
- [ ] `POST /albums/{albumId}/photos` adds a photo and is idempotent
- [ ] `GET /albums/{albumId}/photos` returns only photos in the album, paginated
- [ ] Deleting an album does not delete the photos
- [ ] Cover thumbnail updates to reflect the most recent photo in the album
- [ ] Album detail page renders correctly with the same pagination as the gallery
- [ ] "Add to album" from the photo detail view works end-to-end
