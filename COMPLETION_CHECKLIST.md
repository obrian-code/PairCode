# PairCode — Completion Checklist

Estado actual del proyecto vs. lo necesario para considerarlo **completo y listo para producción**.

---

## 🚨 Críticos (bloquean producción)

- [x] **CSRF Protection** — `AutoValidateAntiforgeryTokenAttribute` global en Program.cs. Todos los formularios tienen `@Html.AntiForgeryToken()`.
- [x] **EF Core Migrations** — Migración `InitialCreate` en `src/Infrastructure/Migrations/`. Production usa `db.Database.Migrate()`.
- [x] **Room Ownership** — `RequireOwnership` en RoomService. Solo el creador puede Close/Cancel/Delete/Open/UpdateName.
- [x] **AuditLogDto duplicado** — No duplicado. Definido solo en `Dtos.cs`. AuditService lo usa mediante instanciación inline.
- [x] **Transacciones** — `AuditService.LogAsync` ya no hace `SaveChangesAsync`. El caller (RoomService, UserService, etc.) controla la transacción atómicamente.

---

## 🔴 Alta Prioridad

- [x] **Tests** — 27 tests unitarios (UserService 7, RoomService 12, ChatService 3, AuditService 3, DashboardService 2). Faltan integration tests y e2e.
- [x] **Rate Limiting** — Fixed window limiter configurado: 10 requests/min en login.
- [x] **Secrets hardcodeados** — Docker Compose usa `${VAR}` sin defaults hardcodeados. Requiere variables de entorno.
- [x] **Admin role en registro** — Solo admins existentes pueden crear cuentas Admin. View y Controller validan.
- [x] **Password policy** — 8+ chars, mayúscula, dígito, carácter especial (RegisterUserValidator + ChangePasswordAsync).
- [x] **SignalR OnDisconnectedAsync** — ChatHub y DocumentHub sobrescriben `OnDisconnectedAsync`. Limpieza de grupos y estado.
- [x] **Concurrencia en SharedDocument** — `RowVersion` con `IsRowVersion()` en EF Core config. Concurrency token activo.

---

## 🟡 Media Prioridad

- [x] **Paginación** — `Room/Index` y `Audit/Index` usan `Skip`/`Take` con page/pageSize. Renderizado server-side.
- [x] **Caché** — DashboardService usa `IMemoryCache` con expiración de 60s.
- [x] **Password Reset** — ForgotPassword / ResetPassword completo: DTOs, entidad, repositorio, EmailService (console), views en Auth.
- [x] **Room UpdateName** — `RoomService.UpdateRoomAsync()` + endpoint `POST /Room/UpdateName` + inline rename en Details view.
- [x] **Email Enumeration** — `RegisterAsync` devuelve "Registration failed" (genérico). `LoginAsync` devuelve "Invalid credentials" (genérico).
- [x] **ExceptionMiddleware** — Retorna JSON para APIs, redirige a HTML para peticiones MVC según header `Accept`.
- [x] **Client-side search** — Room/Index y Audit/Index tienen filtro JS client-side sobre datos paginados. Escala aceptablemente con paginación server-side.
- [x] **Identity packages no utilizados** — No hay referencias a `Microsoft.AspNetCore.Identity.*` en ningún .csproj.

---

## 🟢 Baja Prioridad / Nice-to-have

- [x] **OpenTelemetry** — Console exporter + OTLP exporter apuntando a Jaeger. Jaeger agregado a docker-compose.yml.
- [x] **JWT refresh tokens** — RefreshToken entity + repo + TokenService.GenerateRefreshToken/GetPrincipalFromExpiredToken + AuthController.Logout revoca tokens.
- [x] **Email verification** — EmailVerificationToken entity + repositorio + envío post-registro + endpoint /Auth/VerifyEmail.
- [x] **Localización / i18n** — .resx (en/es) con `IStringLocalizer`, `RequestLocalizationMiddleware` configurado, selector de idioma en footer.
- [x] **Soft delete** — Room.IsDeleted + DeletedAt + MarkDeleted(). RoomRepository filtra por !IsDeleted en todas las queries.
- [x] **Directorios vacíos en Domain** — No existen directorios vacíos. `Domain/Events/`, `Domain/ValueObjects/`, `Domain/Interfaces/` no están presentes.
- [x] **Historial de sesiones** — `ParticipantService.GetUserHistoryAsync()` implementado, view en Profile/History. Consumible vía `Profile/History`.
- [x] **Puertos expuestos en Docker** — Solo `app:5000` expuesto al host. PostgreSQL, Prometheus y Grafana no exponen puertos al host.
- [x] **SignalR library @latest** — `libman.json` usa `@microsoft/signalr@8.0.0` (versión específica, no `@latest`).
- [x] **sin `[FromForm]` / `[Bind]`** — ProfileController.Update corregido con `[FromForm]`. Resto de actions revisadas y conformes.
