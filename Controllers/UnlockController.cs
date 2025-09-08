using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dizparc_elevate.Models.securitySolutionsCommon;
using Microsoft.EntityFrameworkCore;

namespace dizparc_elevate.Controllers
{
    [Authorize]
    public class UnlockController : Controller
    {
        private readonly ILogger<UnlockController> _logger;
        private readonly Sqldb_securitySolutionsCommon _context;

        public UnlockController(ILogger<UnlockController> logger, Sqldb_securitySolutionsCommon context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUsername))
            {
                return Unauthorized();
            }

            var userInfo = await _context.ElevateUsersViews
                .FirstOrDefaultAsync(u => u.Username == currentUsername);

            if (userInfo == null)
            {
                ViewBag.Error = "User not found in the system. Please contact your administrator.";
                ViewBag.Servers = new List<string>();
                ViewBag.Roles = new List<string>();
                return View();
            }

            // Get user's tier permissions
            var userTiers = await _context.ElevatePermissionsViews
                .Where(p => p.ElevateAccount == userInfo.ElevateAccount && p.Type == "tier")
                .Select(p => p.Value)
                .ToListAsync();

            if (!userTiers.Any())
            {
                ViewBag.Error = "No permissions configured for your account. Please contact your administrator.";
                ViewBag.Servers = new List<string>();
                ViewBag.Roles = new List<string>();
                return View();
            }

            // Get available servers based on user's tenant and tiers
            var availableServers = await _context.ElevateServersViews
                .Where(s => s.TenantId == userInfo.TenantId && userTiers.Contains(s.Tier))
                .Select(s => s.ServerName)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            // Get available roles based on user's tier permissions
            var availableRoles = await _context.ElevatePermissionsViews
                .Where(p => p.ElevateAccount == userInfo.ElevateAccount && p.Type == "role")
                .Select(p => p.Value)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            ViewBag.Servers = availableServers;
            ViewBag.Roles = availableRoles;
            ViewBag.ElevateAccount = userInfo.ElevateAccount;
            ViewBag.TenantId = userInfo.TenantId;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string reason, string[] servers, string[] roles)
        {
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

            if (servers == null || servers.Length == 0)
            {
                ModelState.AddModelError("servers", "At least one server must be selected");
                return View();
            }

            if (roles == null || roles.Length == 0)
            {
                ModelState.AddModelError("roles", "At least one role must be selected");
                return View();
            }

            try
            {
                var currentUsername = User.Identity?.Name;
                var userInfo = await _context.ElevateUsersViews
                    .FirstOrDefaultAsync(u => u.Username == currentUsername);

                if (userInfo == null)
                {
                    ViewBag.Error = "User not found in the system. Please contact your administrator.";
                    return View();
                }

                var payload = new
                {
                    elevate_account = userInfo.ElevateAccount,
                    tenant_id = userInfo.TenantId,
                    username = username.Trim(),
                    unlock = new
                    {
                        servers = servers,
                        roles = roles
                    }
                };

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var webhookUrl = Environment.GetEnvironmentVariable("Unlock_ActiveDirectoryAccountWebhook_webhook");
                
                if (string.IsNullOrEmpty(webhookUrl))
                {
                    _logger.LogError("Webhook URL environment variable 'Unlock_ActiveDirectoryAccountWebhook_webhook' is not configured");
                    ViewBag.Error = "System configuration error. Please contact your administrator.";
                    return View();
                }
                var response = await httpClient.PostAsync(webhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully sent unlock request for user {Username} by {RequestedBy}", username, User.Identity?.Name);
                    ViewBag.Success = $"Unlock request for user '{username}' has been sent successfully.";
                    ModelState.Clear();
                }
                else
                {
                    _logger.LogError("Failed to send unlock request for user {Username}. Status: {StatusCode}", username, response.StatusCode);
                    ViewBag.Error = "Failed to send unlock request. Please try again or contact support.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while sending unlock request for user {Username}", username);
                ViewBag.Error = "Network error occurred. Please check your connection and try again.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout while sending unlock request for user {Username}", username);
                ViewBag.Error = "Request timed out. Please try again.";
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