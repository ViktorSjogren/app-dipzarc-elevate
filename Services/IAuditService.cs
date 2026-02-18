namespace dizparc_elevate.Services
{
    public interface IAuditService
    {
        /// <summary>
        /// Logs a structured audit event to the database.
        /// The event is stored as JSON in the elevateAuditLog.event column.
        /// </summary>
        /// <param name="action">Action name, e.g. "UserCreated", "PermissionAssigned"</param>
        /// <param name="eventData">Arbitrary data describing the event (will be serialized to JSON)</param>
        Task LogAsync(string action, object? eventData = null);
    }
}
