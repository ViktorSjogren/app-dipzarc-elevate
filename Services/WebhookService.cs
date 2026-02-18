using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace dizparc_elevate.Services
{
    public class WebhookService : IWebhookService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WebhookService> _logger;

        public WebhookService(IHttpClientFactory httpClientFactory, ILogger<WebhookService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<WebhookResult> PostFromEnvAsync(string envVarName, object payload, int maxDurationSeconds = 60, int timeoutSeconds = 5)
        {
            var rawUrl = Environment.GetEnvironmentVariable(envVarName)?.Trim();

            if (string.IsNullOrEmpty(rawUrl))
            {
                _logger.LogError("Webhook URL environment variable '{EnvVar}' is not configured", envVarName);
                return new WebhookResult
                {
                    Success = false,
                    ErrorMessage = $"Webhook environment variable '{envVarName}' is not configured."
                };
            }

            return await PostAsync(rawUrl, payload, maxDurationSeconds, timeoutSeconds);
        }

        public async Task<WebhookResult> PostAsync(string webhookUrl, object payload, int maxDurationSeconds = 60, int timeoutSeconds = 5)
        {
            var json = JsonSerializer.Serialize(payload);

            // Azure Automation is case-sensitive on percent-encoded hex digits in
            // webhook tokens (e.g. %3d != %3D). .NET's Uri class normalizes these
            // to uppercase, which breaks token matching. Using
            // DangerousDisablePathAndQueryCanonicalization preserves the URL exactly
            // as provided - no decoding, no case changes.
            var uri = new Uri(webhookUrl, new UriCreationOptions
            {
                DangerousDisablePathAndQueryCanonicalization = true
            });

            // Azure Automation DNS round-robins to multiple backend IPs and some
            // can be intermittently unresponsive. We retry with a time-based
            // deadline (default 60s) rather than a fixed attempt count, so flaky
            // backends get plenty of chances to respond.
            var deadline = Stopwatch.StartNew();
            HttpResponseMessage? response = null;
            Exception? lastException = null;
            int attempt = 0;

            while (deadline.Elapsed.TotalSeconds < maxDurationSeconds)
            {
                attempt++;
                try
                {
                    var httpClient = _httpClientFactory.CreateClient("Webhook");
                    httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                    var request = new HttpRequestMessage(HttpMethod.Post, uri);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    // Force HTTP/1.1 to prevent connection multiplexing - ensures
                    // each retry opens a fresh TCP connection (new DNS resolution).
                    request.Version = HttpVersion.Version11;
                    request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    response = await httpClient.SendAsync(request);

                    // Handle 429 (rate limiting) - wait and retry
                    if (response.StatusCode == HttpStatusCode.TooManyRequests
                        && deadline.Elapsed.TotalSeconds < maxDurationSeconds)
                    {
                        var retryAfter = response.Headers.RetryAfter?.Delta?.TotalMilliseconds
                            ?? attempt * 2000;
                        _logger.LogWarning("Webhook attempt {Attempt} got 429 (rate limited). Waiting {Delay}ms... [{Elapsed:F1}s/{Max}s]",
                            attempt, retryAfter, deadline.Elapsed.TotalSeconds, maxDurationSeconds);
                        await Task.Delay((int)retryAfter);
                        response = null;
                        continue;
                    }

                    _logger.LogInformation("Webhook attempt {Attempt} got status {StatusCode} after {Elapsed:F1}s",
                        attempt, (int)response.StatusCode, deadline.Elapsed.TotalSeconds);
                    break;
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    lastException = ex;
                    _logger.LogWarning("Webhook attempt {Attempt} failed: {Message}. [{Elapsed:F1}s/{Max}s]",
                        attempt, ex.Message, deadline.Elapsed.TotalSeconds, maxDurationSeconds);

                    // If we still have time, wait briefly then retry
                    if (deadline.Elapsed.TotalSeconds < maxDurationSeconds)
                    {
                        await Task.Delay(500);
                    }
                }
            }

            if (response == null)
            {
                _logger.LogError(lastException, "Webhook failed after {Attempts} attempts in {Elapsed:F1}s",
                    attempt, deadline.Elapsed.TotalSeconds);
                return new WebhookResult
                {
                    Success = false,
                    Attempts = attempt,
                    ErrorMessage = $"Failed to reach the webhook after {attempt} attempts ({deadline.Elapsed.TotalSeconds:F0}s). Last error: {lastException?.Message}"
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Webhook returned error. Status: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode, responseBody);
            }

            return new WebhookResult
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                ResponseBody = responseBody,
                Attempts = attempt,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"Webhook returned status {(int)response.StatusCode}"
            };
        }
    }
}
