using System.Diagnostics;
using dizparc_elevate.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dizparc_elevate.Controllers
{
    [Authorize] // Add this attribute to require authentication for this controller
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // If user is not authenticated, redirect to sign-in
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("SignIn", "Account");
            }
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous] // Allow anonymous access to error page
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}