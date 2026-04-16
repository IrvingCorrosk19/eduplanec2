using SchoolManager.Models;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Enums;
using BCrypt.Net;
using SchoolManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolManager.Services.Implementations
{
public class UserService : IUserService
{
    private readonly SchoolDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UserService> _logger;

    public UserService(SchoolDbContext context, ICurrentUserService currentUserService, ILogger<UserService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<List<User>> GetAllStudentsAsync()
    {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.SchoolId == null)
                return new List<User>();

        return await _context.Users
            .Where(u => u.Role.ToLower() == "student" || u.Role.ToLower() == "estudiante")
                .Where(u => u.SchoolId == currentUser.SchoolId)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task UpdateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds)
    {
        try
        {
            Console.WriteLine($"=== USER SERVICE UPDATE ===");
            Console.WriteLine($"Usuario ID: {user.Id}");
            Console.WriteLine($"Nombre: {user.Name}");
            Console.WriteLine($"Email: {user.Email}");
            Console.WriteLine($"Celular Principal: {user.CellphonePrimary}");
            Console.WriteLine($"Celular Secundario: {user.CellphoneSecondary}");
            Console.WriteLine($"Subjects Count: {subjectIds?.Count ?? 0}");
            Console.WriteLine($"Groups Count: {groupIds?.Count ?? 0}");

            Console.WriteLine("Actualizando usuario en contexto...");
            
            // En lugar de usar Update() que puede causar problemas con claves únicas,
            // vamos a actualizar solo los campos específicos
            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser != null)
            {
                existingUser.Name = user.Name;
                existingUser.LastName = user.LastName;
                existingUser.Email = user.Email;
                existingUser.DocumentId = user.DocumentId;
                existingUser.Role = user.Role;
                existingUser.Status = user.Status;
                existingUser.DateOfBirth = user.DateOfBirth;
                existingUser.CellphonePrimary = user.CellphonePrimary;
                existingUser.CellphoneSecondary = user.CellphoneSecondary;
                existingUser.Disciplina = user.Disciplina;
                existingUser.Inclusion = user.Inclusion;
                existingUser.Orientacion = user.Orientacion;
                existingUser.Inclusivo = user.Inclusivo;
                existingUser.PasswordHash = user.PasswordHash;
                existingUser.UpdatedAt = DateTime.UtcNow;
                
                // Actualizar Subjects
                existingUser.Subjects.Clear();
                if (subjectIds.Any())
                {
                    var subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
                    foreach (var subject in subjects)
                    {
                        existingUser.Subjects.Add(subject);
                    }
                }

                // Actualizar Groups
                existingUser.Groups.Clear();
                if (groupIds.Any())
                {
                    var groups = await _context.Groups.Where(g => groupIds.Contains(g.Id)).ToListAsync();
                    foreach (var group in groups)
                    {
                        existingUser.Groups.Add(group);
                    }
                }
            }
            
            Console.WriteLine("Guardando cambios en base de datos...");
            await _context.SaveChangesAsync();
            
            Console.WriteLine("Usuario actualizado exitosamente en UserService");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== ERROR EN USER SERVICE UPDATE ===");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }
    public async Task UpdateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds, List<Guid> gradeLevelIds)
    {
        // Actualizar Subjects
        user.Subjects.Clear();
        if (subjectIds.Any())
        {
            var subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
            foreach (var subject in subjects)
            {
                user.Subjects.Add(subject);
            }
        }


        // Actualizar Groups
        user.Groups.Clear();
        if (groupIds.Any())
        {
            var groups = await _context.Groups.Where(g => groupIds.Contains(g.Id)).ToListAsync();
            foreach (var group in groups)
            {
                user.Groups.Add(group);
            }
        }

        // Actualizar GradeLevels
        user.Grades.Clear();
        if (gradeLevelIds.Any())
        {
            var grades = await _context.GradeLevels.Where(g => gradeLevelIds.Contains(g.Id)).ToListAsync();
            foreach (var grade in grades)
            {
                user.Grades.Add(grade);
            }
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
    public async Task<List<User>> GetAllTeachersAsync()
    {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.SchoolId == null)
                return new List<User>();

        return await _context.Users
            .Where(u => u.Role == "teacher")
                .Where(u => u.SchoolId == currentUser.SchoolId)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }
    public async Task CreateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds)
    {
        try
        {
                var currentUser = await _currentUserService.GetCurrentUserAsync();
                if (currentUser == null || currentUser.SchoolId == null)
                    throw new InvalidOperationException("No se puede crear el usuario porque no hay un usuario actual o no tiene un colegio asignado.");

                // Asignar el SchoolId del usuario actual
                user.SchoolId = currentUser.SchoolId;

            // La contraseña ya viene hasheada desde el controlador, no necesitamos hashearla de nuevo
            
            // Cargar las entidades completas desde la base de datos
            var subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
            var groups = await _context.Groups.Where(g => groupIds.Contains(g.Id)).ToListAsync();

            // Asignar las relaciones
            user.Subjects = subjects;
            user.Groups = groups;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Error al crear el usuario y asignar relaciones.", ex);
        }
    }
    public async Task<User?> GetByIdWithRelationsAsync(Guid id)
    {
        try
        {
            Console.WriteLine($"=== GET BY ID WITH RELATIONS ===");
            Console.WriteLine($"Buscando usuario con ID: {id}");
            
            var user = await _context.Users
                .Include(u => u.Subjects)
                .Include(u => u.Groups)
                .Include(u => u.Grades)
                .Include(u => u.SchoolNavigation)
                .FirstOrDefaultAsync(u => u.Id == id);
                
            if (user != null)
            {
                Console.WriteLine($"Usuario encontrado: {user.Name} {user.LastName}");
                Console.WriteLine($"Email: {user.Email}");
                Console.WriteLine($"Celular Principal: {user.CellphonePrimary}");
                Console.WriteLine($"Celular Secundario: {user.CellphoneSecondary}");
                Console.WriteLine($"Subjects: {user.Subjects?.Count ?? 0}");
                Console.WriteLine($"Groups: {user.Groups?.Count ?? 0}");
            }
            else
            {
                Console.WriteLine("Usuario no encontrado");
            }
            
            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== ERROR EN GET BY ID WITH RELATIONS ===");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }

        public async Task<List<User>> GetAllAsync()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.SchoolId == null)
                return new List<User>();

            return await _context.Users
                .Where(u => u.SchoolId == currentUser.SchoolId)
                .ToListAsync();
        }

    public async Task<List<User>> GetAllWithAssignmentsByRoleAsync(string role)
    {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.SchoolId == null)
                return new List<User>();

        return await _context.Users
            .Where(u => u.Role == role)
                .Where(u => u.SchoolId == currentUser.SchoolId)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Subject)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Group)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.GradeLevel)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Area)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Specialty)
            .ToListAsync();
    }

        public async Task<List<User>> GetAllWithAssignmentsByRoleSA(string role)
        {
            return await _context.Users
                .Where(u => u.Role == role)                   
                .Include(u => u.TeacherAssignments)
                    .ThenInclude(ta => ta.SubjectAssignment)
                        .ThenInclude(sa => sa.Subject)
                .Include(u => u.TeacherAssignments)
                    .ThenInclude(ta => ta.SubjectAssignment)
                        .ThenInclude(sa => sa.Group)
                .Include(u => u.TeacherAssignments)
                    .ThenInclude(ta => ta.SubjectAssignment)
                        .ThenInclude(sa => sa.GradeLevel)
                .Include(u => u.TeacherAssignments)
                    .ThenInclude(ta => ta.SubjectAssignment)
                        .ThenInclude(sa => sa.Area)
                .Include(u => u.TeacherAssignments)
                    .ThenInclude(ta => ta.SubjectAssignment)
                        .ThenInclude(sa => sa.Specialty)
                .ToListAsync();
        }
        public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FindAsync(id);

    public async Task CreateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds, List<Guid> gradeLevelIds)
    {
        try
        {
                var currentUser = await _currentUserService.GetCurrentUserAsync();
                if (currentUser == null || currentUser.SchoolId == null)
                    throw new InvalidOperationException("No se puede crear el usuario porque no hay un usuario actual o no tiene un colegio asignado.");

                // Asignar el SchoolId del usuario actual
                user.SchoolId = currentUser.SchoolId;

            // La contraseña ya viene hasheada desde el controlador, no necesitamos hashearla de nuevo
            
            var subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
            var groups = await _context.Groups.Where(g => groupIds.Contains(g.Id)).ToListAsync();
            var grades = await _context.GradeLevels.Where(g => gradeLevelIds.Contains(g.Id)).ToListAsync();

            user.Subjects = subjects;
            user.Groups = groups;
            user.Grades = grades;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Error al crear el usuario y asignar relaciones.", ex);
        }
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }


    public async Task DeleteAsync(Guid id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = await _context.Users
                .Include(u => u.Subjects)
                .Include(u => u.Groups)
                .Include(u => u.Grades)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                throw new InvalidOperationException($"No se encontró el usuario con ID: {id}");

            if (!Enum.TryParse<UserRole>(user.Role, true, out var parsedRole))
                throw new InvalidOperationException($"Rol no válido o no soportado: {user.Role}");

            switch (parsedRole)
            {
                case UserRole.Admin:
                case UserRole.Director:
                case UserRole.Secretaria:
                case UserRole.Student:
                case UserRole.Estudiante:
                case UserRole.Teacher:
                    break;
                default:
                    throw new InvalidOperationException($"Rol no manejado: {parsedRole}");
            }

            await RemoveUserDeletionBlockersAsync(id, parsedRole);

            user.Subjects.Clear();
            user.Groups.Clear();
            user.Grades.Clear();

            await _context.SaveChangesAsync();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (DbUpdateException dbEx)
        {
            await transaction.RollbackAsync();
            _logger.LogError(dbEx, "[UserService] DeleteAsync DbUpdateException UserId={UserId}", id);
            throw new Exception("No se puede eliminar el usuario porque tiene dependencias en otras entidades.", dbEx);
        }
        catch (InvalidOperationException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[UserService] DeleteAsync error UserId={UserId}", id);
            throw new Exception("Error inesperado al eliminar el usuario.", ex);
        }
    }

    private async Task RemoveUserDeletionBlockersAsync(Guid userId, UserRole role)
    {
        var auditLogs = await _context.AuditLogs.Where(a => a.UserId == userId).ToListAsync();
        if (auditLogs.Count != 0)
            _context.AuditLogs.RemoveRange(auditLogs);

        await ClearMessagesForUserAsync(userId);

        var jobIds = await _context.EmailJobs.Where(j => j.CreatedByUserId == userId).Select(j => j.Id).ToListAsync();
        if (jobIds.Count != 0)
        {
            var queueForJobs = await _context.EmailQueues
                .Where(q => q.JobId != null && jobIds.Contains(q.JobId.Value))
                .ToListAsync();
            if (queueForJobs.Count != 0)
                _context.EmailQueues.RemoveRange(queueForJobs);
            var jobs = await _context.EmailJobs.Where(j => jobIds.Contains(j.Id)).ToListAsync();
            if (jobs.Count != 0)
                _context.EmailJobs.RemoveRange(jobs);
        }

        var counselorRows = await _context.CounselorAssignments.Where(c => c.UserId == userId).ToListAsync();
        if (counselorRows.Count != 0)
            _context.CounselorAssignments.RemoveRange(counselorRows);

        if (role is UserRole.Student or UserRole.Estudiante)
        {
            var spa = await _context.StudentPaymentAccesses.Where(x => x.StudentId == userId).ToListAsync();
            if (spa.Count != 0)
                _context.StudentPaymentAccesses.RemoveRange(spa);

            var sas = await _context.StudentAssignments.Where(sa => sa.StudentId == userId).ToListAsync();
            if (sas.Count != 0)
                _context.StudentAssignments.RemoveRange(sas);

            await DeletePrematriculationsForStudentAsync(userId);

            var scores = await _context.StudentActivityScores.Where(s => s.StudentId == userId).ToListAsync();
            if (scores.Count != 0)
                _context.StudentActivityScores.RemoveRange(scores);

            var discS = await _context.DisciplineReports.Where(d => d.StudentId == userId).ToListAsync();
            if (discS.Count != 0)
                _context.DisciplineReports.RemoveRange(discS);

            var oriS = await _context.OrientationReports.Where(o => o.StudentId == userId).ToListAsync();
            if (oriS.Count != 0)
                _context.OrientationReports.RemoveRange(oriS);

            var attS = await _context.Attendances.Where(a => a.StudentId == userId).ToListAsync();
            if (attS.Count != 0)
                _context.Attendances.RemoveRange(attS);
        }

        if (role == UserRole.Teacher)
        {
            var taIds = await _context.TeacherAssignments.Where(t => t.TeacherId == userId).Select(t => t.Id).ToListAsync();
            if (taIds.Count != 0)
            {
                var se = await _context.ScheduleEntries.Where(s => taIds.Contains(s.TeacherAssignmentId)).ToListAsync();
                if (se.Count != 0)
                    _context.ScheduleEntries.RemoveRange(se);
            }

            var tas = await _context.TeacherAssignments.Where(t => t.TeacherId == userId).ToListAsync();
            if (tas.Count != 0)
                _context.TeacherAssignments.RemoveRange(tas);

            var planIds = await _context.TeacherWorkPlans.Where(t => t.TeacherId == userId).Select(t => t.Id).ToListAsync();
            if (planIds.Count != 0)
            {
                var twpLogs = await _context.TeacherWorkPlanReviewLogs
                    .Where(l => planIds.Contains(l.TeacherWorkPlanId))
                    .ToListAsync();
                if (twpLogs.Count != 0)
                    _context.TeacherWorkPlanReviewLogs.RemoveRange(twpLogs);
                var twpDetails = await _context.TeacherWorkPlanDetails
                    .Where(d => planIds.Contains(d.TeacherWorkPlanId))
                    .ToListAsync();
                if (twpDetails.Count != 0)
                    _context.TeacherWorkPlanDetails.RemoveRange(twpDetails);
                var twps = await _context.TeacherWorkPlans.Where(t => planIds.Contains(t.Id)).ToListAsync();
                if (twps.Count != 0)
                    _context.TeacherWorkPlans.RemoveRange(twps);
            }

            var otherPerfLogs =
                await _context.TeacherWorkPlanReviewLogs.Where(l => l.PerformedByUserId == userId).ToListAsync();
            if (otherPerfLogs.Count != 0)
                _context.TeacherWorkPlanReviewLogs.RemoveRange(otherPerfLogs);

            var activities = await _context.Activities.Where(a => a.TeacherId == userId).ToListAsync();
            foreach (var act in activities)
            {
                var attch = await _context.ActivityAttachments.Where(x => x.ActivityId == act.Id).ToListAsync();
                if (attch.Count != 0)
                    _context.ActivityAttachments.RemoveRange(attch);
                var actScores = await _context.StudentActivityScores.Where(x => x.ActivityId == act.Id).ToListAsync();
                if (actScores.Count != 0)
                    _context.StudentActivityScores.RemoveRange(actScores);
            }

            if (activities.Count != 0)
                _context.Activities.RemoveRange(activities);

            var discT = await _context.DisciplineReports.Where(d => d.TeacherId == userId).ToListAsync();
            if (discT.Count != 0)
                _context.DisciplineReports.RemoveRange(discT);

            var oriT = await _context.OrientationReports.Where(o => o.TeacherId == userId).ToListAsync();
            if (oriT.Count != 0)
                _context.OrientationReports.RemoveRange(oriT);

            var attT = await _context.Attendances.Where(a => a.TeacherId == userId).ToListAsync();
            if (attT.Count != 0)
                _context.Attendances.RemoveRange(attT);
        }
    }

    private async Task ClearMessagesForUserAsync(Guid userId)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE messages SET parent_message_id = NULL
            WHERE parent_message_id IN (
                SELECT id FROM messages WHERE sender_id = {userId} OR recipient_id = {userId})");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM messages WHERE sender_id = {userId} OR recipient_id = {userId}");
    }

    private async Task DeletePrematriculationsForStudentAsync(Guid studentId)
    {
        var premIds = await _context.Prematriculations.Where(p => p.StudentId == studentId).Select(p => p.Id)
            .ToListAsync();
        if (premIds.Count == 0)
            return;

        var payments = await _context.Payments
            .Where(p => premIds.Contains(p.PrematriculationId))
            .ToListAsync();
        if (payments.Count != 0)
            _context.Payments.RemoveRange(payments);

        var hist =
            await _context.PrematriculationHistories.Where(h => premIds.Contains(h.PrematriculationId)).ToListAsync();
        if (hist.Count != 0)
            _context.PrematriculationHistories.RemoveRange(hist);

        var prems = await _context.Prematriculations.Where(p => premIds.Contains(p.Id)).ToListAsync();
        if (prems.Count != 0)
            _context.Prematriculations.RemoveRange(prems);
    }

    public async Task<User?> GetByRoleAndSchoolAsync(string role, Guid schoolId)
    {
        try
        {
            return await _context.Users
                .Where(u => u.Role.ToLower() == role.ToLower() && u.SchoolId == schoolId && u.Status == "active")
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario por rol {Role} y escuela {SchoolId}", role, schoolId);
            return null;
        }
    }
//public async Task DeleteAsync(Guid id)
//{
//    try
//    {
//        var user = await _context.Users
//            .Include(u => u.Subjects)
//            .Include(u => u.Groups)
//            .Include(u => u.Grades)
//            .FirstOrDefaultAsync(u => u.Id == id);

//        if (user == null)
//            throw new InvalidOperationException($"No se encontró el usuario con ID: {id}");

//        // Eliminar relaciones explícitas
//        //user.Subjects.Clear();
//        //user.Groups.Clear();
//        //user.Grades.Clear();

//        var role = user.Role.ToLower();


//        // Eliminar asignaciones de profesor
//        var assignments = await _context.TeacherAssignments
//            .Where(ta => ta.TeacherId == id)
//            .ToListAsync();

//        _context.TeacherAssignments.RemoveRange(assignments);

//        await _context.SaveChangesAsync();

//        // Eliminar el usuario
//        _context.Users.Remove(user);
//        await _context.SaveChangesAsync();
//    }
//    catch (DbUpdateException dbEx)
//    {
//        throw new Exception("No se puede eliminar el usuario porque tiene dependencias en otras entidades (como asignaciones de docentes).", dbEx);
//    }
//    catch (Exception ex)
//    {
//        throw new Exception("Error inesperado al eliminar el usuario.", ex);
//    }
//}


public async Task<User?> AuthenticateAsync(string email, string password)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password);
    }
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower().Trim() == email.ToLower().Trim());
    }

    public async Task<(bool success, string message)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        Console.WriteLine($"[UserService.ChangePasswordAsync] Iniciando para userId: {userId}");
        Console.WriteLine($"[UserService.ChangePasswordAsync] currentPassword length: {currentPassword?.Length ?? 0}");
        Console.WriteLine($"[UserService.ChangePasswordAsync] newPassword length: {newPassword?.Length ?? 0}");
        
        try
        {
            var user = await _context.Users.FindAsync(userId);
            Console.WriteLine($"[UserService.ChangePasswordAsync] Usuario encontrado: {(user != null ? $"ID={user.Id}, Email={user.Email}" : "NULL")}");
            
            if (user == null)
            {
                Console.WriteLine($"[UserService.ChangePasswordAsync] ERROR: Usuario no encontrado");
                return (false, "Usuario no encontrado");
            }

            Console.WriteLine($"[UserService.ChangePasswordAsync] PasswordHash actual: {user.PasswordHash?.Substring(0, Math.Min(20, user.PasswordHash?.Length ?? 0))}...");
            Console.WriteLine($"[UserService.ChangePasswordAsync] IsPasswordHashed: {IsPasswordHashed(user.PasswordHash)}");

            // Verificar contraseña actual
            bool currentPasswordValid = false;
            if (IsPasswordHashed(user.PasswordHash))
            {
                Console.WriteLine($"[UserService.ChangePasswordAsync] Verificando contraseña hasheada con BCrypt");
                currentPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
            }
            else
            {
                Console.WriteLine($"[UserService.ChangePasswordAsync] Verificando contraseña en texto plano");
                currentPasswordValid = currentPassword == user.PasswordHash;
            }

            Console.WriteLine($"[UserService.ChangePasswordAsync] Contraseña actual válida: {currentPasswordValid}");

            if (!currentPasswordValid)
            {
                Console.WriteLine($"[UserService.ChangePasswordAsync] ERROR: Contraseña actual incorrecta");
                return (false, "La contraseña actual que ingresaste no es correcta. Por favor, verifica e intenta nuevamente.");
            }

            // Validar nueva contraseña
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                Console.WriteLine($"[UserService.ChangePasswordAsync] ERROR: Nueva contraseña inválida (length: {newPassword?.Length ?? 0})");
                return (false, "La nueva contraseña debe tener al menos 8 caracteres");
            }

            // Verificar que la nueva contraseña sea diferente a la actual
            if (currentPassword == newPassword)
            {
                Console.WriteLine($"[UserService.ChangePasswordAsync] ERROR: Nueva contraseña igual a la actual");
                return (false, "La nueva contraseña debe ser diferente a la actual");
            }

            Console.WriteLine($"[UserService.ChangePasswordAsync] Hasheando nueva contraseña...");
            // Hashear y actualizar contraseña
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            Console.WriteLine($"[UserService.ChangePasswordAsync] Guardando cambios en la base de datos...");
            await _context.SaveChangesAsync();
            Console.WriteLine($"[UserService.ChangePasswordAsync] SUCCESS: Contraseña actualizada exitosamente");
            return (true, "Contraseña actualizada exitosamente");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserService.ChangePasswordAsync] EXCEPCIÓN: {ex.Message}");
            Console.WriteLine($"[UserService.ChangePasswordAsync] StackTrace: {ex.StackTrace}");
            return (false, $"Error inesperado al cambiar la contraseña: {ex.Message}");
        }
    }

    private bool IsPasswordHashed(string passwordHash)
    {
        // BCrypt hashes start with $2a$, $2b$, or $2y$
        return passwordHash.StartsWith("$2") && passwordHash.Length > 20;
    }
}
}
