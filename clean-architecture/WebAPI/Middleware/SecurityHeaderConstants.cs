namespace WebAPI.Middleware;

/// <summary>
/// Centralizes security-related HTTP header names and values so they are
/// easy to audit and do not appear as magic strings throughout the codebase.
/// </summary>
public static class SecurityHeaderConstants
{
    public const string XContentTypeOptionsHeaderName = "X-Content-Type-Options";
    public const string XContentTypeOptionsNoSniffValue = "nosniff";

    public const string XFrameOptionsHeaderName = "X-Frame-Options";
    public const string XFrameOptionsDenyValue = "DENY";

    public const string ReferrerPolicyHeaderName = "Referrer-Policy";
    public const string ReferrerPolicyStrictOriginWhenCrossOriginValue = "strict-origin-when-cross-origin";

    public const string ContentSecurityPolicyHeaderName = "Content-Security-Policy";
    public const string ContentSecurityPolicyDefaultSelfValue = "default-src 'self';";
}
