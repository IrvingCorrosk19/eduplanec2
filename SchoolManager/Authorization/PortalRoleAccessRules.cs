using Microsoft.AspNetCore.Http;

namespace SchoolManager.Authorization;

/// <summary>
/// Reglas de navegación y acceso por rol en el portal (_AdminLayout).
/// Mensajería: todos los autenticados. /User: solo admin y director.
/// </summary>
public static class PortalRoleAccessRules
{
    public enum PortalMenuProfile
    {
        Full,
        MessagingOnly,
        ClubParentsAndMessaging
    }

    private static readonly HashSet<string> MessagingOnlyRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "teacher", "docente", "student", "estudiante"
    };

    private static readonly HashSet<string> UserManagementRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "director"
    };

    private static readonly HashSet<string> AllowedFileActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "GetSchoolLogo"
    };

    private static readonly HashSet<string> AllowedAuthActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Login", "Logout", "AccessDenied"
    };

    /// <summary>Estudiante con plataforma pendiente (redirección del filtro existente).</summary>
    private static readonly HashSet<string> AllowedStudentPlatformActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "AccessPending"
    };

    public static bool IsSuperAdminRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;
        var r = role.Trim();
        return string.Equals(r, "superadmin", StringComparison.OrdinalIgnoreCase)
               || string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSuperAdminPath(PathString path)
    {
        var value = NormalizePath(path.Value);
        return value.StartsWith("/superadmin", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanAccessUserManagement(string? role) =>
        !string.IsNullOrWhiteSpace(role) && UserManagementRoles.Contains(role.Trim());

    public static PortalMenuProfile GetMenuProfile(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return PortalMenuProfile.Full;
        var r = role.Trim();
        if (string.Equals(r, "clubparentsadmin", StringComparison.OrdinalIgnoreCase))
            return PortalMenuProfile.ClubParentsAndMessaging;
        if (MessagingOnlyRoles.Contains(r))
            return PortalMenuProfile.MessagingOnly;
        return PortalMenuProfile.Full;
    }

    public static bool UsesRestrictedSidebar(string? role) =>
        GetMenuProfile(role) != PortalMenuProfile.Full;

    public static bool ShouldHandleAccessDeniedWithoutRedirect(string? role) =>
        UsesRestrictedSidebar(role);

    /// <summary>
    /// Roles con menú reducido: el filtro valida rutas permitidas.
    /// admin, director y demás roles de gestión no pasan por aquí.
    /// </summary>
    public static bool RequiresPortalRouteRestriction(string? role) =>
        UsesRestrictedSidebar(role);

    public static bool IsAllowed(string? role, string? controller, string? action, PathString path)
    {
        var controllerName = controller?.Trim() ?? string.Empty;
        var actionName = action?.Trim() ?? string.Empty;
        var pathValue = NormalizePath(path.Value);

        if (string.Equals(controllerName, "Messaging", StringComparison.OrdinalIgnoreCase))
            return true;

        if (pathValue.StartsWith("/messaging", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(controllerName, "User", StringComparison.OrdinalIgnoreCase)
            || pathValue.StartsWith("/user", StringComparison.OrdinalIgnoreCase))
            return CanAccessUserManagement(role);

        var profile = GetMenuProfile(role);

        if (profile == PortalMenuProfile.Full)
            return true;

        if (string.Equals(controllerName, "Auth", StringComparison.OrdinalIgnoreCase)
            && AllowedAuthActions.Contains(actionName))
            return true;

        if (string.Equals(controllerName, "File", StringComparison.OrdinalIgnoreCase)
            && AllowedFileActions.Contains(actionName))
            return true;

        if (profile == PortalMenuProfile.MessagingOnly)
        {
            if (string.Equals(controllerName, "Student", StringComparison.OrdinalIgnoreCase)
                && AllowedStudentPlatformActions.Contains(actionName))
                return true;
            return false;
        }

        if (profile == PortalMenuProfile.ClubParentsAndMessaging)
        {
            if (pathValue.StartsWith("/clubparents", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(controllerName, "ClubParents", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        return false;
    }

    public static string GetDefaultLandingPath(string? role)
    {
        return GetMenuProfile(role) switch
        {
            PortalMenuProfile.ClubParentsAndMessaging => "/ClubParents/Students",
            PortalMenuProfile.MessagingOnly => "/Messaging/Inbox",
            _ => "/Home/Index"
        };
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var normalized = path.Replace('\\', '/').Trim();
        if (normalized.Length > 0 && !normalized.StartsWith('/'))
            normalized = "/" + normalized;
        return normalized;
    }
}
