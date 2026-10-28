# PTK.SIGMA.Api — Modern N-Tier Backend

Backend ASP.NET Core untuk PTK.SIGMA dengan struktur proyek disusun mengikuti pola folder contoh PTK.NOVA yang diberikan.

## Struktur

```text
PTK.SIGMA.Api/
├── src/
│   ├── PTK.SIGMA.Api/
│   │   ├── .config/
│   │   ├── Attributes/
│   │   ├── Controllers/
│   │   ├── Extensions/
│   │   │   ├── Hubs/
│   │   │   ├── Middlewares/
│   │   │   └── Notifications/
│   │   ├── Models/
│   │   ├── Properties/
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.json
│   │   ├── Program.cs
│   │   ├── PTK.SIGMA.Api.csproj
│   │   ├── PTK.SIGMA.Api.http
│   │   └── WeatherForecast.cs
│   │
│   ├── PTK.SIGMA.Data/
│   │   ├── Api/
│   │   │   └── SigmaApiRepo/
│   │   │       ├── AuthenticationApiRepo/
│   │   │       ├── DashboardApiRepo/
│   │   │       ├── MaterialApiRepo/
│   │   │       ├── MaterialRealizationApiRepo/
│   │   │       ├── RoleApiRepo/
│   │   │       ├── ServiceRealizationApiRepo/
│   │   │       ├── SKTTDocumentRepo/
│   │   │       ├── UserApiRepo/
│   │   │       └── VendorRepo/
│   │   ├── Context/
│   │   │   └── Configurations/
│   │   ├── Dao/
│   │   ├── Extentions/
│   │   │   └── Storage/
│   │   ├── Migrations/
│   │   ├── Properties/
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.json
│   │   ├── PTK.SIGMA.Data.csproj
│   │   └── PTK.SIGMA.Data.http
│   │
│   ├── PTK.SIGMA.Domain/
│   │   ├── Entities/
│   │   ├── SigmaService/
│   │   │   ├── DashboardService/
│   │   │   ├── MaterialRealizationService/
│   │   │   ├── MaterialService/
│   │   │   ├── RoleService/
│   │   │   ├── ServiceRealizationService/
│   │   │   ├── SKTTDocumentService/
│   │   │   ├── UserService/
│   │   │   └── VendorService/
│   │   └── PTK.SIGMA.Domain.csproj
│   │
│   └── PTK.SIGMA.Utilities/
│       ├── Base/
│       ├── Constants/
│       ├── Enums/
│       ├── Helper/
│       └── PTK.SIGMA.Utilities.csproj
│
└── PTK.SIGMA.Api.sln
```

## Teknologi

- ASP.NET Core / .NET 10
- Entity Framework Core + SQL Server
- MinIO untuk penyimpanan file
- SignalR untuk notifikasi aktivitas
- Swagger
- EPPlus untuk Excel
- QuestPDF untuk PDF

## Database

Migration berada di:

```text
src/PTK.SIGMA.Data/Migrations/
```

Perintah umum:

```powershell
dotnet restore
dotnet build
dotnet ef database update --project src/PTK.SIGMA.Data --startup-project src/PTK.SIGMA.Api
dotnet run --project src/PTK.SIGMA.Api
```

## MinIO

Bucket yang digunakan aplikasi adalah `webgudang-storage`. Implementasi penyimpanan berada di:

```text
src/PTK.SIGMA.Data/Extentions/Storage/
```
## Integrasi PTK.SIGMA.Web

Development default: API HTTPS `https://localhost:5052`, API HTTP fallback `http://localhost:5000`, dan MVC frontend `https://localhost:5051`. API dan frontend menggunakan shared ASP.NET Core authentication cookie.

Endpoint pengecekan koneksi: `GET /api/health`.

