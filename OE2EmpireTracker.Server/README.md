# OE2 Empire Tracker — Remote Faction Server

A headless .NET 8 service that provides centralized faction/character data storage, real-time synchronization, and background colony processing for multiple OE2 Empire Tracker clients.

## Quick Start

```bash
# Run with default settings (HTTPS on port 5443, JSON file storage)
dotnet run --project OE2EmpireTracker.Server

# The server generates an Owner token on first run — save it!
# Configure clients with this token to connect.
```

## Configuration

Edit `appsettings.json` or use CLI arguments:

```bash
dotnet run --project OE2EmpireTracker.Server -- --Server:Port=8443 --Storage:Backend=Sqlite
```

| Setting | Default | Description |
|---------|---------|-------------|
| Server:Port | 5443 | HTTPS listen port |
| Storage:Backend | JsonFile | Storage backend (JsonFile, Sqlite, Postgres, DynamoDB) |
| Storage:DataPath | ./data | Data directory for JsonFile backend |
| Storage:ConnectionString | (varies) | Connection string for Sqlite/Postgres |
| Storage:DynamoTableName | OE2EmpireTracker | DynamoDB table name |
| Storage:DynamoRegion | us-east-1 | AWS region for DynamoDB |
| Server:ProcessingEnabled | false | Enable server-side colony processing |
| Server:ProcessingIntervalSeconds | 60 | Background processing tick interval |

## TLS Certificates

On first run, the server generates a self-signed certificate at `./data/server-cert.pfx`. Share the thumbprint (logged on startup) with clients for certificate pinning.

To use a custom certificate:
```json
{
  "Server": {
    "CertificatePath": "/path/to/cert.pfx",
    "CertificatePassword": "your-password"
  }
}
```

## CLI Commands

```bash
# Regenerate the Owner token (invalidates the old one)
dotnet run --project OE2EmpireTracker.Server -- --regenerate-owner-token
```

## API Overview

All endpoints under `/api/v1/` require Bearer token authentication. See `spec/api-reference.md` for the full endpoint map.

- `GET /health` — Health check (no auth)
- `POST /api/v1/tokens` — Create character account (Owner only)
- `GET /api/v1/factions` — List factions
- `GET /api/v1/characters/{uuid}/data/{type}` — Get character data
- `WS /ws?token=...` — WebSocket for real-time push

## Storage Backends

| Backend | Use Case |
|---------|----------|
| JsonFile | Development, single-user, simple deployments |
| Sqlite | Single-server production, no external dependencies |
| Postgres | Multi-instance, managed hosting (AWS RDS) |
| DynamoDB | Serverless AWS deployments |
