using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SchoolManager.Authorization;

namespace SchoolManager.Filters;

/// <summary>
/// Restringe rutas según perfil de menú (mensajería solo, club de padres, etc.).
/// Mensajería permitida para todos; /User solo admin y director.
/// </summary>
public class PortalRoleAccessFilter : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        if (user.Identity?.IsAuthenticated != true)
            return next();

        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        if (PortalRoleAccessRules.IsSuperAdminRole(role)
            || PortalRoleAccessRules.IsSuperAdminPath(httpContext.Request.Path))
            return next();

        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();

        if (PortalRoleAccessRules.IsAllowed(role, controller, action, httpContext.Request.Path))
            return next();

        context.Result = new ViewResult
        {
            ViewName = "~/Views/Auth/AccessDenied.cshtml",
            StatusCode = StatusCodes.Status403Forbidden
        };

        return Task.CompletedTask;
    }
}
