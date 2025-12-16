using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CleverOwl.WebApplication.Models
{
    public class AssignmentToDoViewModel
    {
        public int? AssignmentId { get; set; }
        public String submissionStatus { get; set; }
        public DateTime? DueDate { get; set; }
        public String Description { get; set; }
        public String Title { get; set; }
        public String GradeName { get; set; }
        public String SubjectName { get; set; }
        public int AssignmentKey { get; set; }
    }
}