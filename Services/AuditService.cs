using dizparc_elevate.Models.securitySolutionsCommon;
using System.Text.Json;

namespace dizparc_elevate.Services
{
    public class AuditService : IAuditService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public AuditService(IServiceScopeFactory scopeFactory, IHttpContextAccessor httpContextAccessor, ILogger<AuditService> logger)
        {
            _scopeFactory = scopeFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogAsync(string action, object? eventData = null)
        {
            string? eventJson = null;
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var userName = httpContext?.User?.Identity?.Name ?? "System";

                var eventPayload = new
                {
                    action,
                    data = eventData,
                    ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                    userAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
                    sessionId = httpContext?.Session?.Id
                };

                eventJson = JsonSerializer.Serialize(eventPayload, JsonOptions);

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Sqldb_securitySolutionsCommon>();

                var auditLog = new ElevateAuditLog
                {
                    Event = eventJson,
                    Created = DateTime.UtcNow,
                    CreatedBy = userName,
                    Updated = DateTime.UtcNow,
                    UpdatedBy = userName
                };

                context.ElevateAuditLogs.Add(auditLog);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Never let audit failures break the application - fall back to logger
                _logger.LogError(ex, "Failed to persist audit event to database. Action: {Action}, Event: {Event}", action, eventJson ?? "(serialization failed)");
            }
        }
    }
}
