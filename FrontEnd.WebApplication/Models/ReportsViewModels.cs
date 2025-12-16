using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{
    public class TeachersByTownSectorGradeTableViewModel
    {
        public Nullable<int> CountTeachers { get; set; }
        public Nullable<int> CountRegisteredTeachers { get; set; }
        public Nullable<double> RegisteredTeachersPercentageRatio { get; set; }
        public Nullable<int> CountStudents { get; set; }
        public Nullable<int> CountRegisteredStudents { get; set; }
        public Nullable<double> RegisteredStudentsPercentageRatio { get; set; }
        public string SchoolName { get; set; }
        public int? TownKey { get; set; }
        public string Town { get; set; }
        public int? SectorKey { get; set; }
        public string Sector { get; set; }
        public Nullable<double> SectorIndex { get; set; }
        public int? GradeKey { get; set; }
        public string Grade { get; set; }
        public Nullable<double> GradeIndex { get; set; }

        public int? TotalRecords { get; set; }
        public int? StartRecords { get; set; }
        public int? EndRecords { get; set; }
        public int? SchoolKey { get; internal set; }
    }

    public class StudentsByTownSectorGradeTableViewModel
    {
        public Nullable<int> CountStudents { get; set; }
        public int TownKey { get; set; }
        public string Town { get; set; }
        public int SectorKey { get; set; }
        public string Sector { get; set; }
        public Nullable<int> SectorIndex { get; set; }
        public int GradeKey { get; set; }
        public string Grade { get; set; }
        public Nullable<int> GradeIndex { get; set; }

        public int? TotalRecords { get; set; }
        public int? StartRecords { get; set; }
        public int? EndRecords { get; set; }
    }

}