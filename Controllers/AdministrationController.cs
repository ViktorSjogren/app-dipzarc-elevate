using Azure.Core;
using Azure.Identity;
using dizparc_elevate.Models.securitySolutionsCommon;
using dizparc_elevate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace dizparc_elevate.Controllers
{
    [Authorize(Policy = "RequireAdminAccess")]
    public class AdministrationController : Controller
    {
        private readonly ILogger<AdministrationController> _logger;
        private readonly Sqldb_securitySolutionsCommon _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IWebhookService _webhookService;
        private readonly IGraphService _graphService;
        private readonly IAuditService _auditService;

        public AdministrationController(ILogger<AdministrationController> logger, Sqldb_securitySolutionsCommon context, IHttpClientFactory httpClientFactory, IServiceScopeFactory serviceScopeFactory, IWebhookService webhookService, IGraphService graphService, IAuditService auditService)
        {
            _logger = logger;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _serviceScopeFactory = serviceScopeFactory;
            _webhookService = webhookService;
            _graphService = graphService;
            _auditService = auditService;
        }

        // ──────────────────────────────────────────────
        // Multi-tenant helpers
        // ──────────────────────────────────────────────

        /// <summary>
        /// Resolves the effective customer scope for the current admin.
        /// Global admins (belonging to "Dizparc Security Solutions AB") can switch
        /// between customers via session; other admins are locked to their own customer.
        /// </summary>
        private async Task<(int effectiveCustomerId, bool isGlobalAdmin)> GetAdminScopeAsync()
        {
            var adminUserName = User.Identity?.Name;

            var admin = await _context.ElevateAdmins
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.UserName == adminUserName);

            if (admin == null)
            {
                // Fallback - should not happen because of the auth policy, but be safe
                throw new UnauthorizedAccessException("Admin record not found.");
            }

            bool isGlobalAdmin = admin.Customer.CustomerName == "Dizparc Security Solutions AB";

            int effectiveCustomerId = admin.CustomerId;

            if (isGlobalAdmin)
            {
                var sessionCustomerId = HttpContext.Session.GetInt32("AdminCustomerId");
                if (sessionCustomerId.HasValue)
                {
                    effectiveCustomerId = sessionCustomerId.Value;
                }
            }

            return (effectiveCustomerId, isGlobalAdmin);
        }

        /// <summary>
        /// Populates ViewBag with multi-tenant context (current customer, global admin flag,
        /// and the customer list for the switcher dropdown).
        /// Call this in every GET action that returns a View.
        /// </summary>
        private async Task SetMultiTenantViewBag()
        {
            var (effectiveCustomerId, isGlobalAdmin) = await GetAdminScopeAsync();

            ViewBag.IsGlobalAdmin = isGlobalAdmin;
            ViewBag.CurrentCustomerId = effectiveCustomerId;

            if (isGlobalAdmin)
            {
                ViewBag.Customers = await _context.Customers
                    .OrderBy(c => c.CustomerName)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Allows a global admin to switch the active customer context.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchCustomer(int customerId)
        {
            var (_, isGlobalAdmin) = await GetAdminScopeAsync();

            if (!isGlobalAdmin)
            {
                return Forbid();
            }

            HttpContext.Session.SetInt32("AdminCustomerId", customerId);

            return RedirectToAction(nameof(Index));
        }

        // ──────────────────────────────────────────────
        // Dashboard
        // ──────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            await SetMultiTenantViewBag();
            return View();
        }

        // ──────────────────────────────────────────────
        // Tier helpers
        // ──────────────────────────────────────────────

        private async Task<List<ElevateAvailableTier>> GetAvailableTiersAsync()
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            return await _context.ElevateAvailableTiers
                .Where(t => t.Active == 1 && t.CustomerId == effectiveCustomerId)
                .OrderBy(t => t.TierName)
                .ToListAsync();
        }

        private async Task LoadAvailableTiersToViewBag()
        {
            ViewBag.AvailableTiers = await GetAvailableTiersAsync();
        }

        /// <summary>
        /// Syncs Entra ID group membership when a user's tier changes.
        /// Adds to the new tier's group and removes from the old tier's group
        /// (only if no other elevate accounts for this user remain in that tier).
        /// Failures are logged but do not block the calling operation.
        /// </summary>
        private async Task SyncEntraGroupMembership(string userName, int? oldTierId, int? newTierId)
        {
            try
            {
                var (effectiveCustomerId, _) = await GetAdminScopeAsync();

                // Add to new tier group
                if (newTierId.HasValue)
                {
                    var newTier = await _context.ElevateAvailableTiers
                        .FirstOrDefaultAsync(t => t.ElevateAvailableTiersId == newTierId.Value && t.CustomerId == effectiveCustomerId);
                    if (newTier != null && !string.IsNullOrEmpty(newTier.EntraGroupId))
                    {
                        var added = await _graphService.AddUserToGroupAsync(userName, newTier.EntraGroupId);
                        if (added)
                        {
                            await _auditService.LogAsync("EntraGroupMemberAdded", new { userName, groupId = newTier.EntraGroupId, tierName = newTier.TierName });
                        }
                    }
                }

                // Remove from old tier group (only if different from new tier)
                if (oldTierId.HasValue && oldTierId != newTierId)
                {
                    // Check if user has other elevate accounts still in the old tier
                    var otherAccountsInOldTier = await _context.ElevateUsers
                        .AnyAsync(u => u.UserName == userName && u.Tier == oldTierId.Value);

                    if (!otherAccountsInOldTier)
                    {
                        var oldTier = await _context.ElevateAvailableTiers
                            .FirstOrDefaultAsync(t => t.ElevateAvailableTiersId == oldTierId.Value && t.CustomerId == effectiveCustomerId);
                        if (oldTier != null && !string.IsNullOrEmpty(oldTier.EntraGroupId))
                        {
                            var removed = await _graphService.RemoveUserFromGroupAsync(userName, oldTier.EntraGroupId);
                            if (removed)
                            {
                                await _auditService.LogAsync("EntraGroupMemberRemoved", new { userName, groupId = oldTier.EntraGroupId, tierName = oldTier.TierName });
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation(
                            "User {UserName} still has other accounts in tier {TierId}, keeping Entra group membership",
                            userName, oldTierId.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync Entra group membership for user {UserName} (oldTier={OldTier}, newTier={NewTier})",
                    userName, oldTierId, newTierId);
            }
        }

        // ──────────────────────────────────────────────
        // Server onboarding
        // ──────────────────────────────────────────────

        public async Task<IActionResult> OnboardedServers()
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            var servers = await _context.ElevateAdServersWithOnboardingStatusViews
                .Where(s => s.CustomerId == effectiveCustomerId)
                .OrderBy(s => s.ServerName)
                .ToListAsync();

            var tierLookup = await _context.ElevateAvailableTiers
                .Where(t => t.CustomerId == effectiveCustomerId)
                .ToDictionaryAsync(t => t.TierName, t => t.DisplayName);
            ViewBag.TierDisplayLookup = tierLookup;

            await SetMultiTenantViewBag();

            return View(servers);
        }

        // GET: Server onboarding - tier selection
        public async Task<IActionResult> OnboardServer(string serverName)
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            var server = await _context.ElevateAdServersWithOnboardingStatusViews
                .FirstOrDefaultAsync(s => s.ServerName == serverName && s.CustomerId == effectiveCustomerId);

            if (server == null)
            {
                return NotFound();
            }

            // Only allow onboarding for servers that haven't started yet
            if (server.OnboardingStatus != 0)
            {
                return RedirectToAction(nameof(OnboardedServers));
            }

            await LoadAvailableTiersToViewBag();
            await SetMultiTenantViewBag();

            return View(server);
        }

        // POST: Start server onboarding - call webhook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartServerOnboarding(string serverName, int tier)
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            // Check if server exists in AD servers for this customer
            var adServer = await _context.ElevateAdServers
                .FirstOrDefaultAsync(s => s.ServerName == serverName && s.CustomerId == effectiveCustomerId);

            if (adServer == null)
            {
                return Json(new { success = false, error = "Server not found." });
            }

            // Check if already being onboarded (exists in elevatePermissions with server type)
            var serverPermissionType = await _context.ElevatePermissionTypes
                .FirstOrDefaultAsync(pt => pt.Type == "server");

            if (serverPermissionType == null)
            {
                return Json(new { success = false, error = "Server permission type not configured." });
            }

            var existingPermission = await _context.ElevatePermissions
                .FirstOrDefaultAsync(p => p.Value == serverName && p.Type == serverPermissionType.ElevatePermissionTypesId);

            if (existingPermission != null)
            {
                return Json(new { success = false, error = "Server onboarding already started." });
            }

            var currentUserName = User.Identity?.Name ?? "System";

            // Reserve this server immediately to prevent duplicate onboarding.
            // Status 3 = "Started". If the webhook fails, we remove this record.
            var newPermission = new ElevatePermission
            {
                Tier = tier,
                Type = serverPermissionType.ElevatePermissionTypesId,
                Value = serverName,
                OnboardingStatus = 3,
                CustomerId = effectiveCustomerId,
                Created = DateTime.UtcNow,
                CreatedBy = currentUserName,
                Updated = DateTime.UtcNow,
                UpdatedBy = currentUserName
            };

            _context.ElevatePermissions.Add(newPermission);
            await _context.SaveChangesAsync();

            // Call webhook to create device group
            var webhookEnvVar = Environment.GetEnvironmentVariable("Create_DeviceGroup_webhook")?.Trim() ?? "";

            // Pre-validate Azure auth before starting - fail fast if we can't monitor the job
            if (!string.IsNullOrEmpty(webhookEnvVar))
            {
                var subscriptionId = Environment.GetEnvironmentVariable("Azure_SubscriptionId");
                var resourceGroup = Environment.GetEnvironmentVariable("Azure_ResourceGroup");
                var automationAccount = Environment.GetEnvironmentVariable("Azure_AutomationAccount");

                if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroup) || string.IsNullOrEmpty(automationAccount))
                {
                    _logger.LogError("Azure configuration not complete. Cannot start onboarding without job monitoring capability.");
                    _context.ElevatePermissions.Remove(newPermission);
                    await _context.SaveChangesAsync();
                    return Json(new { success = false, error = "Azure configuration is incomplete. Please contact an administrator." });
                }

                try
                {
                    var credential = new DefaultAzureCredential();
                    await credential.GetTokenAsync(
                        new TokenRequestContext(new[] { "https://management.azure.com/.default" })
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Azure authentication failed. Cannot start onboarding without job monitoring capability.");
                    _context.ElevatePermissions.Remove(newPermission);
                    await _context.SaveChangesAsync();
                    return Json(new { success = false, error = "Something went wrong. Please report this error and ensure the application has valid credentials before starting onboarding." });
                }
            }

            if (!string.IsNullOrEmpty(webhookEnvVar))
            {
                try
                {
                    _logger.LogInformation("Calling device group creation webhook for server: {ServerName}", serverName);

                    var tierRecord = await _context.ElevateAvailableTiers.FindAsync(tier);
                    var payload = new
                    {
                        DeviceName = serverName,
                        Tier = tierRecord?.TierName ?? tier.ToString()
                    };

                    var result = await _webhookService.PostFromEnvAsync("Create_DeviceGroup_webhook", payload);

                    if (!result.Success)
                    {
                        _context.ElevatePermissions.Remove(newPermission);
                        await _context.SaveChangesAsync();
                        return Json(new { success = false, error = result.ErrorMessage ?? "Failed to initiate device group creation." });
                    }

                    // Parse the response to get job ID
                    var responseContent = result.ResponseBody ?? "";
                    _logger.LogInformation("Device group creation webhook response: {Response}", responseContent);

                    string? jobId = null;
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(responseContent);
                        if (jsonDoc.RootElement.TryGetProperty("JobIds", out var jobIds) &&
                            jobIds.ValueKind == JsonValueKind.Array &&
                            jobIds.GetArrayLength() > 0)
                        {
                            jobId = jobIds[0].GetString();
                            _logger.LogInformation("Device group creation job ID: {JobId}", jobId);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Could not parse webhook response for job ID: {Error}", ex.Message);
                    }

                    if (string.IsNullOrEmpty(jobId))
                    {
                        _logger.LogError("No job ID received from webhook");
                        _context.ElevatePermissions.Remove(newPermission);
                        await _context.SaveChangesAsync();
                        return Json(new { success = false, error = "Webhook did not return a job ID." });
                    }

                    // Create job tracking record
                    var jobRecord = new ElevateJob
                    {
                        Type = "device",
                        Reference = serverName,
                        Job = jobId,
                        CustomerId = effectiveCustomerId,
                        Created = DateTime.UtcNow,
                        CreatedBy = currentUserName,
                        Updated = DateTime.UtcNow,
                        UpdatedBy = currentUserName
                    };

                    _context.ElevateJobs.Add(jobRecord);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Server {ServerName} onboarding started. Job ID: {JobId}", serverName, jobId);

                    await _auditService.LogAsync("ServerOnboardingStarted", new { serverName, tier, jobId });

                    // Queue background job to monitor status
                    _ = Task.Run(async () => await MonitorServerJobStatus(jobId, serverName));

                    return Json(new {
                        success = true,
                        message = "Server onboarding initiated. The automation is now running.",
                        serverName = serverName
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during server onboarding");
                    _context.ElevatePermissions.Remove(newPermission);
                    await _context.SaveChangesAsync();
                    return Json(new { success = false, error = "An unexpected error occurred. Please try again." });
                }
            }
            else
            {
                // No webhook configured - update permission record to in-progress status (for testing)
                _logger.LogInformation("No webhook configured, setting server to in-progress status");

                newPermission.OnboardingStatus = 2;
                await _context.SaveChangesAsync();

                return Json(new {
                    success = true,
                    message = "Server moved to in-progress status (no webhook configured).",
                    serverName = serverName
                });
            }
        }

        // Background job to monitor server onboarding job status
        private async Task MonitorServerJobStatus(string jobId, string serverName)
        {
            try
            {
                _logger.LogInformation("Starting background job monitoring for Server Job ID: {JobId}, Server: {ServerName}", jobId, serverName);

                // Get Azure configuration
                var subscriptionId = Environment.GetEnvironmentVariable("Azure_SubscriptionId");
                var resourceGroup = Environment.GetEnvironmentVariable("Azure_ResourceGroup");
                var automationAccount = Environment.GetEnvironmentVariable("Azure_AutomationAccount");

                if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroup) || string.IsNullOrEmpty(automationAccount))
                {
                    _logger.LogError("Azure configuration not complete for job monitoring");
                    await UpdateServerOnboardingStatus(serverName, 0); // Delete record (failed)
                    return;
                }

                // Wait a bit before starting to check
                await Task.Delay(10000); // 10 seconds initial delay

                var maxAttempts = 60; // 10 minutes maximum (60 * 10 seconds)
                var jobStatus = "Processing";

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        // Get Azure Management token
                        var credential = new DefaultAzureCredential();
                        var token = await credential.GetTokenAsync(
                            new TokenRequestContext(new[] { "https://management.azure.com/.default" })
                        );

                        // Construct job status URL
                        var jobUrl = $"https://management.azure.com/subscriptions/{subscriptionId}" +
                                    $"/resourceGroups/{resourceGroup}" +
                                    $"/providers/Microsoft.Automation" +
                                    $"/automationAccounts/{automationAccount}" +
                                    $"/jobs/{jobId}?api-version=2023-11-01";

                        var httpClient = _httpClientFactory.CreateClient();
                        httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", token.Token);
                        httpClient.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));

                        using var statusResponse = await httpClient.GetAsync(jobUrl);

                        if (statusResponse.IsSuccessStatusCode)
                        {
                            var statusJson = await statusResponse.Content.ReadAsStringAsync();
                            using var statusDoc = JsonDocument.Parse(statusJson);

                            if (statusDoc.RootElement.TryGetProperty("properties", out var properties) &&
                                properties.TryGetProperty("provisioningState", out var provisioningState))
                            {
                                jobStatus = provisioningState.GetString() ?? "Unknown";
                                _logger.LogInformation("Server Job {JobId} status: {Status} (attempt {Attempt}/{MaxAttempts})",
                                    jobId, jobStatus, attempt, maxAttempts);

                                if (jobStatus == "Succeeded")
                                {
                                    _logger.LogInformation("Device group creation job {JobId} completed successfully", jobId);
                                    await UpdateServerOnboardingStatus(serverName, 2); // In Progress (manual steps)
                                    return;
                                }
                                else if (jobStatus == "Failed")
                                {
                                    _logger.LogError("Device group creation job {JobId} failed", jobId);
                                    await UpdateServerOnboardingStatus(serverName, 0); // Delete record (failed)
                                    return;
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to get job status for {JobId}: {StatusCode}", jobId, statusResponse.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking job status for {JobId}", jobId);
                    }

                    // Wait before next check
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(10000); // 10 seconds between checks
                    }
                }

                // If we've reached here, the job didn't complete in time
                _logger.LogWarning("Server Job {JobId} monitoring timed out after {MaxAttempts} attempts", jobId, maxAttempts);
                await UpdateServerOnboardingStatus(serverName, 0); // Delete record (failed)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in server job monitoring for Job ID: {JobId}", jobId);
                await UpdateServerOnboardingStatus(serverName, 0); // Delete record (failed)
            }
        }

        // Update server onboarding status in elevatePermissions
        private async Task UpdateServerOnboardingStatus(string serverName, byte status)
        {
            try
            {
                // Create a new scope for database operations
                using var scope = _serviceScopeFactory.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<Sqldb_securitySolutionsCommon>();

                var serverPermissionType = await context.ElevatePermissionTypes
                    .FirstOrDefaultAsync(pt => pt.Type == "server");

                if (serverPermissionType == null)
                {
                    _logger.LogWarning("Server permission type not found");
                    return;
                }

                var permission = await context.ElevatePermissions
                    .FirstOrDefaultAsync(p => p.Value == serverName && p.Type == serverPermissionType.ElevatePermissionTypesId);

                if (permission != null)
                {
                    if (status == 0)
                    {
                        // Delete the record if failed (status 0)
                        context.ElevatePermissions.Remove(permission);
                        _logger.LogInformation("Removed permission record for server {ServerName} due to failed onboarding", serverName);
                    }
                    else
                    {
                        permission.OnboardingStatus = status;
                        permission.Updated = DateTime.UtcNow;
                        permission.UpdatedBy = "System";
                        _logger.LogInformation("Updated server {ServerName} onboarding status to {Status}", serverName, status);
                    }
                    await context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning("Permission record for server {ServerName} not found for status update", serverName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update server {ServerName} status to {Status}", serverName, status);
            }
        }

        // GET: Server onboarding progress - manual steps
        public async Task<IActionResult> OnboardServerProgress(string serverName)
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            var server = await _context.ElevateAdServersWithOnboardingStatusViews
                .FirstOrDefaultAsync(s => s.ServerName == serverName && s.CustomerId == effectiveCustomerId);

            if (server == null)
            {
                return NotFound();
            }

            // Only show progress view for servers in "In Progress" status (2)
            if (server.OnboardingStatus != 2)
            {
                return RedirectToAction(nameof(OnboardedServers));
            }

            await SetMultiTenantViewBag();

            return View(server);
        }

        // POST: Complete server onboarding
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteServerOnboarding(string serverName)
        {
            var serverPermissionType = await _context.ElevatePermissionTypes
                .FirstOrDefaultAsync(pt => pt.Type == "server");

            if (serverPermissionType == null)
            {
                return Json(new { success = false, error = "Server permission type not configured." });
            }

            var permission = await _context.ElevatePermissions
                .FirstOrDefaultAsync(p => p.Value == serverName && p.Type == serverPermissionType.ElevatePermissionTypesId);

            if (permission == null)
            {
                return Json(new { success = false, error = "Server not found." });
            }

            if (permission.OnboardingStatus != 2)
            {
                return Json(new { success = false, error = "Server is not in the correct state for completion." });
            }

            var currentUserName = User.Identity?.Name ?? "System";

            permission.OnboardingStatus = 1; // Complete
            permission.Updated = DateTime.UtcNow;
            permission.UpdatedBy = currentUserName;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Server {ServerName} onboarding completed by {User}", serverName, currentUserName);

            await _auditService.LogAsync("ServerOnboardingCompleted", new { serverName });

            return Json(new { success = true, message = "Server onboarding completed successfully." });
        }

        // POST: Cancel/reset a stuck server onboarding (status 3 = Started)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelServerOnboarding(string serverName)
        {
            var serverPermissionType = await _context.ElevatePermissionTypes
                .FirstOrDefaultAsync(pt => pt.Type == "server");

            if (serverPermissionType == null)
            {
                return Json(new { success = false, error = "Server permission type not configured." });
            }

            var permission = await _context.ElevatePermissions
                .FirstOrDefaultAsync(p => p.Value == serverName && p.Type == serverPermissionType.ElevatePermissionTypesId);

            if (permission == null)
            {
                return Json(new { success = false, error = "No onboarding record found for this server." });
            }

            if (permission.OnboardingStatus != 3)
            {
                return Json(new { success = false, error = "Server is not in a cancellable state." });
            }

            var currentUserName = User.Identity?.Name ?? "System";

            _context.ElevatePermissions.Remove(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Server {ServerName} onboarding cancelled by {User}", serverName, currentUserName);

            await _auditService.LogAsync("ServerOnboardingCancelled", new { serverName });

            return Json(new { success = true, message = "Server onboarding has been cancelled. You can retry onboarding." });
        }

        // Check status of server onboarding job
        [HttpGet]
        public async Task<IActionResult> CheckServerOnboardingStatus(string serverName)
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            var server = await _context.ElevateAdServersWithOnboardingStatusViews
                .FirstOrDefaultAsync(s => s.ServerName == serverName && s.CustomerId == effectiveCustomerId);

            if (server == null)
            {
                return Json(new { success = false, error = "Server not found." });
            }

            return Json(new {
                success = true,
                status = server.OnboardingStatus,
                statusText = server.OnboardingStatus switch
                {
                    0 => "Not Started",
                    1 => "Complete",
                    2 => "In Progress",
                    3 => "Started",
                    _ => "Unknown"
                }
            });
        }

        // ──────────────────────────────────────────────
        // Active permissions
        // ──────────────────────────────────────────────

        public async Task<IActionResult> CurrentlyActivePermissions()
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            var activePermissions = await _context.ElevateActivePermissions
                .Include(p => p.PermissionTypeNavigation)
                .Where(p => p.Active && p.CustomerId == effectiveCustomerId)
                .OrderBy(p => p.UserName)
                .ThenBy(p => p.PermissionType)
                .ToListAsync();

            await SetMultiTenantViewBag();

            return View(activePermissions);
        }

        // ──────────────────────────────────────────────
        // User permissions overview
        // ──────────────────────────────────────────────

        public async Task<IActionResult> UserPermissions()
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            // The ElevateUserPermissionsView does not have a CustomerId column directly,
            // so we filter via the ElevateUsersId which maps to users belonging to this customer.
            var customerUserIds = await _context.ElevateUsers
                .Where(u => u.CustomerId == effectiveCustomerId)
                .Select(u => u.ElevateUsersId)
                .ToListAsync();

            var userPermissions = await _context.ElevateUserPermissionsViews
                .Where(p => customerUserIds.Contains(p.ElevateUsersId))
                .OrderBy(p => p.ElevateAccount)
                .ThenBy(p => p.PermissionType)
                .ToListAsync();

            var tierLookup = await _context.ElevateAvailableTiers
                .Where(t => t.CustomerId == effectiveCustomerId)
                .ToDictionaryAsync(t => t.TierName, t => t.DisplayName);
            ViewBag.TierDisplayLookup = tierLookup;

            await SetMultiTenantViewBag();

            return View(userPermissions);
        }

        // ──────────────────────────────────────────────
        // User administration
        // ──────────────────────────────────────────────

        public async Task<IActionResult> UserAdministration()
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            var users = await _context.ElevateUsers
                .Include(u => u.TierNavigation)
                .Where(u => u.CustomerId == effectiveCustomerId)
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var permissions = await _context.ElevatePermissions
                .Where(p => p.CustomerId == effectiveCustomerId)
                .OrderBy(p => p.Value)
                .ToListAsync();

            ViewBag.Permissions = permissions;
            await LoadAvailableTiersToViewBag();
            await SetMultiTenantViewBag();

            return View(users);
        }

        // GET: Edit user
        public async Task<IActionResult> EditUser(int id)
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            var user = await _context.ElevateUsers
                .Include(u => u.TierNavigation)
                .FirstOrDefaultAsync(u => u.ElevateUsersId == id && u.CustomerId == effectiveCustomerId);
            if (user == null)
            {
                return NotFound();
            }

            await LoadAvailableTiersToViewBag();
            await SetMultiTenantViewBag();

            return View(user);
        }

        // POST: Update user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(int id, string phoneNumber, int tier)
        {
            var user = await _context.ElevateUsers.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, error = "User not found." });
            }

            var currentUserName = User.Identity?.Name ?? "System";
            var oldTier = user.Tier;
            var tierChanged = oldTier != tier;

            user.PhoneNumber = phoneNumber;
            user.Tier = tier;
            user.Updated = DateTime.UtcNow;
            user.UpdatedBy = currentUserName;

            // If tier changed, remove all assigned permissions
            if (tierChanged)
            {
                var assignedPermissions = await _context.ElevateAssignedPermissions
                    .Where(ap => ap.ElevateUserId == id)
                    .ToListAsync();

                _context.ElevateAssignedPermissions.RemoveRange(assignedPermissions);
                _logger.LogInformation("Removed {Count} permissions from user {UserId} due to tier change", assignedPermissions.Count, id);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} updated by {UpdatedBy}. Tier changed: {TierChanged}", id, currentUserName, tierChanged);

            await _auditService.LogAsync("UserUpdated", new { userId = id, userName = user.UserName, phoneNumber, tier, tierChanged, oldTier = tierChanged ? oldTier : (int?)null });

            // Sync Entra group membership if tier changed
            if (tierChanged)
            {
                await SyncEntraGroupMembership(user.UserName, oldTier, tier);
            }

            return Json(new { success = true, message = tierChanged ? "User updated. Permissions were reset due to tier change." : "User updated successfully." });
        }

        // POST: Delete user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.ElevateUsers.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, error = "User not found." });
            }

            var currentUserName = User.Identity?.Name ?? "System";
            var userName = user.UserName;
            var userTier = user.Tier;

            // Remove assigned permissions first
            var assignedPermissions = await _context.ElevateAssignedPermissions
                .Where(ap => ap.ElevateUserId == id)
                .ToListAsync();
            _context.ElevateAssignedPermissions.RemoveRange(assignedPermissions);

            // Remove admin roles if any
            var adminRoles = await _context.ElevateAdmins
                .Where(a => a.UserName == userName)
                .ToListAsync();
            _context.ElevateAdmins.RemoveRange(adminRoles);

            // Remove the user
            _context.ElevateUsers.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserName} (ID: {UserId}) deleted by {DeletedBy}", userName, id, currentUserName);

            await _auditService.LogAsync("UserDeleted", new { userId = id, userName, tier = userTier });

            // Remove from Entra group if no other accounts for this user remain in the same tier
            await SyncEntraGroupMembership(userName, userTier, null);

            return Json(new { success = true, message = $"User '{userName}' has been deleted." });
        }

        // Manage permissions for a specific user
        public async Task<IActionResult> ManageUserPermissions(int id)
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            var user = await _context.ElevateUsers
                .Include(u => u.TierNavigation)
                .FirstOrDefaultAsync(u => u.ElevateUsersId == id && u.CustomerId == effectiveCustomerId);

            if (user == null)
            {
                return NotFound();
            }

            // Get all permissions for the user's tier (with type navigation)
            var availablePermissions = await _context.ElevatePermissions
                .Include(p => p.TypeNavigation)
                .Where(p => p.Tier == user.Tier && p.OnboardingStatus == 1)
                .OrderBy(p => p.Value)
                .ToListAsync();

            // Get currently assigned permission IDs
            var assignedPermissionIds = await _context.ElevateAssignedPermissions
                .Where(ap => ap.ElevateUserId == id)
                .Select(ap => ap.PermissionId)
                .ToListAsync();

            // Get the full assigned permission objects
            var assignedPermissions = availablePermissions
                .Where(p => assignedPermissionIds.Contains(p.ElevatePermissionsId))
                .ToList();

            ViewBag.User = user;
            ViewBag.AvailablePermissions = availablePermissions;
            ViewBag.AssignedPermissions = assignedPermissions;

            await SetMultiTenantViewBag();

            return View();
        }

        // Filter permissions by tier (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetPermissionsByTier(int tier)
        {
            var permissions = await _context.ElevatePermissions
                .Where(p => p.Tier == tier)
                .OrderBy(p => p.Value)
                .Select(p => new
                {
                    id = p.ElevatePermissionsId,
                    name = p.Value,
                    tier = p.Tier,
                    type = p.Type
                })
                .ToListAsync();

            return Json(permissions);
        }

        // Toggle permission assignment
        [HttpPost]
        public async Task<IActionResult> TogglePermission(int userId, int permissionId)
        {
            try
            {
                var existingAssignment = await _context.ElevateAssignedPermissions
                    .FirstOrDefaultAsync(ap => ap.ElevateUserId == userId && ap.PermissionId == permissionId);

                if (existingAssignment != null)
                {
                    // Remove assignment
                    _context.ElevateAssignedPermissions.Remove(existingAssignment);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Permission {PermissionId} removed from user {UserId}", permissionId, userId);

                    await _auditService.LogAsync("PermissionRemoved", new { userId, permissionId });

                    return Json(new { success = true, assigned = false, message = "Permission removed successfully" });
                }
                else
                {
                    // Verify user and permission exist and are compatible
                    var user = await _context.ElevateUsers.FindAsync(userId);
                    var permission = await _context.ElevatePermissions.FindAsync(permissionId);

                    if (user == null || permission == null)
                    {
                        return Json(new { success = false, message = "User or permission not found" });
                    }

                    if (permission.Tier != user.Tier)
                    {
                        return Json(new { success = false, message = "Permission tier does not match user tier" });
                    }

                    // Add assignment
                    var currentUser = User.Identity?.Name ?? "System";
                    var newAssignment = new ElevateAssignedPermission
                    {
                        ElevateUserId = userId,
                        PermissionId = permissionId,
                        CreatedBy = currentUser,
                        UpdatedBy = currentUser
                    };

                    _context.ElevateAssignedPermissions.Add(newAssignment);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Permission {PermissionId} assigned to user {UserId}", permissionId, userId);

                    await _auditService.LogAsync("PermissionAssigned", new { userId, permissionId, userName = user.UserName, permissionValue = permission.Value });

                    return Json(new { success = true, assigned = true, message = "Permission assigned successfully" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling permission {PermissionId} for user {UserId}", permissionId, userId);
                return Json(new { success = false, message = "An error occurred while updating permission" });
            }
        }

        // Assign permission to user
        [HttpPost]
        public async Task<IActionResult> AssignPermission(int userId, int permissionId)
        {
            try
            {
                // Check if already assigned
                var existing = await _context.ElevateAssignedPermissions
                    .AnyAsync(ap => ap.ElevateUserId == userId && ap.PermissionId == permissionId);

                if (existing)
                {
                    return Json(new { success = false, message = "Permission already assigned" });
                }

                // Verify user and permission exist
                var user = await _context.ElevateUsers.FindAsync(userId);
                var permission = await _context.ElevatePermissions.FindAsync(permissionId);

                if (user == null || permission == null)
                {
                    return Json(new { success = false, message = "User or permission not found" });
                }

                // Check if permission tier matches user tier
                if (permission.Tier != user.Tier)
                {
                    return Json(new { success = false, message = "Permission tier does not match user tier" });
                }

                // Create assignment
                var currentUserName = User.Identity?.Name ?? "System";
                var assignment = new ElevateAssignedPermission
                {
                    ElevateUserId = userId,
                    PermissionId = permissionId,
                    CreatedBy = currentUserName,
                    UpdatedBy = currentUserName
                };

                _context.ElevateAssignedPermissions.Add(assignment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Permission {PermissionId} assigned to user {UserId}", permissionId, userId);

                await _auditService.LogAsync("PermissionAssigned", new { userId, permissionId, userName = user.UserName, permissionValue = permission.Value });

                return Json(new { success = true, message = "Permission assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning permission");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // Remove permission from user
        [HttpPost]
        public async Task<IActionResult> RemovePermission(int userId, int permissionId)
        {
            try
            {
                var assignment = await _context.ElevateAssignedPermissions
                    .FirstOrDefaultAsync(ap => ap.ElevateUserId == userId && ap.PermissionId == permissionId);

                if (assignment == null)
                {
                    return Json(new { success = false, message = "Assignment not found" });
                }

                _context.ElevateAssignedPermissions.Remove(assignment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Permission {PermissionId} removed from user {UserId}", permissionId, userId);

                await _auditService.LogAsync("PermissionRemoved", new { userId, permissionId });

                return Json(new { success = true, message = "Permission removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing permission");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // Update user's tier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserTier(int userId, int tier)
        {
            try
            {
                var user = await _context.ElevateUsers.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var oldTier = user.Tier;

                // Update the tier
                user.Tier = tier;
                user.Updated = DateTime.UtcNow;
                user.UpdatedBy = User.Identity?.Name ?? "Unknown";

                // Remove all existing permissions that don't match the new tier
                var invalidPermissions = await _context.ElevateAssignedPermissions
                    .Where(ap => ap.ElevateUserId == userId)
                    .Join(_context.ElevatePermissions,
                        ap => ap.PermissionId,
                        p => p.ElevatePermissionsId,
                        (ap, p) => new { Assignment = ap, Permission = p })
                    .Where(x => x.Permission.Tier != tier)
                    .Select(x => x.Assignment)
                    .ToListAsync();

                _context.ElevateAssignedPermissions.RemoveRange(invalidPermissions);

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} tier updated to {Tier}", userId, tier);

                await _auditService.LogAsync("UserTierChanged", new { userId, userName = user.UserName, oldTier, newTier = tier });

                // Sync Entra group membership
                if (oldTier != tier)
                {
                    await SyncEntraGroupMembership(user.UserName, oldTier, tier);
                }

                return Json(new { success = true, message = $"User tier updated to {tier}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user tier");
                return Json(new { success = false, message = "An error occurred while updating the tier" });
            }
        }

        // ──────────────────────────────────────────────
        // User creation
        // ──────────────────────────────────────────────

        // Create new user - GET
        public async Task<IActionResult> CreateUser()
        {
            await LoadAvailableTiersToViewBag();
            await SetMultiTenantViewBag();

            return View();
        }

        // Create new user - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string userName, string elevateAccount, string phoneNumber, int tier)
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            // Validation
            if (string.IsNullOrWhiteSpace(userName))
            {
                ModelState.AddModelError("userName", "Username is required.");
            }

            if (string.IsNullOrWhiteSpace(elevateAccount))
            {
                ModelState.AddModelError("elevateAccount", "Elevate account is required.");
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                ModelState.AddModelError("phoneNumber", "Phone number is required.");
            }

            if (tier == 0)
            {
                ModelState.AddModelError("tier", "Tier is required.");
            }

            // Check if elevate account already exists (same username can have multiple elevate accounts)
            var existingUser = await _context.ElevateUsers
                .FirstOrDefaultAsync(u => u.ElevateAccount == elevateAccount);

            if (existingUser != null)
            {
                ModelState.AddModelError("", "An elevate account with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAvailableTiersToViewBag();
                await SetMultiTenantViewBag();

                return View();
            }

            // Track whether AD account creation succeeded (only matters if webhook is configured)
            bool adAccountCreated = false;

            // Call webhook to create Active Directory account
            var webhookEnvConfigured = !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("Create_ElevateAccount_webhook")?.Trim());

            if (webhookEnvConfigured)
            {
                try
                {
                    _logger.LogInformation("Calling AD account creation webhook for user: {UserName}", userName);

                    var payload = new { username = elevateAccount };

                    var webhookResult = await _webhookService.PostFromEnvAsync("Create_ElevateAccount_webhook", payload);

                    if (!webhookResult.Success)
                    {
                        ModelState.AddModelError("", webhookResult.ErrorMessage ?? "Failed to create Active Directory account.");

                        await LoadAvailableTiersToViewBag();
                        await SetMultiTenantViewBag();

                        return View();
                    }

                    // Parse the response to get job ID if available
                    var responseContent = webhookResult.ResponseBody ?? "";
                    _logger.LogInformation("AD account creation webhook response: {Response}", responseContent);

                    // Try to parse the response to get JobIds
                    string? jobId = null;
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(responseContent);
                        if (jsonDoc.RootElement.TryGetProperty("JobIds", out var jobIds) &&
                            jobIds.ValueKind == JsonValueKind.Array &&
                            jobIds.GetArrayLength() > 0)
                        {
                            jobId = jobIds[0].GetString();
                            _logger.LogInformation("AD account creation job ID: {JobId}", jobId);

                            // Store job ID in TempData for potential status checking
                            TempData["ADJobId"] = jobId;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Could not parse webhook response for job ID: {Error}", ex.Message);
                        // Continue anyway - the webhook might have succeeded even if we can't parse the response
                    }

                    // Wait for the job to complete and check its status
                    if (!string.IsNullOrEmpty(jobId))
                    {
                        _logger.LogInformation("Monitoring AD account creation job {JobId}", jobId);

                        // Get Azure configuration from environment variables
                        var subscriptionId = Environment.GetEnvironmentVariable("Azure_SubscriptionId");
                        var resourceGroup = Environment.GetEnvironmentVariable("Azure_ResourceGroup");
                        var automationAccount = Environment.GetEnvironmentVariable("Azure_AutomationAccount");

                        if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroup) || string.IsNullOrEmpty(automationAccount))
                        {
                            _logger.LogError("Azure configuration not complete. Cannot monitor job status. Required environment variables: Azure_SubscriptionId, Azure_ResourceGroup, Azure_AutomationAccount");
                            ModelState.AddModelError("", "Azure configuration incomplete. Cannot verify AD account creation status. Please contact system administrator.");

                            await LoadAvailableTiersToViewBag();
                            await SetMultiTenantViewBag();
                            return View();
                        }
                        else
                        {
                            try
                            {
                                // Get Azure Management token
                                var credential = new DefaultAzureCredential();
                                var token = await credential.GetTokenAsync(
                                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                                    HttpContext.RequestAborted
                                ).ConfigureAwait(false);

                                // Construct job status URL
                                var jobUrl = $"https://management.azure.com/subscriptions/{subscriptionId}" +
                                            $"/resourceGroups/{resourceGroup}" +
                                            $"/providers/Microsoft.Automation" +
                                            $"/automationAccounts/{automationAccount}" +
                                            $"/jobs/{jobId}?api-version=2023-11-01";

                                // Poll for job completion
                                var statusClient = _httpClientFactory.CreateClient();
                                statusClient.DefaultRequestHeaders.Authorization =
                                    new AuthenticationHeaderValue("Bearer", token.Token);
                                statusClient.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));

                                string jobStatus = "Processing";
                                int maxAttempts = 30;

                                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                                {
                                    // Check if the request has been cancelled
                                    if (HttpContext.RequestAborted.IsCancellationRequested)
                                    {
                                        _logger.LogWarning("Request cancelled while monitoring job status");
                                        break;
                                    }

                                    _logger.LogInformation("Checking job status, attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);

                                    using var statusResponse = await statusClient.GetAsync(jobUrl).ConfigureAwait(false);

                                    if (statusResponse.IsSuccessStatusCode)
                                    {
                                        var statusJson = await statusResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                                        using var statusDoc = JsonDocument.Parse(statusJson);

                                        if (statusDoc.RootElement.TryGetProperty("properties", out var properties) &&
                                            properties.TryGetProperty("provisioningState", out var provisioningState))
                                        {
                                            jobStatus = provisioningState.GetString() ?? "Unknown";
                                            _logger.LogInformation("Job {JobId} status: {Status}", jobId, jobStatus);

                                            if (jobStatus == "Succeeded")
                                            {
                                                _logger.LogInformation("AD account creation job {JobId} completed successfully", jobId);

                                                // Optionally fetch job output
                                                try
                                                {
                                                    var outputUrl = $"{jobUrl.Replace("?api-version=2023-11-01", "")}/output?api-version=2023-11-01";
                                                    statusClient.DefaultRequestHeaders.Clear();
                                                    statusClient.DefaultRequestHeaders.Authorization =
                                                        new AuthenticationHeaderValue("Bearer", token.Token);
                                                    statusClient.DefaultRequestHeaders.Accept.Add(
                                                        new MediaTypeWithQualityHeaderValue("text/plain"));

                                                    using var outputResponse = await statusClient.GetAsync(outputUrl).ConfigureAwait(false);
                                                    if (outputResponse.IsSuccessStatusCode)
                                                    {
                                                        var output = await outputResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                                                        _logger.LogInformation("Job output: {Output}", output);
                                                        TempData["ADJobOutput"] = output;
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    _logger.LogWarning(ex, "Could not fetch job output, but job succeeded");
                                                }

                                                adAccountCreated = true;
                                                break;
                                            }
                                            else if (jobStatus == "Failed")
                                            {
                                                _logger.LogError("AD account creation job {JobId} failed", jobId);

                                                // Try to get error details
                                                if (properties.TryGetProperty("exception", out var exception))
                                                {
                                                    var errorMessage = exception.GetString();
                                                    ModelState.AddModelError("", $"AD account creation failed: {errorMessage}");
                                                }
                                                else
                                                {
                                                    ModelState.AddModelError("", "AD account creation job failed. Please check Azure Automation logs.");
                                                }

                                                await LoadAvailableTiersToViewBag();
                                                await SetMultiTenantViewBag();
                                                return View();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Failed to get job status: {StatusCode}", statusResponse.StatusCode);
                                    }

                                    // If not succeeded or failed, wait before next attempt
                                    if (attempt < maxAttempts && jobStatus != "Succeeded")
                                    {
                                        await Task.Delay(10000).ConfigureAwait(false); // Wait 10 seconds
                                    }
                                }

                                // Check final status
                                if (jobStatus != "Succeeded")
                                {
                                    _logger.LogWarning("Job {JobId} did not complete successfully within timeout. Final status: {Status}", jobId, jobStatus);
                                    ModelState.AddModelError("", $"AD account creation did not complete within time limit. Job status: {jobStatus}");

                                    await LoadAvailableTiersToViewBag();
                                    await SetMultiTenantViewBag();
                                    return View();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error monitoring job status");
                                // Fall back to waiting
                                _logger.LogWarning("Could not monitor job status, waiting 30 seconds");
                                await Task.Delay(30000);
                            }
                        }
                    }
                    else
                    {
                        // No job ID received, this is an error
                        _logger.LogError("No job ID received from webhook");
                        ModelState.AddModelError("", "AD account creation webhook did not return a job ID");

                        await LoadAvailableTiersToViewBag();
                        await SetMultiTenantViewBag();
                        return View();
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Network error calling AD account creation webhook");
                    ModelState.AddModelError("", "Network error creating Active Directory account. Please try again.");

                    await LoadAvailableTiersToViewBag();
                    await SetMultiTenantViewBag();

                    return View();
                }
                catch (TaskCanceledException)
                {
                    _logger.LogError("Timeout calling AD account creation webhook");
                    ModelState.AddModelError("", "Timeout creating Active Directory account. The operation took too long.");

                    await LoadAvailableTiersToViewBag();
                    await SetMultiTenantViewBag();

                    return View();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during AD account creation");
                    ModelState.AddModelError("", "An unexpected error occurred during AD account creation.");

                    await LoadAvailableTiersToViewBag();
                    await SetMultiTenantViewBag();

                    return View();
                }
            }
            else
            {
                _logger.LogInformation("No AD webhook configured, treating as success");
                adAccountCreated = true; // No webhook = no AD account needed
            }

            // Only create the user if AD account creation succeeded (or wasn't needed)
            if (adAccountCreated || !webhookEnvConfigured)
            {
                try
                {
                    // Create the new user
                    var newUser = new ElevateUser
                    {
                        UserName = userName,
                        ElevateAccount = elevateAccount,
                        PhoneNumber = phoneNumber,
                        Tier = tier,
                        Status = "active",
                        CustomerId = effectiveCustomerId,
                        Created = DateTime.UtcNow,
                        CreatedBy = User.Identity?.Name ?? "System"
                    };

                    _context.ElevateUsers.Add(newUser);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserName} created successfully with ID {UserId}. AD Account: {ADCreated}",
                        userName, newUser.ElevateUsersId, webhookEnvConfigured);

                    await _auditService.LogAsync("UserCreated", new { userId = newUser.ElevateUsersId, userName, elevateAccount, tier });

                    // Add user to the tier's Entra group
                    await SyncEntraGroupMembership(userName, null, tier);

                    TempData["SuccessMessage"] = $"User {userName} created successfully!";
                    TempData["NewUserId"] = newUser.ElevateUsersId;

                    // Force immediate redirect to prevent double-submission
                    await Task.Yield();

                    return RedirectToAction(nameof(UserAdministration));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving user to database or redirecting");
                    ModelState.AddModelError("", "An error occurred while saving the user. Please try again.");

                    await LoadAvailableTiersToViewBag();
                    await SetMultiTenantViewBag();
                    return View();
                }
            }
            else
            {
                _logger.LogError("AD account creation failed. User not added to database.");
                ModelState.AddModelError("", "Failed to create Active Directory account. User was not added to the database.");

                await LoadAvailableTiersToViewBag();
                await SetMultiTenantViewBag();
                return View();
            }
        }

        // Async user creation endpoint - creates user with pending status and launches background job
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserAsync(string userName, string elevateAccount, string phoneNumber, int tier)
        {
            var (effectiveCustomerId, _) = await GetAdminScopeAsync();

            // Validate input
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Json(new { success = false, error = "Username is required." });
            }

            if (string.IsNullOrWhiteSpace(elevateAccount))
            {
                return Json(new { success = false, error = "Elevate account is required." });
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return Json(new { success = false, error = "Phone number is required." });
            }

            if (tier == 0)
            {
                return Json(new { success = false, error = "Tier is required." });
            }

            // Check if elevate account already exists (same username can have multiple elevate accounts)
            var existingUser = await _context.ElevateUsers
                .FirstOrDefaultAsync(u => u.ElevateAccount == elevateAccount);

            if (existingUser != null)
            {
                return Json(new { success = false, error = "An elevate account with this name already exists." });
            }

            // Get current user info for audit fields
            var currentUserName = User.Identity?.Name ?? "System";

            // Call webhook to create Active Directory account
            var webhookEnvConfigured2 = !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("Create_ElevateAccount_webhook")?.Trim());

            if (webhookEnvConfigured2)
            {
                try
                {
                    _logger.LogInformation("Calling AD account creation webhook for user: {UserName}", userName);

                    var payload = new { username = elevateAccount };

                    var webhookResult = await _webhookService.PostFromEnvAsync("Create_ElevateAccount_webhook", payload);

                    if (!webhookResult.Success)
                    {
                        // Create user with error status
                        var errorUser = new ElevateUser
                        {
                            UserName = userName,
                            ElevateAccount = elevateAccount,
                            PhoneNumber = phoneNumber,
                            Tier = tier,
                            Status = "error",
                            CustomerId = effectiveCustomerId,
                            Created = DateTime.UtcNow,
                            CreatedBy = currentUserName
                        };

                        _context.ElevateUsers.Add(errorUser);
                        await _context.SaveChangesAsync();

                        return Json(new { success = false, error = "Failed to initiate Active Directory account creation. User created with error status." });
                    }

                    // Parse the response to get job ID
                    var responseContent = webhookResult.ResponseBody ?? "";
                    _logger.LogInformation("AD account creation webhook response: {Response}", responseContent);

                    string? jobId = null;
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(responseContent);
                        if (jsonDoc.RootElement.TryGetProperty("JobIds", out var jobIds) &&
                            jobIds.ValueKind == JsonValueKind.Array &&
                            jobIds.GetArrayLength() > 0)
                        {
                            jobId = jobIds[0].GetString();
                            _logger.LogInformation("AD account creation job ID: {JobId}", jobId);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Could not parse webhook response for job ID: {Error}", ex.Message);
                    }

                    if (string.IsNullOrEmpty(jobId))
                    {
                        _logger.LogError("No job ID received from webhook");

                        // Create user with error status
                        var errorUser = new ElevateUser
                        {
                            UserName = userName,
                            ElevateAccount = elevateAccount,
                            PhoneNumber = phoneNumber,
                            Tier = tier,
                            Status = "error",
                            CustomerId = effectiveCustomerId,
                            Created = DateTime.UtcNow,
                            CreatedBy = currentUserName
                        };

                        _context.ElevateUsers.Add(errorUser);
                        await _context.SaveChangesAsync();

                        return Json(new { success = false, error = "AD account creation webhook did not return a job ID. User created with error status." });
                    }

                    // Create user with pending status
                    var newUser = new ElevateUser
                    {
                        UserName = userName,
                        ElevateAccount = elevateAccount,
                        PhoneNumber = phoneNumber,
                        Tier = tier,
                        Status = "pending",
                        CustomerId = effectiveCustomerId,
                        Created = DateTime.UtcNow,
                        CreatedBy = currentUserName
                    };

                    _context.ElevateUsers.Add(newUser);
                    await _context.SaveChangesAsync();

                    // Create job tracking record
                    var jobRecord = new ElevateJob
                    {
                        Type = "user",
                        Reference = newUser.ElevateUsersId.ToString(),
                        Job = jobId,
                        CustomerId = effectiveCustomerId,
                        Created = DateTime.UtcNow,
                        CreatedBy = currentUserName,
                        Updated = DateTime.UtcNow,
                        UpdatedBy = currentUserName
                    };

                    _context.ElevateJobs.Add(jobRecord);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserName} created with pending status. Job ID: {JobId}", userName, jobId);

                    await _auditService.LogAsync("UserCreated", new { userId = newUser.ElevateUsersId, userName, elevateAccount, tier, status = "pending", jobId });

                    // Add user to the tier's Entra group
                    await SyncEntraGroupMembership(userName, null, tier);

                    // Queue background job to monitor status
                    _ = Task.Run(async () => await MonitorJobStatus(jobId, newUser.ElevateUsersId));

                    return Json(new {
                        success = true,
                        message = "User creation initiated. The account will be activated once the AD account is created.",
                        userId = newUser.ElevateUsersId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during user creation");
                    return Json(new { success = false, error = "An unexpected error occurred. Please try again." });
                }
            }
            else
            {
                // No webhook configured, create user directly with active status
                try
                {
                    var newUser = new ElevateUser
                    {
                        UserName = userName,
                        ElevateAccount = elevateAccount,
                        PhoneNumber = phoneNumber,
                        Tier = tier,
                        Status = "active",
                        CustomerId = effectiveCustomerId,
                        Created = DateTime.UtcNow,
                        CreatedBy = currentUserName
                    };

                    _context.ElevateUsers.Add(newUser);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserName} created successfully with active status (no AD account)", userName);

                    await _auditService.LogAsync("UserCreated", new { userId = newUser.ElevateUsersId, userName, elevateAccount, tier, status = "active" });

                    // Add user to the tier's Entra group
                    await SyncEntraGroupMembership(userName, null, tier);

                    return Json(new {
                        success = true,
                        message = "User created successfully.",
                        userId = newUser.ElevateUsersId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user in database");
                    return Json(new { success = false, error = "Failed to create user in database." });
                }
            }
        }

        // Background job to monitor Azure job status
        private async Task MonitorJobStatus(string jobId, int userId)
        {
            try
            {
                _logger.LogInformation("Starting background job monitoring for Job ID: {JobId}, User ID: {UserId}", jobId, userId);

                // Get Azure configuration
                var subscriptionId = Environment.GetEnvironmentVariable("Azure_SubscriptionId");
                var resourceGroup = Environment.GetEnvironmentVariable("Azure_ResourceGroup");
                var automationAccount = Environment.GetEnvironmentVariable("Azure_AutomationAccount");

                if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroup) || string.IsNullOrEmpty(automationAccount))
                {
                    _logger.LogError("Azure configuration not complete for job monitoring");
                    await UpdateUserStatus(userId, "error");
                    return;
                }

                // Wait a bit before starting to check
                await Task.Delay(10000); // 10 seconds initial delay

                var maxAttempts = 60; // 10 minutes maximum (60 * 10 seconds)
                var jobStatus = "Processing";

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        // Get Azure Management token
                        var credential = new DefaultAzureCredential();
                        var token = await credential.GetTokenAsync(
                            new TokenRequestContext(new[] { "https://management.azure.com/.default" })
                        );

                        // Construct job status URL
                        var jobUrl = $"https://management.azure.com/subscriptions/{subscriptionId}" +
                                    $"/resourceGroups/{resourceGroup}" +
                                    $"/providers/Microsoft.Automation" +
                                    $"/automationAccounts/{automationAccount}" +
                                    $"/jobs/{jobId}?api-version=2023-11-01";

                        var httpClient = _httpClientFactory.CreateClient();
                        httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                        httpClient.DefaultRequestHeaders.Accept.Add(
                            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                        using var statusResponse = await httpClient.GetAsync(jobUrl);

                        if (statusResponse.IsSuccessStatusCode)
                        {
                            var statusJson = await statusResponse.Content.ReadAsStringAsync();
                            using var statusDoc = JsonDocument.Parse(statusJson);

                            if (statusDoc.RootElement.TryGetProperty("properties", out var properties) &&
                                properties.TryGetProperty("provisioningState", out var provisioningState))
                            {
                                jobStatus = provisioningState.GetString() ?? "Unknown";
                                _logger.LogInformation("Job {JobId} status: {Status} (attempt {Attempt}/{MaxAttempts})",
                                    jobId, jobStatus, attempt, maxAttempts);

                                if (jobStatus == "Succeeded")
                                {
                                    _logger.LogInformation("AD account creation job {JobId} completed successfully", jobId);
                                    await UpdateUserStatus(userId, "active");
                                    return;
                                }
                                else if (jobStatus == "Failed")
                                {
                                    _logger.LogError("AD account creation job {JobId} failed", jobId);
                                    await UpdateUserStatus(userId, "error");
                                    return;
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to get job status for {JobId}: {StatusCode}", jobId, statusResponse.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking job status for {JobId}", jobId);
                    }

                    // Wait before next check
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(10000); // 10 seconds between checks
                    }
                }

                // If we've reached here, the job didn't complete in time
                _logger.LogWarning("Job {JobId} monitoring timed out after {MaxAttempts} attempts", jobId, maxAttempts);
                await UpdateUserStatus(userId, "error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in job monitoring for Job ID: {JobId}", jobId);
                await UpdateUserStatus(userId, "error");
            }
        }

        // Update user status in database
        private async Task UpdateUserStatus(int userId, string status)
        {
            try
            {
                // Create a new scope for database operations
                using var scope = _serviceScopeFactory.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<Sqldb_securitySolutionsCommon>();

                var user = await context.ElevateUsers.FindAsync(userId);
                if (user != null)
                {
                    user.Status = status;
                    user.Updated = DateTime.UtcNow;
                    user.UpdatedBy = "System";
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Updated user {UserId} status to {Status}", userId, status);
                }
                else
                {
                    _logger.LogWarning("User {UserId} not found for status update", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user {UserId} status to {Status}", userId, status);
            }
        }

        // Check status of user creation job
        [HttpGet]
        public async Task<IActionResult> CheckUserCreationStatus(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return Json(new { success = false, error = "Job ID is required." });
            }

            // Retrieve job info from session
            var jobInfoJson = HttpContext.Session.GetString($"UserCreationJob_{jobId}");
            if (string.IsNullOrEmpty(jobInfoJson))
            {
                return Json(new { success = false, error = "Job not found or expired." });
            }

            UserCreationJobInfo? jobInfo = null;
            try
            {
                jobInfo = JsonSerializer.Deserialize<UserCreationJobInfo>(jobInfoJson);
            }
            catch
            {
                return Json(new { success = false, error = "Failed to parse job information." });
            }

            if (jobInfo == null)
            {
                return Json(new { success = false, error = "Invalid job information." });
            }

            // Get Azure configuration
            var subscriptionId = Environment.GetEnvironmentVariable("Azure_SubscriptionId");
            var resourceGroup = Environment.GetEnvironmentVariable("Azure_ResourceGroup");
            var automationAccount = Environment.GetEnvironmentVariable("Azure_AutomationAccount");

            if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroup) || string.IsNullOrEmpty(automationAccount))
            {
                _logger.LogError("Azure configuration not complete for job status check");

                // If we can't check status, just create the user after a reasonable wait
                if ((DateTime.UtcNow - jobInfo.StartTime).TotalSeconds > 30)
                {
                    return await CompleteUserCreation(jobInfo, true);
                }

                return Json(new {
                    success = true,
                    status = "Processing",
                    message = "Cannot verify AD status, but will complete shortly..."
                });
            }

            try
            {
                // Get Azure Management token
                var credential = new DefaultAzureCredential();
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    HttpContext.RequestAborted
                );

                // Construct job status URL
                var jobUrl = $"https://management.azure.com/subscriptions/{subscriptionId}" +
                            $"/resourceGroups/{resourceGroup}" +
                            $"/providers/Microsoft.Automation" +
                            $"/automationAccounts/{automationAccount}" +
                            $"/jobs/{jobId}?api-version=2023-11-01";

                var statusClient = _httpClientFactory.CreateClient();
                statusClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Token);
                statusClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                using var statusResponse = await statusClient.GetAsync(jobUrl);

                if (statusResponse.IsSuccessStatusCode)
                {
                    var statusJson = await statusResponse.Content.ReadAsStringAsync();
                    using var statusDoc = JsonDocument.Parse(statusJson);

                    string jobStatus = "Unknown";
                    if (statusDoc.RootElement.TryGetProperty("properties", out var properties) &&
                        properties.TryGetProperty("provisioningState", out var provisioningState))
                    {
                        jobStatus = provisioningState.GetString() ?? "Unknown";
                        _logger.LogInformation("Job {JobId} status: {Status}", jobId, jobStatus);

                        if (jobStatus == "Succeeded")
                        {
                            _logger.LogInformation("AD account creation job {JobId} completed successfully", jobId);
                            return await CompleteUserCreation(jobInfo, true);
                        }
                        else if (jobStatus == "Failed")
                        {
                            _logger.LogError("AD account creation job {JobId} failed", jobId);

                            // Try to get error details
                            string? errorMessage = null;
                            if (properties.TryGetProperty("exception", out var exception))
                            {
                                errorMessage = exception.GetString();
                            }

                            return Json(new {
                                success = false,
                                error = errorMessage ?? "AD account creation job failed."
                            });
                        }
                        else
                        {
                            // Still processing
                            return Json(new {
                                success = true,
                                status = jobStatus,
                                message = "AD account creation in progress..."
                            });
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to get job status: {StatusCode}", statusResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking job status");
            }

            // If we can't determine status but enough time has passed, complete the user creation
            if ((DateTime.UtcNow - jobInfo.StartTime).TotalMinutes > 5)
            {
                _logger.LogWarning("Job status check inconclusive after 5 minutes, proceeding with user creation");
                return await CompleteUserCreation(jobInfo, true);
            }

            return Json(new {
                success = true,
                status = "Processing",
                message = "Checking AD account status..."
            });
        }

        private async Task<IActionResult> CompleteUserCreation(UserCreationJobInfo jobInfo, bool adAccountCreated)
        {
            try
            {
                // Check if elevate account was already created (in case of duplicate calls)
                var existingUser = await _context.ElevateUsers
                    .FirstOrDefaultAsync(u => u.ElevateAccount == jobInfo.ElevateAccount);

                if (existingUser != null)
                {
                    _logger.LogInformation("User already exists, returning success");

                    // Clean up session
                    HttpContext.Session.Remove($"UserCreationJob_{jobInfo.JobId}");

                    return Json(new {
                        success = true,
                        completed = true,
                        message = "User already exists in the system."
                    });
                }

                var (effectiveCustomerId, _) = await GetAdminScopeAsync();

                // Create the new user
                var newUser = new ElevateUser
                {
                    UserName = jobInfo.UserName,
                    ElevateAccount = jobInfo.ElevateAccount,
                    PhoneNumber = jobInfo.PhoneNumber,
                    Tier = jobInfo.Tier,
                    Status = adAccountCreated ? "active" : "error",
                    CustomerId = effectiveCustomerId,
                    Created = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                _context.ElevateUsers.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserName} created successfully. AD Account: {ADCreated}",
                    jobInfo.UserName, adAccountCreated ? "Yes" : "No");

                await _auditService.LogAsync("UserCreated", new { userId = newUser.ElevateUsersId, userName = jobInfo.UserName, elevateAccount = jobInfo.ElevateAccount, tier = jobInfo.Tier, adAccountCreated });

                // Add user to the tier's Entra group
                await SyncEntraGroupMembership(jobInfo.UserName, null, jobInfo.Tier);

                // Clean up session
                HttpContext.Session.Remove($"UserCreationJob_{jobInfo.JobId}");

                return Json(new {
                    success = true,
                    completed = true,
                    message = adAccountCreated
                        ? "User created successfully with Active Directory account."
                        : "User created successfully (AD account creation may be pending)."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user in database");
                return Json(new { success = false, error = "Failed to create user in database." });
            }
        }

        // ──────────────────────────────────────────────
        // Entra user search
        // ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> SearchEntraUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return Json(new List<object>());
            }

            try
            {
                var sanitizedQuery = query.Trim().Replace("'", "''");

                var credential = new DefaultAzureCredential();
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }),
                    HttpContext.RequestAborted
                ).ConfigureAwait(false);

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Token);
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var filter = $"startswith(displayName,'{sanitizedQuery}') or startswith(userPrincipalName,'{sanitizedQuery}')";
                var url = $"https://graph.microsoft.com/v1.0/users?$filter={Uri.EscapeDataString(filter)}&$select=displayName,userPrincipalName,mail&$top=20";

                using var response = await client.GetAsync(url).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Graph API search failed: {StatusCode}", response.StatusCode);
                    return Json(new List<object>());
                }

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);

                var results = new List<object>();
                if (doc.RootElement.TryGetProperty("value", out var value))
                {
                    foreach (var user in value.EnumerateArray())
                    {
                        results.Add(new
                        {
                            displayName = user.TryGetProperty("displayName", out var dn) ? dn.GetString() : "",
                            userPrincipalName = user.TryGetProperty("userPrincipalName", out var upn) ? upn.GetString() : "",
                            mail = user.TryGetProperty("mail", out var m) ? m.GetString() : ""
                        });
                    }
                }

                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Entra ID users");
                return Json(new List<object>());
            }
        }

        // ──────────────────────────────────────────────
        // Helper class
        // ──────────────────────────────────────────────

        private class UserCreationJobInfo
        {
            public string JobId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string ElevateAccount { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public int Tier { get; set; }
            public DateTime StartTime { get; set; }
        }
    }
}
