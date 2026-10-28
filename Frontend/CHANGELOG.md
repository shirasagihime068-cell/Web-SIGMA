# PTK.SIGMA Web Changelog

## MVC rebuild
- Frontend reorganized to match the PTK.Nova-style MVC solution structure.
- Added Controllers, Models, Views, Extensions, Configuration and layer projects.
- Added same-origin `/api/*` reverse proxy so browser API calls reach the PTK.SIGMA.Api backend without cross-origin cookie issues.
- Preserved the existing SIGMA UI and static assets.

## Authentication
- Added shared authentication cookie + DataProtection configuration for Web/API.
- Added local database role enrichment for authenticated users.
