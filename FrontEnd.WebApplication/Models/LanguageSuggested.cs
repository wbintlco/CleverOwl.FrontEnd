using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Castle.MicroKernel.SubSystems.Conversion;

namespace FrontEnd.WebApplication.Models
{
    public class LanguageSuggested
    {
        public string fieldKey { get; set; }
        public string arabic { get; set; }
        public string french { get; set; }
        public string english { get; set; }
    }
}