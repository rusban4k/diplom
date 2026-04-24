using diplom.Data;
using diplom.Models;
using diplom.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace diplom.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly LoggingService _loggingService;
        private readonly AnalyticsService _analyticsService;

        public AdminController(AppDbContext context, LoggingService loggingService, AnalyticsService analyticsService)
        {
            _context = context;
            _loggingService = loggingService;
            _analyticsService = analyticsService;
        }

        // Список пользователей
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // Блокировка пользователя
        public async Task<IActionResult> ToggleBlock(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            user.IsBlocked = !user.IsBlocked;

            await _context.SaveChangesAsync();
            await _loggingService.LogAsync(HttpContext, user.IsBlocked ? "User blocked" : "User unblocked", "Security", $"Target user email: {user.Email}");

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Analytics()
        {
            var totalEvents = await _context.AnalyticsEvents.CountAsync();

            var totalRegistrations = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "Register");
            var totalLogins = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "Login");
            var totalLogouts = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "Logout");

            var totalCourseListViews = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "ViewCourses");
            var totalCourseViews = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "ViewCourse");
            var totalLessonViews = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "ViewLesson");

            var totalPremiumBlocked = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "ViewPremiumBlocked");
            var totalPremiumGranted = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "PremiumGranted");
            var totalPremiumRevoked = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "PremiumRevoked");

            var totalCoursesCreated = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "CreateCourse");
            var totalModulesCreated = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "CreateModule");
            var totalLessonsCreated = await _context.AnalyticsEvents.CountAsync(a => a.EventType == "CreateLesson");

            ViewBag.TotalEvents = totalEvents;

            ViewBag.TotalRegistrations = totalRegistrations;
            ViewBag.TotalLogins = totalLogins;
            ViewBag.TotalLogouts = totalLogouts;

            ViewBag.TotalCourseListViews = totalCourseListViews;
            ViewBag.TotalCourseViews = totalCourseViews;
            ViewBag.TotalLessonViews = totalLessonViews;

            ViewBag.TotalPremiumBlocked = totalPremiumBlocked;
            ViewBag.TotalPremiumGranted = totalPremiumGranted;
            ViewBag.TotalPremiumRevoked = totalPremiumRevoked;

            ViewBag.TotalCoursesCreated = totalCoursesCreated;
            ViewBag.TotalModulesCreated = totalModulesCreated;
            ViewBag.TotalLessonsCreated = totalLessonsCreated;

            ViewBag.ChartCourseListViews = totalCourseListViews;
            ViewBag.ChartCourseViews = totalCourseViews;
            ViewBag.ChartLessonViews = totalLessonViews;
            ViewBag.ChartPremiumBlocked = totalPremiumBlocked;

            ViewBag.ChartPremiumGranted = totalPremiumGranted;
            ViewBag.ChartPremiumRevoked = totalPremiumRevoked;

            ViewBag.ChartRegistrations = totalRegistrations;
            ViewBag.ChartLogins = totalLogins;
            ViewBag.ChartLogouts = totalLogouts;

            // График по дням: последние 7 дней
            var startDate = DateTime.UtcNow.Date.AddDays(-6);

            var dailyEvents = await _context.AnalyticsEvents
                .Where(a => a.Timestamp >= startDate)
                .GroupBy(a => a.Timestamp.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var dateLabels = new List<string>();
            var dateCounts = new List<int>();

            for (int i = 0; i < 7; i++)
            {
                var date = startDate.AddDays(i);
                dateLabels.Add(date.ToString("dd.MM"));

                var match = dailyEvents.FirstOrDefault(x => x.Date == date);
                dateCounts.Add(match?.Count ?? 0);
            }

            ViewBag.DailyLabels = System.Text.Json.JsonSerializer.Serialize(dateLabels);
            ViewBag.DailyCounts = System.Text.Json.JsonSerializer.Serialize(dateCounts);

            var recentEvents = await _context.AnalyticsEvents
                .OrderByDescending(a => a.Timestamp)
                .Take(30)
                .ToListAsync();

            return View(recentEvents);
        }

        public async Task<IActionResult> Logs()
        {
            var logs = await _context.Logs
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .ToListAsync();

            return View(logs);
        }

        public async Task<IActionResult> TogglePremium(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            if (user.Role == "Admin")
            {
                return RedirectToAction("Users");
            }

            if (user.Role == "Premium")
            {
                user.Role = "User";
                user.PremiumAssignedAt = null;

                await _loggingService.LogAsync(
                    HttpContext,
                    "Premium revoked",
                    "Security",
                    $"Target user email: {user.Email}");

                await _analyticsService.TrackEventAsync(HttpContext, "PremiumRevoked", $"Admin/TogglePremium/{id}");
            }
            else
            {
                user.Role = "Premium";
                user.PremiumAssignedAt = DateTime.UtcNow;

                await _loggingService.LogAsync(
                    HttpContext,
                    "Premium granted",
                    "Security",
                    $"Target user email: {user.Email}");

                await _analyticsService.TrackEventAsync(HttpContext, "PremiumGranted", $"Admin/TogglePremium/{id}");
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> ToggleActivation(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            if (user.Role == "Admin")
            {
                return RedirectToAction("Users");
            }

            user.IsActive = !user.IsActive;

            await _loggingService.LogAsync(
                HttpContext,
                user.IsActive ? "User activated" : "User deactivated",
                "Security",
                $"Target user email: {user.Email}");

            await _analyticsService.TrackEventAsync(
                HttpContext,
                user.IsActive ? "UserActivated" : "UserDeactivated",
                $"Admin/ToggleActivation/{id}");

            await _context.SaveChangesAsync();

            return RedirectToAction("Users");
        }
    }
}