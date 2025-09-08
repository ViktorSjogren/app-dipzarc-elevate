using dizparc_elevate.Middleware;
using dizparc_elevate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Threading.RateLimiting;

namespace dizparc_elevate
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public Startup(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configure HSTS
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            // Configure secure cookies
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
                options.Secure = _env.IsDevelopment()
                    ? Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest
                    : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.CheckConsentNeeded = context => false;
            });

            // Configure Data Protection
            var dataProtectionBuilder = services.AddDataProtection()
                .SetApplicationName("entraPam")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            if (!_env.IsDevelopment())
            {
                // In production, consider storing keys in Azure Key Vault or persistent storage
                // dataProtectionBuilder.PersistKeysToAzureBlobStorage(connectionString, containerName, blobName);
                // dataProtectionBuilder.ProtectKeysWithAzureKeyVault(keyIdentifier, credential);
            }

            // Configure enhanced session security
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Extended for auth flow
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = _env.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Name = "SessionId";
            });

            // Add Rate Limiting
            services.AddRateLimiter(options =>
            {
                // Global rate limiter
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100, // 100 requests per minute per IP
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                // Specific policy for login attempts
                options.AddPolicy("LoginPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 5, // 5 login attempts per minute
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
                };
            });

            // Configure Security Headers using NetEscapades
            // Note: This will be configured in the Configure method instead

            // Add enhanced authentication
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(options =>
                {
                    options.ClientId = Environment.GetEnvironmentVariable("AzureAd_ClientId");
                    options.Instance = Environment.GetEnvironmentVariable("AzureAd_Instance");
                    options.Domain = Environment.GetEnvironmentVariable("AzureAd_Domain");
                    options.TenantId = Environment.GetEnvironmentVariable("AzureAd_TenantId");
                    options.CallbackPath = Environment.GetEnvironmentVariable("AzureAd_CallbackPath");

                    // Enhanced security settings
                    options.ResponseType = "code";
                    options.UseTokenLifetime = true;
                    options.UsePkce = true;

                    // Set proper scopes
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add("user.read");

                    // Additional security options
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = false;

                    // OAuth-specific cookie settings (keep minimal to avoid correlation issues)
                    options.NonceCookie.SameSite = SameSiteMode.Lax;
                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                    options.NonceCookie.SecurePolicy = _env.IsDevelopment()
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;
                    options.CorrelationCookie.SecurePolicy = _env.IsDevelopment()
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;
                });

            // Configure Microsoft Identity options with enhanced token validation
            services.Configure<MicrosoftIdentityOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Events.OnTokenValidated += async context =>
                {
                    // Extract and store user tenant ID
                    string? userTenantId = context.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
                    if (!string.IsNullOrEmpty(userTenantId))
                    {
                        context.HttpContext?.Session.SetString("UserTenantId", userTenantId);
                        context.Properties?.Items.Add("userTenantId", userTenantId);
                    }

                    // Log successful authentication for audit
                    var logger = context.HttpContext?.RequestServices.GetRequiredService<ILogger<Startup>>();
                    var userName = context.Principal?.Identity?.Name ?? "Unknown";
                    var ipAddress = context.HttpContext?.Connection.RemoteIpAddress?.ToString();

                    logger?.LogInformation(
                        "User {UserName} successfully authenticated from IP {IpAddress} at {Timestamp}",
                        userName, ipAddress, DateTime.UtcNow);
                };

                options.Events.OnAuthenticationFailed += async context =>
                {
                    // Log failed authentication attempts
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();

                    logger.LogWarning(
                        "Authentication failed from IP {IpAddress} at {Timestamp}. Error: {Error}",
                        ipAddress, DateTime.UtcNow, context.Exception?.Message);
                };
            });

            // Configure Anti-forgery protection
            services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.SecurePolicy = _env.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = "RequestVerificationToken";
            });

            // Configure authorization policies
            services.AddAuthorization(options =>
            {
                // Base policy requiring authentication
                options.AddPolicy("RequireAuthenticatedUser", policy =>
                {
                    policy.RequireAuthenticatedUser();
                });

                // Policy for administrative functions (add specific claims/roles as needed)
                options.AddPolicy("RequireAdminAccess", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    // Add specific role or claim requirements here
                    // policy.RequireRole("Admin");
                    // policy.RequireClaim("admin_access", "true");
                });

                // Policy for privileged operations
                options.AddPolicy("RequirePrivilegedAccess", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    // Add specific requirements for PAM operations
                });
            });

            // Add controllers with comprehensive security
            services.AddControllersWithViews(options =>
            {
                // Require authentication for all controllers by default
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));

                // Add anti-forgery token validation for all POST actions
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

            }).AddRazorRuntimeCompilation();

            services.AddRazorPages()
                .AddMicrosoftIdentityUI();

            // Add HTTP client with security configurations
            services.AddHttpClient("SecureClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "entraPam/1.0");
            });

            services.AddHttpContextAccessor();

            // Add audit service for security logging
            services.AddScoped<IAuditService, AuditService>();

            // Configure logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();

                if (!_env.IsDevelopment())
                {
                    // Add structured logging for production
                    builder.AddEventSourceLogger();
                }
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // security headers with netEscapades
            app.UseSecurityHeaders(policies =>
                policies
                    .AddDefaultSecurityHeaders()
                    .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 31536000) // 1 year
                    .AddContentSecurityPolicy(builder =>
                    {
                        builder.AddDefaultSrc().Self();
                        builder.AddScriptSrc().Self().UnsafeInline();
                        builder.AddStyleSrc().Self().UnsafeInline();
                        builder.AddImgSrc().Self().Data();
                        builder.AddFontSrc().Self();
                        builder.AddFormAction().Self();
                        builder.AddFrameAncestors().Self();
                        builder.AddConnectSrc().Self();
                        builder.AddMediaSrc().Self();
                        builder.AddObjectSrc().None();
                        builder.AddBaseUri().Self();
                        builder.AddBlockAllMixedContent();
                        builder.AddUpgradeInsecureRequests();
                    })
                    .AddPermissionsPolicy(builder =>
                    {
                        builder.AddCamera().None();
                        builder.AddMicrophone().None();
                        builder.AddGeolocation().None();
                        builder.AddGyroscope().None();
                        builder.AddAccelerometer().None();
                        builder.AddMagnetometer().None();
                        builder.AddPayment().None();
                        builder.AddUsb().None();
                    })
                    .AddFrameOptionsSameOrigin()
                    .AddContentTypeOptionsNoSniff()
                    .AddReferrerPolicyStrictOriginWhenCrossOrigin()
                    .RemoveServerHeader()
            );

            // Add custom security middleware AFTER authentication
            // Don't add these before authentication as they can interfere with OAuth flows
            // app.UseRequestSizeValidation(10 * 1024 * 1024); // 10MB limit
            // app.UseSecurityMonitoring();

            // Optionally enable IP whitelist in production
            if (!env.IsDevelopment() && _config.GetValue<bool>("Security:EnableIPWhitelist"))
            {
                app.UseIPWhitelist();
            }

            // Enforce HTTPS
            app.UseHttpsRedirection();

            // Serve static files securely
            app.UseStaticFiles();

            // Use cookie policy
            app.UseCookiePolicy();

            // Use session with security configurations
            app.UseSession();

            // Use rate limiting
            app.UseRateLimiter();

            // Use routing
            app.UseRouting();

            // Authentication must come before authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Add custom security middleware AFTER authentication to avoid OAuth interference
            app.UseRequestSizeValidation(10 * 1024 * 1024); // 10MB limit
            app.UseSecurityMonitoring();

            // Configure endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "account",
                    pattern: "account/{action}",
                    defaults: new { controller = "Account" });

                endpoints.MapRazorPages();
            });
        }
    }
}
