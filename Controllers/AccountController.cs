using diplom.Data;
using diplom.Models;
using diplom.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using diplom.Services;
using Microsoft.AspNetCore.RateLimiting;



namespace diplom.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AnalyticsService _analyticsService;
        private readonly LoggingService _loggingService;


        public AccountController(AppDbContext context, AnalyticsService analyticsService, LoggingService loggingService)
        {
            _context = context;
            _analyticsService = analyticsService;
            _loggingService = loggingService;
        }

        // Регистрация (GET)
        public IActionResult Register()
        {
            return View();
        }

        // Регистрация (POST)
        [HttpPost]
        [EnableRateLimiting("AuthPolicy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("", "Пользователь уже существует");
                return View(model);
            }

            var user = new User
            {
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "User",
                IsActive = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await _analyticsService.TrackEventAsync(HttpContext, "Register", "Account/Register");
            await _loggingService.LogAsync(HttpContext, "User registered", "Info", $"Email: {user.Email}");

            return RedirectToAction("ActivationRequired");
        }

        public IActionResult ActivationRequired()
        {
            return View();
        }

        [HttpPost]
        [EnableRateLimiting("AuthPolicy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                await _loggingService.LogAsync(HttpContext, "Failed login attempt", "Warning", $"Email: {model.Email}");
                ModelState.AddModelError("", "Неверный логин или пароль");
                return View(model);
            }

            if (user.IsBlocked)
            {
                await _loggingService.LogAsync(HttpContext, "Blocked user login attempt", "Warning", $"Email: {user.Email}");
                ModelState.AddModelError("", "Пользователь заблокирован");
                return View(model);
            }

            if (!user.IsActive && user.Role != "Admin")
            {
                await _loggingService.LogAsync(HttpContext, "Inactive user login attempt", "Warning", $"Email: {user.Email}");
                ViewBag.ShowActivationHelp = true;

                ModelState.AddModelError("", "Аккаунт не активирован.");

                return View(model);
            }

            var passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (!passwordValid)
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= 5)
                {
                    user.IsBlocked = true;

                    await _loggingService.LogAsync(
                        HttpContext,
                        "User blocked after failed logins",
                        "Security",
                        $"Email: {user.Email}");
                }

                await _context.SaveChangesAsync();

                await _loggingService.LogAsync(HttpContext, "Failed login attempt", "Warning", $"Email: {user.Email}");
                ModelState.AddModelError("", "Неверный логин или пароль");
                return View(model);
            }

            user.FailedLoginAttempts = 0;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            await _analyticsService.TrackEventAsync(HttpContext, "Login", "Account/Login");
            await _loggingService.LogAsync(HttpContext, "User login", "Info", $"Email: {user.Email}");

            return RedirectToAction("Index", "Home");
        }
        // Вход (GET)
        public IActionResult Login()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _loggingService.LogAsync(HttpContext, "User logout", "Info");
            await _analyticsService.TrackEventAsync(HttpContext, "Logout", "Account/Logout");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}

