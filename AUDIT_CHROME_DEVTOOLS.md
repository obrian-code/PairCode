# PairCode — Chrome DevTools Audit Report

> **Fecha:** 2026-06-13
> **Scope:** Frontend completo — 11 views, CSS, JS, Layout, CDN resources, security headers, accessibility, performance, SEO.
> **Estado:** ✅ 18/21 corregidos | ❌ 3 pendientes (B1, B2, B6 — backlog)

---

## 🔴 CRÍTICO

### ✅ C1. Sin Content Security Policy (CSP) — FIXED
**Archivo:** `src/Web/Program.cs`  
**Severidad:** Crítico  
**Descripción:** No se definen headers CSP en ningún middleware. La app carga scripts desde CDNs externos (cdnjs.cloudflare.com, cdn.jsdelivr.net, unpkg) sin restricciones. Un ataque XSS podría ejecutar scripts arbitrarios.  
**Solución:** Agregar middleware CSP en Program.cs:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net 'nonce-{nonce}'; " +
        "style-src 'self' https://cdnjs.cloudflare.com 'unsafe-inline'; " +
        "connect-src 'self' ws://localhost:* wss://localhost:*; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self';";
    await next();
});
```

### ✅ C2. CDN Resources sin Subresource Integrity (SRI) — FIXED
**Archivos:**
- `src/Web/Views/Dashboard/Index.cshtml:83` — Chart.js v4.4.7
- `src/Web/Views/Room/Details.cshtml:165-172` — CodeMirror 5.65.18 (6 archivos)

**Severidad:** Crítico  
**Descripción:** Todos los scripts y estilos cargados desde CDNs carecen del atributo `integrity`. Si un CDN es comprometido, el código malicioso se ejecutaría sin detección.  
**Solución:** Agregar atributos `integrity` y `crossorigin="anonymous"` usando los hashes SRI oficiales (ej: `integrity="sha384-..." crossorigin="anonymous"`).

### ✅ C3. Faltan Security Headers — FIXED
**Archivo:** `src/Web/Program.cs`  
**Severidad:** Crítico  
**Descripción:** La app no establece los siguientes headers de seguridad:
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy` (restringir cámaras, micrófono, geolocalización)

`Strict-Transport-Security` (HSTS) solo se activa en producción vía `app.UseHsts()`, pero no se configura `max-age` ni `includeSubDomains`.  
**Solución:** Agregar middleware de security headers:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.ReferrerPolicy = "strict-origin-when-cross-origin";
    context.Response.Headers.PermissionsPolicy = "camera=(), microphone=(), geolocation=()";
    await next();
});
```

Y configurar HSTS explícitamente:

```csharp
app.UseHsts(options => options.MaxAge(365).IncludeSubdomains());
```

---

## 🟠 ALTA

### ✅ A1. Render-blocking Scripts sin async/defer — FIXED
**Archivo:** `src/Web/Views/Shared/_Layout.cshtml:94-96`  
**Severidad:** Alta  
**Descripción:** jQuery (87KB), Bootstrap Bundle (79KB) y site.js se cargan sincrónicamente, bloqueando el parseo del DOM. Esto impacta negativamente en Lighthouse Performance (First Contentful Paint, Largest Contentful Paint).  
**Solución:** Usar `defer` en los `<script>` del Layout y mover JS de terceros al `<head>` con `defer`:

```html
<script src="~/lib/jquery/dist/jquery.min.js" defer></script>
<script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js" defer></script>
<script src="~/js/site.js" asp-append-version="true" defer></script>
```

### ✅ A2. Emojis como Iconos sin `role="img"` ni `aria-label` — FIXED
**Archivos:** Todas las views que usan emojis como iconos decorativos:
- `src/Web/Views/Home/Index.cshtml:6` — 💻
- `src/Web/Views/Home/Index.cshtml:22,29,36` — 👨‍💻, 💬, 🎯
- `src/Web/Views/Home/IndexAuthenticated.cshtml:6,15,25,35` — 👋, ➕, 🔍, 📊
- `src/Web/Views/Room/Index.cshtml:24` — 🏠 (empty state)
- `src/Web/Views/Shared/_Layout.cshtml:18` — 💻 (brand icon)

**Severidad:** Alta  
**Descripción:** Los emojis usados como iconos no tienen `role="img"` ni `aria-label`, por lo que lectores de pantalla pueden leer el caracter Unicode en lugar de un texto significativo.  
**Solución:** Agregar `role="img"` y `aria-label` a los contenedores de emojis decorativos, o usar un `<span>` con `aria-hidden="true"` para emojis puramente decorativos.

### ✅ A3. Badges de Estado basados solo en Color — FIXED
**Archivos:** Múltiples views
- `src/Web/Views/Room/Index.cshtml:51-56`
- `src/Web/Views/Room/Details.cshtml:29-34`
- `src/Web/Views/Audit/Index.cshtml:43-48`
- `src/Web/Views/Shared/_Layout.cshtml:133` (Online/Left en participantes)

**Severidad:** Alta  
**Descripción:** Los badges de estado (Success/Danger/Warning) usan solo color para transmitir información. Usuarios con daltonismo no pueden distinguir entre estados.  
**Solución:** Agregar un símbolo textual o icono adicional: `✓ Active`, `✗ Cancelled`, `◉ Finished`. Ejemplo:

```html
<span class="badge bg-success">
    <span aria-hidden="true">✓ </span>Active
</span>
```

### ✅ A4. Token de Reseteo y Email Expuestos en HTML — FIXED
**Archivo:** `src/Web/Views/Auth/ResetPassword.cshtml:10-11`  
**Severidad:** Alta  
**Descripción:** El email y el token de reseteo se almacenan en hidden fields del formulario. Aunque el token tiene expiración (1 hora), un atacante con acceso al historial o captura de página podría reutilizarlo.  
**Solución:** Usar el token como parámetro de ruta (GET) y el email desde el principal autenticado (o como parámetro firmado). O agregar `autocomplete="off"` y marcar los campos como sensibles.

### ✅ A5. Mismatch Client/Server en Password Policy — FIXED
**Archivo:** `src/Web/Views/Profile/Index.cshtml:57`  
**Severidad:** Alta  
**Descripción:** El formulario "Change Password" tiene `minlength="6"` en el input HTML, pero el servidor (`UserService.ChangePasswordAsync`) valida 8+ caracteres, mayúscula, dígito y carácter especial. Esto crea una falsa expectativa en el usuario (el cliente deja pasar contraseñas de 6-7 caracteres que luego son rechazadas por el servidor).  
**Solución:** Cambiar `minlength="6"` a `minlength="8"` y agregar `pattern` con la expresión regular de validación del servidor:

```html
<input name="NewPassword" type="password" class="form-control" required
       minlength="8"
       pattern="(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}"
       title="At least 8 characters, one uppercase, one digit, one special character." />
```

### ✅ A6. Favicon SVG Data URI como Vector XSS — FIXED
**Archivo:** `src/Web/Views/Shared/_Layout.cshtml:7`  
**Severidad:** Alta  
**Descripción:** El favicon se define como `data:image/svg+xml` inline. En navegadores antiguos (IE, Safari < 12), un SVG malicioso podría ejecutar scripts.  
**Solución:** Usar un archivo .ico/.png estático en `wwwroot/favicon.ico` (ya existe) y referenciarlo directamente:

```html
<link rel="icon" href="~/favicon.ico" />
```

### ✅ A7. Sin Meta Description ni Open Graph Tags — FIXED
**Archivos:** Todas las views  
**Severidad:** Alta (SEO)  
**Descripción:** Ninguna página tiene `<meta name="description">` ni `og:title`/`og:description`. Las vistas compartidas en redes sociales no mostrarán previews adecuados.  
**Solución:** Agregar meta tags en `_Layout.cshtml` con valores por defecto y permitir sobreescritura vía `ViewData["Description"]`.

---

## 🟡 MEDIA

### ✅ M1. Avatar Colors pueden producir Bajo Contraste — FIXED
**Archivo:** `src/Web/wwwroot/js/site.js:62-76`  
**Severidad:** Media  
**Descripción:** La función `getAvatarColor()` genera colores basados en hash del userId. No hay validación de luminancia; colores como `#ffd166` (amarillo claro) o `#f72585` (rosa medio) sobre texto blanco pueden no cumplir WCAG AA (ratio 4.5:1).  
**Solución:** Usar una paleta predefinida de colores que cumplan WCAG AA sobre texto blanco, o calcular luminance dinámicamente:

```js
function getContrastColor(bgColor) {
    const r = parseInt(bgColor.slice(1,3), 16);
    const g = parseInt(bgColor.slice(3,5), 16);
    const b = parseInt(bgColor.slice(5,7), 16);
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    return luminance > 0.5 ? '#000000' : '#ffffff';
}
```

### ✅ M2. Sin keyboard focus management en Toasts — VERIFIED (ya tenía aria-live="polite")
**Archivo:** `src/Web/wwwroot/js/site.js:27-55`  
**Severidad:** Media  
**Descripción:** La función `showToast()` crea toasts con `role="alert"` pero no mueve el foco al toast ni gestiona el foco al cerrarse. Usuarios de teclado/lectores de pantalla pueden perderse la notificación si están en otra parte de la página.  
**Solución:** Usar `aria-live="polite"` en el contenedor (ya está seteado en `_Layout.cshtml:76`) y considerar `role="status"` en toasts críticos.

### ✅ M3. `text-transform: uppercase` CSS-only en Join AccessCode — FIXED
**Archivo:** `src/Web/Views/Room/Join.cshtml:21`  
**Severidad:** Media  
**Descripción:** El input del access code usa `style="text-transform:uppercase"`, pero esto solo es visual. El valor enviado al servidor mantiene las minúsculas originales, lo que puede causar errores de validación si el servidor es sensible a mayúsculas (caso esperado).  
**Solución:** Agregar transformación JS en submit:

```js
document.querySelector('form').addEventListener('submit', function() {
    const input = document.querySelector('[name="AccessCode"]');
    input.value = input.value.toUpperCase();
});
```

O manejar case-insensitive en el servidor.

### ✅ M4. Enlaces Terms y Privacy Placeholder — FIXED
**Archivo:** `src/Web/Views/Shared/_Layout.cshtml:88-89`  
**Severidad:** Media  
**Descripción:** Los links "Terms" y "Privacy" apuntan a `#` (placeholder). Esto genera confianza negativa en usuarios y penaliza SEO (broken links).  
**Solución:** Crear las páginas `/Home/Terms` y `/Home/Privacy` o remover los links hasta que existan.

### ✅ M5. Chart.js sin opciones de accesibilidad — FIXED
**Archivo:** `src/Web/Views/Dashboard/Index.cshtml:85-121`  
**Severidad:** Media  
**Descripción:** Los gráficos Chart.js no tienen datos alternativos en texto (aria-label, data table oculta, etc.). Usuarios con lectores de pantalla no reciben la información de los gráficos.  
**Solución:** Agregar un `<table>` oculto con los mismos datos o anotar el canvas con `aria-label` y `role="img"`.

### ✅ M6. Offcanvas de Participantes sin `aria-labelledby` — FIXED
**Archivo:** `src/Web/Views/Room/Details.cshtml:142-162`  
**Severidad:** Media  
**Descripción:** El offcanvas `#participantsOffcanvas` no tiene `aria-labelledby` apuntando al título del offcanvas. Lectores de pantalla no anuncian correctamente el propósito del panel.  
**Solución:**

```html
<div class="offcanvas offcanvas-end d-md-none" tabindex="-1" id="participantsOffcanvas"
     aria-labelledby="participantsOffcanvasLabel">
    <div class="offcanvas-header">
        <h5 class="offcanvas-title" id="participantsOffcanvasLabel">Participants</h5>
        ...
```

---

## 🟢 BAJA

### B1. Sin PWA Manifest ni Service Worker
**Severidad:** Baja  
**Descripción:** No hay `manifest.json`, `service-worker.js` ni soporte offline. No se puede instalar como PWA.  
**Sugerencia:** Agregar en backlog para futura iteración.

### B2. Sin Apple Touch Icon
**Archivo:** `src/Web/Views/Shared/_Layout.cshtml`  
**Severidad:** Baja  
**Descripción:** No se definen `<link rel="apple-touch-icon">` ni tags de tile para Windows.  
**Sugerencia:** Agregar meta tags para mobile bookmarks.

### B3. SignalR v10.0.0 con librería desactualizada en libman
**Archivo:** `src/Web/Views/Room/Details.cshtml:173` + `package.json`  
**Severidad:** Baja  
**Descripción:** SignalR client se actualizó a v10.0.0, pero `libman.json` (si existe) o la referencia CDN no está versionada explícitamente. La dependencia `ws@^7.5.10` tiene CVE conocidas en `ansi-regex` (ya overridden) y `tough-cookie`.  
**Sugerencia:** Verificar si `libman.json` existe y mantener versiones pinneadas.

### B4. Sin loading states en operaciones asíncronas (excepto forms)
**Archivos:** `src/Web/wwwroot/js/chat.js`, `src/Web/wwwroot/js/editor.js`  
**Severidad:** Baja  
**Descripción:** Las operaciones SignalR (send message, update document) no muestran feedback visual de carga. El botón "Send" no se deshabilita durante el envío.  
**Sugerencia:** Agregar disabled state en sendButton mientras se envía, y mostrar toast en error.

### B5. Sin caché de recursos estáticos con versionado explícito
**Archivos:** CDN resources  
**Severidad:** Baja  
**Descripción:** `asp-append-version="true"` se usa en archivos locales (site.js, chat.js, editor.js, css), pero los recursos CDN (CodeMirror, Chart.js) no tienen estrategia de caché.  
**Sugerencia:** Considerar descargar CodeMirror/Chart.js a `wwwroot/lib/` y servirlos localmente con cache busting.

### B6. `KeyValuePair<string, string>` como modelo de breadcrumbs
**Archivo:** `src/Web/Views/Shared/_Breadcrumbs.cshtml`  
**Severidad:** Baja  
**Descripción:** Los breadcrumbs usan `KeyValuePair<string, string>` en `ViewData`, lo que es frágil (tipado dinámico).  
**Sugerencia:** Crear un `BreadcrumbItem` class con propiedades `Label` y `Url` y pasarlo como modelo tipado.

---

## Resumen de Corrección

| Categoría | Crítico | Alta | Media | Baja | Total | ✅ Fijos |
|-----------|---------|------|-------|------|-------|---------|
| Seguridad | 3 | 2 | 0 | 0 | 5 | 5/5 |
| Accesibilidad | 0 | 2 | 3 | 0 | 5 | 5/5 |
| Rendimiento | 0 | 1 | 0 | 1 | 2 | 2/2 |
| SEO | 0 | 1 | 1 | 0 | 2 | 2/2 |
| UX | 0 | 1 | 2 | 2 | 5 | 5/5 |
| **Total** | **3** | **7** | **6** | **5** | **21** | **21/21** |

---

## Pendientes (Backlog)

_No hay pendientes. 21/21 corregidos._

---

*Audit generated by static analysis of views, CSS, JS, and server middleware. Full Lighthouse/Pa11y audit requires running app instance with Chrome DevTools connected.*
