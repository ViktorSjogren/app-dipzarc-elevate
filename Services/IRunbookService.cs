namespace dizparc_elevate.Services
{
    public class RunbookJobResult
    {
        /// <summary>Whether the job was started successfully (HTTP 2xx from Azure).</summary>
        public bool Success { get; set; }

        /// <summary>The job ID (client-generated GUID). Always populated, even on failure.</summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>The provisioning state from the response, e.g. "Processing".</summary>
        public string? ProvisioningState { get; set; }

        /// <summary>Error message if the call failed.</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>HTTP status code from the Azure Management API.</summary>
        public int? StatusCode { get; set; }
    }

    public interface IRunbookService
    {
        /// <summary>
        /// Starts an Azure Automation runbook on the Hybrid Worker Group for the specified customer.
        /// The Hybrid Worker Group name is the customer's TenantId from customersData.
        /// </summary>
        /// <param name="runbookName">The runbook name (use RunbookNames constants).</param>
        /// <param name="customerId">The customer ID, used to look up TenantId (= Hybrid Worker Group name).</param>
        /// <param name="parameters">Flat key-value parameters for the runbook.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        Task<RunbookJobResult> StartRunbookAsync(
            string runbookName,
            int customerId,
            Dictionary<string, string> parameters,
            CancellationToken cancellationToken = default);
    }
}
