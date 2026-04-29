# SyncCore — Photo App (Google Photos-like)

## Project Overview

A minimal, self-hosted photo management web app inspired by Google Photos.
Users can upload, organise, and browse photos with fast thumbnail delivery via Azure Blob Storage.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 18 + TypeScript, Vite, TailwindCSS, TanStack Query (React Query), React Router v6 |
| Backend | .NET 8 C# Web API, EF Core 8, MediatR (CQRS), FluentValidation |
| Database | PostgreSQL (Azure Database for PostgreSQL Flexible Server; Docker for local dev) |
| Auth | Azure AD B2C — MSAL React (frontend), Microsoft.Identity.Web (backend) |
| Storage | Azure Blob Storage — two containers: `photos-original`, `photos-thumbnails` |
| Thumbnails | SixLabors.ImageSharp (open-source, no native deps) |
| Deployment | Azure Static Web Apps (frontend) + Azure Container Apps (backend API) |

---

## Repository Structure

```
sync-core/
├── src/
│   ├── SyncCore.Api/            # .NET 8 Web API
│   │   ├── Features/            # MediatR feature slices (CQRS)
│   │   ├── Infrastructure/      # EF Core, Blob Storage, ImageSharp services
│   │   └── Program.cs
│   └── SyncCore.Web/            # React + Vite frontend
│       ├── src/
│       │   ├── features/        # Feature-based folder structure
│       │   ├── components/      # Shared UI components
│       │   ├── api/             # TanStack Query hooks + axios client
│       │   └── main.tsx
├── .github/
│   ├── copilot-instructions.md  # This file
│   └── instructions/            # Per-feature instruction files
├── infra/                       # Azure Bicep IaC
└── docker-compose.yml           # Local dev (PostgreSQL + API)
```

---

## Coding Conventions

### Backend (.NET)
- Use **vertical slice / feature folder** pattern via MediatR — one folder per feature under `Features/`
- Each slice contains: `Command.cs` / `Query.cs`, `Handler.cs`, `Validator.cs`, `Endpoint.cs`
- Use **Minimal APIs** (not controllers) — register endpoints via extension methods
- Return `Results<T, ProblemHttpResult>` from endpoints
- Use `CancellationToken` on all async methods
- All DB access via EF Core; use `IPhotoDbContext` interface for testability
- Blob operations via `IBlobStorageService` abstraction (wraps Azure SDK)
- Thumbnail generation via `IThumbnailService` abstraction (wraps ImageSharp)
- Never expose internal IDs — use ULIDs (`Ulid`) as public identifiers
- Validate all inputs with FluentValidation; return RFC 7807 problem details on errors

### Frontend (React)
- Use **feature-based folder structure**: `src/features/<feature-name>/`
- Each feature folder contains: `index.tsx`, `api.ts` (TanStack Query hooks), `types.ts`, components
- Use TanStack Query for all server state — no Redux/Zustand for server data
- Use React Router v6 `createBrowserRouter` (no legacy `<BrowserRouter>`)
- Style with **TailwindCSS** utility classes only — no CSS modules or styled-components
- All API calls via a shared `apiClient` (axios instance with B2C token interceptor)
- TypeScript strict mode — no `any`, no non-null assertions unless unavoidable

---

## Azure Blob Storage Design

```
Storage Account: synccorephotos
├── Container: photos-original     (private — access via SAS tokens or API proxy)
└── Container: photos-thumbnails   (private — access via SAS tokens)
```

- **Original photos**: stored with path `{userId}/{photoId}/original{ext}`
- **Thumbnail (300×300 square crop)**: `{userId}/{photoId}/thumb_300.webp`
- **Preview (max 1200px wide, aspect-preserved)**: `{userId}/{photoId}/preview_1200.webp`
- Thumbnails are generated asynchronously after upload using ImageSharp
- Serve thumbnails via short-lived SAS tokens (15 min) to keep containers private

---

## Auth Pattern (Azure AD B2C)

- B2C tenant with a single **Sign-up / Sign-in** user flow
- Frontend uses `@azure/msal-react` — acquire token silently, attach as `Bearer` header
- Backend uses `Microsoft.Identity.Web` — validates JWT, exposes `UserId` claim as `NameIdentifier`
- All API endpoints require `[Authorize]`; user is scoped to their own photos only

---

## Thumbnail Generation (ImageSharp)

Library: `SixLabors.ImageSharp` (MIT licence)

```csharp
// Thumb 300×300 square crop
image.Mutate(x => x
    .AutoOrient()
    .Resize(new ResizeOptions { Size = new Size(300, 300), Mode = ResizeMode.Crop })
);
await image.SaveAsWebpAsync(stream);

// Preview 1200px max width, aspect-preserved
image.Mutate(x => x
    .AutoOrient()
    .Resize(new ResizeOptions { Size = new Size(1200, 0), Mode = ResizeMode.Max })
);
await image.SaveAsWebpAsync(stream);
```

- Always call `AutoOrient()` to respect EXIF rotation
- Save as **WebP** for best size/quality ratio
- Run thumbnail generation in the API after upload (async via background task or inline)

---

## Feature Build Order

Build features in this order (each has a dedicated instruction file):

| # | Feature | Instruction File |
|---|---|---|
| 1 | Project Setup & Infrastructure | `instructions/01-setup.instructions.md` |
| 2 | Authentication (Azure AD B2C) | `instructions/02-auth.instructions.md` |
| 3 | Photo Upload | `instructions/03-upload.instructions.md` |
| 4 | Thumbnail Generation | `instructions/04-thumbnails.instructions.md` |
| 5 | Photo Gallery View | `instructions/05-gallery.instructions.md` |
| 6 | Photo Detail View | `instructions/06-photo-detail.instructions.md` |
| 7 | Albums | `instructions/07-albums.instructions.md` |
| 8 | Favourites | `instructions/08-favourites.instructions.md` |
| 9 | Search | `instructions/09-search.instructions.md` |
| 10 | Trash / Delete | `instructions/10-trash.instructions.md` |
