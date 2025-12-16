using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RLI.EntityFramework.EDM;

namespace FrontEnd.WebApplication.Models
{
    public class LanguageModel
    {
        public int LanguagesContentKey { get; set; }
        public int? FieldKey { get; set; }
        public string FieldValue { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public int? LanguageKey { get; set; }
        public virtual Field Field { get; set; }
        public virtual Field Field1 { get; set; }
        public virtual Field Field2 { get; set; }
        public virtual Language Language { get; set; }
        public virtual Language Language1 { get; set; }
        public virtual Language Language2 { get; set; }
    }
}