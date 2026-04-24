using diplom.Data;
using diplom.Models;
using diplom.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplom.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AnalyticsService _analyticsService;
        private readonly LoggingService _loggingService;

        public CourseController(AppDbContext context, AnalyticsService analyticsService, LoggingService loggingService)
        {
            _context = context;
            _analyticsService = analyticsService;
            _loggingService = loggingService;
        }

        // Список курсов
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            await _analyticsService.TrackEventAsync(HttpContext, "ViewCourses", "Course/Index");

            return View(courses);
        }

        // Страница курса
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound();

            if (course.IsPremium && !User.IsInRole("Premium") && !User.IsInRole("Admin"))
            {
                await _analyticsService.TrackEventAsync(HttpContext, "ViewPremiumBlocked", $"Course/Details/{id}");
                await _loggingService.LogAsync(HttpContext, "Premium course access denied", "Warning", $"CourseId: {id}");
                return RedirectToAction("PremiumRequired");
            }

            await _analyticsService.TrackEventAsync(HttpContext, "ViewCourse", $"Course/Details/{id}");
            await _loggingService.LogAsync(HttpContext, "Course viewed", "Info", $"CourseId: {id}, Title: {course.Title}");

            return View(course);
        }

        // Страница урока
        public async Task<IActionResult> Lesson(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.CourseModule)
                    .ThenInclude(m => m.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound();

            var course = lesson.CourseModule?.Course;

            if (course == null)
                return NotFound();

            if (course.IsPremium && !lesson.IsPreview && !User.IsInRole("Premium") && !User.IsInRole("Admin"))
            {
                await _analyticsService.TrackEventAsync(HttpContext, "ViewPremiumLessonBlocked", $"Course/Lesson/{id}");
                await _loggingService.LogAsync(HttpContext, "Premium lesson access denied", "Warning", $"LessonId: {id}");
                return RedirectToAction("PremiumRequired");
            }

            await _analyticsService.TrackEventAsync(HttpContext, "ViewLesson", $"Course/Lesson/{id}");
            await _loggingService.LogAsync(HttpContext, "Lesson viewed", "Info", $"LessonId: {id}, Title: {lesson.Title}");

            return View(lesson);
        }

        // Страница, если нужен premium
        public IActionResult PremiumRequired()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Course model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                model.CreatedByUserId = userId;
            }

            model.CreatedAt = DateTime.UtcNow;

            _context.Courses.Add(model);
            await _context.SaveChangesAsync();

            await _loggingService.LogAsync(
                HttpContext,
                "Course created",
                "Info",
                $"Course title: {model.Title}, Premium: {model.IsPremium}");

            await _analyticsService.TrackEventAsync(HttpContext, "CreateCourse", "Course/Create");

            return RedirectToAction("Index");
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateModule(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);

            if (course == null)
                return NotFound();

            ViewBag.CourseId = courseId;
            ViewBag.CourseTitle = course.Title;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateModule(CourseModule model)
        {
            if (!ModelState.IsValid)
            {
                var course = await _context.Courses.FindAsync(model.CourseId);
                ViewBag.CourseId = model.CourseId;
                ViewBag.CourseTitle = course?.Title;
                return View(model);
            }

            _context.CourseModules.Add(model);
            await _context.SaveChangesAsync();

            await _loggingService.LogAsync(
                HttpContext,
                "Course module created",
                "Info",
                $"Module title: {model.Title}, CourseId: {model.CourseId}");

            await _analyticsService.TrackEventAsync(HttpContext, "CreateModule", $"Course/CreateModule/{model.CourseId}");

            return RedirectToAction("Details", new { id = model.CourseId });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateLesson(int moduleId)
        {
            var module = await _context.CourseModules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null)
                return NotFound();

            ViewBag.ModuleId = moduleId;
            ViewBag.ModuleTitle = module.Title;
            ViewBag.CourseId = module.CourseId;
            ViewBag.CourseTitle = module.Course?.Title;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateLesson(Lesson model)
        {
            if (!ModelState.IsValid)
            {
                var module = await _context.CourseModules
                    .Include(m => m.Course)
                    .FirstOrDefaultAsync(m => m.Id == model.CourseModuleId);

                ViewBag.ModuleId = model.CourseModuleId;
                ViewBag.ModuleTitle = module?.Title;
                ViewBag.CourseId = module?.CourseId;
                ViewBag.CourseTitle = module?.Course?.Title;

                return View(model);
            }

            _context.Lessons.Add(model);
            await _context.SaveChangesAsync();

            await _loggingService.LogAsync(
                HttpContext,
                "Lesson created",
                "Info",
                $"Lesson title: {model.Title}, ModuleId: {model.CourseModuleId}");

            await _analyticsService.TrackEventAsync(HttpContext, "CreateLesson", $"Course/CreateLesson/{model.CourseModuleId}");


            var parentModule = await _context.CourseModules.FindAsync(model.CourseModuleId);

            return RedirectToAction("Details", new { id = parentModule?.CourseId });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
                return NotFound();

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Course model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var course = await _context.Courses.FindAsync(model.Id);

            if (course == null)
                return NotFound();

            course.Title = model.Title;
            course.Description = model.Description;
            course.PreviewText = model.PreviewText;
            course.IsPremium = model.IsPremium;
            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _loggingService.LogAsync(
                HttpContext,
                "Course updated",
                "Info",
                $"CourseId: {course.Id}, Title: {course.Title}");

            await _analyticsService.TrackEventAsync(HttpContext, "EditCourse", $"Course/Edit/{course.Id}");

            return RedirectToAction("Details", new { id = course.Id });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
                return NotFound();

            return View(course);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
                return NotFound();

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            await _loggingService.LogAsync(
                HttpContext,
                "Course deleted",
                "Security",
                $"CourseId: {id}, Title: {course.Title}");

            await _analyticsService.TrackEventAsync(HttpContext, "DeleteCourse", $"Course/Delete/{id}");

            return RedirectToAction("Index");
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditModule(int id)
        {
            var module = await _context.CourseModules.FindAsync(id);

            if (module == null)
                return NotFound();

            return View(module);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditModule(CourseModule model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var module = await _context.CourseModules.FindAsync(model.Id);

            if (module == null)
                return NotFound();

            module.Title = model.Title;
            module.OrderNumber = model.OrderNumber;

            await _context.SaveChangesAsync();

            await _loggingService.LogAsync(
                HttpContext,
                "Module updated",
                "Info",
                $"ModuleId: {module.Id}, Title: {module.Title}");

            await _analyticsService.TrackEventAsync(HttpContext, "EditModule", $"Course/EditModule/{module.Id}");

            return RedirectToAction("Details", new { id = module.CourseId });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var module = await _context.CourseModules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null)
                return NotFound();

            return View(module);
        }

        [HttpPost, ActionName("DeleteModule")]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteModuleConfirmed(int id)
        {
            var module = await _context.CourseModules.FindAsync(id);

            if (module == null)
                return NotFound();

            var courseId = module.CourseId;
            var moduleTitle = module.Title;

            _context.CourseModules.Remove(module);
            await _context.SaveChangesAsync();

            await _loggingService.LogAsync(
                HttpContext,
                "Module deleted",
                "Security",
                $"ModuleId: {id}, Title: {moduleTitle}");

            await _analyticsService.TrackEventAsync(HttpContext, "DeleteModule", $"Course/DeleteModule/{id}");

            return RedirectToAction("Details", new { id = courseId });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditLesson(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);

            if (lesson == null)
                return NotFound();

            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditLesson(Lesson model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var lesson = await _context.Lessons.FindAsync(model.Id);

            if (lesson == null)
                return NotFound();

            lesson.Title = model.Title;
            lesson.Body = model.Body;
            lesson.OrderNumber = model.OrderNumber;
            lesson.IsPreview = model.IsPreview;

            await _context.SaveChangesAsync();

            await _loggingService.LogAsync(
                HttpContext,
                "Lesson updated",
                "Info",
                $"LessonId: {lesson.Id}, Title: {lesson.Title}");

            await _analyticsService.TrackEventAsync(HttpContext, "EditLesson", $"Course/EditLesson/{lesson.Id}");

            var module = await _context.CourseModules.FindAsync(lesson.CourseModuleId);

            return RedirectToAction("Details", new { id = module?.CourseId });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.CourseModule)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound();

            return View(lesson);
        }

        [HttpPost, ActionName("DeleteLesson")]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLessonConfirmed(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);

            if (lesson == null)
                return NotFound();

            var moduleId = lesson.CourseModuleId;
            var lessonTitle = lesson.Title;

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            await _loggingService.LogAsync(
                HttpContext,
                "Lesson deleted",
                "Security",
                $"LessonId: {id}, Title: {lessonTitle}");

            await _analyticsService.TrackEventAsync(HttpContext, "DeleteLesson", $"Course/DeleteLesson/{id}");

            var module = await _context.CourseModules.FindAsync(moduleId);

            return RedirectToAction("Details", new { id = module?.CourseId });
        }
    }
}