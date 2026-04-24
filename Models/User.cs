using System;
using System.ComponentModel.DataAnnotations;

namespace diplom.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "User"; // User, Premium, Admin

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsBlocked { get; set; } = false;

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LastLoginAt { get; set; }

        public DateTime? PremiumAssignedAt { get; set; }

        public bool IsActive { get; set; } = false;
    }
}