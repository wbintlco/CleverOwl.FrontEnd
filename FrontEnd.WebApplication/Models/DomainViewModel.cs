using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{
    public class DomainViewModel
    {
        public int DomainKey { get; set; }
        public string Domain1 { get; set; }
        public int? SubjectKey { get; set; }
        public int? GradeKey { get; set; }
        public int? DomainIndex { get; set; }
        public Guid? LocalDomainGUID { get; set; }
        public int? StatusKey { get; set; }

        public string LanguageTransaltionKeys { get; set; }
        public string DomainNamesPerLanguage { get; set; }
        public string andContinue { get; set; }
    }
}