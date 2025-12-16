using RLI.EntityFramework.EDM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{
    public class ObjectiveViewModel
    {
        public bool? ContinueIndex { get; set; }
        public int ObjectiveKey { get; set; }
        [Required]
        public int? GradeKey { get; set; }
        [Required]
        public int? SubjectKey { get; set; }

        [Required]
        public int? DomainKey { get; set; }

    
        public int? DomainsObjectiveKey { get; set; }
        public int? StatusKey { get; set; }

        [Required]
        public string Objective1 { get; set; }

     
        public Nullable<int> LanguageKey { get; set; }
        public Nullable<System.Guid> LocalObjectiveGUID { get; set; }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<DomainsObjective> DomainsObjectives { get; set; }
        //public virtual DataGUID DataGUID { get; set; }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<Lesson> Lessons { get; set; }
    }

    public class ObjectiveTableViewModel
    {
        
        public int ObjectiveKey { get; set; }
        public string Objective1 { get; set; }
        public int? GradeKey { get; set; }
        public string Grade1 { get; set; }

        public int? SubjectKey { get; set; }
        public string Subject1 { get; set; }

        public int? DomainKey { get; set; }
        public string Domain1 { get; set; }

        public int? StatusKey { get; set; }
        public string Status1 { get; set; }

        public Nullable<int> LanguageKey { get; set; }
        public string Language1 { get; set; }

        public int? StartRecords { get; set; }
        public int? EndRecords { get; set; }
        public int? TotalRecords { get; set; }
        
    }
}