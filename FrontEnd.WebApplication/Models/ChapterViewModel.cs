using RLI.EntityFramework.EDM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace RLI.WebApplication.Models
{
    public class ChapterViewModel
    {
  
        public int ChapterKey { get; set; }
        [Required]
        public string ChapterName { get; set; }
       
        public Nullable<int> CycleKey { get; set; }
        [Required]
        public Nullable<int> SubjectKey { get; set; }
        [Required]
        public Nullable<int> GradeKey { get; set; }
       
        public Nullable<int> ChapterIndex { get; set; }
        public Nullable<int> ParentChapterKey { get; set; }
    
        public Nullable<int> LanguageKey { get; set; }
        public Nullable<System.Guid> LocalChapterGUID { get; set; }
        [Required]
        public Nullable<int> ChapterTypeKey { get; set; }

        public virtual Grade Grade { get; set; }
        public virtual DataGUID DataGUID { get; set; }
        public virtual Subject Subject { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ChaptersTopic> ChaptersTopics { get; set; }
        public virtual ChaptersType ChaptersType { get; set; }
    }

    public class ChapterTableViewModel
    {
        public int ChapterKey { get; set; }
        
        public string ChapterName { get; set; }

        public Nullable<int> GradeKey { get; set; }
        public string GradeName { get; set; }
        public Nullable<int> SubjectKey { get; set; }
        public string SubjectName { get; set; }
        public Nullable<int> ChapterTypeKey { get; set; }
        public string Library { get; set; }
        public string Status { get; set; }
        public Nullable<int> StatusKey { get; set; }

        public int? TotalRecords { get; set; }
        public int? StartRecords { get; set; }
        public int? EndRecords { get; set; }
        public int? ChapterIndex { get; internal set; }
    }

}