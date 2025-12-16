using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Castle.MicroKernel.SubSystems.Conversion;

namespace FrontEnd.WebApplication.Models
{
    public class LanguageContentExcel
    {
        public string Field { get; set; }
        public string LanguagesContentKey { get; set; }

        public string FieldValue { get; set; }
        public string Language { get; set; }

    }
}