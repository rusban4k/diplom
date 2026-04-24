using diplom.Data;
using diplom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using diplom.Services;

namespace diplom.Controllers
{
    public class ContentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AnalyticsService _analyticsService;
        private readonly LoggingService _loggingService;

        public ContentController(AppDbContext context, AnalyticsService analyticsService, LoggingService loggingService)
        {
            _context = context;
            _analyticsService = analyticsService;
            _loggingService = loggingService;
        }

        // Список записей
        public async Task<IActionResult> Index()
        {
            var contents = await _context.Contents
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(contents);
        }

        // Просмотр одной записи
        public async Task<IActionResult> Details(int id)
        {
            var content = await _context.Contents
                .FirstOrDefaultAsync(c => c.Id == id);

            if (content == null)
                return NotFound();

            await _analyticsService.TrackEventAsync(HttpContext, "ViewContent", $"Content/Details/{id}");

            return View(content);
        }

        // Создание записи (GET)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // Создание записи (POST)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Content model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                model.CreatedByUserId = userId;
            }

            model.CreatedAt = DateTime.UtcNow;

            _context.Contents.Add(model);
            await _context.SaveChangesAsync();

            await _loggingService.LogAsync(HttpContext, "Content created", "Info", $"Content title: {model.Title}");

            return RedirectToAction("Index");
        }
    }
}