# PTK.SIGMA — Modern N-Tier Architecture

Struktur proyek PTK.SIGMA disusun mengikuti pola struktur PTK.NOVA pada contoh yang diberikan, dengan nama proyek tetap menggunakan prefix `PTK.SIGMA`.

```text
PTK.SIGMA.Api
 │
 ├── Controllers
 ├── Extensions
 └── Models
 │
 ▼
PTK.SIGMA.Domain
 │
 └── SigmaService
     ├── DashboardService
     ├── MaterialRealizationService
     ├── MaterialService
     ├── RoleService
     ├── ServiceRealizationService
     ├── SKTTDocumentService
     ├── UserService
     └── VendorService
 │
 ▼
PTK.SIGMA.Data
 │
 ├── Api/SigmaApiRepo
 ├── Context
 ├── Dao
 ├── Extentions
 └── Migrations
 │
 ▼
PTK.SIGMA.Utilities
 ├── Base
 ├── Constants
 ├── Enums
 └── Helper
```

Peran layer:

- `PTK.SIGMA.Api`: HTTP API, controllers, SignalR hub, middleware, dan konfigurasi aplikasi.
- `PTK.SIGMA.Domain`: entity domain, kontrak service, DTO, dan boundary service.
- `PTK.SIGMA.Data`: akses SQL Server/EF Core, repository/service implementation, migration, MinIO, reporting, dan background service.
- `PTK.SIGMA.Utilities`: enum, constant, response, helper, dan primitive umum yang dipakai lintas layer.

Relasi project:

```text
PTK.SIGMA.Api
 ├── PTK.SIGMA.Domain
 ├── PTK.SIGMA.Data
 └── PTK.SIGMA.Utilities

PTK.SIGMA.Data
 ├── PTK.SIGMA.Domain
 └── PTK.SIGMA.Utilities

PTK.SIGMA.Domain
 └── PTK.SIGMA.Utilities
```
