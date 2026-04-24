using System.Diagnostics;
using diplom.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using diplom.Services;

namespace diplom.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AnalyticsService _analyticsService;

        public HomeController(ILogger<HomeController> logger, AnalyticsService analyticsService)
        {
            _logger = logger;
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Index()
        {
            await _analyticsService.TrackEventAsync(HttpContext, "ViewHome", "Home/Index");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        public IActionResult Secret()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminPanel()
        {
            return View();
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult Profile()
        {
            return View();
        }
    }
}
