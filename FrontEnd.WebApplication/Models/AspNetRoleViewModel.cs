using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FrontEnd.WebApplication.Models
{
    public class AspNetRoleViewModel
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int? RoleType { get; set; }
        public string Continue { get; set; }
    }
}