using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RLI.WebApplication.Models
{
    public class EditFAQsViewModel
    {
        [Key]
        public int FAQKey { get; set; }

        [Required]
        public int LanguageKey { get; set; }

        [Required]
        public int CategoryKey { get; set; }

        [Required]
        [AllowHtml]
        public string Question { get; set; }

        [Required]
        [AllowHtml]
        public string Answer { get; set; }

        [Required]
        public int DisplayIndex { get; set; }

    }
}