using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RLI.WebApplication.Models
{
    public class EditLanguageContentsViewModel
    {
        [Key]
        public int LanguagesContentKey { get; set; }

        [Required]
        public int FieldKey { get; set; }

        [Required]
        public int LanguageKey { get; set; }

        [Required]
        [AllowHtml]
        public string FieldValue { get; set; }

        [Required]
        public string Controller { get; set; }

        [Required]
        public string Action { get; set; }

    }
}