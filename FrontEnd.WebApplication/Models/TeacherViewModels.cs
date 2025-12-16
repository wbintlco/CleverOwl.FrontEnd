using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{

    public class TeacherSearchModel
    {
        [Required]
        public int? SectorKey { get; set; }

        [Required]
        public int? SchoolKey { get; set; }

        [Required]
        public int? TownKey { get; set; }

        [Required]
        public int? GradeKey { get; set; }

        [Required]
        public int? SubjectKey { get; set; }

        public string FirstName { get; set; }

        public string FatherName { get; set; }

        public string LastName { get; set; }

    }

    [Serializable]
    public class TeacherRegisterModel
    {
        [Required]
        public int SectorKey { get; set; }

        [Required]
        public int SchoolKey { get; set; }

        [Required]
        public int TownKey { get; set; }

        [Required]
        public int GradeKey { get; set; }

        [Required]
        public int SubjectKey { get; set; }

        [Required]
        public int FirstNameTeacherKey { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 2)]
        [DataType(DataType.Text)]
        public string FirstName { get; set; }

        [Required]
        public int FatherNameTeacherKey { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 2)]
        [DataType(DataType.Text)]
        public string FatherName { get; set; }

        [Required]
        public int LastNameTeacherKey { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 2)]
        [DataType(DataType.Text)]
        public string LastName { get; set; }

        [DataType(DataType.Text)]
        public string Username { get; set; }

        [DataType(DataType.Text)]
        public string Password { get; set; }

        public int? ExactTeacherKey { get; set; }

    }

    public class TeacherSecurityQuestionsModel
    {
        [Required]
        public int LuckyNumber { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 3)]
        [DataType(DataType.Text)]
        [Display(Name = "Question 1")]
        public string FavoriteColor { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 3)]
        [DataType(DataType.Text)]
        [Display(Name = "Question 2")]
        public string BirthCity { get; set; }
    }

    public class TeacherSecurityAnswerModel
    {
        [Key]
        public string UserId { get; set; }

        [Required]
        public int QuestionIndex { get; set; }

        [Required]
        //[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 3)]
        [DataType(DataType.Text)]
        [Display(Name = "Answer")]
        public string Answer { get; set; }
    }
}