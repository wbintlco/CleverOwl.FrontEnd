using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{
    public class StudentSchoolRegisterViewModels
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int LuckyNumber { get; set; }

        public string FavoriteColor { get; set; }

        public string BirthCity { get; set; }

        public string Status { get; set; }

        public string Result { get; set; }
    }

    public class TeacherSchoolRegisterViewModels
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int LuckyNumber { get; set; }

        public string FavoriteColor { get; set; }

        public string BirthCity { get; set; }

        public string Status { get; set; }

        public string Result { get; set; }
    }

    public class SchoolGSuiteClassesRegisterViewModels
    {
        public long SSTGKey { get; set; }
        public string TownName { get; set; }
        public string SchoolName { get; set; }

        public string Grade { get; set; }

        public string Subject { get; set; }

        public string Status { get; set; }
        public string Description { get; set; }

    }

    public class GSuiteNonRegisteredViewModels
    {
        public string id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }


    }
}