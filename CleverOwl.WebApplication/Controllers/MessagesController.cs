using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using RLI.Common.DataObjects;
using RLI.Common.Globals;
using RLI.Common.Managers;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Attributes;
using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CleverOwl.WebApplication.Controllers
{
    [System.Web.Mvc.Authorize]
    public class MessagesController : BaseController
    {
        [MoodleAdmin]
        [MoodleToken]
        public async System.Threading.Tasks.Task<ActionResult> Index(string conversationId = null, string toUserKey = null)
        {
            ViewBag.conversationId = conversationId;
            ViewBag.toUserKey = toUserKey;
            string userKey = CurrentUser.Id;
            RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.Userkey == userKey && r.ESystem.ESystemName == "Moodle").FirstOrDefaultAsync();
            string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;
            ViewBag.currentMoodleUserId = currentMoodleUserId;

            MessageConversationsModel messageConversationsModel = await RLI.Common.Managers.MessagesManager.GetMoodleMessageConversations(currentMoodleUserId, HttpContext);
            List<MessageConversationsModel.Conversation> allUserConversations = messageConversationsModel.conversations;
            List<ContactModel> contacts = new List<ContactModel>();
            if (contacts != null)
            {
                foreach (MessageConversationsModel.Conversation userConversation in allUserConversations)
                {
                    ContactModel contact = new ContactModel();
                    contact.coversationID = userConversation.id;
                    MessageConversationsModel.Member member = userConversation.members.ElementAt(0);
                    contact.memberID = member.id;
                    contact.fullname = member.fullname;
                    contact.profileurl = member.profileurl;
                    contact.profileimageurl = member.profileimageurl;
                    contact.profileimageurlsmall = member.profileimageurlsmall;
                    contact.isonline = member.isonline;
                    contact.isread = userConversation.isread;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.iscontact = member.iscontact;
                    contact.isdeleted = member.isdeleted;
                    contact.canmessageevenifblocked = member.canmessageevenifblocked;
                    contact.canmessage = member.canmessage;
                    contact.requirescontact = member.requirescontact;
                    contact.contactrequests = member.contactrequests;
                    contact.lastMessage = userConversation.messages.ElementAt(0).text;
                    contacts.Add(contact);
                }
            }
            return View(contacts);
        }

        [MoodleAdmin]
        [MoodleToken]

        public async System.Threading.Tasks.Task<List<ContactModel>> GetMessagesContacts(string currentMoodleUserId)
        {
            MessageConversationsModel messageConversationsModel = await RLI.Common.Managers.MessagesManager.GetMoodleMessageConversations(currentMoodleUserId.ToString(), HttpContext) ;
            //string methodName = "core_message_get_conversations";
        
            //Dictionary<string, string> body = new Dictionary<string, string>();
            //string instanceIndicator = RLI.Common.Managers.ConfigurationManager.getApplicationInstanceIndicator();
            //string token = string.Empty;
            //if (this.HttpContext == null)
            //{
            //    token = MoodleManager.retrieveAdminToken(this.HttpContext);
            //}
            //else
            //{
            //    token = MoodleManager.retrieveAdminToken();
            //}
            //body.Add("wstoken", token);          
            //body.Add("wsfunction", methodName);
            //body.Add("moodlewsrestformat", "json");
            //body.Add("userid", currentMoodleUserId);
            //body.Add("type", "1");
            //body.Add("limitnum", "51");
            //body.Add("limitfrom", "0");
            //body.Add("favourites", "0");
            //body.Add("mergeself", "1");

            //string executeTokenPost = await MoodleManager.executeTokenPost($"{RLI.Common.Managers.ConfigurationManager.getMoodleAPIUrl()}webservice/rest/server.php", body);
            //MessageConversationsModel messageConversationsModel = JsonConvert.DeserializeObject<MessageConversationsModel>(executeTokenPost);
            List<MessageConversationsModel.Conversation> allUserConversations = messageConversationsModel.conversations;
            List<ContactModel> contacts = new List<ContactModel>();
            if (contacts != null && allUserConversations!=null)
            {
                foreach (MessageConversationsModel.Conversation userConversation in allUserConversations)
                {
                    ContactModel contact = new ContactModel();
                    contact.coversationID = userConversation.id;
                    MessageConversationsModel.Member member = userConversation.members.ElementAt(0);
                    contact.memberID = member.id;
                    contact.fullname = member.fullname;
                    contact.profileurl = member.profileurl;
                    contact.profileimageurl = member.profileimageurl;
                    contact.profileimageurlsmall = member.profileimageurlsmall;
                    contact.isonline = member.isonline;
                    contact.isread = userConversation.isread;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.iscontact = member.iscontact;
                    contact.isdeleted = member.isdeleted;
                    contact.canmessageevenifblocked = member.canmessageevenifblocked;
                    contact.canmessage = member.canmessage;
                    contact.requirescontact = member.requirescontact;
                    contact.contactrequests = member.contactrequests;
                    contact.lastMessage = userConversation.messages.ElementAt(0).text;
                    contacts.Add(contact);
                }
            }

            return contacts;
        }
        [HttpPost]
        [MoodleAdmin]
        [MoodleToken]
        public async Task<ActionResult> GetConversation(int id, int toUser_id, bool newMessage = false)
        {
            string userKey = CurrentUser.Id;
            RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.Userkey == userKey && r.ESystem.ESystemName == "Moodle").FirstOrDefaultAsync();
            string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;
            ViewBag.currentMoodleUserId = currentMoodleUserId;
            try
            {
                if (id < 0)
                {
                    ViewBag.to_userId = toUser_id;
                    
                    return PartialView("_EmptyConversation");
                }
                else
                {
                    string currentDate = DateTime.UtcNow.ToString("MM-dd-yyyy");
                    ViewBag.currentDateList = currentDate.Split('-');
                    await RLI.Common.Managers.MessagesManager.MoodleMarkMessagesAsRead(id.ToString(), currentMoodleUserId.ToString(), HttpContext);
                    ConversationMessagesModel conversationMessagesModel = await RLI.Common.Managers.MessagesManager.MoodleGetConversationMessages(userKey, id.ToString(), HttpContext);

                    if (newMessage)
                    {
                        string toUserIdString = conversationMessagesModel.messages.FirstOrDefault().useridfrom.ToString();
                        RemoteAuthentication RAFromUser = await db.RemoteAuthentications.Where(r => r.EsystemRemoteId == toUserIdString && r.ESystem.ESystemName == "Moodle"&&r.Userkey==userKey).FirstOrDefaultAsync();

                        Timeline timeline = new Timeline();

                        timeline.TimeLineEntityKey = conversationMessagesModel.id;
                        timeline.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "Messages").FirstOrDefaultAsync()).TimelineComponentKey;
                        timeline.CreatedAt = DateTime.Now;
                        timeline.UserKey = userKey;
                        if (RAFromUser != null)
                        {
                            timeline.CreatedByUserKey = RAFromUser.Userkey;
                        }
                        timeline.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "External").FirstOrDefaultAsync()).EntitySourceKey;

                        db.Timelines.Add(timeline);
                        await db.SaveChangesAsync();
                        DefaultHubManager hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
                        RLI.WebApplication.Managers.MoodleSignalRManager MoodleSignalRManager = hubManager.ResolveHub("MoodleSignalRManager") as RLI.WebApplication.Managers.MoodleSignalRManager;
                        MoodleSignalRManager.SendTimelineSignal(userKey, timeline.TimelineKey.ToString());
                    }
                    ConversationMessagesModel conversationMessagesModelResult = new ConversationMessagesModel();
                    conversationMessagesModelResult.id = conversationMessagesModel.id;
                    conversationMessagesModelResult.members = conversationMessagesModel.members;
                    conversationMessagesModelResult.messages = conversationMessagesModel.messages.OrderByDescending(m => m.timecreated).Take(10).ToList();
                    ViewBag.to_userId = toUser_id;
                    ViewBag.convId = id;
                    ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Messages", "Index", CurrentLanguageIdentifier);
                    ViewBag.UserName = conversationMessagesModelResult.members.Where(m => m.id == toUser_id).Select(m => m.fullname).FirstOrDefault();
                    ViewBag.UserImage = conversationMessagesModelResult.members.Where(m => m.id == toUser_id).Select(m => m.profileurl).FirstOrDefault();
                    return PartialView("_Messages", conversationMessagesModelResult);
                }
            }
            catch (Exception e)
            {
                return Json("error");
            }

        }
        [HttpPost]
        [MoodleAdmin]
        [MoodleToken]
        public async Task<ActionResult> GetPreviousMessages(int conversationid, int skip)
        {
            if (conversationid < 0)
            {
                return Json(404);
            }
            else
            {
                string userKey = CurrentUser.Id;
                RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.Userkey == userKey && r.ESystem.ESystemName == "Moodle").FirstOrDefaultAsync();
                string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;
                ViewBag.currentMoodleUserId = currentMoodleUserId;
                string currentDate = DateTime.UtcNow.ToString("MM-dd-yyyy");
                ViewBag.currentDateList = currentDate.Split('-');
                ConversationMessagesModel conversationMessagesModel = await RLI.Common.Managers.MessagesManager.MoodleGetConversationMessages(userKey, conversationid.ToString(), HttpContext);
                ConversationMessagesModel conversationMessagesModelResult = new ConversationMessagesModel();
                conversationMessagesModelResult.id = conversationMessagesModel.id;
                conversationMessagesModelResult.members = conversationMessagesModel.members;
                conversationMessagesModelResult.messages = conversationMessagesModel.messages.OrderByDescending(m => m.timecreated).Skip(skip * 10).Take(10).ToList();
                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Messages", "Index", CurrentLanguageIdentifier);
                return PartialView("_Messages", conversationMessagesModelResult);
            }
           
        }

        [HttpPost]
        [MoodleAdmin]
        [MoodleToken]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetUserConversations()
        {
            string userKey = CurrentUser.Id;
            RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.Userkey == userKey && r.ESystem.ESystemName == "Moodle").FirstOrDefaultAsync();
            string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;
            ViewBag.currentMoodleUserId = currentMoodleUserId;
            try
            {

                //string currentDate = DateTime.UtcNow.ToString("MM-dd-yyyy");
                //ViewBag.currentDateList = currentDate.Split('-');

                //get all the user coversations.
                MessageConversationsModel messageConversationsModel = await RLI.Common.Managers.MessagesManager.GetMoodleMessageConversations(currentMoodleUserId.ToString(), HttpContext);
                List<MessageConversationsModel.Conversation> allUserConversations = messageConversationsModel.conversations;
                List<ContactModel> contacts = new List<ContactModel>();
                foreach (MessageConversationsModel.Conversation userConversation in allUserConversations)
                {
                    ContactModel contact = new ContactModel();
                    contact.coversationID = userConversation.id;
                    MessageConversationsModel.Member member = userConversation.members.ElementAt(0);
                    contact.memberID = member.id;
                    contact.fullname = member.fullname;
                    contact.profileurl = member.profileurl;
                    contact.profileimageurl = member.profileimageurl;
                    contact.profileimageurlsmall = member.profileimageurlsmall;
                    contact.isonline = member.isonline;
                    contact.isread = userConversation.isread;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.iscontact = member.iscontact;
                    contact.isdeleted = member.isdeleted;
                    contact.canmessageevenifblocked = member.canmessageevenifblocked;
                    contact.canmessage = member.canmessage;
                    contact.requirescontact = member.requirescontact;
                    contact.contactrequests = member.contactrequests;
                    contact.lastMessage = userConversation.messages.ElementAt(0).text;
                    contacts.Add(contact);
                }
                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Messages", "Index", CurrentLanguageIdentifier);
                return PartialView("~/Views/Messages/_Chat.cshtml", contacts);

            }
            catch (Exception e)
            {
                return Json("error");
            }

        }

        [HttpPost]
        [MoodleAdmin]
        [MoodleToken]
        public async Task<ActionResult> SearchChat(string convId, string username)
        {
            string userKey = CurrentUser.Id;
            RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.Userkey == userKey && r.ESystem.ESystemName == "Moodle").FirstOrDefaultAsync();
            string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;
            ViewBag.currentMoodleUserId = currentMoodleUserId;
            if (username == "")
            {
                //get all the user coversations.
                MessageConversationsModel messageConversationsModel = await RLI.Common.Managers.MessagesManager.GetMoodleMessageConversations(currentMoodleUserId.ToString(), HttpContext);
                List<MessageConversationsModel.Conversation> allUserConversations = messageConversationsModel.conversations;
                List<ContactModel> contacts = new List<ContactModel>();
                foreach (MessageConversationsModel.Conversation userConversation in allUserConversations)
                {
                    ContactModel contact = new ContactModel();
                    contact.coversationID = userConversation.id;
                    MessageConversationsModel.Member member = userConversation.members.ElementAt(0);
                    contact.memberID = member.id;
                    contact.fullname = member.fullname;
                    contact.profileurl = member.profileurl;
                    contact.profileimageurl = member.profileimageurl;
                    contact.profileimageurlsmall = member.profileimageurlsmall;
                    contact.isonline = member.isonline;
                    contact.isread = userConversation.isread;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.iscontact = member.iscontact;
                    contact.isdeleted = member.isdeleted;
                    contact.canmessageevenifblocked = member.canmessageevenifblocked;
                    contact.canmessage = member.canmessage;
                    contact.requirescontact = member.requirescontact;
                    contact.contactrequests = member.contactrequests;
                    contact.lastMessage = userConversation.messages.ElementAt(0).text;
                    contacts.Add(contact);
                }
                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Messages", "Index", CurrentLanguageIdentifier);
                return PartialView("_Chat", contacts);
            }
            else
            {
                //get all the user coversations.
                MessageConversationsModel messageConversationsModel = await RLI.Common.Managers.MessagesManager.GetMoodleMessageConversations(currentMoodleUserId.ToString(), HttpContext);
                List<MessageConversationsModel.Conversation> allUserConversations = messageConversationsModel.conversations;
                //get all the contact searched by the user.
                MessageSearchViewModel messageSearchViewModel = await RLI.Common.Managers.MessagesManager.MoodleMessageSearchUsers(currentMoodleUserId.ToString(), username, HttpContext);
                List<MessageSearchViewModel.Noncontact> allContacts = messageSearchViewModel.noncontacts;
                //search for the contacts with conversations (that the user texted them before).
                List<MessageConversationsModel.Conversation> userConversations = new List<MessageConversationsModel.Conversation>();
                foreach (MessageSearchViewModel.Noncontact contact in allContacts)
                {
                    int currentContactId = contact.id;
                    MessageConversationsModel.Conversation conversationWithContact = allUserConversations.Where(m => m.members.ElementAt(0).id == currentContactId).FirstOrDefault();
                    if (conversationWithContact != null)
                    {
                        userConversations.Add(conversationWithContact);
                    }
                }
                //search for the contacts without conversations (that the user did not texted them before).
                List<MessageSearchViewModel.Noncontact> contactsWithoutConversations = new List<MessageSearchViewModel.Noncontact>();
                foreach (MessageSearchViewModel.Noncontact contact in allContacts)
                {
                    int currentContactId = contact.id;
                    int count = allUserConversations.Where(m => m.members.ElementAt(0).id == currentContactId).Count();
                    if (count == 0)
                    {
                        contactsWithoutConversations.Add(contact);
                    }
                }
                //merge contact without and with conversation into a list of contacts
                List<ContactModel> contacts = new List<ContactModel>();
                foreach (MessageConversationsModel.Conversation userConversation in userConversations)
                {
                    ContactModel contact = new ContactModel();
                    contact.coversationID = userConversation.id;
                    MessageConversationsModel.Member member = userConversation.members.ElementAt(0);
                    contact.memberID = member.id;
                    contact.fullname = member.fullname;
                    contact.profileurl = member.profileurl;
                    contact.profileimageurl = member.profileimageurl;
                    contact.profileimageurlsmall = member.profileimageurlsmall;
                    contact.isonline = member.isonline;
                    contact.isread = userConversation.isread;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.iscontact = member.iscontact;
                    contact.isdeleted = member.isdeleted;
                    contact.canmessageevenifblocked = member.canmessageevenifblocked;
                    contact.canmessage = member.canmessage;
                    contact.requirescontact = member.requirescontact;
                    contact.contactrequests = member.contactrequests;
                    contact.lastMessage = userConversation.messages.ElementAt(0).text;
                    contacts.Add(contact);
                }
                int negativeIndex = -1;
                foreach (MessageSearchViewModel.Noncontact member in contactsWithoutConversations)
                {
                    ContactModel contact = new ContactModel();
                    contact.coversationID = negativeIndex--;
                    contact.memberID = member.id;
                    contact.fullname = member.fullname;
                    contact.profileurl = member.profileurl;
                    contact.profileimageurl = member.profileimageurl;
                    contact.profileimageurlsmall = member.profileimageurlsmall;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.showonlinestatus = member.showonlinestatus;
                    contact.iscontact = member.iscontact;
                    contact.isdeleted = member.isdeleted;
                    contact.isread = true;
                    contact.canmessageevenifblocked = member.canmessageevenifblocked;
                    contact.canmessage = member.canmessage;
                    contact.requirescontact = member.requirescontact;
                    contact.contactrequests = member.contactrequests;
                    contact.lastMessage = "";
                    contacts.Add(contact);
                }
                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Messages", "Index", CurrentLanguageIdentifier);
                return PartialView("_Chat", contacts);
            }

        }
        [HttpPost]
        [MoodleAdmin]
        [MoodleToken]
        public async Task<ActionResult> SendMessage(string converId, string message, string fromUserID, string toUserID)
        {
            string userKey = CurrentUser.Id;
            RemoteAuthentication toUserKey = await db.RemoteAuthentications.Where(r => r.EsystemRemoteId == toUserID && r.ESystem.ESystemName == "Moodle").OrderByDescending(r => r.RemoteAuthenticationKey).FirstOrDefaultAsync();
            RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.Userkey == userKey && r.ESystem.ESystemName == "Moodle").FirstOrDefaultAsync();
            string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;
            ViewBag.currentMoodleUserId = currentMoodleUserId;
            if (int.Parse(converId) < 0)
            {
                converId = "";
            }
            if (converId == "")
            {
                try
                {
                    SendMessageModel sendNewMessage = await RLI.Common.Managers.MessagesManager.MoodleSendMessageToUser(message, fromUserID, toUserID, "0", HttpContext);
                }catch(Exception e)
                {

                }
                DefaultHubManager hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
                RLI.WebApplication.Managers.MoodleSignalRManager MoodleSignalRManager = hubManager.ResolveHub("MoodleSignalRManager") as RLI.WebApplication.Managers.MoodleSignalRManager;
              
                string currentDate = DateTime.UtcNow.ToString("MM-dd-yyyy");
                ViewBag.currentDateList = currentDate.Split('-');
                //get the new conversation id
                MessageConversationsModel messageConversationsModel = await RLI.Common.Managers.MessagesManager.GetMoodleMessageConversations(currentMoodleUserId.ToString(), HttpContext);
                List<MessageConversationsModel.Conversation> conversations = messageConversationsModel.conversations;
                int to_userId = int.Parse(toUserID);
                int newConversationId = conversations.Where(m => m.members.ElementAt(0).id == to_userId).Select(m => m.id).FirstOrDefault();
                ConversationMessagesModel conversationMessagesModel = await RLI.Common.Managers.MessagesManager.MoodleGetConversationMessages(userKey, newConversationId.ToString(), HttpContext);
                ViewBag.to_userId = to_userId;

                Timeline timeline = new Timeline();

                timeline.TimeLineEntityKey = newConversationId;
                timeline.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "Messages").FirstOrDefaultAsync()).TimelineComponentKey;
                timeline.CreatedAt = DateTime.Now;
                timeline.UserKey = toUserKey.AspNetUser.Id;
                timeline.CreatedByUserKey = userKey;
                timeline.Description = message;
                timeline.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "External").FirstOrDefaultAsync()).EntitySourceKey;

                db.Timelines.Add(timeline);
                await db.SaveChangesAsync();
               await MoodleSignalRManager.SendTimelineSignal(toUserKey.AspNetUser.Id, timeline.TimelineKey.ToString());
                await MoodleSignalRManager.Send(toUserKey.AspNetUser.Id, message);
                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Messages", "Index", CurrentLanguageIdentifier);
                return PartialView("_Messages", conversationMessagesModel);
            }
            else
            {
                SendMessageModel sendMessage = await RLI.Common.Managers.MessagesManager.MoodleSendMessageToConversation(converId, message, HttpContext);
                string currentDate = DateTime.UtcNow.ToString("MM-dd-yyyy");
                ViewBag.currentDateList = currentDate.Split('-');
                ConversationMessagesModel conversationMessagesModel = await RLI.Common.Managers.MessagesManager.MoodleGetConversationMessages(userKey, converId, HttpContext);
                int to_userId = int.Parse(toUserID);
                ViewBag.to_userId = to_userId;
                DefaultHubManager hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
                RLI.WebApplication.Managers.MoodleSignalRManager MoodleSignalRManager = hubManager.ResolveHub("MoodleSignalRManager") as RLI.WebApplication.Managers.MoodleSignalRManager;
                await MoodleSignalRManager.Send(toUserKey.AspNetUser.Id, message);

                var latestTimlineMessage = await db.Timelines.Where(t => t.UserKey == toUserKey.AspNetUser.Id
                && t.TimelineComponent.ComponentName == "Messages").OrderByDescending(t=> t.CreatedAt).ToListAsync();
                if (latestTimlineMessage.Count() != 0 && latestTimlineMessage.FirstOrDefault().CreatedAt <= DateTime.Now.AddDays(-1))
                {

                    Timeline timeline = new Timeline();
                    timeline.TimeLineEntityKey = int.Parse(converId);
                    timeline.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "Messages").FirstOrDefaultAsync()).TimelineComponentKey;
                    timeline.CreatedAt = DateTime.Now;
                    timeline.UserKey = toUserKey.AspNetUser.Id;
                    timeline.CreatedByUserKey = userKey;
                    timeline.Description = message;
                    timeline.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "External").FirstOrDefaultAsync()).EntitySourceKey;

                    db.Timelines.Add(timeline);
                    await db.SaveChangesAsync();
                   await MoodleSignalRManager.SendTimelineSignal(toUserKey.AspNetUser.Id, timeline.TimelineKey.ToString());

                }

                ConversationMessagesModel conversationMessagesModelResult = new ConversationMessagesModel();
                conversationMessagesModelResult.id = conversationMessagesModel.id;
                conversationMessagesModelResult.members = conversationMessagesModel.members;
                conversationMessagesModelResult.messages = conversationMessagesModel.messages.OrderByDescending(m => m.timecreated).Take(10).ToList();
                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Messages", "Index", CurrentLanguageIdentifier);
                return PartialView("_Messages", conversationMessagesModelResult);
            }


        }

    }
}