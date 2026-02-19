using dizparc_elevate.Models.securitySolutionsCommon;
using Microsoft.EntityFrameworkCore;

namespace dizparc_elevate.Services
{
    public class ElevateJobMonitorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ElevateJobMonitorService> _logger;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(10);

        public ElevateJobMonitorService(
            IServiceScopeFactory scopeFactory,
            ILogger<ElevateJobMonitorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ElevateJobMonitorService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessJobsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Unhandled error in ElevateJobMonitorService cycle");
                }

                await Task.Delay(PollInterval, stoppingToken);
            }

            _logger.LogInformation("ElevateJobMonitorService stopped");
        }

        private async Task ProcessJobsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Sqldb_securitySolutionsCommon>();
            var runbookService = scope.ServiceProvider.GetRequiredService<IRunbookService>();

            var jobs = await context.ElevateJobs.ToListAsync(ct);
            if (jobs.Count == 0) return;

            _logger.LogDebug("Processing {Count} pending jobs", jobs.Count);

            foreach (var job in jobs)
            {
                try
                {
                    // Check for timeout
                    if (job.Created.HasValue && DateTime.UtcNow - job.Created.Value > JobTimeout)
                    {
                        _logger.LogWarning("Job {JobId} timed out (created {Created})", job.Job, job.Created);
                        await HandleFailedJob(context, job);
                        continue;
                    }

                    var status = await runbookService.GetJobStatusAsync(job.Job, ct);

                    if (!status.Success)
                    {
                        // Azure API error - skip, retry next cycle
                        _logger.LogWarning("Could not check status for job {JobId}: {Error}", job.Job, status.ErrorMessage);
                        continue;
                    }

                    switch (status.ProvisioningState)
                    {
                        case "Succeeded":
                            _logger.LogInformation("Job {JobId} succeeded (type={Type}, ref={Reference})", job.Job, job.Type, job.Reference);
                            await HandleSucceededJob(context, job);
                            break;

                        case "Failed":
                            _logger.LogWarning("Job {JobId} failed (type={Type}, ref={Reference})", job.Job, job.Type, job.Reference);
                            await HandleFailedJob(context, job);
                            break;

                        default:
                            // Still processing - do nothing
                            _logger.LogDebug("Job {JobId} still processing: {State}", job.Job, status.ProvisioningState);
                            break;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error processing job {JobId}", job.Job);
                }
            }
        }

        private async Task HandleSucceededJob(Sqldb_securitySolutionsCommon context, ElevateJob job)
        {
            if (job.Type == "user")
            {
                if (int.TryParse(job.Reference, out var userId))
                {
                    var user = await context.ElevateUsers.FindAsync(userId);
                    if (user != null)
                    {
                        user.Status = "active";
                        user.Updated = DateTime.UtcNow;
                        user.UpdatedBy = "System";
                    }
                }
            }
            else if (job.Type == "device")
            {
                var serverPermissionType = await context.ElevatePermissionTypes
                    .FirstOrDefaultAsync(pt => pt.Type == "server");

                if (serverPermissionType != null)
                {
                    var permission = await context.ElevatePermissions
                        .FirstOrDefaultAsync(p => p.Value == job.Reference && p.Type == serverPermissionType.ElevatePermissionTypesId);

                    if (permission != null)
                    {
                        permission.OnboardingStatus = 2; // In Progress (manual steps)
                        permission.Updated = DateTime.UtcNow;
                        permission.UpdatedBy = "System";
                    }
                }
            }

            context.ElevateJobs.Remove(job);
            await context.SaveChangesAsync();
        }

        private async Task HandleFailedJob(Sqldb_securitySolutionsCommon context, ElevateJob job)
        {
            if (job.Type == "user")
            {
                if (int.TryParse(job.Reference, out var userId))
                {
                    var user = await context.ElevateUsers.FindAsync(userId);
                    if (user != null)
                    {
                        user.Status = "error";
                        user.Updated = DateTime.UtcNow;
                        user.UpdatedBy = "System";
                    }
                }
            }
            else if (job.Type == "device")
            {
                var serverPermissionType = await context.ElevatePermissionTypes
                    .FirstOrDefaultAsync(pt => pt.Type == "server");

                if (serverPermissionType != null)
                {
                    var permission = await context.ElevatePermissions
                        .FirstOrDefaultAsync(p => p.Value == job.Reference && p.Type == serverPermissionType.ElevatePermissionTypesId);

                    if (permission != null)
                    {
                        context.ElevatePermissions.Remove(permission);
                    }
                }
            }

            context.ElevateJobs.Remove(job);
            await context.SaveChangesAsync();
        }
    }
}
