using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Attributes;
using RLI.WebApplication.Models;
using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CleverOwl.WebApplication.Controllers
{
    [System.Web.Mvc.Authorize]
    public class BookmarkController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        // GET: Bookmark

        [MoodleToken]
        public async Task<ActionResult> Index()
        {

            string userKey = CurrentUser.Id;
            RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == userKey).FirstOrDefaultAsync();
            string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;
            ViewBag.currentMoodleUserId = currentMoodleUserId;
            List<Calendar> events = new List<Calendar>();

            string[] assignmentsArrayStr = await db.TimeLineComponentModes.Where(t => t.UserKey == userKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Assignments").OrderByDescending(a => a.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
            int?[] assignmentsArrayInt = assignmentsArrayStr.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => (int?)Convert.ToInt32(x)).ToArray();
            List<Assignment> assignmentsList = await db.Assignments.Where(a => assignmentsArrayInt.Contains(a.AssignmentKey)).Distinct().Take(6).ToListAsync();

            string[] lesonsArrayStr = await db.TimeLineComponentModes.Where(t => t.UserKey == userKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Lessons").OrderByDescending(a => a.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
            int?[] lessonsArrayInt = lesonsArrayStr.Where(l => !String.IsNullOrWhiteSpace(l)).Select(l => (int?)Convert.ToInt32(l)).ToArray();
            List<Lesson> lessons = await db.Lessons.Where(l => lessonsArrayInt.Contains(l.LessonKey)).Distinct().OrderBy(l => l.LessonKey).Take(6).ToListAsync();

            string[] sessionsArrayStr = await db.TimeLineComponentModes.Where(t => t.UserKey == userKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "SynchronousSessions").OrderByDescending(a => a.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
            int?[] sessionsArrayInt = sessionsArrayStr.Where(s => !String.IsNullOrWhiteSpace(s)).Select(s => (int?)Convert.ToInt32(s)).ToArray();
            List<SynchronousSession> sessions = await db.SynchronousSessions.Where(s => sessionsArrayInt.Contains(s.SynchronousSessionKey)).Distinct().OrderBy(s => s.CreationDate).Take(6).ToListAsync();

            string[] eventsIdArrayStr = await db.TimeLineComponentModes.Where(e => e.UserKey == userKey && e.Mode.Mode1 == "Bookmarked" && e.TimelineComponent.ComponentName == "Events").OrderByDescending(t => t.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
            List<RLI.Common.DataObjects.MoodleEventViewModel> eventsList = new List<RLI.Common.DataObjects.MoodleEventViewModel>();
            foreach (string eventId in eventsIdArrayStr)
            {
                RLI.Common.DataObjects.MoodleEventViewModel moodleEvent = await RLI.Common.Managers.MoodleEventsManager.GetCalendarEventById(eventId, HttpContext);
                if (moodleEvent != null)
                {
                    if (moodleEvent.@event != null)
                    {
                        eventsList.Add(moodleEvent);
                    }
                }
            }

            BookmarkViewModel bookmarkViewModel = new BookmarkViewModel();
            bookmarkViewModel.AssignmentsList = assignmentsList;
            bookmarkViewModel.LessonsList = lessons;
            bookmarkViewModel.SessionsList = sessions;
            bookmarkViewModel.EventsList = eventsList.OrderBy(e => e.@event.timestart).Take(6).ToList();
            return View(bookmarkViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [MoodleToken]
        public async Task<ActionResult> GetNextEvents(int skipEvents)
        {
            try
            {
                string userkey = CurrentUser.Id;
                string[] eventsIdArrayStr = await db.TimeLineComponentModes.Where(e => e.UserKey == userkey && e.Mode.Mode1 == "Bookmarked" && e.TimelineComponent.ComponentName == "Events").Select(t => t.EntityKey).Distinct().ToArrayAsync();
                List<RLI.Common.DataObjects.MoodleEventViewModel> eventsList = new List<RLI.Common.DataObjects.MoodleEventViewModel>();
                foreach (string eventId in eventsIdArrayStr)
                {
                    RLI.Common.DataObjects.MoodleEventViewModel moodleEvent = await RLI.Common.Managers.MoodleEventsManager.GetCalendarEventById(eventId, HttpContext);
                    if (moodleEvent != null)
                    {
                        eventsList.Add(moodleEvent);
                    }
                }
                List<RLI.Common.DataObjects.MoodleEventViewModel> result = eventsList.OrderBy(e => e.@event.timestart).Skip(skipEvents * 6).Take(6).ToList();
                int count = result.Count();
                if (count > 0)
                {
                    ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Bookmark", "Index", CurrentLanguageIdentifier);
                    return PartialView("_CalendarEventsListingCard", result);
                }
                else
                {
                    return Json(404);
                }
            }
            catch (Exception e)
            {
                return Json(500);
            }

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveEventBookmark(int event_key)
        {
            string eventKey = event_key.ToString();
            string userKey = CurrentUser.Id;
            var timeLineComponentMode = await db.TimeLineComponentModes.Where(t => t.EntityKey == eventKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Events" && t.UserKey == userKey).FirstOrDefaultAsync();
            int result = 400;
            if (timeLineComponentMode != null)
            {
                result = await RLI.Common.Managers.MoodleEventsManager.RemoveBookmark((int)timeLineComponentMode.TimeLineComponentModeKey);
                if (result == 200)
                {
                    DefaultHubManager hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
                    RLI.WebApplication.Managers.MoodleSignalRManager MoodleSignalRManager = hubManager.ResolveHub("MoodleSignalRManager") as RLI.WebApplication.Managers.MoodleSignalRManager;
                    MoodleSignalRManager.SendBookmarkSignal(userKey);
                }
            }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePathBookmark(int path_Key)
        {
            string userKey = CurrentUser.Id;
            string assignmentKey = path_Key.ToString();
            var timeLineComponentMode = await db.TimeLineComponentModes.Where(t => t.EntityKey == assignmentKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "LearningPaths" && t.UserKey == userKey).FirstOrDefaultAsync();
            int result = 400;
            if (timeLineComponentMode != null)
            {
                result = await RLI.Common.Managers.PathManager.RemoveBookmark((int)timeLineComponentMode.TimeLineComponentModeKey);
                if (result == 200)
                {
                    DefaultHubManager hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
                    RLI.WebApplication.Managers.MoodleSignalRManager MoodleSignalRManager = hubManager.ResolveHub("MoodleSignalRManager") as RLI.WebApplication.Managers.MoodleSignalRManager;
                    MoodleSignalRManager.SendBookmarkSignal(userKey);
                }
            }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveBookmark(int assignment_Key)
        {
            string userKey = CurrentUser.Id;
            string assignmentKey = assignment_Key.ToString();
            var timeLineComponentMode = await db.TimeLineComponentModes.Where(t => t.EntityKey == assignmentKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Assignments" && t.UserKey == userKey).FirstOrDefaultAsync();
            int result = 400;
            if (timeLineComponentMode != null)
            {
                result = await RLI.Common.Managers.AssignmentsManager.RemoveBookmark((int)timeLineComponentMode.TimeLineComponentModeKey);
                if (result == 200)
                {
                    DefaultHubManager hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
                    RLI.WebApplication.Managers.MoodleSignalRManager MoodleSignalRManager = hubManager.ResolveHub("MoodleSignalRManager") as RLI.WebApplication.Managers.MoodleSignalRManager;
                    MoodleSignalRManager.SendBookmarkSignal(userKey);
                }
            }
            return Json(result);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLessonBookmark(int lesson_Key)
        {
            string userKey = CurrentUser.Id;
            string lessonKey = lesson_Key.ToString();
            var timeLineComponentMode = await db.TimeLineComponentModes.Where(t => t.EntityKey == lessonKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Lessons" && t.UserKey == userKey).FirstOrDefaultAsync();
            int result = 400;
            if (timeLineComponentMode != null)
            {
                result = await RLI.Common.Managers.LessonsManager.RemoveBookmark((int)timeLineComponentMode.TimeLineComponentModeKey);
                if (result == 200)
                {
                    DefaultHubManager hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
                    RLI.WebApplication.Managers.MoodleSignalRManager MoodleSignalRManager = hubManager.ResolveHub("MoodleSignalRManager") as RLI.WebApplication.Managers.MoodleSignalRManager;
                    MoodleSignalRManager.SendBookmarkSignal(userKey);
                }
            }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNewBookmarks()
        {
            List<Calendar> events = new List<Calendar>();

            string[] assignmentsArrayStr = await db.TimeLineComponentModes.Where(t => t.UserKey == CurrentUser.Id && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Assignments").OrderByDescending(a => a.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
            int?[] assignmentsArrayInt = assignmentsArrayStr.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => (int?)Convert.ToInt32(x)).ToArray();
            List<RLI.EntityFramework.EDM.Assignment> assignmentsList = await db.Assignments.Where(a => assignmentsArrayInt.Contains(a.AssignmentKey)).Distinct().OrderByDescending(s => s.CreatedAt).Take(1).ToListAsync();

            string[] lesonsArrayStr = await db.TimeLineComponentModes.Where(t => t.UserKey == CurrentUser.Id && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Lessons").OrderByDescending(a => a.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
            int?[] lessonsArrayInt = lesonsArrayStr.Where(l => !String.IsNullOrWhiteSpace(l)).Select(l => (int?)Convert.ToInt32(l)).ToArray();
            List<Lesson> lessons = await db.Lessons.Where(l => lessonsArrayInt.Contains(l.LessonKey)).Distinct().OrderByDescending(l => l.LessonKey).Take(1).ToListAsync();

            string[] sessionsArrayStr = await db.TimeLineComponentModes.Where(t => t.UserKey == CurrentUser.Id && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "SynchronousSessions").OrderByDescending(a => a.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
            int?[] sessionsArrayInt = sessionsArrayStr.Where(s => !String.IsNullOrWhiteSpace(s)).Select(s => (int?)Convert.ToInt32(s)).ToArray();
            List<SynchronousSession> sessions = await db.SynchronousSessions.Where(s => sessionsArrayInt.Contains(s.SynchronousSessionKey)).Distinct().OrderByDescending(s => s.CreationDate).Take(1).ToListAsync();

            string[] pathsArrayStr = await db.TimeLineComponentModes.Where(t => t.UserKey == CurrentUser.Id && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "LearningPaths").OrderByDescending(a => a.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
            int?[] pathsArrayInt = pathsArrayStr.Where(s => !String.IsNullOrWhiteSpace(s)).Select(s => (int?)Convert.ToInt32(s)).ToArray();
            List<Path> paths = await db.Paths.Where(s => pathsArrayInt.Contains(s.PathKey)).Distinct().OrderByDescending(s => s.CreatedAt).Take(1).ToListAsync();


            string[] eventsIdArrayStr = await db.TimeLineComponentModes.Where(e => e.UserKey == CurrentUser.Id && e.Mode.Mode1 == "Bookmarked" && e.TimelineComponent.ComponentName == "Events").Select(t => t.EntityKey).Distinct().ToArrayAsync();
            List<RLI.Common.DataObjects.MoodleEventViewModel> eventsList = new List<RLI.Common.DataObjects.MoodleEventViewModel>();
            foreach (string eventId in eventsIdArrayStr)
            {

                RLI.Common.DataObjects.MoodleEventViewModel moodleEvent = await RLI.Common.Managers.MoodleEventsManager.GetCalendarEventById(eventId, HttpContext);
                if (moodleEvent != null)
                {
                    if (moodleEvent.@event != null)
                    {
                        eventsList.Add(moodleEvent);
                    }
                }
            }

            BookmarkViewModel bookmarkViewModel = new BookmarkViewModel();
            bookmarkViewModel.AssignmentsList = assignmentsList;
            bookmarkViewModel.LessonsList = lessons;
            bookmarkViewModel.SessionsList = sessions;
            bookmarkViewModel.EventsList = eventsList.OrderByDescending(e => e.@event.timestart).Take(1).ToList();
            bookmarkViewModel.PathsList = paths;
            Bookmarks = bookmarkViewModel;
            return PartialView("_BookmarksSideMenu", bookmarkViewModel);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveSessionBookmark(int session_key)
        {
            string userKey = CurrentUser.Id;
            string sessionKey = session_key.ToString();
            var timeLineComponentMode = await db.TimeLineComponentModes.Where(t => t.EntityKey == sessionKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "SynchronousSessions" && t.UserKey == userKey).FirstOrDefaultAsync();
            int result = 400;
            if (timeLineComponentMode != null)
            {
                result = await RLI.Common.Managers.SynchronousSessionsManager.RemoveBookmark((int)timeLineComponentMode.TimeLineComponentModeKey);
                if (result == 200)
                {
                    DefaultHubManager hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
                    RLI.WebApplication.Managers.MoodleSignalRManager MoodleSignalRManager = hubManager.ResolveHub("MoodleSignalRManager") as RLI.WebApplication.Managers.MoodleSignalRManager;
                    MoodleSignalRManager.SendBookmarkSignal(userKey);
                }
            }
            return Json(result);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextAssignments(int skipAssignments)
        {
            try
            {
                string userKey = CurrentUser.Id;
                string[] assignmentsArrayStr = await db.TimeLineComponentModes.Where(t => t.UserKey == userKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Assignments").OrderBy(a => a.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
                int?[] assignmentsArrayInt = assignmentsArrayStr.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => (int?)Convert.ToInt32(x)).ToArray();
                List<Assignment> assignmentsList = await db.Assignments.Where(a => assignmentsArrayInt.Contains(a.AssignmentKey)).Distinct().OrderBy(a => a.CreatedAt).Skip(skipAssignments * 6).Take(6).ToListAsync();
                int count = assignmentsList.Count();
                if (count > 0)
                {
                    ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Assignments", "Index", CurrentLanguageIdentifier);
                    return PartialView("_AssignmentCard", assignmentsList);
                }
                else
                {
                    return Json(404);
                }
            }
            catch (Exception e)
            {
                return Json(500);
            }


        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextLessons(int skipLessons)
        {
            try
            {
                string userKey = CurrentUser.Id;
                string[] lesonsArrayStr = await db.TimeLineComponentModes.Where(t => t.UserKey == userKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Lessons").OrderBy(a => a.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
                int?[] lessonsArrayInt = lesonsArrayStr.Where(l => !String.IsNullOrWhiteSpace(l)).Select(l => (int?)Convert.ToInt32(l)).ToArray();
                List<Lesson> lessonsList = await db.Lessons.Where(l => lessonsArrayInt.Contains(l.LessonKey)).Distinct().OrderBy(l => l.LessonKey).Skip(skipLessons * 6).Take(6).ToListAsync();
                int count = lessonsList.Count();
                if (count > 0)
                {
                    ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Bookmark", "Index", CurrentLanguageIdentifier);
                    return PartialView("_LessonsListingCard", lessonsList);
                }
                else
                {
                    return Json(404);
                }
            }
            catch (Exception e)
            {
                return Json("500");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextSessions(int skipSessions)
        {
            try
            {
                string userKey = CurrentUser.Id;
                string[] sessionsArrayStr = await db.TimeLineComponentModes.Where(t => t.UserKey == userKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "SynchronousSessions").OrderBy(a => a.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
                int?[] sessionsArrayInt = sessionsArrayStr.Where(s => !String.IsNullOrWhiteSpace(s)).Select(s => (int?)Convert.ToInt32(s)).ToArray();
                List<SynchronousSession> sessions = await db.SynchronousSessions.Where(s => sessionsArrayInt.Contains(s.SynchronousSessionKey)).Distinct().OrderBy(s => s.CreationDate).Skip(skipSessions * 6).Take(6).ToListAsync();
                int count = sessions.Count();
                if (count > 0)
                {
                    ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Bookmark", "Index", CurrentLanguageIdentifier);
                    return PartialView("_SessionsCard", sessions);
                }
                else
                {
                    return Json(404);
                }
            }
            catch (Exception e)
            {
                return Json("500");
            }
        }
        
    }
}