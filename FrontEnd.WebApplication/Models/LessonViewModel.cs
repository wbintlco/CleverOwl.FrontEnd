using RLI.EntityFramework.EDM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{
    public class LessonViewModel
    {
     
        public int LessonKey { get; set; }
        public string LessonName { get; set; }
        public Guid? FileKey { get; set; }
        //[Url]
        [Required]
        public string LessonURL { get; set; }

        [Required]
        public Nullable<int> LanguageKey { get; set; }
        [Required]
        public Nullable<int> ESystemKey { get; set; }
      
        

        
        public Nullable<int> TopicKey { get; set; }
        public List<int?> ObjectiveKey { get; set; }

        public string Remark { get; set; }
        public string SecondaryLessonURL { get; set; }

        [Required]
        public Nullable<int> LessonTypeKey { get; set; }

   
        public virtual ESystem ESystem { get; set; }

   
        public virtual Language Language { get; set; }

       
        public virtual Topic Topic { get; set; }
        
        public virtual LessonType LessonType { get; set; }
        
        
      
        public Nullable<int> GradeKey { get; set; }
       
        public Nullable<int> SubjectKey { get; set; }

     
        public Nullable<int> ChapterTypeKey { get; set; }

     
        public Nullable<int> ChapterKey { get; set; }
        public Nullable<int> DomainKey { get; set; }
        
        public int? StatusKey { get; set; }
    }

    public class LessonTableViewModel
    {
        public int LessonKey { get; set; }
        public int? StatusKey { get; set; }
        public string Status { get; set; }
        public string Grade { get; set; }
        public string Subject { get; set; }
        public string Library { get; set; }

        public string Chapter { get; set; }
        public string Language { get; set; }
        public string Topic { get; set; }
        public string LessonName { get; set; }
        public string LessonURL { get; set; }
        public string Provider { get; set; }
        public int? TotalRecords { get; set; }
        public int? StartRecords { get; set; }
        public int? EndRecords { get; set; }
        public string LessonType { get; internal set; }
        public int? LessonTypeKey { get; internal set; }
        public int? ChapterIndex { get; internal set; }
    }
}