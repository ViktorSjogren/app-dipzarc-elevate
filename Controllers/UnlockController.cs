using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dizparc_elevate.Services;
using Microsoft.EntityFrameworkCore;
using dizparc_elevate.Models.securitySolutionsCommon;
using System.Text.Json;

namespace dizparc_elevate.Controllers
{
    [Authorize]
    public class UnlockController : Controller
    {
        private readonly ILogger<UnlockController> _logger;
        private readonly Sqldb_securitySolutionsCommon _context;
        private readonly IRunbookService _runbookService;
        private readonly IAuditService _auditService;

        public UnlockController(ILogger<UnlockController> logger, Sqldb_securitySolutionsCommon context, IRunbookService runbookService, IAuditService auditService)
        {
            _logger = logger;
            _context = context;
            _runbookService = runbookService;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            await PopulateViewBagData();
            return View();
        }

        private async Task PopulateViewBagData(string? selectedElevateAccount = null)
        {
            var currentUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUsername))
            {
                ViewBag.Error = "Authentication error. Please sign in again.";
                ViewBag.Servers = new List<string>();
                ViewBag.Roles = new List<string>();
                ViewBag.ElevateAccounts = new List<string>();
                return;
            }

            // Find ALL active elevate accounts for this username
            var userAccounts = await _context.ElevateUsers
                .Where(u => u.UserName == currentUsername)
                .ToListAsync();

            if (!userAccounts.Any())
            {
                ViewBag.Error = "User not found in the system. Please contact your administrator.";
                ViewBag.Servers = new List<string>();
                ViewBag.Roles = new List<string>();
                ViewBag.ElevateAccounts = new List<string>();
                return;
            }

            var accountNames = userAccounts.Select(u => u.ElevateAccount).OrderBy(a => a).ToList();
            ViewBag.ElevateAccounts = accountNames;

            // Determine which account to show permissions for
            var activeAccount = selectedElevateAccount;
            if (string.IsNullOrEmpty(activeAccount) || !accountNames.Contains(activeAccount))
            {
                activeAccount = accountNames.First();
            }

            ViewBag.ElevateAccount = activeAccount;

            // Get available servers based on selected account's permissions
            var availableServers = await _context.ElevateUserPermissionsViews
                .Where(s => s.ElevateAccount == activeAccount && s.PermissionType == "server")
                .Select(s => s.PermissionValue)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            // Get available AD roles based on selected account's permissions
            var availableRoles = await _context.ElevateUserPermissionsViews
                .Where(s => s.ElevateAccount == activeAccount && s.PermissionType == "ad_role")
                .Select(s => s.PermissionValue)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            if (!availableServers.Any() && !availableRoles.Any())
            {
                ViewBag.Error = "No servers or roles configured for your account. Please contact your administrator.";
                ViewBag.Servers = new List<string>();
                ViewBag.Roles = new List<string>();
                return;
            }

            ViewBag.Servers = availableServers;
            ViewBag.Roles = availableRoles;
        }

        [HttpGet]
        public async Task<IActionResult> GetAccountPermissions(string elevateAccount)
        {
            var currentUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUsername))
            {
                return Json(new { error = "Authentication error." });
            }

            // Verify the account belongs to this user
            var userAccount = await _context.ElevateUsers
                .FirstOrDefaultAsync(u => u.UserName == currentUsername && u.ElevateAccount == elevateAccount);

            if (userAccount == null)
            {
                return Json(new { error = "Account not found or does not belong to you." });
            }

            var servers = await _context.ElevateUserPermissionsViews
                .Where(s => s.ElevateAccount == elevateAccount && s.PermissionType == "server")
                .Select(s => s.PermissionValue)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var roles = await _context.ElevateUserPermissionsViews
                .Where(s => s.ElevateAccount == elevateAccount && s.PermissionType == "ad_role")
                .Select(s => s.PermissionValue)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return Json(new { servers, roles });
        }

        public IActionResult Success(string username)
        {
            ViewBag.Username = username;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string reason, string[] servers, string[] roles, string elevateAccount)
        {
            // Always populate ViewBag data first
            await PopulateViewBagData(elevateAccount);

            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError("username", "Username is required");
                return View();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                ModelState.AddModelError("reason", "Reason is required");
                return View();
            }

            if ((servers == null || servers.Length == 0) && (roles == null || roles.Length == 0))
            {
                ModelState.AddModelError("", "At least one server or role must be selected");
                return View();
            }

            try
            {
                var currentUsername = User.Identity?.Name;

                // Look up the specific ElevateUser record by both username AND elevateAccount
                var userInfo = await _context.ElevateUsers
                    .FirstOrDefaultAsync(u => u.UserName == currentUsername && u.ElevateAccount == elevateAccount);

                if (userInfo == null)
                {
                    ViewBag.Error = "Elevate account not found or does not belong to you. Please contact your administrator.";
                    return View();
                }

                // Serialize unlock data as JSON string for the runbook parameter
                var unlockData = JsonSerializer.Serialize(new { servers, roles });

                var parameters = new Dictionary<string, string>
                {
                    ["elevate_account"] = userInfo.ElevateAccount,
                    ["username"] = username.Trim(),
                    ["unlock"] = unlockData
                };

                var result = await _runbookService.StartRunbookAsync(
                    RunbookNames.UnlockActiveDirectoryAccount,
                    userInfo.CustomerId,
                    parameters);

                if (!result.Success)
                {
                    ViewBag.Error = result.ErrorMessage ?? "Failed to send unlock request. Please try again.";
                    return View();
                }

                _logger.LogInformation("Successfully sent unlock request for user {Username} (account: {ElevateAccount}) by {RequestedBy}",
                    username, elevateAccount, User.Identity?.Name);

                await _auditService.LogAsync("UnlockRequested", new { elevateAccount, username = username.Trim(), servers, roles });

                // Record active permissions for each selected server
                if (servers != null && servers.Length > 0)
                {
                    var serverType = await _context.ElevatePermissionTypes
                        .FirstOrDefaultAsync(pt => pt.Type == "server");
                    if (serverType != null)
                    {
                        foreach (var server in servers)
                        {
                            var existing = await _context.ElevateActivePermissions
                                .FirstOrDefaultAsync(ap => ap.UserName == username.Trim()
                                    && ap.PermissionType == serverType.ElevatePermissionTypesId
                                    && ap.Permission == server);

                            if (existing != null)
                            {
                                existing.Active = true;
                                existing.UpdatedBy = currentUsername ?? "system";
                            }
                            else
                            {
                                _context.ElevateActivePermissions.Add(new ElevateActivePermission
                                {
                                    UserName = username.Trim(),
                                    PermissionType = serverType.ElevatePermissionTypesId,
                                    Permission = server,
                                    Active = true,
                                    ManuallyAssigned = false,
                                    CustomerId = userInfo.CustomerId,
                                    CreatedBy = currentUsername ?? "system",
                                    UpdatedBy = currentUsername ?? "system"
                                });
                            }
                        }
                    }
                }

                // Record active permissions for each selected role
                if (roles != null && roles.Length > 0)
                {
                    var roleType = await _context.ElevatePermissionTypes
                        .FirstOrDefaultAsync(pt => pt.Type == "ad_role");
                    if (roleType != null)
                    {
                        foreach (var role in roles)
                        {
                            var existing = await _context.ElevateActivePermissions
                                .FirstOrDefaultAsync(ap => ap.UserName == username.Trim()
                                    && ap.PermissionType == roleType.ElevatePermissionTypesId
                                    && ap.Permission == role);

                            if (existing != null)
                            {
                                existing.Active = true;
                                existing.UpdatedBy = currentUsername ?? "system";
                            }
                            else
                            {
                                _context.ElevateActivePermissions.Add(new ElevateActivePermission
                                {
                                    UserName = username.Trim(),
                                    PermissionType = roleType.ElevatePermissionTypesId,
                                    Permission = role,
                                    Active = true,
                                    ManuallyAssigned = false,
                                    CustomerId = userInfo.CustomerId,
                                    CreatedBy = currentUsername ?? "system",
                                    UpdatedBy = currentUsername ?? "system"
                                });
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending unlock request for user {Username}", username);
                ViewBag.Error = "An unexpected error occurred. Please try again or contact support.";
            }

            return View();
        }
    }
}
