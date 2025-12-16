using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FrontEnd.WebApplication.Models
{
    public class CycleViewModel
    {
        public string Cycle1 { get; set; }
        public int CycleKey { get; set; }
        public Guid? LocalCycleGUID { get; set; }
        public string LanguageTransaltionKeys { get; set; }
        public string CycleNamesPerLanguage { get; set; }
        public string Continue { get; set; }
        public bool Visibility { get; set; }
    }
}