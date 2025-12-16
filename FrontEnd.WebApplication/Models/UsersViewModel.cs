using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{
    public class UsersCreateViewModel
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string ShortDescription { get; set; }

        [Required]
        public string[] Roles { get; set; }
    }

    public class UsersTableViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ShortDescription { get; set; }
        public string Status { get; set; }
        public DateTime? CreationDate { get; set; }
        public int Count { get; set; }
        public int? TotalRecords { get; set; }
        public int? StartRecords { get; set; }
        public int? EndRecords { get; set; }
    }
}