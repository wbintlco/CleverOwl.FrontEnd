using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FrontEnd.WebApplication.Models
{
    public class CrowdContentViewModel
    {
        
        [Required]
        public int Grade { get; set; }
        [Required]
        public int Subject { get; set; }
        [Required]
        public int Chapter { get; set; }
        public int Topic { get; set; }
        [Required]
        public int LessonType { get; set; }
        [Required]
        public int Provider { get; set; }
        [Required]
        public string LessonName { get; set; }
        [Required]
        public string Remark { get; set; }
        [Required]
        public int FileType { get; set; }
        [Required]
        public int Library { get; set; }



    }
}