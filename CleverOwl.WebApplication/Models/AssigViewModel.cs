
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CleverOwl.WebApplication.Models
{
    public class AssigViewModel
    {
        public bool IsCreatePage { get; set; }
        public int GradeToPass { get; set; }
        public List<string> FileTypes { get; set; }
        public int MaxSubSize { get; set; }
        public int MaxFilesNumber { get; set; }
        public bool EnableWordLimit { get; set; }
        public int? WordLimit { get; set; }
        public bool FilesSubmission { get; set; }
        public bool OnlineText { get; set; }
        [Required]
        public string RemindMeToGradeBy { get; set; }
        [Required]
        public string DueDate { get; set; }
        [Required]
        public string AllowSubmissionFrom { get; set; }
        public string Attachements { get; set; }
        public string DescAttachementsId { get; set; }
        public int? Return { get; set; }
        public int? Update { get; set; }
        public string Add { get; set; }
        public int? CourseModule { get; set; }
        public int? Instance { get; set; }
        public int CourseId { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string AssignmetName { get; set; }
        [Required]
        public int GradeKey { get; set; }
        [Required]
        public int SubjectKey { get; set; }
        [Required]
        public int SchoolKey { get; set; }
        [Required]
        public int UserCourseKey { get; set; }
        public bool HideFromStudents { get; set; }
        public string FileTypesStr { get; set; }
        public List<RLI.Common.DataObjects.Introattachment> IntroAttachments { get; set; }
        
    }
}