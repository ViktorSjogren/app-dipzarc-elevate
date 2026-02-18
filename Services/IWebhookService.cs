using System.Net;

namespace dizparc_elevate.Services
{
    public class WebhookResult
    {
        public bool Success { get; set; }
        public HttpStatusCode? StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public string? ErrorMessage { get; set; }
        public int Attempts { get; set; }
    }

    public interface IWebhookService
    {
        /// <summary>
        /// Posts JSON payload to a webhook URL with retry logic.
        /// Retries for up to <paramref name="maxDurationSeconds"/> seconds.
        /// </summary>
        Task<WebhookResult> PostAsync(string webhookUrl, object payload, int maxDurationSeconds = 60, int timeoutSeconds = 5);

        /// <summary>
        /// Reads a webhook URL from an environment variable, then posts with retry logic.
        /// Returns a failure result if the env var is missing or empty.
        /// </summary>
        Task<WebhookResult> PostFromEnvAsync(string envVarName, object payload, int maxDurationSeconds = 60, int timeoutSeconds = 5);
    }
}
