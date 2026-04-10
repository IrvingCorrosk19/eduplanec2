using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

public class ScheduleService : IScheduleService
{
    private readonly SchoolDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ScheduleService(SchoolDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ScheduleEntry> CreateEntryAsync(
        Guid teacherAssignmentId,
        Guid timeSlotId,
        byte dayOfWeek,
        Guid academicYearId,
        Guid currentUserId)
    {
        if (dayOfWeek < 1 || dayOfWeek > 7)
            throw new ArgumentException("DayOfWeek debe estar entre 1 (Lunes) y 7 (Domingo).", nameof(dayOfWeek));

        var ta = await _context.TeacherAssignments
            .Include(t => t.SubjectAssignment)
            .Include(t => t.Teacher)
            .FirstOrDefaultAsync(t => t.Id == teacherAssignmentId)
            .ConfigureAwait(false);

        if (ta == null)
            throw new InvalidOperationException("No se encontró la asignación docente indicada.");

        var timeSlot = await _context.TimeSlots.FindAsync(timeSlotId).ConfigureAwait(false);
        if (timeSlot == null)
            throw new InvalidOperationException("No se encontró el bloque horario indicado.");

        var academicYear = await _context.AcademicYears.FindAsync(academicYearId).ConfigureAwait(false);
        if (academicYear == null)
            throw new InvalidOperationException("No se encontró el año académico indicado.");

        // C) Seguridad: Teacher solo puede crear horarios de sus propias TeacherAssignments
        var role = await _currentUserService.GetCurrentUserRoleAsync().ConfigureAwait(false);
        var isTeacher = string.Equals(role, "teacher", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(role, "docente", StringComparison.OrdinalIgnoreCase);
        if (isTeacher && ta.TeacherId != currentUserId)
            throw new UnauthorizedAccessException("Solo puede asignar horarios a sus propias materias. La asignación docente no le pertenece.");

        // A) Conflicto docente: mismo docente no puede tener mismo año + día + bloque
        var teacherConflict = await _context.ScheduleEntries
            .Include(e => e.TeacherAssignment)
            .AnyAsync(e =>
                e.AcademicYearId == academicYearId &&
                e.DayOfWeek == dayOfWeek &&
                e.TimeSlotId == timeSlotId &&
                e.TeacherAssignment.TeacherId == ta.TeacherId,
                CancellationToken.None)
            .ConfigureAwait(false);
        if (teacherConflict)
            throw new InvalidOperationException(
                "Conflicto de horario: el docente ya tiene una clase asignada en el mismo día y bloque para este año académico.");

        // B) Conflicto grupo: mismo grupo no puede tener mismo año + día + bloque (vía otra TeacherAssignment -> mismo GroupId)
        var groupId = ta.SubjectAssignment.GroupId;
        var groupConflict = await _context.ScheduleEntries
            .Include(e => e.TeacherAssignment)
                .ThenInclude(t => t!.SubjectAssignment)
            .AnyAsync(e =>
                e.AcademicYearId == academicYearId &&
                e.DayOfWeek == dayOfWeek &&
                e.TimeSlotId == timeSlotId &&
                e.TeacherAssignment.SubjectAssignment.GroupId == groupId,
                CancellationToken.None)
            .ConfigureAwait(false);
        if (groupConflict)
            throw new InvalidOperationException(
                "Conflicto de horario: el grupo ya tiene una clase asignada en el mismo día y bloque para este año académico.");

        var entry = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            TeacherAssignmentId = teacherAssignmentId,
            TimeSlotId = timeSlotId,
            DayOfWeek = dayOfWeek,
            AcademicYearId = academicYearId,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ScheduleEntries.Add(entry);
        await _context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        return await _context.ScheduleEntries
            .Include(e => e.TeacherAssignment)
                .ThenInclude(t => t!.SubjectAssignment)
                    .ThenInclude(s => s!.Subject)
            .Include(e => e.TeacherAssignment)
                .ThenInclude(t => t!.Teacher)
            .Include(e => e.TimeSlot)
            .Include(e => e.AcademicYear)
            .FirstAsync(e => e.Id == entry.Id, CancellationToken.None)
            .ConfigureAwait(false);
    }

    public async Task DeleteEntryAsync(Guid id, Guid currentUserId)
    {
        var entry = await _context.ScheduleEntries
            .Include(e => e.TeacherAssignment)
            .FirstOrDefaultAsync(e => e.Id == id, CancellationToken.None)
            .ConfigureAwait(false);

        if (entry == null)
            throw new InvalidOperationException("No se encontró la entrada de horario indicada.");

        var role = await _currentUserService.GetCurrentUserRoleAsync().ConfigureAwait(false);
        var isTeacher = string.Equals(role, "teacher", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(role, "docente", StringComparison.OrdinalIgnoreCase);
        if (isTeacher && entry.TeacherAssignment.TeacherId != currentUserId)
            throw new UnauthorizedAccessException("Solo puede eliminar horarios de sus propias asignaciones.");

        _context.ScheduleEntries.Remove(entry);
        await _context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public async Task<List<ScheduleEntry>> GetByTeacherAsync(Guid teacherId, Guid academicYearId)
    {
        return await _context.ScheduleEntries
            .Include(e => e.TeacherAssignment)
                .ThenInclude(t => t!.SubjectAssignment)
                    .ThenInclude(s => s!.Subject)
            .Include(e => e.TeacherAssignment)
                .ThenInclude(t => t!.SubjectAssignment)
                    .ThenInclude(s => s!.Group)
            .Include(e => e.TeacherAssignment)
                .ThenInclude(t => t!.Teacher)
            .Include(e => e.TimeSlot)
            .Include(e => e.AcademicYear)
            .Where(e => e.TeacherAssignment.TeacherId == teacherId && e.AcademicYearId == academicYearId)
            .OrderBy(e => e.DayOfWeek)
            .ThenBy(e => e.TimeSlot.DisplayOrder)
            .ToListAsync(CancellationToken.None)
            .ConfigureAwait(false);
    }

    public async Task<List<ScheduleEntry>> GetByGroupAsync(Guid groupId, Guid academicYearId)
    {
        return await _context.ScheduleEntries
            .Include(e => e.TeacherAssignment)
                .ThenInclude(t => t!.SubjectAssignment)
                    .ThenInclude(s => s!.Subject)
            .Include(e => e.TeacherAssignment)
                .ThenInclude(t => t!.SubjectAssignment)
                    .ThenInclude(s => s!.Group)
            .Include(e => e.TeacherAssignment)
                .ThenInclude(t => t!.Teacher)
            .Include(e => e.TimeSlot)
            .Include(e => e.AcademicYear)
            .Where(e =>
                e.TeacherAssignment.SubjectAssignment.GroupId == groupId &&
                e.AcademicYearId == academicYearId)
            .OrderBy(e => e.DayOfWeek)
            .ThenBy(e => e.TimeSlot.DisplayOrder)
            .ToListAsync(CancellationToken.None)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<ScheduleEntry>> GetByStudentUserAsync(Guid studentUserId, Guid academicYearId)
    {
        var userSchoolId = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == studentUserId)
            .Select(u => u.SchoolId)
            .FirstOrDefaultAsync(CancellationToken.None)
            .ConfigureAwait(false);

        if (userSchoolId == null || userSchoolId == Guid.Empty)
            return new List<ScheduleEntry>();

        var assignment = await _context.StudentAssignments
            .AsNoTracking()
            .Include(sa => sa.Group)
            .Where(sa =>
                sa.StudentId == studentUserId &&
                sa.IsActive &&
                (sa.AcademicYearId == academicYearId || sa.AcademicYearId == null))
            .OrderByDescending(sa => sa.AcademicYearId != null)
            .ThenByDescending(sa => sa.CreatedAt)
            .FirstOrDefaultAsync(CancellationToken.None)
            .ConfigureAwait(false);

        if (assignment == null || assignment.Group == null)
            return new List<ScheduleEntry>();

        if (assignment.Group.SchoolId != userSchoolId)
            return new List<ScheduleEntry>();

        return await GetByGroupAsync(assignment.GroupId, academicYearId).ConfigureAwait(false);
    }
}
