using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using RLI.EntityFramework.EDM;

namespace RLI.WebApplication.Models
{
    public class TopicViewModel
    {

        public int TopicKey { get; set; }
        [Required]
        public int GradeKey { get; set; }
        [Required]
        public int SubjectKey { get; set; }
        [Required]
        public int ChapterTypeKey { get; set; }
        [Required]
        public int ChapterKey { get; set; }


        [Required]
        public string Topic1 { get; set; }
        [Required]
        public string Prefix { get; set; }
        public Nullable<int> ParentTopicKey { get; set; }

        [Required]
        public Nullable<int> LanguageKey { get; set; }
        public Nullable<System.Guid> LocalTopicGUID { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ChaptersTopic> ChaptersTopics { get; set; }
        public virtual DataGUID DataGUID { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Lesson> Lessons { get; set; }
    }

    public class TopicTableViewModel
    {

        public int TopicKey { get; set; }

        public string Topic1 { get; set; }

        public string GradeName { get; set; }

        public string SubjectName { get; set; }

        public string Library { get; set; }

        public string ChapterName { get; set; }
        public string Status { get; set; }
        public string LanguageName { get; set; }
        public int? GradeKey { get; set; }
       
        public int? SubjectKey { get; set; }
      
        public int? ChapterTypeKey { get; set; }
     
        public int? ChapterKey { get; set; }
        public int? StatusKey { get; set; }
        public int? TotalRecords { get; set; }
        public int? StartRecords { get; set; }
        public int? EndRecords { get; set; }
        public int? ChapterIndex { get; internal set; }
    }
}