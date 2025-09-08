using dizparc_elevate.Middleware;
using dizparc_elevate.Services;
using dizparc_elevate.Models.securitySolutionsCommon;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

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
            // Configure localization
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("sv-SE")
                };

                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                
                // Add culture providers - cookie first, then query string
                options.RequestCultureProviders.Clear();
                options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
                options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
                options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
            });

            // Configure HSTS for production
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
                // In production, consider storing keys in Azure Key Vault
                // dataProtectionBuilder.PersistKeysToAzureBlobStorage(connectionString, containerName, blobName);
                // dataProtectionBuilder.ProtectKeysWithAzureKeyVault(keyIdentifier, credential);
            }

            // Configure enhanced session security
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = _env.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Name = ".AspNetCore.Session";
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

            // WORKING AUTHENTICATION - Keep it simple but add back security events
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(options =>
                {
                    options.ClientId = Environment.GetEnvironmentVariable("AzureAd_ClientId");
                    options.Instance = Environment.GetEnvironmentVariable("AzureAd_Instance");
                    options.Domain = Environment.GetEnvironmentVariable("AzureAd_Domain");
                    options.TenantId = Environment.GetEnvironmentVariable("AzureAd_TenantId");
                    options.CallbackPath = Environment.GetEnvironmentVariable("AzureAd_CallbackPath");

                    // Add token validation events for security logging
                    options.Events = new OpenIdConnectEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            // Extract and store user tenant ID
                            string? userTenantId = context.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
                            if (!string.IsNullOrEmpty(userTenantId))
                            {
                                context.HttpContext?.Session.SetString("UserTenantId", userTenantId);
                            }

                            // Log successful authentication for audit
                            var logger = context.HttpContext?.RequestServices.GetRequiredService<ILogger<Startup>>();
                            var auditService = context.HttpContext?.RequestServices.GetService<IAuditService>();
                            var userName = context.Principal?.Identity?.Name ?? "Unknown";
                            var ipAddress = context.HttpContext?.Connection.RemoteIpAddress?.ToString();

                            logger?.LogInformation(
                                "User {UserName} successfully authenticated from IP {IpAddress} at {Timestamp}",
                                userName, ipAddress, DateTime.UtcNow);

                            if (auditService != null)
                            {
                                await auditService.LogAsync("UserSignIn", "Authentication", true,
                                    $"User {userName} signed in from IP {ipAddress}");
                            }
                        },
                        OnAuthenticationFailed = async context =>
                        {
                            // Log failed authentication attempts
                            var logger = context.HttpContext?.RequestServices.GetRequiredService<ILogger<Startup>>();
                            var auditService = context.HttpContext?.RequestServices.GetService<IAuditService>();
                            var ipAddress = context.HttpContext?.Connection.RemoteIpAddress?.ToString();

                            logger?.LogWarning(
                                "Authentication failed from IP {IpAddress} at {Timestamp}. Error: {Error}",
                                ipAddress, DateTime.UtcNow, context.Exception?.Message);

                            if (auditService != null)
                            {
                                await auditService.LogSecurityEventAsync(
                                    "AuthenticationFailed",
                                    "Failed authentication attempt",
                                    $"IP: {ipAddress}, Error: {context.Exception?.Message}");
                            }

                            context.HandleResponse();
                            context.Response.Redirect($"/Account/AuthError?error={Uri.EscapeDataString(context.Exception?.Message ?? "Unknown error")}");
                        },
                        OnRemoteFailure = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect($"/Account/AuthError?error={Uri.EscapeDataString(context.Failure?.Message ?? "Unknown error")}");
                            return Task.CompletedTask;
                        }
                    };
                });

            var securitySolutionsCommonConnectionString = _config.GetConnectionString("securitySolutionsCommonSqlConnection");

            services.AddDbContext<Sqldb_securitySolutionsCommon>(options =>
                options.UseSqlServer(securitySolutionsCommonConnectionString));
            services.AddScoped<Sqldb_securitySolutionsCommon>();

            // Configure Anti-forgery protection
            services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.SecurePolicy = _env.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".AspNetCore.Antiforgery";
            });

            // Configure authorization policies
            services.AddAuthorization(options =>
            {
                // Base policy requiring authentication
                options.AddPolicy("RequireAuthenticatedUser", policy =>
                {
                    policy.RequireAuthenticatedUser();
                });

                // Policy for administrative functions
                options.AddPolicy("RequireAdminAccess", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    // Add specific role or claim requirements here when needed
                    // policy.RequireRole("Admin");
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

            })
            .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization()
            .AddRazorRuntimeCompilation();

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

            // Add security headers using NetEscapades
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
                        builder.AddConnectSrc().Self().From("https://login.microsoftonline.com");
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
            
            // Use localization middleware
            var localizationOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(localizationOptions);

            // Authentication must come before authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Add custom security middleware AFTER authentication to avoid OAuth interference
            app.UseRequestSizeValidation(10 * 1024 * 1024); // 10MB limit
            app.UseSecurityMonitoring(); // Your security monitoring middleware

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