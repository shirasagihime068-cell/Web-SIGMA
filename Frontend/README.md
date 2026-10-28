# PTK.SIGMA.Web

Frontend MVC PTK.SIGMA yang terhubung ke `PTK.SIGMA.Api` melalui reverse proxy same-origin `/api/*`.

```text
PTK.SIGMA.Web/
├── PTK.SIGMA.Data/
├── PTK.SIGMA.Domain/
├── PTK.SIGMA.Utilities/
├── PTK.SIGMA.Web/
└── PTK.SIGMA.Web.sln
```

## Development

Backend default: `https://localhost:5052` (HTTP fallback `http://localhost:5000`).
Frontend default: `https://localhost:5051` (HTTP fallback `http://localhost:5050`).

Request data browser memakai `/api/*` pada origin frontend; `BackendApiClient` meneruskan cookie sesi ke backend. SignalR notifikasi menggunakan endpoint backend HTTPS `https://localhost:5052/notificationHub` agar tidak terkena mixed-content blocking.

```powershell
# Backend
cd PTK.SIGMA.Api
dotnet restore
dotnet build
dotnet run --project src/PTK.SIGMA.Api --launch-profile https

# Frontend
cd PTK.SIGMA.Web
dotnet restore
dotnet build
dotnet run --project PTK.SIGMA.Web --launch-profile https
```
