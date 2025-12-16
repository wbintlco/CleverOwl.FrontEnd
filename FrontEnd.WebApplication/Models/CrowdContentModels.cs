using RLI.WebApplication.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FrontEnd.WebApplication.Models
{
    public class ReviewProfileViewModel
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public string AlternativeEmail { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Sector { get; set; }

        [Required]
        [BoolValidator(true, "You can't proceed without acknowledging the accuracy of the information provided.")]
        [Display(Name = "Acknowledge the Accuracy of the Information")]
        public bool AcknowledgeAccuracyInformation { get; set; }
    }

    public class ContentContributionViewModel
    {
        [Required]
        public ContentContributionPairViewModel[] ContentContribution { get; set; }
    }

    public class ContentContributionPairViewModel
    {
        [Required]
        public int ContributionGrade { get; set; }
        [Required]
        public int ContributionSubject { get; set; }
    }
}