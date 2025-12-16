using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FrontEnd.WebApplication.Models
{
    public class SubjectViewModel
    {
        public string Subject1 { get; set; }
        public int SubjectKey { get; set; }
        public int? StatusKey { get; set; }
        public Guid? LocalSubjectGUID { get; set; }
        public string LanguageTransaltionKeys { get; set; }
        public string SubjectNamesPerLanguage { get; set; }
        public string Continue { get; set; }
        public bool Visibility { get; set; }
        public int? SubjectIndex { get; set; }
    }
}