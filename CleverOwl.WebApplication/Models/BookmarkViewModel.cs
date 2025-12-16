using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RLI.EntityFramework.EDM;

namespace CleverOwl.WebApplication.Models
{
    
    public class BookmarkViewModel
    {
        public List<Assignment> AssignmentsList { get; set; }
        public List<Lesson> LessonsList { get; set; }
        public List<SynchronousSession> SessionsList { get; set; }
        public List<RLI.Common.DataObjects.MoodleEventViewModel> EventsList { get; set; }
        
    }
}