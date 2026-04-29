---
applyTo: "**"
---

# Feature 10 — Trash / Delete

## Goal
Implement a two-step deletion flow modelled on Google Photos: deleting a photo moves it to Trash (soft delete), where it is kept for 30 days before being permanently deleted. Users can restore photos from Trash or permanently delete them early. Blob files are only removed from Azure Blob Storage on permanent deletion.

---

## Data Model

The `Photo` entity already has `IsDeleted` (added in feature 03). Extend it:

```csharp
// In Photo entity
public bool IsDeleted { get; set; }
public DateTimeOffset? DeletedAt { get; set; }   // set on soft delete
```

### Migration
```bash
dotnet ef migrations add AddDeletedAt -p src/SyncCore.Api
dotnet ef database update -p src/SyncCore.Api
```

Also add a partial index in the EF config:
```csharp
builder.HasIndex(p => new { p.UserId, p.DeletedAt })
    .HasFilter("\"IsDeleted\" = true");
```

---

## Backend — Feature slices under `Features/Photos/`

### 1. Soft delete — `DELETE /photos/{publicId}`
**`DeletePhotoCommand.cs`**
```csharp
public record DeletePhotoCommand(string PublicId, string UserId) : IRequest<Unit>;
```

**`DeletePhotoHandler.cs`**
1. Fetch by `PublicId` + `UserId`, `IsDeleted = false`
2. Set `photo.IsDeleted = true`, `photo.DeletedAt = DateTimeOffset.UtcNow`
3. `SaveChangesAsync`
4. Do NOT delete blobs yet

**Endpoint**: `DELETE /photos/{publicId}` → returns HTTP 204

### 2. List trash — `GET /photos/trash`
**`GetTrashedPhotosQuery.cs`**
- Same pagination pattern as feature 05
- Filter: `IsDeleted = true AND UserId = current`
- Include `DeletedAt` in the response DTO so the UI can show "Deletes in X days"

### 3. Restore photo — `POST /photos/{publicId}/restore`
**`RestorePhotoCommand.cs`**
- Set `IsDeleted = false`, `DeletedAt = null`
- Returns the photo DTO

### 4. Permanently delete — `DELETE /photos/{publicId}/permanent`
**`PermanentDeletePhotoCommand.cs`**
1. Fetch by `PublicId` + `UserId`, `IsDeleted = true` (only allow permanently deleting trashed photos)
2. Delete blobs from both containers:
   - `blobStorage.DeleteAsync(opts.Value.OriginalContainer, photo.BlobPath, ct)`
   - if `ThumbnailPath` not null: `blobStorage.DeleteAsync(opts.Value.ThumbnailContainer, photo.ThumbnailPath, ct)`
   - if `PreviewPath` not null: `blobStorage.DeleteAsync(opts.Value.ThumbnailContainer, photo.PreviewPath, ct)`
3. Delete `AlbumPhoto` rows (or rely on cascade)
4. Delete the `Photo` row from DB

### `IBlobStorageService` addition
```csharp
Task DeleteAsync(string containerName, string blobPath, CancellationToken ct);
```

### Auto-purge background service — `Infrastructure/BackgroundJobs/TrashPurgeService.cs`
```csharp
// IHostedService or BackgroundService
// Runs once per day (use PeriodicTimer)
// Finds all photos where IsDeleted = true AND DeletedAt < UtcNow - 30 days
// Calls PermanentDeletePhotoCommand for each
```

Register in `Program.cs`:
```csharp
builder.Services.AddHostedService<TrashPurgeService>();
```

---

## Frontend

### `src/features/trash/`

**`api.ts`**
```ts
export function useTrashedPhotos() { /* useInfiniteQuery GET /photos/trash */ }
export function useDeletePhoto() {
  // useMutation DELETE /photos/:id
  // On success: remove photo from ["photos"] query cache optimistically
}
export function useRestorePhoto() { /* useMutation POST /photos/:id/restore */ }
export function usePermanentDeletePhoto() { /* useMutation DELETE /photos/:id/permanent */ }
```

**`TrashPage.tsx`**
- Same grid layout as gallery
- Each thumbnail shows a "Deletes in X days" overlay badge
- Action bar (on selection or hover): "Restore" and "Delete permanently" buttons
- "Empty trash" button — calls permanent delete on all trashed photos
- Empty state: "Your trash is empty"

**Delete confirmation — `DeleteConfirmDialog.tsx`**
- Simple modal: "Move to Trash? You can restore it within 30 days."
- Used from photo detail page and gallery selection bar

### Wiring in existing components

**`PhotoDetailPage.tsx`** (feature 06)
- "Delete" button in action bar → opens `DeleteConfirmDialog` → on confirm, calls `useDeletePhoto`
- On success: navigate back to gallery and show a toast "Photo moved to trash. Undo?" (Undo calls `useRestorePhoto`)

**`PhotoThumbnail.tsx`** (feature 05)
- On hover show a subtle trash icon if user is in selection mode

### Routes
```tsx
{ path: "/trash", element: <RequireAuth><TrashPage /></RequireAuth> }
```

### Nav link
Add "Trash 🗑" to the sidebar navigation.

---

## Acceptance Criteria
- [ ] `DELETE /photos/{publicId}` sets `IsDeleted=true` and `DeletedAt` but does not delete blobs
- [ ] Soft-deleted photos no longer appear in `GET /photos` or album/favourite queries
- [ ] `GET /photos/trash` returns only the current user's trashed photos
- [ ] `POST /photos/{publicId}/restore` clears `IsDeleted` and photo reappears in gallery
- [ ] `DELETE /photos/{publicId}/permanent` deletes both DB row and all three blobs
- [ ] Background service permanently deletes photos that have been in trash for > 30 days
- [ ] Delete from the UI shows a confirmation dialog before proceeding
- [ ] "Undo" toast after soft delete works within the same page session
- [ ] Empty trash action deletes all trashed photos permanently after confirmation
