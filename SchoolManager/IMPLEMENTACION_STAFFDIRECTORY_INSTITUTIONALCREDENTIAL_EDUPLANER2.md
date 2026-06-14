# Implementación StaffDirectory + InstitutionalCredential — eduplaner2

**Proyecto:** `C:\Proyectos\eduplaner2\SchoolManager`  
**Referencia:** `C:\Proyectos\EduplanerIIC\SchoolManager`  
**Fecha:** 2026-06-14

---

## 1. Backup realizado (FASE 1)

| Campo | Valor |
|-------|--------|
| **Ruta** | `C:\Proyectos\eduplaner2\SchoolManager\Backups\backup_eduplaner2_staffcredential_20260614_122339.dump` |
| **Fecha/hora** | 2026-06-14 12:24:03 (local) |
| **Tamaño** | 646 431 bytes (~631 KB) |
| **Formato** | `pg_dump -Fc` (custom) |
| **Base de datos** | Render — `schoolmanager_hx5i` |
| **Validación** | Archivo existe y tamaño > 0 KB |

---

## 2. Análisis proyecto destino (FASE 2)

**Antes de la implementación:**

| Componente | Estado previo |
|------------|---------------|
| `/SuperAdmin/StaffDirectory` | No existía |
| `/InstitutionalCredential/ui` | No existía |
| `InstitutionalCredentialController` | No existía |
| Tablas `staff_*` / `institutional_*` | No existían en BD |
| `StudentIdCard`, fotos, QR, `school_id_card_settings` | Ya existían |
| `IUserPhotoService`, `IQrSignatureService`, rate limit `ScanApiPolicy` | Ya registrados |

---

## 3. Archivos creados

### Controladores
- `Controllers/InstitutionalCredentialController.cs`

### Modelos
- `Models/StaffInstitutionalProfile.cs`
- `Models/InstitutionalCredentialCard.cs`
- `Models/StaffQrToken.cs`

### Servicios
- `Services/Implementations/InstitutionalCredentialService.cs`
- `Services/Implementations/InstitutionalCredentialPdfService.cs`
- `Services/Implementations/InstitutionalCredentialHtmlCaptureService.cs`
- `Services/Implementations/InstitutionalCredentialImageService.cs`
- `Services/Interfaces/IInstitutionalCredentialService.cs`
- `Services/Interfaces/IInstitutionalCredentialPdfService.cs`
- `Services/Interfaces/IInstitutionalCredentialImageService.cs`
- `Services/Interfaces/IInstitutionalCredentialHtmlCaptureService.cs`

### Helpers / Options / DTOs / ViewModels
- `Helpers/StaffInstitutionalProfileAccess.cs`
- `Helpers/StaffInstitutionalRoleFilter.cs`
- `Helpers/StaffMemberPublicLink.cs`
- `Helpers/InstitutionalCardNumberHelper.cs`
- `Options/InstitutionalCredentialOptions.cs`
- `Dtos/InstitutionalCredentialCardDto.cs`
- `Dtos/StaffCardRenderDto.cs`
- `ViewModels/SuperAdminStaffDirectoryViewModels.cs`
- `ViewModels/InstitutionalCredentialGenerateViewModel.cs`
- `ViewModels/StaffMemberPublicProfileVm.cs`

### Vistas / assets
- `Views/SuperAdmin/StaffDirectory.cshtml`
- `Views/InstitutionalCredential/Index.cshtml`
- `Views/InstitutionalCredential/Generate.cshtml`
- `Views/InstitutionalCredential/PublicMemberProfile.cshtml`
- `Views/InstitutionalCredential/PublicMemberInvalid.cshtml`
- `wwwroot/css/superadmin-staff-pages.css`

### Migración
- `Migrations/20260614172611_AddInstitutionalStaffCredentialTables.cs`
- `Migrations/20260614172611_AddInstitutionalStaffCredentialTables.Designer.cs`

---

## 4. Archivos modificados

- `Controllers/SuperAdminController.cs` — acciones StaffDirectory (GET + 3 POST JSON)
- `Services/Implementations/SuperAdminService.cs` — `GetStaffDirectoryPageAsync`
- `Services/Interfaces/ISuperAdminService.cs`
- `Models/SchoolDbContext.cs` — DbSets + configuración EF
- `Program.cs` — registro DI credencial institucional
- `Views/Shared/_SuperAdminLayout.cshtml` — menú Directorio personal + Credencial institucional
- `Migrations/SchoolDbContextModelSnapshot.cs`
- `appsettings.json` — sección `InstitutionalCredential:PublicBaseUrl` (local; puede estar fuera de git)

---

## 5. Migración aplicada (FASE 4)

**Migración:** `20260614172611_AddInstitutionalStaffCredentialTables`

**SQL ejecutado (solo DDL):**
- `CREATE TABLE institutional_credential_cards`
- `CREATE TABLE staff_institutional_profiles`
- `CREATE TABLE staff_qr_tokens`
- Índices únicos y FK a `users.id` (CASCADE)

**Sin** INSERT/UPDATE/DELETE de datos de negocio.

**Verificación post-migración:** las 3 tablas existen en Render producción.

---

## 6. Endpoints disponibles

| Método | Ruta | Auth |
|--------|------|------|
| GET | `/SuperAdmin/StaffDirectory` | superadmin |
| POST | `/SuperAdmin/StaffDirectoryUpdatePhoto` | superadmin |
| POST | `/SuperAdmin/StaffDirectoryRemovePhoto` | superadmin |
| POST | `/SuperAdmin/StaffDirectorySaveProfile` | superadmin |
| GET | `/InstitutionalCredential/ui` | SuperAdmin/superadmin |
| GET | `/InstitutionalCredential/ui/generate/{userId}` | SuperAdmin/superadmin |
| GET | `/InstitutionalCredential/ui/print/{userId}` | SuperAdmin/superadmin → PDF |
| POST | `/InstitutionalCredential/api/generate/{userId}` | SuperAdmin/superadmin |
| GET | `/InstitutionalCredential/api/list-json` | SuperAdmin/superadmin |
| GET | `/InstitutionalCredential/api/list-filters` | SuperAdmin/superadmin |
| GET | `/InstitutionalCredential/api/qr-preview/{userId}` | SuperAdmin/superadmin |
| GET | `/InstitutionalCredential/member?t=` | Público (rate limit) |
| GET | `/InstitutionalCredential/member/{token}` | Público (rate limit) |

---

## 7. Compilación

```
dotnet restore  → OK
dotnet build    → Build succeeded — 0 Warning(s), 0 Error(s)
```

---

## 8. Seguridad (FASE 5)

- StaffDirectory y UI credencial: `[Authorize(Roles = "superadmin")]` / `SuperAdmin,superadmin`.
- POST StaffDirectory: `[ValidateAntiForgeryToken]`.
- Perfil público QR: `[AllowAnonymous]` + `ScanApiPolicy` (60 req/min/IP).
- Carnets estudiantiles: **no modificados** (tablas y servicios `StudentIdCard*` intactos).

---

## 9. Riesgos pendientes

1. **`InstitutionalCredential:PublicBaseUrl` vacío** — Configurar en Render para QR con URL absoluta escaneable.
2. **PII en perfil público QR** — Email, sangre, alergias, emergencia visibles sin login (heredado del diseño fuente).
3. **Allowlist de roles distinta** — Directorio usa allowlist explícita; listado credencial usa “no estudiante”.
4. **`GenerateApi` sin anti-forgery** — Igual que proyecto fuente; mitigado por cookie de sesión.
5. **PDF nativo puede crear tarjeta/token** si no existen al imprimir (fallback).

---

## 10. Pruebas manuales sugeridas

1. Login como superadmin → `/SuperAdmin/StaffDirectory` lista personal con filtros.
2. Editar cargo/departamento/código → guardar → refrescar fila.
3. Subir/quitar foto de un docente → thumbnail actualizado.
4. `/InstitutionalCredential/ui` → DataTables carga personal.
5. Generar credencial → vista previa en `/ui/generate/{userId}`.
6. Imprimir PDF → descarga solo frente del carnet.
7. Escanear QR → `/InstitutionalCredential/member?t=...` perfil público.
8. Confirmar `/StudentIdCard/ui` sigue operativo sin regresiones.

---

## 11. Notas

- Proyecto **fuente** (`EduplanerIIC`) no fue modificado.
- Backup local en `Backups/` — no incluir en git si contiene dumps de producción.
- Archivo no relacionado detectado en working tree: `Scripts/delete_target_grade_group_pairs.sql` — **excluido del commit**.
