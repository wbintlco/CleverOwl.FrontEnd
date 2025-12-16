using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CleverOwl.WebApplication.Models
{
    public class Validation
    {
        public string name { get; set; }
        public int gradeKey { get; set; }
        public string description { get; set; }
        public DateTime AllowSubmission { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime ReminderDate {get;set;}
        public bool? OnlineText { get; set; }
        public bool? FileSubmissions { get; set; }
        public int? WordLimit { get; set; }
        public int? MaxNumberFile { get; set; }

    }
}