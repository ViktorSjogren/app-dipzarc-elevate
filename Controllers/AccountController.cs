// Add this AccountController to handle login/logout
// Controllers/AccountController.cs

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dizparc_elevate.Services;

namespace dizparc_elevate.Controllers
{
    [AllowAnonymous] // This controller should allow anonymous access
    public class AccountController : Controller
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuditService auditService, ILogger<AccountController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            var redirectUrl = Url.Action("Index", "Home");
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> SignOut()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await _auditService.LogAsync("UserSignOut", "Authentication", true,
                    $"User {User.Identity.Name} signed out");
            }

            var callbackUrl = Url.Action("SignedOut", "Account", values: null, protocol: Request.Scheme);

            return SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                "Cookies",
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult SignedOut()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AuthError(string? error = null)
        {
            ViewBag.ErrorMessage = error ?? "An authentication error occurred.";
            
            _logger.LogError("Authentication error page accessed with error: {Error}", error);
            
            return View();
        }

        [HttpGet]
        public IActionResult DebugConfig()
        {
            // Only allow in development
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                return NotFound();
            }

            var config = new
            {
                ClientId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AzureAd_ClientId")) ? "SET" : "NOT SET",
                Instance = Environment.GetEnvironmentVariable("AzureAd_Instance") ?? "NOT SET",
                Domain = Environment.GetEnvironmentVariable("AzureAd_Domain") ?? "NOT SET", 
                TenantId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AzureAd_TenantId")) ? "SET" : "NOT SET",
                CallbackPath = Environment.GetEnvironmentVariable("AzureAd_CallbackPath") ?? "NOT SET"
            };

            return Json(config);
        }
    }
}