using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace diplom.Models
{
    public class CourseModule
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public int OrderNumber { get; set; }

        public int CourseId { get; set; }

        public Course? Course { get; set; }

        public List<Lesson> Lessons { get; set; } = new();
    }
}