using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace CleverOwl.WebApplication.Models
{
    public class ContactModel
    {
        public int memberID { get; set; }
        public int coversationID { get; set; }
        public string fullname { get; set; }
        public string profileurl { get; set; }
        public string profileimageurl { get; set; }
        public string profileimageurlsmall { get; set; }
        public bool isonline { get; set; }
        public bool isread { get; set; }
        public bool showonlinestatus { get; set; }
        public bool isblocked { get; set; }
        public bool iscontact { get; set; }
        public bool isdeleted { get; set; }
        public object canmessageevenifblocked { get; set; }
        public object canmessage { get; set; }
        public object requirescontact { get; set; }
        public List<object> contactrequests { get; set; }
        
        public string lastMessage { get; set; }
    }
}