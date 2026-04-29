---
applyTo: "**"
---

# Feature 09 — Search

## Goal
Allow users to search their photos by file name and date range using a simple text + date filter UI. Results are displayed in the same gallery grid, paginated. No AI/ML or full-text indexing required for the MVP — PostgreSQL `ILIKE` and date comparisons are sufficient.

---

## Backend

### Extend `GetPhotosQuery` — `Features/Photos/List/`

Add optional search parameters to the existing query:

```csharp
public record GetPhotosQuery(
    string UserId,
    string? Cursor,
    int PageSize = 50,
    string? SearchTerm = null,      // partial match on FileName
    DateOnly? DateFrom = null,      // filter TakenAt >= DateFrom
    DateOnly? DateTo = null,        // filter TakenAt <= DateTo (end of day)
    bool FavouritesOnly = false     // from feature 08
) : IRequest<GetPhotosResult>;
```

**`GetPhotosHandler.cs`** additions
```csharp
if (!string.IsNullOrWhiteSpace(query.SearchTerm))
    q = q.Where(p => EF.Functions.ILike(p.FileName, $"%{query.SearchTerm}%"));

if (query.DateFrom.HasValue)
    q = q.Where(p => p.TakenAt >= query.DateFrom.Value.ToDateTime(TimeOnly.MinValue));

if (query.DateTo.HasValue)
    q = q.Where(p => p.TakenAt <= query.DateTo.Value.ToDateTime(TimeOnly.MaxValue));
```

> Security note: `EF.Functions.ILike` is parameterised by EF Core — no SQL injection risk.

**`GetPhotosEndpoint.cs`** additions
```csharp
app.MapGet("/photos", async (
    [FromQuery] string? cursor,
    [FromQuery] int pageSize,
    [FromQuery] string? q,          // renamed from searchTerm for brevity
    [FromQuery] DateOnly? dateFrom,
    [FromQuery] DateOnly? dateTo,
    [FromQuery] bool favourites,
    ...) =>
```

---

## Frontend

### `src/features/search/`

**`SearchBar.tsx`**
- Text input bound to a `query` state string (debounced 300 ms with a `useDebounce` hook)
- Two date pickers: "From" and "To" (use native `<input type="date" />` for simplicity)
- Clear button that resets all filters
- On change: update URL search params so filters are shareable/bookmarkable

**`useSearchParams` hook pattern**
```ts
// Read filters from the URL
const [searchParams, setSearchParams] = useSearchParams();
const q = searchParams.get("q") ?? "";
const dateFrom = searchParams.get("dateFrom") ?? undefined;
const dateTo = searchParams.get("dateTo") ?? undefined;
```

**`api.ts`** — extend `usePhotos`
```ts
export function usePhotos(filters?: {
  q?: string;
  dateFrom?: string;
  dateTo?: string;
  favouritesOnly?: boolean;
}) {
  return useInfiniteQuery<PhotoPage>({
    queryKey: ["photos", filters],
    queryFn: ({ pageParam }) =>
      apiClient
        .get<PhotoPage>("/photos", {
          params: { cursor: pageParam, pageSize: 50, ...filters },
        })
        .then((r) => r.data),
    getNextPageParam: (last) => last.nextCursor ?? undefined,
    initialPageParam: undefined,
  });
}
```

**`SearchPage.tsx`** (or integrate into `GalleryPage.tsx`)
- Render `<SearchBar />` at the top
- Pass `{ q, dateFrom, dateTo }` to `usePhotos`
- When all filters are empty, behaves identically to the main gallery
- Show result count hint: "Showing results for '{q}'" when a filter is active
- Empty state: "No photos match your search"

### Route
Option A — integrate search into the gallery route (`/`) with query params  
Option B — dedicated route `{ path: "/search", element: <SearchPage /> }`  
Recommended: **Option A** — keeps the UX simple.

---

## Acceptance Criteria
- [ ] `GET /photos?q=beach` returns only photos whose file names contain "beach" (case-insensitive)
- [ ] `GET /photos?dateFrom=2026-01-01&dateTo=2026-03-31` returns only photos taken in Q1 2026
- [ ] Filters can be combined (e.g. `?q=holiday&dateFrom=2025-12-01`)
- [ ] Filters do not affect other users' photos
- [ ] Search input is debounced so the API is not called on every keystroke
- [ ] Active filters are reflected in the URL so the page can be shared or refreshed
- [ ] Clearing filters returns to the full gallery without a page reload
- [ ] Empty results show a friendly empty-state message
