---
applyTo: "**"
---

# Feature 03 — Photo Upload

## Goal
Allow authenticated users to upload one or more photos (JPEG, PNG, HEIC, WebP) directly to the API. The API streams the file to Azure Blob Storage (`photos-original` container) and persists photo metadata to PostgreSQL. Thumbnail generation is triggered asynchronously (handled in Feature 04).

---

## Data Model

### `Photo` entity — `Infrastructure/Data/Entities/Photo.cs`
```csharp
public class Photo
{
    public int Id { get; set; }                     // internal PK
    public string PublicId { get; set; } = default!; // ULID — exposed to clients
    public string UserId { get; set; } = default!;   // B2C object ID
    public string FileName { get; set; } = default!; // original file name
    public string ContentType { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public string BlobPath { get; set; } = default!; // {userId}/{publicId}/original{ext}
    public string? ThumbnailPath { get; set; }        // set after thumbnail generation
    public string? PreviewPath { get; set; }          // set after thumbnail generation
    public DateTimeOffset TakenAt { get; set; }       // from EXIF or upload time
    public DateTimeOffset UploadedAt { get; set; }
    public bool IsDeleted { get; set; }               // soft delete (feature 10)
}
```

### EF Core configuration
- Table name: `photos`
- `PublicId` has a unique index
- `UserId` has an index (most queries filter by user)
- `IsDeleted` has a filtered index (WHERE IsDeleted = false)

### Migration
```bash
dotnet ef migrations add AddPhotosTable -p src/SyncCore.Api
dotnet ef database update -p src/SyncCore.Api
```

---

## Backend

### Feature slice — `Features/Photos/Upload/`

**`UploadPhotoCommand.cs`**
```csharp
public record UploadPhotoCommand(
    IFormFile File,
    string UserId
) : IRequest<UploadPhotoResult>;

public record UploadPhotoResult(string PublicId, string FileName, DateTimeOffset TakenAt);
```

**`UploadPhotoValidator.cs`**
- File must not be null
- Content type must be one of: `image/jpeg`, `image/png`, `image/webp`, `image/heic`
- File size must be ≤ 50 MB

**`UploadPhotoHandler.cs`**
1. Generate `publicId = Ulid.NewUlid().ToString()`
2. Determine extension from content type
3. Build blob path: `{userId}/{publicId}/original{ext}`
4. Stream file to `photos-original` container via `IBlobStorageService.UploadAsync`
5. Extract `TakenAt` from EXIF (use `MetadataExtractor` NuGet package or fall back to `DateTimeOffset.UtcNow`)
6. Persist `Photo` entity to DB
7. (Optional) Enqueue background thumbnail job (feature 04 can hook in here)
8. Return `UploadPhotoResult`

**`UploadPhotoEndpoint.cs`**
```csharp
app.MapPost("/photos", async (
    IFormFile file,
    IMediator mediator,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    var userId = CurrentUser.GetUserId(user);
    var result = await mediator.Send(new UploadPhotoCommand(file, userId), ct);
    return Results.Created($"/photos/{result.PublicId}", result);
})
.RequireAuthorization()
.DisableAntiforgery()
.WithTags("Photos");
```

### `IBlobStorageService` additions
```csharp
Task UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken ct);
```

### NuGet additions
```
MetadataExtractor   # EXIF reading (open-source, MIT)
```

---

## Frontend

### Feature folder — `src/features/upload/`

**`types.ts`**
```ts
export interface UploadedPhoto {
  publicId: string;
  fileName: string;
  takenAt: string; // ISO 8601
}
```

**`api.ts`** — TanStack Query mutation
```ts
export function useUploadPhotos() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (files: File[]) => {
      const results: UploadedPhoto[] = [];
      for (const file of files) {
        const form = new FormData();
        form.append("file", file);
        const { data } = await apiClient.post<UploadedPhoto>("/photos", form);
        results.push(data);
      }
      return results;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["photos"] }),
  });
}
```

**`UploadButton.tsx`**
- Hidden `<input type="file" multiple accept="image/*" />`
- Visible styled button that triggers the input
- On change: call `useUploadPhotos` mutation
- Show a progress indicator per file (use mutation `isPending`)
- On success: show a toast / brief success message

**`DropZone.tsx`** (optional but recommended)
- Full-page drag-and-drop overlay that accepts dropped image files
- Delegates to the same upload mutation

### Route
Register as a floating action button available on the gallery route (added in feature 05).

---

## Acceptance Criteria
- [ ] `POST /photos` with a valid JPEG returns HTTP 201 with `publicId`
- [ ] Blob appears in `photos-original` container at the correct path
- [ ] `Photo` row is inserted in PostgreSQL with correct metadata
- [ ] `POST /photos` with a non-image file returns HTTP 400 with problem details
- [ ] `POST /photos` with a file > 50 MB returns HTTP 400
- [ ] Upload from the React UI succeeds and the new photo appears in the gallery (after feature 05)
- [ ] Multiple files can be selected and are uploaded sequentially with progress shown
