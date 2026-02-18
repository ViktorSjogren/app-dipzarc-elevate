using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using dizparc_elevate.Models.securitySolutionsCommon;

namespace dizparc_elevate.Authorization
{
    /// <summary>
    /// Requirement that checks the elevateAdmins table.
    /// If AdminRole is set, only admins with that specific role are authorized.
    /// If AdminRole is null, any admin role is sufficient.
    /// </summary>
    public class ElevateAdminRequirement : IAuthorizationRequirement
    {
        public string? AdminRole { get; }

        public ElevateAdminRequirement(string? adminRole = null)
        {
            AdminRole = adminRole;
        }
    }

    public class ElevateAdminHandler : AuthorizationHandler<ElevateAdminRequirement>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ElevateAdminHandler> _logger;

        public ElevateAdminHandler(IServiceScopeFactory scopeFactory, ILogger<ElevateAdminHandler> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, ElevateAdminRequirement requirement)
        {
            var username = context.User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Sqldb_securitySolutionsCommon>();

            var query = dbContext.ElevateAdmins
                .Where(a => a.UserName == username);

            // Filter by specific admin role if required
            if (!string.IsNullOrEmpty(requirement.AdminRole))
            {
                query = query.Where(a => a.AdminRole == requirement.AdminRole);
            }

            var isAdmin = await query.AnyAsync();

            if (isAdmin)
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Unauthorized admin access attempt by {Username} (required role: {Role})",
                    username, requirement.AdminRole ?? "any");
            }
        }
    }
}
