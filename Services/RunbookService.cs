using Azure.Identity;
using Azure.Core;
using dizparc_elevate.Models.securitySolutionsCommon;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace dizparc_elevate.Services
{
    public class RunbookService : IRunbookService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RunbookService> _logger;

        private readonly string _subscriptionId;
        private readonly string _resourceGroup;
        private readonly string _automationAccount;

        public RunbookService(
            IHttpClientFactory httpClientFactory,
            IServiceScopeFactory scopeFactory,
            ILogger<RunbookService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _scopeFactory = scopeFactory;
            _logger = logger;

            _subscriptionId = Environment.GetEnvironmentVariable("Azure_SubscriptionId")
                ?? throw new InvalidOperationException("Azure_SubscriptionId environment variable is not configured.");
            _resourceGroup = Environment.GetEnvironmentVariable("Azure_ResourceGroup")
                ?? throw new InvalidOperationException("Azure_ResourceGroup environment variable is not configured.");
            _automationAccount = Environment.GetEnvironmentVariable("Azure_AutomationAccount")
                ?? throw new InvalidOperationException("Azure_AutomationAccount environment variable is not configured.");
        }

        public async Task<RunbookJobResult> StartRunbookAsync(
            string runbookName,
            int customerId,
            Dictionary<string, string> parameters,
            CancellationToken cancellationToken = default)
        {
            var jobId = Guid.NewGuid().ToString();

            try
            {
                // Look up TenantId (= Hybrid Worker Group name) for the customer
                string? tenantId;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<Sqldb_securitySolutionsCommon>();
                    tenantId = await context.CustomersData
                        .Where(cd => cd.CustomerId == customerId)
                        .Select(cd => cd.TenantId)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                if (string.IsNullOrEmpty(tenantId))
                {
                    _logger.LogError(
                        "No TenantId found for CustomerId {CustomerId}. Cannot start runbook {Runbook}.",
                        customerId, runbookName);
                    return new RunbookJobResult
                    {
                        Success = false,
                        JobId = jobId,
                        ErrorMessage = $"No TenantId (Hybrid Worker Group) configured for customer {customerId}."
                    };
                }

                // Acquire Azure Management token
                var credential = new DefaultAzureCredential();
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    cancellationToken);

                // Build the PUT request
                var url = $"https://management.azure.com/subscriptions/{_subscriptionId}"
                    + $"/resourceGroups/{_resourceGroup}"
                    + $"/providers/Microsoft.Automation"
                    + $"/automationAccounts/{_automationAccount}"
                    + $"/jobs/{jobId}?api-version=2023-11-01";

                var body = new
                {
                    properties = new
                    {
                        runbook = new { name = runbookName },
                        parameters = parameters,
                        runOn = tenantId
                    }
                };

                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var httpClient = _httpClientFactory.CreateClient("AzureManagement");
                using var request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation(
                    "Starting runbook {Runbook} for CustomerId {CustomerId} on HybridWorker {HWG}. JobId: {JobId}",
                    runbookName, customerId, tenantId, jobId);

                using var response = await httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Failed to start runbook {Runbook}. Status: {Status}, Response: {Response}",
                        runbookName, (int)response.StatusCode, responseBody);

                    return new RunbookJobResult
                    {
                        Success = false,
                        JobId = jobId,
                        StatusCode = (int)response.StatusCode,
                        ErrorMessage = $"Azure Automation returned {(int)response.StatusCode}: {responseBody}"
                    };
                }

                // Parse provisioning state from response
                string? provisioningState = null;
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("properties", out var props) &&
                        props.TryGetProperty("provisioningState", out var ps))
                    {
                        provisioningState = ps.GetString();
                    }
                }
                catch (JsonException)
                {
                    // Not fatal - we still have a successful HTTP status
                }

                _logger.LogInformation(
                    "Runbook {Runbook} started. JobId: {JobId}, ProvisioningState: {State}",
                    runbookName, jobId, provisioningState ?? "unknown");

                return new RunbookJobResult
                {
                    Success = true,
                    JobId = jobId,
                    StatusCode = (int)response.StatusCode,
                    ProvisioningState = provisioningState
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception starting runbook {Runbook} for CustomerId {CustomerId}. JobId: {JobId}",
                    runbookName, customerId, jobId);

                return new RunbookJobResult
                {
                    Success = false,
                    JobId = jobId,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }
    }
}
