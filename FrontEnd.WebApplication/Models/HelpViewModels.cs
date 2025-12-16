using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{
    public class ContactViewModel
    {
        public string UserKey { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "EmailConfirmation")]
        [Compare("Email", ErrorMessage = "The email and confirmation email do not match.")]
        public string EmailConfirmation { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Subject")]
        public string Subject { get; set; }

        [Required]
        [Display(Name = "Text")]
        public string Text { get; set; }
    }
}