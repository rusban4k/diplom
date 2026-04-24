using diplom.Data;
using diplom.Models;
using System.Security.Claims;

namespace diplom.Services
{
    public class LoggingService
    {
        private readonly AppDbContext _context;

        public LoggingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(HttpContext httpContext, string action, string level = "Info", string? details = null)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;

            if (int.TryParse(userIdClaim, out int parsedUserId))
            {
                userId = parsedUserId;
            }

            var logEntry = new LogEntry
            {
                Action = action,
                Level = level,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Details = details,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.Logs.Add(logEntry);
            await _context.SaveChangesAsync();
        }
    }
}