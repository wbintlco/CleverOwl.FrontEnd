using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FrontEnd.WebApplication.Models
{
    public class ContentCurriculumViewModel
    {
        [Required]
        [Display(Name = "Acknowledge the Accuracy of the Information")]
        public bool AcknowledgeAccuracyInformation { get; set; }

        [Required]
        public int GradeKey { get; set; }
        [Required]
        public int SubjectKey { get; set; }
        [Required]
        public int ChapterKey { get; set; }
        [Required]
        public int TopicKey { get; set; }
        [Required]
        public int LessonType { get; set; }

        [Required]
        public int ProviderKey { get; set; }

        [Required]
        public string Remark { get; set; }
        [Required]
        public string LessonName { get; set; }
        




    }
}