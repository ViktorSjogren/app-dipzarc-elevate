using Azure.Core;
using Azure.Identity;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace dizparc_elevate.Services
{
    public class GraphService : IGraphService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GraphService> _logger;
        private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

        public GraphService(IHttpClientFactory httpClientFactory, ILogger<GraphService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> AddUserToGroupAsync(string userPrincipalName, string groupId)
        {
            try
            {
                var userId = await ResolveUserIdAsync(userPrincipalName);
                if (userId == null)
                {
                    _logger.LogWarning("Could not resolve Entra user ID for UPN {UPN}", userPrincipalName);
                    return false;
                }

                var client = await CreateGraphClientAsync();

                var jsonBody = $"{{\"@odata.id\":\"{GraphBaseUrl}/directoryObjects/{userId}\"}}";

                var request = new HttpRequestMessage(HttpMethod.Post, $"{GraphBaseUrl}/groups/{groupId}/members/$ref")
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                using var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Added user {UPN} to Entra group {GroupId}", userPrincipalName, groupId);
                    return true;
                }

                // 400 with "already exist" means they're already a member - treat as success
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (responseBody.Contains("already exist", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("User {UPN} is already a member of Entra group {GroupId}", userPrincipalName, groupId);
                        return true;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to add user {UPN} to Entra group {GroupId}. Status: {Status}, Response: {Response}",
                    userPrincipalName, groupId, (int)response.StatusCode, errorContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {UPN} to Entra group {GroupId}", userPrincipalName, groupId);
                return false;
            }
        }

        public async Task<bool> RemoveUserFromGroupAsync(string userPrincipalName, string groupId)
        {
            try
            {
                var userId = await ResolveUserIdAsync(userPrincipalName);
                if (userId == null)
                {
                    _logger.LogWarning("Could not resolve Entra user ID for UPN {UPN}", userPrincipalName);
                    return false;
                }

                var client = await CreateGraphClientAsync();

                var request = new HttpRequestMessage(HttpMethod.Delete,
                    $"{GraphBaseUrl}/groups/{groupId}/members/{userId}/$ref");

                using var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Removed user {UPN} from Entra group {GroupId}", userPrincipalName, groupId);
                    return true;
                }

                // 404 means they're not a member - treat as success
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("User {UPN} was not a member of Entra group {GroupId}", userPrincipalName, groupId);
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to remove user {UPN} from Entra group {GroupId}. Status: {Status}, Response: {Response}",
                    userPrincipalName, groupId, (int)response.StatusCode, errorContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UPN} from Entra group {GroupId}", userPrincipalName, groupId);
                return false;
            }
        }

        private async Task<string?> ResolveUserIdAsync(string userPrincipalName)
        {
            var client = await CreateGraphClientAsync();

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{GraphBaseUrl}/users/{Uri.EscapeDataString(userPrincipalName)}?$select=id");

            using var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to resolve user {UPN}. Status: {Status}, Response: {Response}",
                    userPrincipalName, (int)response.StatusCode, errorContent);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("id").GetString();
        }

        private async Task<HttpClient> CreateGraphClientAsync()
        {
            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token.Token);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}
