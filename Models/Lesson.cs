using System.ComponentModel.DataAnnotations;

namespace diplom.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public int OrderNumber { get; set; }

        public bool IsPreview { get; set; } = false;

        public int CourseModuleId { get; set; }

        public CourseModule? CourseModule { get; set; }
    }
}