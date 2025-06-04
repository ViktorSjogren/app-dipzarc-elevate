using System.Security.Claims;

namespace dizparc_elevate.Services
{
    public class AuditService : IAuditService
    {
        private readonly ILogger<AuditService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ILogger<AuditService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task LogAsync(string action, string resource, bool success, string? details = null)
        {
            var auditData = GetAuditContext();

            _logger.LogInformation(
                "AUDIT: {Action} on {Resource} by {User} from {IpAddress} was {Status}. " +
                "SessionId: {SessionId}, UserAgent: {UserAgent}, Timestamp: {Timestamp}. Details: {Details}",
                action,
                resource,
                auditData.UserName,
                auditData.IpAddress,
                success ? "Successful" : "Failed",
                auditData.SessionId,
                auditData.UserAgent,
                DateTime.UtcNow,
                details ?? "None");

            return Task.CompletedTask;
        }

        public Task LogSecurityEventAsync(string eventType, string description, string? additionalData = null)
        {
            var auditData = GetAuditContext();

            _logger.LogWarning(
                "SECURITY_EVENT: {EventType} - {Description}. " +
                "User: {User}, IP: {IpAddress}, SessionId: {SessionId}, " +
                "UserAgent: {UserAgent}, Timestamp: {Timestamp}. Data: {AdditionalData}",
                eventType,
                description,
                auditData.UserName,
                auditData.IpAddress,
                auditData.SessionId,
                auditData.UserAgent,
                DateTime.UtcNow,
                additionalData ?? "None");

            return Task.CompletedTask;
        }

        public Task LogPrivilegedActionAsync(string action, string targetResource, bool success, string? details = null)
        {
            var auditData = GetAuditContext();

            _logger.LogCritical(
                "PRIVILEGED_ACTION: {Action} on {TargetResource} by {User} from {IpAddress} was {Status}. " +
                "SessionId: {SessionId}, UserAgent: {UserAgent}, Timestamp: {Timestamp}. " +
                "TenantId: {TenantId}, Details: {Details}",
                action,
                targetResource,
                auditData.UserName,
                auditData.IpAddress,
                success ? "Successful" : "Failed",
                auditData.SessionId,
                auditData.UserAgent,
                DateTime.UtcNow,
                auditData.TenantId,
                details ?? "None");

            return Task.CompletedTask;
        }

        private AuditContext GetAuditContext()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            return new AuditContext
            {
                UserName = httpContext?.User?.Identity?.Name ?? "Anonymous",
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown",
                SessionId = httpContext?.Session?.Id ?? "No Session",
                TenantId = httpContext?.Session?.GetString("UserTenantId") ?? "Unknown",
                UserId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown"
            };
        }
    }

    public class AuditContext
    {
        public string UserName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
