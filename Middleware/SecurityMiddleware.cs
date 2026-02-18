using dizparc_elevate.Services;

namespace dizparc_elevate.Middleware
{
    public class SecurityMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityMonitoringMiddleware> _logger;

        public SecurityMonitoringMiddleware(RequestDelegate next, ILogger<SecurityMonitoringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Monitor for suspicious activity
            await MonitorSuspiciousActivity(context);

            // Continue with the request
            await _next(context);
        }

        private async Task MonitorSuspiciousActivity(HttpContext context)
        {
            var request = context.Request;

            // Only try to get audit service if DI container is available and request is not for auth endpoints
            IAuditService? auditService = null;
            try
            {
                // Skip audit logging for authentication endpoints to avoid circular dependencies
                if (request.Path.StartsWithSegments("/signin-oidc") ||
                    request.Path.StartsWithSegments("/signout-oidc") ||
                    request.Path.StartsWithSegments("/signout-callback-oidc") ||
                    request.Path.StartsWithSegments("/MicrosoftIdentity") ||
                    request.Path.StartsWithSegments("/Account/SignIn") ||
                    request.Path.StartsWithSegments("/Account/SignOut"))
                {
                    return; // Don't monitor auth endpoints
                }

                auditService = context.RequestServices.GetService<IAuditService>();
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not get audit service: {Exception}", ex.Message);
                return; // Continue without audit logging if service not available
            }

            // Check for suspicious patterns in the request
            var suspiciousPatterns = new[]
            {
                "script", "javascript:", "vbscript:", "onload", "onerror",
                "select", "union", "insert", "delete", "drop", "create",
                "../", "..\\", "%2e%2e", "%252e%252e"
            };

            var queryString = request.QueryString.ToString().ToLower();
            var path = request.Path.ToString().ToLower();
            var userAgent = request.Headers["User-Agent"].ToString().ToLower();

            // Check query parameters
            foreach (var pattern in suspiciousPatterns)
            {
                if (queryString.Contains(pattern) || path.Contains(pattern))
                {
                    if (auditService != null)
                    {
                        try
                        {
                            await auditService.LogAsync("SuspiciousRequest", new
                                {
                                    message = $"Suspicious pattern detected: {pattern}",
                                    path = request.Path.ToString(),
                                    queryString = request.QueryString.ToString(),
                                    userAgent
                                });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Failed to log security event: {Exception}", ex.Message);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("SECURITY_EVENT: Suspicious pattern detected: {Pattern} in path: {Path}", pattern, request.Path);
                    }
                }
            }

            // Check for suspicious user agents
            var suspiciousUserAgents = new[]
            {
                "sqlmap", "nikto", "nessus", "openvas", "nmap",
                "burpsuite", "owasp zap", "w3af", "metasploit"
            };

            foreach (var suspiciousAgent in suspiciousUserAgents)
            {
                if (userAgent.Contains(suspiciousAgent))
                {
                    if (auditService != null)
                    {
                        try
                        {
                            await auditService.LogAsync("SuspiciousUserAgent", new
                                {
                                    message = $"Potentially malicious user agent detected: {suspiciousAgent}",
                                    userAgent,
                                    ip = context.Connection.RemoteIpAddress?.ToString()
                                });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Failed to log security event: {Exception}", ex.Message);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("SECURITY_EVENT: Suspicious user agent detected: {Agent} from IP: {IP}", suspiciousAgent, context.Connection.RemoteIpAddress);
                    }
                }
            }

            // Check for excessive request size
            if (request.ContentLength > 10 * 1024 * 1024) // 10MB
            {
                if (auditService != null)
                {
                    try
                    {
                        await auditService.LogAsync("LargeRequest", new
                            {
                                message = "Request size exceeds normal limits",
                                contentLength = request.ContentLength,
                                path = request.Path.ToString()
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to log security event: {Exception}", ex.Message);
                    }
                }
                else
                {
                    _logger.LogWarning("SECURITY_EVENT: Large request detected: {Size} bytes for path: {Path}", request.ContentLength, request.Path);
                }
            }

            // Monitor for rapid successive requests (basic DDoS detection)
            var clientIP = context.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(clientIP))
            {
                // This would typically use a distributed cache like Redis in production
                // For now, we'll just log the information
                _logger.LogDebug("Request from IP: {ClientIP} at {Timestamp}", clientIP, DateTime.UtcNow);
            }
        }
    }

    public class RequestSizeValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestSizeValidationMiddleware> _logger;
        private readonly long _maxRequestSize;

        public RequestSizeValidationMiddleware(RequestDelegate next, ILogger<RequestSizeValidationMiddleware> logger, long maxRequestSize = 10 * 1024 * 1024)
        {
            _next = next;
            _logger = logger;
            _maxRequestSize = maxRequestSize;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.ContentLength > _maxRequestSize)
            {
                _logger.LogWarning("Request rejected due to size limit. Size: {Size}, Limit: {Limit}, IP: {IP}",
                    context.Request.ContentLength, _maxRequestSize, context.Connection.RemoteIpAddress);

                context.Response.StatusCode = 413; // Payload Too Large
                await context.Response.WriteAsync("Request size too large");
                return;
            }

            await _next(context);
        }
    }

    public class IPWhitelistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IPWhitelistMiddleware> _logger;
        private readonly HashSet<string> _allowedIPs;

        public IPWhitelistMiddleware(RequestDelegate next, ILogger<IPWhitelistMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;

            // Load allowed IPs from configuration
            var allowedIPs = configuration.GetSection("Security:AllowedIPs").Get<string[]>() ?? Array.Empty<string>();
            _allowedIPs = new HashSet<string>(allowedIPs);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip IP validation in development
            var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
            {
                await _next(context);
                return;
            }

            var clientIP = context.Connection.RemoteIpAddress?.ToString();

            // If no whitelist is configured, allow all requests
            if (_allowedIPs.Count == 0)
            {
                await _next(context);
                return;
            }

            if (string.IsNullOrEmpty(clientIP) || !_allowedIPs.Contains(clientIP))
            {
                _logger.LogWarning("Access denied for IP: {ClientIP}", clientIP);

                var auditService = context.RequestServices.GetRequiredService<IAuditService>();
                await auditService.LogAsync("UnauthorizedIPAccess", new
                    {
                        message = "Access attempt from non-whitelisted IP",
                        ip = clientIP,
                        path = context.Request.Path.ToString()
                    });

                context.Response.StatusCode = 403; // Forbidden
                await context.Response.WriteAsync("Access denied");
                return;
            }

            await _next(context);
        }
    }

    // Extension methods to easily add middleware
    public static class SecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityMonitoring(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityMonitoringMiddleware>();
        }

        public static IApplicationBuilder UseRequestSizeValidation(this IApplicationBuilder builder, long maxSize = 10 * 1024 * 1024)
        {
            return builder.UseMiddleware<RequestSizeValidationMiddleware>(maxSize);
        }

        public static IApplicationBuilder UseIPWhitelist(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IPWhitelistMiddleware>();
        }
    }
}
