using RLI.EntityFramework.EDM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CleverOwl.WebApplication.Models
{
    public class ToDoViewModel
    {
        public List<AssignmentToDoViewModel> Assignments { get; set; }
        public IEnumerable<SynchronousSession> Sessions { get; set; }
        public List<Grade> grades { get; set; }
        public DateTime TodosDate { get; set; }
    }
}