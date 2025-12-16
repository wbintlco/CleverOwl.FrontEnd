using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CleverOwl.WebApplication.Models
{
    public class LessonBookmarkViewModel
    {
        public int? TimeLineComponentModeKey { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
        public string Grade { get; set; }
        public string Subject { get; set; }
        public int GradeKey { get; set; }
        public int subjectKey { get; set; }

    }
}