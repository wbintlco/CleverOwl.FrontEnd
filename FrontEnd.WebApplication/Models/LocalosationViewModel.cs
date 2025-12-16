using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FrontEnd.WebApplication.Models
{
    public class LocalosationViewModel
    {
        public int interfaceLocalisationKey { get; set; }
        public string controller { get; set; }
        public string action { get; set; }
        public string fieldKey { get; set; }
        public string english { get; set; }
        public string arabic { get; set; }
        public string french { get; set; }
    }
}