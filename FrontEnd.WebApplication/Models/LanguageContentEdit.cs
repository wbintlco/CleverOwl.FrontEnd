using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FrontEnd.WebApplication.Models
{
    public class LanguageContentEdit
    {
        public int LanguagesContentKey { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string FieldKey { get; set; }
        public string FieldValue { get; set; }
        public string Language { get; set; }
    }
}