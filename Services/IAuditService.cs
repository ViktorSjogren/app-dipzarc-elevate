namespace dizparc_elevate.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string resource, bool success, string? details = null);
        Task LogSecurityEventAsync(string eventType, string description, string? additionalData = null);
        Task LogPrivilegedActionAsync(string action, string targetResource, bool success, string? details = null);
    }
}
