using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SchoolManager.Authorization;

namespace SchoolManager.Filters;

/// <summary>
/// Restringe al rol <c>clubparentsadmin</c> a Mensajería, User/Index (y APIs que usa) y ClubParents.
/// Devuelve HTTP 403 con la vista AccessDenied, sin redirección.
/// SuperAdmin y rutas /SuperAdmin no se evalúan.
/// </summary>
public class ClubParentsAdminAccessFilter : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        if (user.Identity?.IsAuthenticated != true)
            return next();

        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        if (ClubParentsAdminAccessRules.IsSuperAdminRole(role)
            || ClubParentsAdminAccessRules.IsSuperAdminPath(httpContext.Request.Path))
            return next();

        if (!ClubParentsAdminAccessRules.IsRestrictedRole(role))
            return next();

        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();

        if (ClubParentsAdminAccessRules.IsAllowed(controller, action, httpContext.Request.Path))
            return next();

        context.Result = new ViewResult
        {
            ViewName = "~/Views/Auth/AccessDenied.cshtml",
            StatusCode = StatusCodes.Status403Forbidden
        };

        return Task.CompletedTask;
    }
}
