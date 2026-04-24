using diplom.Data;
using diplom.Models;
using System.Security.Claims;

namespace diplom.Services
{
    public class AnalyticsService
    {
        private readonly AppDbContext _context;

        public AnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task TrackEventAsync(HttpContext httpContext, string eventType, string? page = null)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;

            if (int.TryParse(userIdClaim, out int parsedUserId))
            {
                userId = parsedUserId;
            }

            var analyticsEvent = new AnalyticsEvent
            {
                EventType = eventType,
                Page = page,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext.Request.Headers["User-Agent"].ToString()
            };

            _context.AnalyticsEvents.Add(analyticsEvent);
            await _context.SaveChangesAsync();
        }
    }
}