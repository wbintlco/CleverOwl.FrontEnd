using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{
    public class PersonalizeSchoolDetailsViewModel
    {
        [Key]
        public int? SchoolKey { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 4)]
        [DataType(DataType.Text)]
        public string SchoolName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 4)]
        [DataType(DataType.Text)]
        public string SchoolAddress { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 4)]
        [DataType(DataType.EmailAddress)]
        public string SchoolEmail { get; set; }

        [Required]
        [Display(Name = "School Number")]
        public string SchoolPhone { get; set; }
    }

    public class PersonalizeThemeViewModel
    {

        [Key]
        public int? SchoolID { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public string TopMenuTextColor { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public string TopMenuIconColor { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public string TopMenuBackgroundColor { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public string LeftMenuTextColor { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public string LeftMenuIconColor { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public string LeftMenuTitleColor { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public string LeftMenuBackgroundColor { get; set; }
    }
}