using System;
using System.ComponentModel.DataAnnotations;

namespace diplom.Models
{
    public class AnalyticsEvent
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Page { get; set; }

        public int? UserId { get; set; }

        public User? User { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }
    }
}