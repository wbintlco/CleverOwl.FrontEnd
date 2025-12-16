namespace System.Data.Entity.Core.Objects
{
    internal class Grade
    {
        public int GradeKey { get; set; }
        public string DefaultGrade1 { get; set; }
        public string Grade1 { get; set; }
        public int? GradeIndex { get; set; }
        public Guid? LocalGradeGUID { get; set; }
    }
}