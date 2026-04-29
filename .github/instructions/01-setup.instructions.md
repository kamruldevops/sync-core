---
applyTo: "**"
---

# Feature 01 — Project Setup & Infrastructure

## Goal
Scaffold the full solution with both projects wired together, local dev environment running, and Azure infrastructure defined as code.

---

## Backend — `SyncCore.Api`

### Create the project
```bash
dotnet new webapi -n SyncCore.Api --use-minimal-apis -o src/SyncCore.Api
dotnet new sln -n SyncCore
dotnet sln add src/SyncCore.Api
```

### NuGet packages to install
```
Microsoft.EntityFrameworkCore.Design
Npgsql.EntityFrameworkCore.PostgreSQL
MediatR
FluentValidation.DependencyInjectionExtensions
Azure.Storage.Blobs
SixLabors.ImageSharp
Microsoft.Identity.Web
Microsoft.Identity.Web.MicrosoftGraph   # optional
Ulid                                     # or use the built-in System.Ulid in .NET 8
```

### Project structure to create
```
SyncCore.Api/
├── Features/                 # one sub-folder per feature added later
├── Infrastructure/
│   ├── Data/
│   │   ├── PhotoDbContext.cs
│   │   ├── IPhotoDbContext.cs
│   │   └── Migrations/
│   ├── BlobStorage/
│   │   ├── IBlobStorageService.cs
│   │   └── BlobStorageService.cs
│   └── Thumbnails/
│       ├── IThumbnailService.cs
│       └── ThumbnailService.cs        # implemented in feature 04
├── Common/
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs
└── Program.cs
```

### `Program.cs` wiring
- Register `PhotoDbContext` with Npgsql connection string from `appsettings.json`
- Register `IBlobStorageService` / `IThumbnailService` as scoped
- Add MediatR scanning `typeof(Program).Assembly`
- Add FluentValidation pipeline behaviour
- Add `Microsoft.Identity.Web` JWT bearer auth (wired in feature 02)
- Add CORS policy allowing `http://localhost:5173` (Vite dev server)
- Map all endpoint extension methods

### `appsettings.json` config keys
```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=synccore;Username=postgres;Password=postgres"
  },
  "AzureBlobStorage": {
    "ConnectionString": "",
    "OriginalContainer": "photos-original",
    "ThumbnailContainer": "photos-thumbnails"
  },
  "AzureAdB2C": {
    "Instance": "",
    "ClientId": "",
    "Domain": "",
    "SignUpSignInPolicyId": ""
  }
}
```

---

## Frontend — `SyncCore.Web`

### Create the project
```bash
npm create vite@latest SyncCore.Web -- --template react-ts
cd SyncCore.Web
npm install
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
npm install @tanstack/react-query axios react-router-dom @azure/msal-react @azure/msal-browser
```

### TailwindCSS — `tailwind.config.js`
```js
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: { extend: {} },
  plugins: [],
}
```

### Shared API client — `src/api/client.ts`
- Create an axios instance with `baseURL` from `import.meta.env.VITE_API_BASE_URL`
- Add a request interceptor that attaches `Authorization: Bearer <token>` (token logic wired in feature 02)

### App shell — `src/App.tsx`
- Wrap with `QueryClientProvider` and `MsalProvider`
- Set up `createBrowserRouter` with placeholder routes

### Environment file — `.env.local`
```
VITE_API_BASE_URL=http://localhost:5001
VITE_B2C_CLIENT_ID=
VITE_B2C_AUTHORITY=
VITE_B2C_REDIRECT_URI=http://localhost:5173
```

---

## Docker Compose — local dev

File: `docker-compose.yml` at repo root

```yaml
version: "3.9"
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: synccore
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

Run with: `docker compose up -d`

---

## Azure Infrastructure (Bicep)

Directory: `infra/`

Create the following Bicep modules:
- `main.bicep` — top-level orchestration
- `modules/storage.bicep` — Storage Account + two containers (`photos-original`, `photos-thumbnails`)
- `modules/postgres.bicep` — Azure Database for PostgreSQL Flexible Server
- `modules/containerapp.bicep` — Azure Container Apps environment + API app
- `modules/staticwebapp.bicep` — Azure Static Web Apps resource

Key parameters to expose: `location`, `appName`, `postgresAdminPassword`

---

## Acceptance Criteria
- [ ] `docker compose up` starts PostgreSQL with no errors
- [ ] `dotnet run` in `SyncCore.Api` starts on port 5001 and returns 200 on `GET /healthz`
- [ ] `npm run dev` in `SyncCore.Web` starts on port 5173 with no TS errors
- [ ] EF Core migration `InitialCreate` runs successfully against local Postgres
- [ ] Bicep templates pass `az bicep build` with no errors
