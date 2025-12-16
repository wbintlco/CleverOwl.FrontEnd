using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CleverOwl.WebApplication.Models
{
    public class ContainerHeaderComponentsModel
    {
        
        public List<Route> Routes { get; set; }
        public string TitleName { get; set; }
        public string IconType { get; set; }
        public string IconSource { get; set; }
        public bool ContainsSearchBar { get; set; }
        public bool ContainsActionButton { get; set; }
        public string ActionButtonName { get; set; }
        public string ActionButtonUrl { get; set; }
        
    }
}