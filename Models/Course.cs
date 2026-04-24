using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace diplom.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? PreviewText { get; set; }

        public bool IsPremium { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int? CreatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }

        public List<CourseModule> Modules { get; set; } = new();
    }
}