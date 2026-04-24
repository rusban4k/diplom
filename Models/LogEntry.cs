using System;
using System.ComponentModel.DataAnnotations;

namespace diplom.Models
{
    public class LogEntry
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Level { get; set; } = "Info";

        public int? UserId { get; set; }

        public User? User { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? Details { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }
    }
}