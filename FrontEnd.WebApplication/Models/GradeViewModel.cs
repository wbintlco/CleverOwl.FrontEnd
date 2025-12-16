using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FrontEnd.WebApplication.Models
{
    public class GradeViewModel
    {
        public string Grade1 { get; set; }
        public int CycleKey { get; set; }
        public int GradeKey { get; set; }
        public int? StatusKey { get; set; }
        public Guid? LocalGradeGUID { get; set; }
        public string LanguageTransaltionKeys { get; set; }
        public string GradeNamesPerLanguage { get; set; }
        public string Continue { get; set; }
        public bool Visibility { get; set; }
        public int? GradeIndex { get; set; }
    }
}