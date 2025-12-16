using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CleverOwl.WebApplication.Models
{
    public class CheckedStudentInfo
    {
        public string id { get; set; }
        public string username { get; set; }
        public Dictionary<string, string> metadata { get; set; }
    }
}