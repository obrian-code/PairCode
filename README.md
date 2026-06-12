# PairCode

![CI](https://github.com/USER/REPO/actions/workflows/ci.yml/badge.svg)
![Docker](https://github.com/USER/REPO/actions/workflows/docker.yml/badge.svg)

Plataforma de **pair programming** con editor de código compartido, chat en tiempo real y gestión de salas con roles.

## Stack

| Capa | Tecnología |
|------|-----------|
| Runtime | .NET 8 |
| Arquitectura | Clean Architecture (Domain, Application, Infrastructure, Web) |
| ORM | Entity Framework Core 8 + Npgsql |
| BD | PostgreSQL 16 |
| Auth | JWT Bearer + BCrypt.Net |
| Tiempo real | SignalR |
| Validación | FluentValidation |
| Contenedores | Docker + Docker Compose |
| Frontend | ASP.NET Core MVC (Razor) |

## Estructura

```
src/
├── Domain/           # Entidades y Enums (0 dependencias externas)
│   ├── Entities/     → User, Room, Participant, ChatMessage, SharedDocument, AuditLog
│   └── Enums/        → UserRole, RoomStatus
├── Application/      # DTOs, Interfaces, Servicios, Validadores
│   ├── DTOs/         → 15 records
│   ├── Interfaces/   → 7 interfaces
│   ├── Services/     → 7 servicios
│   └── Validators/   → 4 validadores FluentValidation
├── Infrastructure/   # EF Core DbContext, Repositorios, TokenService
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Configurations/ → 6 configuraciones Fluent API
│   ├── Repositories/ → 6 repositorios
│   └── Services/     → TokenService
└── Web/              # Controllers, Hubs SignalR, Middleware, Views
    ├── Controllers/  → Auth, Home, Room, Dashboard, Profile, Audit
    ├── Hubs/         → ChatHub, DocumentHub
    ├── Middleware/    → ExceptionMiddleware
    └── Views/        → 16 Razor Views
```

## Roles

- **Admin** — Acceso completo, auditoría
- **Interviewer** — Crea y gestiona salas
- **Candidate** — Se une a salas mediante código

## Features

- Auth con JWT + BCrypt (cookie HttpOnly)
- CRUD de salas con código de acceso (6 chars)
- Join/Leave por código
- Ciclo de vida de sala: Created → Active → Finished / Cancelled
- Chat en tiempo real (SignalR)
- Editor de código compartido con versionado
- Dashboard con métricas
- Auditoría de todas las operaciones
- Manejo global de excepciones
- Validación con FluentValidation

## Quick Start

```bash
# Con Docker
docker compose up -d

# Sin Docker (requiere PostgreSQL en local)
# Configurar ConnectionStrings__DefaultConnection en appsettings.json
dotnet run --project src/Web
```

La app queda disponible en `http://localhost:5000`.

## Configuración

| Variable | Descripción |
|----------|------------|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión PostgreSQL |
| `Jwt__Key` | Clave secreta JWT (min 32 caracteres) |
| `Jwt__Issuer` | Emisor del token |
| `Jwt__Audience` | Audiencia del token |

En **producción** usar variables de entorno o Secret Manager. No committear secrets.

## CI/CD

### Workflows

| Workflow | Disparador | Acción |
|----------|-----------|--------|
| `ci.yml` | Push a `main`/`develop` + PR a `main` | Build + Test |
| `docker.yml` | Push a `main` + tags `v*` | Build Docker → push a GHCR |
| `deploy.yml` | Al completar `docker.yml` en `main` | SSH deploy a producción |

### Secrets requeridos en GitHub

| Secret | Uso |
|--------|-----|
| `DEPLOY_HOST` | IP o dominio del servidor |
| `DEPLOY_USER` | Usuario SSH |
| `DEPLOY_SSH_KEY` | Clave privada SSH |
| `DEPLOY_PORT` | Puerto SSH (default 22) |

### Variables requeridas en GitHub

| Variable | Uso |
|----------|-----|
| `REGISTRY` | Registry (default `ghcr.io`) |
| `DEPLOY_PATH` | Ruta en servidor (default `/app/paircode`) |

## Monitoreo

| Endpoint | Puerto | Descripción |
|----------|--------|-------------|
| `GET /health` | 8080 | Health check (DB + uptime) |
| `GET /metrics` | 8080 | Métricas Prometheus |
| Grafana | 3000 | Dashboards (admin/admin) |
| Prometheus | 9090 | UI de queries |

```bash
docker compose up -d
# Abrir http://localhost:3000 → dashboard "PairCode"
```

## Desarrollo

```bash
# Restaurar
dotnet restore

# Compilar
dotnet build

# Ejecutar
dotnet run --project src/Web
```
