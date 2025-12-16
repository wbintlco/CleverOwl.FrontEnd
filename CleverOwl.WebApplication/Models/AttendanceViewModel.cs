using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CleverOwl.WebApplication.Models
{
    public class AttendanceViewModel
    {
        public int? PresentialSessionKey { get; set; }
        public DateTime AttendedAt { get; set; }
        public string AttendedUserKey { get; set; }
    }
}