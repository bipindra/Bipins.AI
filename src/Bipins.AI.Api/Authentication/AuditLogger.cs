using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bipins.AI.Api.Authentication;

/// <summary>
/// Service for logging authentication and authorization events for audit purposes.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an authentication event.
    /// </summary>
    void LogAuthentication(string userId, string tenantId, string authenticationMethod, bool success, string? reason = null);

    /// <summary>
    /// Logs an authorization event.
    /// </summary>
    void LogAuthorization(string userId, string tenantId, string resource, string action, bool success, string? reason = null);

    /// <summary>
    /// Logs a tenant access event.
    /// </summary>
    void LogTenantAccess(string userId, string tenantId, string action, bool success, string? reason = null);
}

/// <summary>
/// Default implementation of audit logger using ILogger.
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogger"/> class.
    /// </summary>
    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void LogAuthentication(string userId, string tenantId, string authenticationMethod, bool success, string? reason = null)
    {
        var level = success ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(
            level,
            "Authentication: User={UserId}, Tenant={TenantId}, Method={Method}, Success={Success}, Reason={Reason}",
            userId,
            tenantId,
            authenticationMethod,
            success,
            reason ?? "N/A");
    }

    /// <inheritdoc />
    public void LogAuthorization(string userId, string tenantId, string resource, string action, bool success, string? reason = null)
    {
        var level = success ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(
            level,
            "Authorization: User={UserId}, Tenant={TenantId}, Resource={Resource}, Action={Action}, Success={Success}, Reason={Reason}",
            userId,
            tenantId,
            resource,
            action,
            success,
            reason ?? "N/A");
    }

    /// <inheritdoc />
    public void LogTenantAccess(string userId, string tenantId, string action, bool success, string? reason = null)
    {
        var level = success ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(
            level,
            "TenantAccess: User={UserId}, Tenant={TenantId}, Action={Action}, Success={Success}, Reason={Reason}",
            userId,
            tenantId,
            action,
            success,
            reason ?? "N/A");
    }
}
