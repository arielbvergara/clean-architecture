using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebAPI.Authorization;

/// <summary>
/// Authorization handler for the <see cref="AdminOnlyRequirement"/> that:
/// - Allows access when the current principal is an administrator.
/// - Denies access when the principal is authenticated but not an admin,
///   and emits a security event that can be surfaced to observability
///   providers (for example, Sentry) via ISecurityEventNotifier.
///
/// Unauthenticated callers are handled by the fallback policy and are not
/// logged as admin-endpoint access denials.
/// </summary>
public sealed class AdminOnlyHandler(
    ILogger<AdminOnlyHandler> logger,
    ISecurityEventNotifier securityEventNotifier)
    : AuthorizationHandler<AdminOnlyRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminOnlyRequirement requirement)
    {
        // If there is no authenticated identity, let the fallback policy
        // produce a 401 response without emitting an admin-access event.
        if (context.User?.Identity is not { IsAuthenticated: true })
        {
            return;
        }

        if (context.User.IsAdmin())
        {
            context.Succeed(requirement);
            return;
        }

        // At this point we have an authenticated principal that is not an
        // administrator attempting to access an AdminOnly endpoint.
        var httpContext = GetHttpContext(context.Resource);
        var correlationId = httpContext?.TraceIdentifier;
        var route = httpContext?.Request.Path.Value;
        var subjectId = context.User.GetExternalAuthId() ?? context.User.Identity?.Name;

        logger.LogWarning(
            "Admin-only endpoint access denied for subject {SubjectId} on route {Route} with correlation {CorrelationId}",
            subjectId ?? "<unknown>",
            route ?? "<unknown>",
            correlationId ?? "<none>");

        var properties = new Dictionary<string, string?>
        {
            ["Route"] = route,
            ["AuthenticationType"] = context.User.Identity?.AuthenticationType
        };

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.AdminEndpointAccessDenied,
            subjectId,
            SecurityEventOutcomes.Failure,
            correlationId,
            properties,
            CancellationToken.None);

        // Do not call context.Succeed here; by leaving the requirement
        // unsatisfied, the framework will return 403 Forbidden for this
        // authenticated non-admin principal.
    }

    private static HttpContext? GetHttpContext(object? resource)
    {
        switch (resource)
        {
            case HttpContext httpContext:
                return httpContext;
            case AuthorizationFilterContext filterContext:
                return filterContext.HttpContext;
            default:
                return null;
        }
    }
}
