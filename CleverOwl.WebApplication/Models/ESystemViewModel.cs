using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FrontEnd.WebApplication.Models
{
    public class ESystemViewModel
    {
        public int ESystemKey { get; set; }
        public int EsystemCategoryKey { get; set; }
        public string ESystemName { get; set; }
        public int? ESystemIndex { get; set; }
        public string Continue { get; set; }
    }
}