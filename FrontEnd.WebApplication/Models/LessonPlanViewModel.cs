using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{
    public class LessonPlanViewModel
    {
        public string title { get; set; }
        public string grade { get; set; }
        public string subject { get; set; }
        public double? time { get; set; }
        public string lessonOverView { get; set; }
        public string creationDate { get; set; }
        public string assessment { get; set; }
        public int?[] objectives { get; set; }
        public int?[] DLPlans { get; set; }
        public string plans { get; set; }
    }

    public class PlanViewModel
    {
        public int? planId { get; set; }
        public string planValue { get; set; }
    }
}