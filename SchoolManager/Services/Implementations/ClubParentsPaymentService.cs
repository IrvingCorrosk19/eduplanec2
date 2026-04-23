using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using UserEntity = SchoolManager.Models.User;

namespace SchoolManager.Services.Implementations;

/// <summary>Implementación del servicio Club de Padres. No modifica StudentIdCardService, PaymentService ni AuthService.</summary>
public class ClubParentsPaymentService : IClubParentsPaymentService
{
    private readonly SchoolDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ClubParentsPaymentService> _logger;

    private const string CarnetPendiente = "Pendiente";
    private const string CarnetPagado = "Pagado";
    private const string PlatformPendiente = "Pendiente";
    private const string PlatformActivo = "Activo";

    public ClubParentsPaymentService(
        SchoolDbContext context,
        ICurrentUserService currentUserService,
        ILogger<ClubParentsPaymentService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClubParentsStudentDto>> GetStudentsAsync(
        Guid? gradeId = null,
        Guid? groupId = null,
        string? carnetStatus = null,
        string? platformStatus = null,
        string? search = null,
        string? shift = null,
        string? cedula = null)
    {
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null)
        {
            _logger.LogWarning("[ClubParents] GetStudentsAsync: usuario sin escuela.");
            return Array.Empty<ClubParentsStudentDto>();
        }

        var schoolId = school.Id;
        var carnetFilter = string.IsNullOrWhiteSpace(carnetStatus) ? null : carnetStatus.Trim();
        var platformFilter = string.IsNullOrWhiteSpace(platformStatus) ? null : platformStatus.Trim();
        var searchTerm = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var shiftFilter = string.IsNullOrWhiteSpace(shift) ? null : shift.Trim();
        var cedulaFilter = string.IsNullOrWhiteSpace(cedula) ? null : cedula.Trim();

        _logger.LogInformation("[ClubParents] GetStudentsAsync querying Users: Role IN (student,estudiante) AND (User.SchoolId={SchoolId} OR asignación activa al colegio)", schoolId);

        IQueryable<UserEntity> userQuery = _context.Users
            .Where(u => u.Role != null && (u.Role.ToLower() == "student" || u.Role.ToLower() == "estudiante"))
            .Where(u => u.SchoolId == schoolId
                || u.StudentAssignments.Any(sa => sa.IsActive && sa.Grade.SchoolId == schoolId));

        if (searchTerm != null)
        {
            var likePattern = "%" + searchTerm + "%";
            var digitsOnly = new string(searchTerm.Where(char.IsDigit).ToArray());
            userQuery = userQuery.Where(u =>
                EF.Functions.ILike(u.Name, likePattern)
                || EF.Functions.ILike(u.LastName, likePattern)
                || EF.Functions.ILike(u.Name + " " + u.LastName, likePattern)
                || EF.Functions.ILike(u.Email, likePattern)
                || (u.DocumentId != null && EF.Functions.ILike(u.DocumentId, likePattern))
                || (digitsOnly.Length > 0 && u.DocumentId != null
                    && u.DocumentId.Replace(".", "").Replace("-", "").Replace(" ", "")
                        .Contains(digitsOnly)));
        }

        if (cedulaFilter != null)
        {
            var likeCed = "%" + cedulaFilter + "%";
            var digitsOnlyCed = new string(cedulaFilter.Where(char.IsDigit).ToArray());
            userQuery = userQuery.Where(u =>
                u.DocumentId != null
                && (EF.Functions.ILike(u.DocumentId, likeCed)
                    || (digitsOnlyCed.Length > 0
                        && u.DocumentId.Replace(".", "").Replace("-", "").Replace(" ", "").Contains(digitsOnlyCed))));
        }

        if (shiftFilter != null)
            userQuery = userQuery.Where(u => u.Shift == shiftFilter);

        if (gradeId.HasValue || groupId.HasValue)
        {
            userQuery = userQuery.Where(u => u.StudentAssignments.Any(sa =>
                sa.IsActive
                && (!gradeId.HasValue || sa.GradeId == gradeId.Value)
                && (!groupId.HasValue || sa.GroupId == groupId.Value)));
        }

        var accessesForSchool = _context.StudentPaymentAccesses.Where(a => a.SchoolId == schoolId);

        var joined = from u in userQuery
            join spa in accessesForSchool on u.Id equals spa.StudentId into spaGroup
            from spa in spaGroup.DefaultIfEmpty()
            select new { u, spa };

        if (carnetFilter != null)
        {
            joined = joined.Where(x => (x.spa == null ? CarnetPendiente : x.spa.CarnetStatus) == carnetFilter);
        }

        if (platformFilter != null)
        {
            joined = joined.Where(x => (x.spa == null ? PlatformPendiente : x.spa.PlatformAccessStatus) == platformFilter);
        }

        var list = await joined
            .Select(x => new ClubParentsStudentDto
            {
                Id = x.u.Id,
                FullName = (x.u.Name + " " + x.u.LastName) ?? "",
                DocumentId = x.u.DocumentId,
                Grade = x.u.StudentAssignments.Where(sa => sa.IsActive).Select(sa => sa.Grade.Name).FirstOrDefault() ?? "Sin asignar",
                Group = x.u.StudentAssignments.Where(sa => sa.IsActive).Select(sa => sa.Group.Name).FirstOrDefault() ?? "Sin asignar",
                CarnetStatus = x.spa == null ? CarnetPendiente : x.spa.CarnetStatus,
                PlatformAccessStatus = x.spa == null ? PlatformPendiente : x.spa.PlatformAccessStatus
            })
            .OrderBy(x => x.FullName)
            .ToListAsync();

        _logger.LogInformation("[ClubParents] GetStudentsAsync found {Count} users for SchoolId={SchoolId}", list.Count, schoolId);

        return list;
    }

    /// <inheritdoc />
    public async Task<StudentPaymentStatusDto> GetStudentPaymentStatusAsync(Guid studentId)
    {
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null)
            return new StudentPaymentStatusDto { StudentId = studentId, CarnetStatus = CarnetPendiente, PlatformAccessStatus = PlatformPendiente };

        var access = await _context.StudentPaymentAccesses
            .FirstOrDefaultAsync(a => a.StudentId == studentId && a.SchoolId == school.Id);

        if (access == null)
            return new StudentPaymentStatusDto
            {
                StudentId = studentId,
                CarnetStatus = CarnetPendiente,
                PlatformAccessStatus = PlatformPendiente
            };

        return new StudentPaymentStatusDto
        {
            StudentId = studentId,
            CarnetStatus = access.CarnetStatus,
            PlatformAccessStatus = access.PlatformAccessStatus,
            CarnetStatusUpdatedAt = access.CarnetStatusUpdatedAt,
            PlatformStatusUpdatedAt = access.PlatformStatusUpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task MarkCarnetAsPaidAsync(Guid studentId)
    {
        var userId = await _currentUserService.GetCurrentUserIdAsync();
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null)
            throw new InvalidOperationException("Usuario sin escuela asignada.");

        var student = await FindStudentInSchoolAsync(studentId, school.Id);
        if (student == null)
            throw new InvalidOperationException("Estudiante no encontrado o no pertenece a su escuela.");

        var access = await _context.StudentPaymentAccesses
            .FirstOrDefaultAsync(a => a.StudentId == studentId && a.SchoolId == school.Id);

        if (access == null)
        {
            access = new StudentPaymentAccess
            {
                StudentId = studentId,
                SchoolId = school.Id,
                CarnetStatus = CarnetPendiente,
                PlatformAccessStatus = PlatformPendiente,
                CreatedAt = DateTime.UtcNow
            };
            _context.StudentPaymentAccesses.Add(access);
        }

        if (access.CarnetStatus != CarnetPendiente)
            throw new InvalidOperationException($"Solo se puede marcar como Pagado desde estado Pendiente. Estado actual: {access.CarnetStatus}.");

        var now = DateTime.UtcNow;
        access.CarnetStatus = CarnetPagado;
        access.CarnetStatusUpdatedAt = now;
        access.CarnetUpdatedByUserId = userId;
        access.UpdatedAt = now;

        await _context.SaveChangesAsync();
        _logger.LogInformation("[ClubParents] Carnet marcado como Pagado StudentId={StudentId} por UserId={UserId}", studentId, userId);
    }

    /// <inheritdoc />
    public async Task ActivatePlatformAsync(Guid studentId)
    {
        var userId = await _currentUserService.GetCurrentUserIdAsync();
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null)
            throw new InvalidOperationException("Usuario sin escuela asignada.");

        var student = await FindStudentInSchoolAsync(studentId, school.Id);
        if (student == null)
            throw new InvalidOperationException("Estudiante no encontrado o no pertenece a su escuela.");

        var access = await _context.StudentPaymentAccesses
            .FirstOrDefaultAsync(a => a.StudentId == studentId && a.SchoolId == school.Id);

        if (access == null)
        {
            access = new StudentPaymentAccess
            {
                StudentId = studentId,
                SchoolId = school.Id,
                CarnetStatus = CarnetPendiente,
                PlatformAccessStatus = PlatformPendiente,
                CreatedAt = DateTime.UtcNow
            };
            _context.StudentPaymentAccesses.Add(access);
        }

        if (access.PlatformAccessStatus != PlatformPendiente)
            throw new InvalidOperationException($"Solo se puede activar plataforma desde estado Pendiente. Estado actual: {access.PlatformAccessStatus}.");

        var now = DateTime.UtcNow;
        access.PlatformAccessStatus = PlatformActivo;
        access.PlatformStatusUpdatedAt = now;
        access.PlatformUpdatedByUserId = userId;
        access.UpdatedAt = now;

        await _context.SaveChangesAsync();
        _logger.LogInformation("[ClubParents] Plataforma activada StudentId={StudentId} por UserId={UserId}", studentId, userId);
    }

    private Task<User?> FindStudentInSchoolAsync(Guid studentId, Guid schoolId) =>
        _context.Users.FirstOrDefaultAsync(u =>
            u.Id == studentId
            && u.Role != null
            && (u.Role.ToLower() == "student" || u.Role.ToLower() == "estudiante")
            && (u.SchoolId == schoolId
                || u.StudentAssignments.Any(sa => sa.IsActive && sa.Grade.SchoolId == schoolId)));
}
