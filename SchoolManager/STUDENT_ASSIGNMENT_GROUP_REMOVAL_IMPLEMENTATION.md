# Implementación - Quitar matrícula/grupo en StudentAssignment

Fecha: 2026-06-17  
Proyecto: `C:\Proyectos\eduplaner2\SchoolManager`  
Estado: implementado localmente, compilado con 0 errores.

## Backups previos

Documento:

`BACKUP_STUDENT_ASSIGNMENT_BEFORE_CHANGES.md`

Ubicación:

`C:\Proyectos\eduplaner2\Backups\StudentAssignment_20260617_175024`

Archivos:

- Proyecto: `SchoolManager_project_20260617_175024.tar.gz`
- Base de datos: `schoolmanager_hx5i_full_20260617_175024.dump`

Ambos fueron verificados localmente antes de modificar código.

## Archivos modificados

- `Controllers/StudentAssignmentController.cs`
- `Services/Implementations/StudentAssignmentService.cs`
- `Views/StudentAssignment/Index.cshtml`

Archivos nuevos:

- `BACKUP_STUDENT_ASSIGNMENT_BEFORE_CHANGES.md`
- `STUDENT_ASSIGNMENT_GROUP_REMOVAL_IMPLEMENTATION.md`

## API

Endpoint nuevo:

`POST /StudentAssignment/RemoveEnrollment`

Parámetros:

- `studentAssignmentId`
- `removeActiveSubjects`

Validaciones:

- Matrícula válida.
- Usuario autenticado.
- Rol permitido: `admin`, `secretaria`, `director`.
- Matrícula existente.
- Matrícula activa.
- Misma escuela del usuario actual.

## Comportamiento

La eliminación se implementó como soft delete:

- `student_assignments.is_active = false`
- `student_assignments.end_date = DateTime.UtcNow`

No se ejecuta delete físico.

Nota: esta versión de `eduplaner2` no tiene `StudentSubjectAssignment` ni tabla equivalente de materias por estudiante. Por eso el endpoint inactiva únicamente la matrícula/grupo y registra en auditoría que no hay materias por estudiante que inactivar.

## Modal

En `Views/StudentAssignment/Index.cshtml`, cada grupo actual ahora se renderiza con botón:

`Quitar`

El flujo AJAX:

1. Confirma con el usuario.
2. Llama `POST /StudentAssignment/RemoveEnrollment`.
3. Actualiza la lista del modal.
4. Actualiza la celda de la tabla principal.
5. No recarga la página.

## Auditoría

Se registra en `audit_logs`:

- Usuario.
- Rol.
- Escuela.
- Fecha UTC.
- IP.
- `StudentAssignmentId`.
- Estudiante.
- Grado.
- Grupo.
- Jornada.
- Nota de que este proyecto no tiene tabla `student_subject_assignments`.

## Validación

Comando:

```powershell
dotnet build "C:\Proyectos\eduplaner2\SchoolManager\SchoolManager.csproj"
```

Resultado:

- `Build succeeded.`
- `0 Warning(s)`
- `0 Error(s)`
