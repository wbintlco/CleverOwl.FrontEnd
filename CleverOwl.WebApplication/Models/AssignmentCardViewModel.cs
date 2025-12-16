using RLI.EntityFramework.EDM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CleverOwl.WebApplication.Models
{
    public class AssignmentCardViewModel
    {
        public int AssignmentKey { get; set; }
        public string CourseTitle { get; set; }
        public string AssignmentTitle { get; set; }
        public DateTime? TimeModifed { get; set; }
        public string Description { get; set; }
        public int? CourseId { get; set; }
        public int? AssignmentId { get; set; }
        public int? CourseModuleID { get; set; }
        public DateTime? DueDate { get; set; }
        public string CourseShortName { get; set; }
        public string CourseLongName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? SchoolSubjectTeacherGradeKey { get; set; }
        public virtual SchoolSubjectTeacherGrade SchoolSubjectTeacherGrade { get; set; }
        public string Submissions { get; set; }
        public string participantCount { get; set; }
        public string Status { get; set; }
        public string Grade { get; set; }
    }
}