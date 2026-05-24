using Microsoft.AspNetCore.Http;

namespace SchoolManager.Authorization;

/// <summary>
/// Rutas permitidas para el rol <c>clubparentsadmin</c>.
/// SuperAdmin y el resto de roles no aplican estas reglas.
/// </summary>
public static class ClubParentsAdminAccessRules
{
    public const string RoleName = "clubparentsadmin";

    private static readonly HashSet<string> AllowedUserActions = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(UserActions.Index),
        nameof(UserActions.GetUserJson),
        nameof(UserActions.CreateJson),
        nameof(UserActions.UpdateJson),
        nameof(UserActions.Delete),
        nameof(UserActions.DeleteConfirmed),
        nameof(UserActions.UpdatePhoto),
        nameof(UserActions.RemovePhoto),
        nameof(UserActions.SendPasswordEmail),
    };

    private static readonly HashSet<string> AllowedFileActions = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(FileActions.GetSchoolLogo),
    };

    private static readonly HashSet<string> AllowedAuthActions = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(AuthActions.Login),
        nameof(AuthActions.Logout),
        nameof(AuthActions.AccessDenied),
    };

    public static bool IsRestrictedRole(string? role) =>
        string.Equals(role?.Trim(), RoleName, StringComparison.OrdinalIgnoreCase);

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

    public static bool IsAllowed(string? controller, string? action, PathString path)
    {
        var controllerName = controller?.Trim() ?? string.Empty;
        var actionName = action?.Trim() ?? string.Empty;
        var pathValue = NormalizePath(path.Value);

        if (pathValue.StartsWith("/clubparents", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(controllerName, "Messaging", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(controllerName, "ClubParents", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(controllerName, "User", StringComparison.OrdinalIgnoreCase)
            && AllowedUserActions.Contains(actionName))
            return true;

        if (string.Equals(controllerName, "File", StringComparison.OrdinalIgnoreCase)
            && AllowedFileActions.Contains(actionName))
            return true;

        if (string.Equals(controllerName, "Auth", StringComparison.OrdinalIgnoreCase)
            && AllowedAuthActions.Contains(actionName))
            return true;

        return false;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var normalized = path.Replace('\\', '/').Trim();
        if (normalized.Length > 0 && !normalized.StartsWith('/'))
            normalized = "/" + normalized;
        return normalized;
    }

    // Nombres de acciones referenciados sin acoplar a controladores concretos en compilación cruzada.
    private static class UserActions
    {
        public const string Index = "Index";
        public const string GetUserJson = "GetUserJson";
        public const string CreateJson = "CreateJson";
        public const string UpdateJson = "UpdateJson";
        public const string Delete = "Delete";
        public const string DeleteConfirmed = "DeleteConfirmed";
        public const string UpdatePhoto = "UpdatePhoto";
        public const string RemovePhoto = "RemovePhoto";
        public const string SendPasswordEmail = "SendPasswordEmail";
    }

    private static class FileActions
    {
        public const string GetSchoolLogo = "GetSchoolLogo";
    }

    private static class AuthActions
    {
        public const string Login = "Login";
        public const string Logout = "Logout";
        public const string AccessDenied = "AccessDenied";
    }
}
