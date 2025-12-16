using System;
using System.ComponentModel.DataAnnotations;


namespace RLI.WebApplication.Models
{
    public class StudentSearchModel
    {
        [Required]
        public int? SectorKey { get; set; }

        [Required]
        public int? SchoolKey { get; set; }

        [Required]
        public int? TownKey { get; set; }

        [Required]
        public int? GradeKey { get; set; }

        public string FirstName { get; set; }

        public string FatherName { get; set; }

        public string LastName { get; set; }

    }

    [Serializable]
    public class StudentRegisterModel
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
        public int FirstNameStudentKey { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 2)]
        [DataType(DataType.Text)]
        public string FirstName { get; set; }

        [Required]
        public int FatherNameStudentKey { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 2)]
        [DataType(DataType.Text)]
        public string FatherName { get; set; }

        [Required]
        public int LastNameStudentKey { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 2)]
        [DataType(DataType.Text)]
        public string LastName { get; set; }

        [DataType(DataType.Text)]
        public string Username { get; set; }

        [DataType(DataType.Text)]
        public string Password { get; set; }

        public int? ExactStudentKey { get; set; }

    }

    public class StudentSecurityQuestionsModel
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

    public class StudentSecurityAnswerModel
    {
        [Key]
        public string UserId { get; set; }

        [Required]
        public int QuestionIndex { get; set; }

        [Required]
        //[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 1)]
        [DataType(DataType.Text)]
        [Display(Name = "Answer")]
        public string Answer { get; set; }
    }

}