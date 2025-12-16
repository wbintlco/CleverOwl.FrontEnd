using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{
    public class CreateCalendarViewModel
    {
        [Required]     
        public Nullable<System.DateTime> StartDate { get; set; }
        [Required]
        public Nullable<System.DateTime> EndDate { get; set; }
        [Required]
        public Nullable<int> GradeKey { get; set; }
        [Required]
        public Nullable<int> SubjectKey { get; set; }
        [Required]
        public Nullable<int> ChapterKey { get; set; }
        [Required]
        public Nullable<int> TopicKey { get; set; }
    }
}