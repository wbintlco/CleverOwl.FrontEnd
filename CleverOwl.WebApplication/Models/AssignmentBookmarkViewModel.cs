using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CleverOwl.WebApplication.Models
{
    public class AssignmentBookmarkViewModel
    {
        public int? TimeLineComponentModeKey { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public DateTime? TimeModifed { get; set; }
        public string Description { get; set; }
        public string Grade { get; set; }
        public int GradeKey { get; set; }
        public string Subject { get; set; }
        public int SubjectKey { get; set; }
        public int? CourseId { get; set; }
        public int? CourseModuleID { get; set; }
        public int? AssignmentId { get; set; }
        public int AssignmentKey { get; set; }
    }
}