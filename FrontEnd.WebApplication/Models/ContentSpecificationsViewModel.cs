using RLI.WebApplication.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FrontEnd.WebApplication.Models
{
    public class ContentSpecificationsViewModel
    {
        
        //[BoolValidator(true, "You can't proceed without acknowledging the accuracy of the information provided.")]
        [Display(Name = "Acknowledge the Accuracy of the Information")]
        public bool AcknowledgeAccuracyInformation { get; set; }
    }
}