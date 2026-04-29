# SyncCore — Local Development Setup

## Prerequisites
- Docker Desktop
- Node.js 20+
- .NET 8 SDK

## Quick Start

### 1. Start PostgreSQL (and optionally the API) via Docker

```bash
# PostgreSQL only (run API locally for hot-reload):
docker compose up postgres -d

# Or run everything in Docker:
cp .env.example .env   # fill in Azure credentials
docker compose up -d
```

### 2. Run the API locally

Fill in `src/SyncCore.Api/appsettings.Development.json` (or use user secrets):
```json
{
  "AzureBlobStorage": {
    "ConnectionString": "your-blob-connection-string"
  },
  "AzureAdB2C": {
    "Instance": "https://your-tenant.b2clogin.com",
    "ClientId": "your-client-id",
    "Domain": "your-tenant.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_signupsignin"
  }
}
```

```bash
cd src/SyncCore.Api
dotnet run
# API runs on http://localhost:5001
# Swagger at http://localhost:5001/swagger
```

### 3. Run the frontend

```bash
cd src/SyncCore.Web
cp .env.local.example .env.local   # fill in B2C values
npm install
npm run dev
# App runs on http://localhost:5173
```

## Architecture

| Service | Local | Docker |
|---|---|---|
| PostgreSQL | Docker (port 5432) | Docker |
| .NET API | `dotnet run` (port 5001) | Docker (port 5001) |
| React frontend | `npm run dev` (port 5173) | Not containerised |
| Azure AD B2C | Cloud (Azure) | Cloud (Azure) |
| Azure Blob Storage | Cloud (Azure) | Cloud (Azure) |

## Blob Storage Containers

Create these in your Azure Storage account:
- `photos-original` (private)
- `photos-thumbnails` (private)
