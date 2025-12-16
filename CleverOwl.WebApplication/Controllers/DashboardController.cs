using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using RLI.Common.DataObjects;
using RLI.Common.Managers;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Attributes;
using RLI.WebApplication.Controllers;
using RLI.WebApplication.Models;
using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace CleverOwl.WebApplication.Controllers
{
    [System.Web.Mvc.Authorize]
    public class DashboardController : BaseController
    {
        //private OpenCurateEntities db = new OpenCurateEntities();


        // GET: Resources
        [MoodleToken]
        [MoodleAdmin]

        public async Task<ActionResult> Index()
        {
            var AllQueryTime = new Stopwatch();
            AllQueryTime.Start();
            var timeOne = new Stopwatch();
            timeOne.Start();
            await LogManager.log("Dashboard, Index | Debug", "Started");

            ViewBag.DateTimeWhenPageWasLoaded = DateTime.Now;
            string currentUserId = CurrentUser.Id;
            var timelineOfCureentUser = db.Timelines.Where(t => t.UserKey == CurrentUser.Id).OrderByDescending(t => t.CreatedAt).Take(10);
            string remoteId = "";
            timeOne.Stop();
            TimeSpan timeTaken = timeOne.Elapsed;
            await LogManager.log("timeOne", timeTaken.ToString());
            var timeTwo = new Stopwatch();
            timeTwo.Start();
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student" || RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher")
            {
                remoteId = (await db.RemoteAuthentications.Where(r => r.AspNetUser.Id == currentUserId && r.ESystem.ESystemName == "Moodle").FirstOrDefaultAsync()).EsystemRemoteId;
            }
            timeTwo.Stop();
            TimeSpan timeTake = timeTwo.Elapsed;
            await LogManager.log("timeTwo", timeTake.ToString());
            var timeThree = new Stopwatch();
            timeThree.Start();
            var timeLineLessonsIds = timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Lessons").Select(t => t.TimeLineEntityKey);
            var timeLineAssignmentsIds = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Assignments").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
            var synchronousSessionsIds = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "SynchronousSessions").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
            var pathsIds = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "LearningPaths").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
            var timeLineEventsIds = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Events").Select(t => t.TimeLineEntityKey).ToArrayAsync();
            var timeLineNotificationsIds = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Notifications").Select(t => t.TimeLineEntityKey).ToArrayAsync();
            DateTime Yesterday = DateTime.Now.AddDays(-1);
            var timeLineMessagesIds = timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Messages" && t.CreatedAt >= Yesterday).Select(t => t.TimeLineEntityKey);

            var timelineOfCureentUserCount = db.Timelines.Where(t => t.UserKey == CurrentUser.Id).OrderByDescending(t => t.CreatedAt);

            var timeLineLessonsCount = timelineOfCureentUserCount.Where(t => t.TimelineComponent.ComponentName == "Lessons").Count();
            var timeLineAssignments = await timelineOfCureentUserCount.Where(t => t.TimelineComponent.ComponentName == "Assignments").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
            var timeLineAssignmentsCount = await db.Assignments.Where(a => timeLineAssignments.Contains(a.AssignmentKey) && a.DueDate >= DateTime.Now).CountAsync();

            var synchronousSessions = await timelineOfCureentUserCount.Where(t => t.TimelineComponent.ComponentName == "SynchronousSessions").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
            var synchronousSessionsCount = await db.SynchronousSessions.Where(a => synchronousSessions.Contains(a.SynchronousSessionKey) && a.SessionDate >= DateTime.Now).CountAsync();

            ViewBag.timeLineLessonsCount = timeLineLessonsCount;
            ViewBag.timeLineAssignmentsCount = timeLineAssignmentsCount;
            ViewBag.synchronousSessionsCount = synchronousSessionsCount;

            await LogManager.log("Dashboard, Index | Debug", "Moodle Calendar Events");
            timeThree.Stop();
            TimeSpan timTakee = timeThree.Elapsed;
            await LogManager.log("timeThree", timTakee.ToString());
            List<MoodleEventViewModel> moodleEventViewModels = new List<MoodleEventViewModel>();
            var timeFour = new Stopwatch();
            timeFour.Start();
            foreach (var eventId in timeLineEventsIds)
            {
                MoodleEventViewModel moodleEventViewModel = new MoodleEventViewModel();
                string eventKey = eventId.ToString();
                moodleEventViewModel = await RLI.Common.Managers.MoodleEventsManager.GetCalendarEventById(eventKey, HttpContext);

                moodleEventViewModels.Add(moodleEventViewModel);
            }

            await LogManager.log("Dashboard, Index | Debug", "Timeline View Model");

            ViewBag.TimeLineEvents = moodleEventViewModels;
            TimelineViewModel timelineViewModel = new TimelineViewModel();
            timelineViewModel.Timelines = await timelineOfCureentUser.ToListAsync();
            timeFour.Stop();
            TimeSpan tiTaks = timeFour.Elapsed;
            await LogManager.log("timeFour", tiTaks.ToString());
            var timeFive = new Stopwatch();
            timeFive.Start();
            var assignments = await db.Assignments.Where(a => timeLineAssignmentsIds.Contains(a.AssignmentKey)).ToListAsync();
            List<AssignmentTimelineComponentViewModel> assignmentTimelineComponentViewModels = new List<AssignmentTimelineComponentViewModel>();


            var assignmentIds = assignments
    .Where(a => a.AssignmentId.HasValue)
    .Select(a => (long)a.AssignmentId.Value)
    .ToList();

            var submissionCounts = MoodleManager.GetSubmissionCounts(assignmentIds);
            var studentCounts = MoodleManager.GetStudentCounts(assignmentIds);

            var submissionCountsDict = submissionCounts.ToDictionary(x => x.AssignmentId, x => x.SubmissionCount);
            var studentCountsDict = studentCounts.ToDictionary(x => x.AssignmentId, x => x.StudentCount);

            foreach (var a in assignments)
            {
                AssignmentTimelineComponentViewModel assignmentTimelineComponent = new AssignmentTimelineComponentViewModel();
                assignmentTimelineComponent.AssignmentId = a.AssignmentId;
                assignmentTimelineComponent.AssignmentKey = a.AssignmentKey;
                assignmentTimelineComponent.AssignmentLessons = a.AssignmentLessons;
                assignmentTimelineComponent.AssignmentTitle = a.AssignmentTitle;
                assignmentTimelineComponent.AssignmentType = a.AssignmentType;
                assignmentTimelineComponent.AssignmentTypeKey = a.AssignmentTypeKey;
                assignmentTimelineComponent.CreatedAt = a.CreatedAt;
                assignmentTimelineComponent.StudentAssignmentReports = a.StudentAssignmentReports;
                assignmentTimelineComponent.CourseId = a.CourseId;
                assignmentTimelineComponent.CourseLongName = a.CourseLongName;
                assignmentTimelineComponent.CourseModuleID = a.CourseModuleID;
                assignmentTimelineComponent.CourseShortName = a.CourseShortName;
                assignmentTimelineComponent.CourseTitle = a.CourseTitle;
                assignmentTimelineComponent.Description = a.Description;
                assignmentTimelineComponent.DueDate = a.DueDate;
                assignmentTimelineComponent.SchoolSubjectTeacherGrade = a.SchoolSubjectTeacherGrade;
                assignmentTimelineComponent.SchoolSubjectTeacherGradeKey = a.SchoolSubjectTeacherGradeKey;
                assignmentTimelineComponent.TimeModifed = a.TimeModifed;
                timeFive.Stop();
                TimeSpan tTke = timeFive.Elapsed;
                await LogManager.log("timeFive", tTke.ToString());
                var timeSix = new Stopwatch();
                timeSix.Start();
                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student" || RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher")
                {
                    //  assignmentTimelineComponent.SubmissionDetails = await SubmissionsManager.GetAssignSubmissionDetails(a.AssignmentId.ToString(), remoteId, HttpContext);                 
                }




                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher")
                {
                    if (a.AssignmentId.HasValue)
                    {
                        var assignmentId = (long)a.AssignmentId.Value;

                        var submissionCount = submissionCountsDict.ContainsKey(assignmentId)
                            ? submissionCountsDict[assignmentId]
                            : 0;

                        var studentCount = studentCountsDict.ContainsKey(assignmentId)
                            ? studentCountsDict[assignmentId]
                            : 0;

                        assignmentTimelineComponent.Submissions = submissionCount.ToString();
                        assignmentTimelineComponent.participantCount = studentCount.ToString();

                    }
                }

                assignmentTimelineComponentViewModels.Add(assignmentTimelineComponent);
                timeSix.Stop();
                TimeSpan tttttTake = timeSix.Elapsed;
                await LogManager.log("timeSix", tttttTake.ToString());
            }


            var timeSeven = new Stopwatch();
            timeSeven.Start();
            timelineViewModel.Assignments = assignmentTimelineComponentViewModels;
            timelineViewModel.Lessons = await db.Lessons.Where(l => timeLineLessonsIds.Contains(l.LessonKey)).ToListAsync();
            timelineViewModel.Notifications = await db.Alerts.Where(l => timeLineNotificationsIds.Contains(l.AlertKey)).ToListAsync();
            timelineViewModel.SynchronousSessions = await db.SynchronousSessions.Where(a => synchronousSessionsIds.Contains(a.SynchronousSessionKey)).OrderByDescending(s => s.SessionDate).ToListAsync();
            timelineViewModel.Paths = await db.Paths.Where(p => pathsIds.Contains(p.PathKey)).ToListAsync();
            timelineViewModel.MoodleEvents = moodleEventViewModels;

            ViewBag.StartedDateOfUser = CurrentUser.CreationDate;
            timeSeven.Stop();
            TimeSpan ttTake = timeSeven.Elapsed;
            await LogManager.log("timeSeven", ttTake.ToString());
            await LogManager.log("Dashboard, Index | Debug", "Completed");
            var timeHeight = new Stopwatch();
            timeHeight.Start();
            string instanceIndicator = RLI.Common.Managers.ConfigurationManager.getApplicationInstanceIndicator();
            List<GetOnboardingQuestionnaire_Result> questionnaire = new List<GetOnboardingQuestionnaire_Result>();


            var user = CurrentUser;
            // Always set defaults first
            ViewBag.HasVirtualClass = false;
            ViewBag.HasGECompanion = false;
            ViewBag.HasAuthoring = false;
            ViewBag.HasMInstructor = false;
            ViewBag.HasLessonsBank = false;
            ViewBag.HasPresentialSessions = false;
            ViewBag.HasDigitalLibrary = false;
            ViewBag.HasInternationalCurriculum = false;
            ViewBag.HasEdumedia = false;
            ViewBag.HasEnergia = false;
            ViewBag.HasEGooru = false;
            

            if (User.Identity.GetUserId() != null)
            {
                // Collect all possible schoolKeys
                List<int> schoolKeys = new List<int>();

                // Teacher
                if (CurrentUser.TeacherKey != null)
                {
                    var sstg = db.SchoolSubjectTeacherGrades
                        .FirstOrDefault(t => t.TeacherKey == CurrentUser.TeacherKey);
                    if (sstg?.SchoolKey != null)
                        schoolKeys.Add(sstg.SchoolKey.Value);
                }

                // Student
                if (CurrentUser.Studentkey != null)
                {
                    var student = db.Students
                        .FirstOrDefault(s => s.StudentKey == CurrentUser.Studentkey);
                    if (student?.SchoolKey != null)
                        schoolKeys.Add(student.SchoolKey.Value);
                }

                // Coordinator / Other scoped roles
                var scopedSchoolKeys = db.UserRoleScopes
                    .Where(u => u.UserId == CurrentUser.Id && u.ScopingTypeKey == 1) // school scoping
                    .Select(s => s.ScopingEntity)
                    .Where(k => k.HasValue)
                    .Select(k => k.Value)
                    .ToList();

                schoolKeys.AddRange(scopedSchoolKeys);

                // Remove duplicates
                schoolKeys = schoolKeys.Distinct().ToList();

                // Now check features across all schools
                if (schoolKeys.Any())
                {
                    var schoolFeatures = db.SchoolFeatures
                        .Where(sf => schoolKeys.Contains(sf.SchoolKey))
                        .Select(sf => sf.FeatureTypeKey)
                        .ToList();

                    ViewBag.HasVirtualClass = schoolFeatures.Contains((int)RLI.Common.Enums.FeatureTypeEnum.VirtualClass);
                    ViewBag.HasGECompanion = schoolFeatures.Contains((int)RLI.Common.Enums.FeatureTypeEnum.GECompanion);
                    ViewBag.HasAuthoring = schoolFeatures.Contains((int)RLI.Common.Enums.FeatureTypeEnum.Authoring);
                    ViewBag.HasMInstructor = schoolFeatures.Contains((int)RLI.Common.Enums.FeatureTypeEnum.MInstructor);
                    ViewBag.HasLessonsBank = schoolFeatures.Contains((int)RLI.Common.Enums.FeatureTypeEnum.LessonsBank);
                    ViewBag.HasPresentialSessions = schoolFeatures.Contains((int)RLI.Common.Enums.FeatureTypeEnum.PresentialSessions);


                    var esystems = db.SchoolsEsystems.Where(s => s.SchoolKey.HasValue && schoolKeys.Contains(s.SchoolKey.Value) &&(s.StatusKey == (int)RLI.Common.Enums.StatusEnum.StatusAssigned ||s.StatusKey == (int)RLI.Common.Enums.StatusEnum.StatusPendingToBeAssigned))
     .Select(s => s.PartnerTypeKey)
     .ToList();

                    ViewBag.HasDigitalLibrary = esystems.Contains(2);
                    ViewBag.HasInternationalCurriculum = esystems.Contains(3);
                    ViewBag.HasEdumedia = esystems.Contains(4);

                        var partners= db.SchoolsEsystems.Where(s => s.SchoolKey.HasValue && schoolKeys.Contains(s.SchoolKey.Value) && (s.StatusKey == (int)RLI.Common.Enums.StatusEnum.StatusAssigned || s.StatusKey == (int)RLI.Common.Enums.StatusEnum.StatusPendingToBeAssigned))
     .Select(s => s.EsystemKey)
     .ToList();
                    var usergooru = await db.RemoteAuthentications.Where(r => r.Userkey == currentUserId && r.ESystemKey == (int)RLI.Common.Enums.ESystemEnum.MyGooru).FirstOrDefaultAsync();
                    var userenergia = await db.RemoteAuthentications.Where(r => r.Userkey == currentUserId && r.ESystemKey == (int)RLI.Common.Enums.ESystemEnum.EnergiaSOI).FirstOrDefaultAsync();
                    ViewBag.HasEnergia = partners.Contains((int)RLI.Common.Enums.ESystemEnum.EnergiaSOI)
                     && userenergia != null;

                    ViewBag.HasEGooru = partners.Contains((int)RLI.Common.Enums.ESystemEnum.MyGooru)
                                        && usergooru != null;


                }
            }


            if (User.Identity.IsAuthenticated && instanceIndicator == "cleverOwl")
            {

                var userId = CurrentUser.Id;
                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
                {

                    //questionnaire = db.GetUserQuestionnaire(userId).ToList();
                    questionnaire = db.GetOnboardingQuestionnaire(userId).ToList();

                    if (questionnaire.Count() != 0)
                    {
                        ViewBag.Questionnaire = questionnaire;
                        return View(timelineViewModel);
                    }
                }
            }

            timeHeight.Stop();
            TimeSpan tttrrttTake = timeHeight.Elapsed;
            await LogManager.log("timeHeight", tttrrttTake.ToString());
            AllQueryTime.Stop();
            TimeSpan AlltimeTaken = AllQueryTime.Elapsed;
            await LogManager.log("AlltimeTaken", AlltimeTaken.ToString());

            return View(timelineViewModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetLatestSession()
        {
            try
            {
                string currentUserId = CurrentUser.Id;
                var timelineOfCureentUser = db.Timelines.Where(t => t.UserKey == CurrentUser.Id).OrderByDescending(t => t.CreatedAt);
                var synchronousSessionsIds = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "SynchronousSessions").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
                var synchronousSessions = await db.SynchronousSessions.Where(a => synchronousSessionsIds.Contains(a.SynchronousSessionKey)).OrderByDescending(s => s.SessionDate).ToListAsync();

                SynchronousSession latestSession = null;
                string subjectOfLatestSession = "";

                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher")
                {
                    latestSession = db.SynchronousSessions.OrderBy(s => s.SessionDate).Where(s => s.CreatedByUserKey == currentUserId && s.SessionDate >= DateTime.Now).FirstOrDefault();
                }

                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
                {
                    AspNetUser currentUser = db.AspNetUsers.Where(a => a.Id == currentUserId).FirstOrDefault();
                    int studentGradeKey = (int)currentUser.Student.GradeKey;
                    int studentSchoolKey = (int)currentUser.Student.SchoolKey;
                    latestSession = db.SynchronousSessions.OrderBy(s => s.SessionDate).Where(s => s.SchoolSubjectTeacherGrade.GradeKey == studentGradeKey && s.SchoolSubjectTeacherGrade.SchoolKey == studentSchoolKey && s.SessionDate >= DateTime.Now).FirstOrDefault();
                }

                if (latestSession != null)
                {
                    int? SchoolSubjectTeacherGradeKey = latestSession.SchoolSubjectTeacherGradeKey;
                    subjectOfLatestSession = db.SchoolSubjectTeacherGrades.Where(s => s.SchoolSubjectTeacherGradeKey == SchoolSubjectTeacherGradeKey).FirstOrDefault().Subject.Subject1;
                    Dictionary<string, string> latestSessionDictionary = new Dictionary<string, string>();
                    latestSessionDictionary.Add("latestSessionDate", latestSession.SessionDate.ToString());
                    latestSessionDictionary.Add("subjectOfLatestSession", subjectOfLatestSession);
                    latestSessionDictionary.Add("sessionUrl", latestSession.MeetingURL);
                    //JavaScriptSerializer js = new JavaScriptSerializer();
                    //string jsonData = js.Serialize(latestSessionDictionary);
                    //return Json(jsonData);
                    return Json(latestSessionDictionary);
                }

                return Json(404);
            }
            catch (Exception e)
            {
                return Json(500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RefreshBadges()
        {
            try
            {
                string currentUserId = CurrentUser.Id;
                var timelineOfCureentUserCount = db.Timelines.Where(t => t.UserKey == CurrentUser.Id).OrderByDescending(t => t.CreatedAt);
                var timeLineLessonsCount = timelineOfCureentUserCount.Where(t => t.TimelineComponent.ComponentName == "Lessons").Count();
                var timeLineAssignments = await timelineOfCureentUserCount.Where(t => t.TimelineComponent.ComponentName == "Assignments").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
                var timeLineAssignmentsCount = await db.Assignments.Where(a => timeLineAssignments.Contains(a.AssignmentKey) && a.DueDate >= DateTime.Now).CountAsync();

                var synchronousSessions = await timelineOfCureentUserCount.Where(t => t.TimelineComponent.ComponentName == "SynchronousSessions").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
                var synchronousSessionsCount = await db.SynchronousSessions.Where(a => synchronousSessions.Contains(a.SynchronousSessionKey) && a.SessionDate >= DateTime.Now).CountAsync();

                Dictionary<string, int> badgesDictionary = new Dictionary<string, int>();
                badgesDictionary.Add("timeLineLessonsCount", timeLineLessonsCount);
                badgesDictionary.Add("timeLineAssignmentsCount", timeLineAssignmentsCount);
                badgesDictionary.Add("synchronousSessionsCount", synchronousSessionsCount);

                return Json(badgesDictionary);
            }
            catch (Exception e)
            {
                return Json(500);
            }
            return Json(200);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetTimelineData(string newTimelineKey)
        {
            try
            {
                int timelineKey = int.Parse(newTimelineKey);
                var timelineOfCureentUser = db.Timelines.Where(t => t.TimelineKey == timelineKey);

                var timeLineLessonsIds = timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Lessons").Select(t => t.TimeLineEntityKey);
                var timeLineAssignmentsIds = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Assignments").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
                var synchronousSessionsIds = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "SynchronousSessions").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
                var pathsIds = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "LearningPaths").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
                var timeLineEventsIds = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Events").Select(t => t.TimeLineEntityKey).ToArrayAsync();
                var timeLineNotificationsIds = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Notifications").Select(t => t.TimeLineEntityKey).ToArrayAsync();
                DateTime Yesterday = DateTime.Now.AddDays(-1);
                var timeLineMessagesIds = timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Messages" && t.CreatedAt >= Yesterday).Select(t => t.TimeLineEntityKey);

                var timelineOfCureentUserCount = db.Timelines.Where(t => t.UserKey == CurrentUser.Id).OrderByDescending(t => t.CreatedAt);

                var timeLineLessonsCount = timelineOfCureentUserCount.Where(t => t.TimelineComponent.ComponentName == "Lessons").Count();
                var timeLineAssignments = await timelineOfCureentUserCount.Where(t => t.TimelineComponent.ComponentName == "Assignments").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
                var timeLineAssignmentsCount = await db.Assignments.Where(a => timeLineAssignments.Contains(a.AssignmentKey) && a.DueDate >= DateTime.Now).CountAsync();

                var synchronousSessions = await timelineOfCureentUserCount.Where(t => t.TimelineComponent.ComponentName == "SynchronousSessions").Select(t => t.TimeLineEntityKey).Distinct().ToArrayAsync();
                var synchronousSessionsCount = await db.SynchronousSessions.Where(a => synchronousSessions.Contains(a.SynchronousSessionKey) && a.SessionDate >= DateTime.Now).CountAsync();
                //var timeLineEventsCount = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Events").CountAsync();
                //var timeLineNotificationsCount = await timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Notifications").CountAsync();
                //var timeLineMessagesCount = timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Messages").Select(t => t.TimeLineEntityKey);

                ViewBag.timeLineLessonsCount = timeLineLessonsCount;
                ViewBag.timeLineAssignmentsCount = timeLineAssignmentsCount;
                ViewBag.synchronousSessionsCount = synchronousSessionsCount;

                List<MoodleEventViewModel> moodleEventViewModels = new List<MoodleEventViewModel>();
                foreach (var eventId in timeLineEventsIds)
                {
                    MoodleEventViewModel moodleEventViewModel = new MoodleEventViewModel();
                    string eventKey = eventId.ToString();
                    moodleEventViewModel = await RLI.Common.Managers.MoodleEventsManager.GetCalendarEventById(eventKey, HttpContext);

                    moodleEventViewModels.Add(moodleEventViewModel);
                }

                ViewBag.TimeLineEvents = moodleEventViewModels;
                TimelineViewModel timelineViewModel = new TimelineViewModel();
                timelineViewModel.Timelines = await timelineOfCureentUser.ToListAsync();

                string currentUserId = CurrentUser.Id;
                string remoteId = (await db.RemoteAuthentications.Where(r => r.AspNetUser.Id == currentUserId && r.ESystem.ESystemName == "Moodle").FirstOrDefaultAsync()).EsystemRemoteId;
                var assignments = await db.Assignments.Where(a => timeLineAssignmentsIds.Contains(a.AssignmentKey)).ToListAsync();
                List<AssignmentTimelineComponentViewModel> assignmentTimelineComponentViewModels = new List<AssignmentTimelineComponentViewModel>();

                foreach (var a in assignments)
                {
                    AssignmentTimelineComponentViewModel assignmentTimelineComponent = new AssignmentTimelineComponentViewModel();
                    assignmentTimelineComponent.AssignmentId = a.AssignmentId;
                    assignmentTimelineComponent.AssignmentKey = a.AssignmentKey;
                    assignmentTimelineComponent.AssignmentLessons = a.AssignmentLessons;
                    assignmentTimelineComponent.AssignmentTitle = a.AssignmentTitle;
                    assignmentTimelineComponent.AssignmentType = a.AssignmentType;
                    assignmentTimelineComponent.AssignmentTypeKey = a.AssignmentTypeKey;
                    assignmentTimelineComponent.CreatedAt = a.CreatedAt;
                    assignmentTimelineComponent.StudentAssignmentReports = a.StudentAssignmentReports;
                    assignmentTimelineComponent.CourseId = a.CourseId;
                    assignmentTimelineComponent.CourseLongName = a.CourseLongName;
                    assignmentTimelineComponent.CourseModuleID = a.CourseModuleID;
                    assignmentTimelineComponent.CourseShortName = a.CourseShortName;
                    assignmentTimelineComponent.CourseTitle = a.CourseTitle;
                    assignmentTimelineComponent.Description = a.Description;
                    assignmentTimelineComponent.DueDate = a.DueDate;
                    assignmentTimelineComponent.SchoolSubjectTeacherGrade = a.SchoolSubjectTeacherGrade;
                    assignmentTimelineComponent.SchoolSubjectTeacherGradeKey = a.SchoolSubjectTeacherGradeKey;
                    assignmentTimelineComponent.TimeModifed = a.TimeModifed;
                    assignmentTimelineComponent.SubmissionDetails = await SubmissionsManager.GetAssignSubmissionDetails(a.AssignmentId.ToString(), remoteId, HttpContext);

                    assignmentTimelineComponentViewModels.Add(assignmentTimelineComponent);
                }

                timelineViewModel.Assignments = assignmentTimelineComponentViewModels;
                timelineViewModel.Lessons = await db.Lessons.Where(l => timeLineLessonsIds.Contains(l.LessonKey)).ToListAsync();
                timelineViewModel.Notifications = await db.Alerts.Where(l => timeLineNotificationsIds.Contains(l.AlertKey)).ToListAsync();
                timelineViewModel.SynchronousSessions = await db.SynchronousSessions.Where(a => synchronousSessionsIds.Contains(a.SynchronousSessionKey)).OrderByDescending(s => s.SessionDate).ToListAsync();
                timelineViewModel.Paths = await db.Paths.Where(p => pathsIds.Contains(p.PathKey)).OrderByDescending(s => s.CreatedAt).ToListAsync();
                timelineViewModel.MoodleEvents = moodleEventViewModels;

                return PartialView("_TimelineComponent", timelineViewModel);
            }
            catch (Exception e)
            {
                return Json(500);
            }
            return Json(200);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CheckIfSubmissionAndUpdate(string postKey)
        {
            int submissionsCount = 0;
            int assKey = int.Parse(postKey);
            var assign = await db.Assignments.Where(a => a.AssignmentKey == assKey).FirstOrDefaultAsync();
            if (assign != null)
            {
                try
                {
                    if (assign != null)
                    {
                        GetAssignmentGradingSummaryResult getAssignmentGradingSummaryResult = await SubmissionsManager.GetAssignGradingSummary(assign.AssignmentId.ToString(), HttpContext);
                        submissionsCount = (int)(getAssignmentGradingSummaryResult.ParticipantCount - getAssignmentGradingSummaryResult.SubmissionsSubmittedCount);
                        return Json(submissionsCount);
                    }
                }
                catch (Exception e)
                {
                    return Json(0);
                }
            }

            return Json(0);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextTimelineItems(int skipItems)
        {
            try
            {
                string userKey = CurrentUser.Id;
                string[] assignmentsArrayStr = await db.TimeLineComponentModes.Where(t => t.UserKey == userKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Assignments").OrderByDescending(a => a.CreatedAt).Select(t => t.EntityKey).Distinct().ToArrayAsync();
                int?[] assignmentsArrayInt = assignmentsArrayStr.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => (int?)Convert.ToInt32(x)).ToArray();

                var timelineOfCureentUser = await db.Timelines.Where(t => t.UserKey == CurrentUser.Id).OrderByDescending(t => t.CreatedAt).Skip(skipItems * 10).Take(10).ToArrayAsync();
                int count = timelineOfCureentUser.Count();
                if (count > 0)
                {
                    var timeLineLessonsIds = timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Lessons").Select(t => t.TimeLineEntityKey).ToArray();
                    var timeLineAssignmentsIds = timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Assignments").Select(t => t.TimeLineEntityKey).Distinct().ToArray();
                    var synchronousSessionsIds = timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "SynchronousSessions").Select(t => t.TimeLineEntityKey).Distinct().ToArray();
                    var timeLineEventsIds = timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Events").Select(t => t.TimeLineEntityKey).ToArray();
                    var timeLineNotificationsIds = timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Notifications").Select(t => t.TimeLineEntityKey).ToArray();
                    DateTime Yesterday = DateTime.Now.AddDays(-1);
                    var timeLineMessagesIds = timelineOfCureentUser.Where(t => t.TimelineComponent.ComponentName == "Messages" && t.CreatedAt >= Yesterday).Select(t => t.TimeLineEntityKey).ToArray();

                    List<MoodleEventViewModel> moodleEventViewModels = new List<MoodleEventViewModel>();
                    foreach (var eventId in timeLineEventsIds)
                    {
                        MoodleEventViewModel moodleEventViewModel = new MoodleEventViewModel();
                        string eventKey = eventId.ToString();
                        moodleEventViewModel = await RLI.Common.Managers.MoodleEventsManager.GetCalendarEventById(eventKey, HttpContext);

                        moodleEventViewModels.Add(moodleEventViewModel);

                    }

                    TimelineViewModel timelineViewModel = new TimelineViewModel();
                    timelineViewModel.Timelines = timelineOfCureentUser.ToList();
                    string currentUserId = CurrentUser.Id;
                    string remoteId = (await db.RemoteAuthentications.Where(r => r.AspNetUser.Id == currentUserId && r.ESystem.ESystemName == "Moodle").FirstOrDefaultAsync()).EsystemRemoteId;
                    var assignments = await db.Assignments.Where(a => timeLineAssignmentsIds.Contains(a.AssignmentKey)).ToListAsync();
                    List<AssignmentTimelineComponentViewModel> assignmentTimelineComponentViewModels = new List<AssignmentTimelineComponentViewModel>();


                    var assignmentIds = assignments
    .Where(a => a.AssignmentId.HasValue)
    .Select(a => (long)a.AssignmentId.Value)
    .ToList();

                    var submissionCounts = MoodleManager.GetSubmissionCounts(assignmentIds);
                    var studentCounts = MoodleManager.GetStudentCounts(assignmentIds);

                    var submissionCountsDict = submissionCounts.ToDictionary(x => x.AssignmentId, x => x.SubmissionCount);
                    var studentCountsDict = studentCounts.ToDictionary(x => x.AssignmentId, x => x.StudentCount);

                    foreach (var a in assignments)
                    {
                        AssignmentTimelineComponentViewModel assignmentTimelineComponent = new AssignmentTimelineComponentViewModel();
                        assignmentTimelineComponent.AssignmentId = a.AssignmentId;
                        assignmentTimelineComponent.AssignmentKey = a.AssignmentKey;
                        assignmentTimelineComponent.AssignmentLessons = a.AssignmentLessons;
                        assignmentTimelineComponent.AssignmentTitle = a.AssignmentTitle;
                        assignmentTimelineComponent.AssignmentType = a.AssignmentType;
                        assignmentTimelineComponent.AssignmentTypeKey = a.AssignmentTypeKey;
                        assignmentTimelineComponent.CreatedAt = a.CreatedAt;
                        assignmentTimelineComponent.StudentAssignmentReports = a.StudentAssignmentReports;
                        assignmentTimelineComponent.CourseId = a.CourseId;
                        assignmentTimelineComponent.CourseLongName = a.CourseLongName;
                        assignmentTimelineComponent.CourseModuleID = a.CourseModuleID;
                        assignmentTimelineComponent.CourseShortName = a.CourseShortName;
                        assignmentTimelineComponent.CourseTitle = a.CourseTitle;
                        assignmentTimelineComponent.Description = a.Description;
                        assignmentTimelineComponent.DueDate = a.DueDate;
                        assignmentTimelineComponent.SchoolSubjectTeacherGrade = a.SchoolSubjectTeacherGrade;
                        assignmentTimelineComponent.SchoolSubjectTeacherGradeKey = a.SchoolSubjectTeacherGradeKey;
                        assignmentTimelineComponent.TimeModifed = a.TimeModifed;

                        //   assignmentTimelineComponent.SubmissionDetails = await SubmissionsManager.GetAssignSubmissionDetails(a.AssignmentId.ToString(), remoteId, HttpContext);
                        if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher")
                        {
                            if (a.AssignmentId.HasValue)
                            {
                                var assignmentId = (long)a.AssignmentId.Value;

                                var submissionCount = submissionCountsDict.ContainsKey(assignmentId)
                                    ? submissionCountsDict[assignmentId]
                                    : 0;

                                var studentCount = studentCountsDict.ContainsKey(assignmentId)
                                    ? studentCountsDict[assignmentId]
                                    : 0;

                                assignmentTimelineComponent.Submissions = submissionCount.ToString();
                                assignmentTimelineComponent.participantCount = studentCount.ToString();

                            }
                        }

                        assignmentTimelineComponentViewModels.Add(assignmentTimelineComponent);
                    }

                    timelineViewModel.Assignments = assignmentTimelineComponentViewModels;
                    timelineViewModel.Lessons = await db.Lessons.Where(l => timeLineLessonsIds.Contains(l.LessonKey)).ToListAsync();
                    timelineViewModel.Notifications = await db.Alerts.Where(l => timeLineNotificationsIds.Contains(l.AlertKey)).ToListAsync();
                    timelineViewModel.SynchronousSessions = await db.SynchronousSessions.Where(a => synchronousSessionsIds.Contains(a.SynchronousSessionKey)).OrderByDescending(s => s.SessionDate).ToListAsync();
                    timelineViewModel.MoodleEvents = moodleEventViewModels;
                    return PartialView("_TimelineComponent", timelineViewModel);
                }
                else
                {
                    return Json(404);
                }
            }
            catch (Exception e)
            {
                await LogManager.log("Dashboard, GetNextTimelineItems | Debug", e.ToString());
                return Json(500);
            }


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BookmarkAssignmentComponent(int componentKey)
        {

            string userKey = CurrentUser.Id;
            int result = 400;

            var categoryKey = (await db.TimelineComponents.Where(t => t.ComponentName == "Assignments").FirstOrDefaultAsync()).TimelineComponentKey;
            List<RLI.EntityFramework.EDM.TimeLineComponentMode> timeLineComponentMode = new List<RLI.EntityFramework.EDM.TimeLineComponentMode>();
            string assignmentKey = componentKey.ToString();
            timeLineComponentMode = await db.TimeLineComponentModes.Where(t => t.EntityKey == assignmentKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Assignments" && t.UserKey == userKey).ToListAsync();

            if (timeLineComponentMode.Count == 0)
            {
                result = await RLI.Common.Managers.AssignmentsManager.BookmarkAssignment(componentKey, categoryKey, userKey);
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
        public async Task<ActionResult> BookmarkPathComponent(int componentPathKey)
        {

            string userKey = CurrentUser.Id;
            int result = 400;

            var categoryKey = (await db.TimelineComponents.Where(t => t.ComponentName == "LearningPaths").FirstOrDefaultAsync()).TimelineComponentKey;
            List<RLI.EntityFramework.EDM.TimeLineComponentMode> timeLineComponentMode = new List<RLI.EntityFramework.EDM.TimeLineComponentMode>();
            string assignmentKey = componentPathKey.ToString();
            timeLineComponentMode = await db.TimeLineComponentModes.Where(t => t.EntityKey == assignmentKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "LearningPaths" && t.UserKey == userKey).ToListAsync();

            if (timeLineComponentMode.Count == 0)
            {
                result = await RLI.Common.Managers.PathManager.BookmarkPath(componentPathKey, categoryKey, userKey);
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
        public async Task<ActionResult> RefreshWhatsToday()
        {
            List<RLI.EntityFramework.EDM.Assignment> TodayAssignments = new List<RLI.EntityFramework.EDM.Assignment>();
            List<RLI.EntityFramework.EDM.SynchronousSession> TodaySessions = new List<RLI.EntityFramework.EDM.SynchronousSession>();
            List<RLI.EntityFramework.EDM.Lesson> TodayLessons = new List<RLI.EntityFramework.EDM.Lesson>();
            List<RLI.Common.DataObjects.MoodleEventViewModel> TodayEvents = new List<RLI.Common.DataObjects.MoodleEventViewModel>();


            DateTime lastdayDateTime = DateTime.Now;
            TimeSpan ts = new TimeSpan(12, 00, 0);
            lastdayDateTime = lastdayDateTime.Date + ts;

            DateTime tomorrowDateTime = DateTime.Now.AddDays(1);
            tomorrowDateTime = tomorrowDateTime.Date + ts;
            DateTime today = DateTime.Now.Date;
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
            {
                var student = await db.Students.Where(s => s.StudentKey == CurrentUser.Studentkey).FirstOrDefaultAsync();
                TodayAssignments = await db.Assignments.Where(a => a.DueDate.Value == today && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey).ToListAsync();
                TodaySessions = await db.SynchronousSessions.Where(a => a.SessionDate == today && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey).ToListAsync();
                var todayLessonsId = await db.Timelines.Where(t => t.UserKey == CurrentUser.Id && t.EntitySource.Value == "Internal" && t.TimelineComponent.ComponentName == "Lessons" && t.CreatedAt.Value >= lastdayDateTime && t.CreatedAt.Value <= tomorrowDateTime).Select(t => t.TimeLineEntityKey).ToArrayAsync();
                TodayLessons = await db.Lessons.Where(l => todayLessonsId.Contains(l.LessonKey)).ToListAsync();
            }

            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher")
            {
                var teacher = await db.SchoolSubjectTeacherGrades.Where(s => s.TeacherKey == CurrentUser.TeacherKey).ToListAsync();
                var grades = teacher.Select(t => t.GradeKey).ToArray();
                var subjects = teacher.Select(t => t.SubjectKey).ToArray();
                var schools = teacher.Select(t => t.SchoolKey).ToArray();
                TodayAssignments = await db.Assignments.Where(a => a.DueDate.Value == today && grades.Contains(a.SchoolSubjectTeacherGrade.GradeKey) && subjects.Contains(a.SchoolSubjectTeacherGrade.SubjectKey) && schools.Contains(a.SchoolSubjectTeacherGrade.SchoolKey)).ToListAsync();
                TodaySessions = await db.SynchronousSessions.Where(a => a.SessionDate >= lastdayDateTime && a.SessionDate <= tomorrowDateTime && grades.Contains(a.SchoolSubjectTeacherGrade.GradeKey) && subjects.Contains(a.SchoolSubjectTeacherGrade.SubjectKey) && schools.Contains(a.SchoolSubjectTeacherGrade.SchoolKey)).ToListAsync();
            }

            return PartialView("_WhatsTodaySideMenu", new ViewDataDictionary { { "todayAssignments", TodayAssignments }, { "todaySessions", TodaySessions }, { "todayEvents", TodayEvents }, { "todayLessons", TodayLessons } });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BookmarkLessonComponent(int lessKey)
        {

            string userKey = CurrentUser.Id;
            int result = 400;

            var categoryKey = (await db.TimelineComponents.Where(t => t.ComponentName == "Lessons").FirstOrDefaultAsync()).TimelineComponentKey;
            List<RLI.EntityFramework.EDM.TimeLineComponentMode> timeLineComponentMode = new List<RLI.EntityFramework.EDM.TimeLineComponentMode>();
            string lessonKey = lessKey.ToString();
            timeLineComponentMode = await db.TimeLineComponentModes.Where(t => t.EntityKey == lessonKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Lessons" && t.UserKey == userKey).ToListAsync();

            if (timeLineComponentMode.Count == 0)
            {
                result = await RLI.Common.Managers.LessonsManager.BookmarkLesson(lessKey, categoryKey, userKey);
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
        public async Task<ActionResult> SnoozeLessonComponent(int lessId, string snoozeLessonDate)
        {

            string userKey = CurrentUser.Id;
            int result = 400;

            var categoryKey = (await db.TimelineComponents.Where(t => t.ComponentName == "Lessons").FirstOrDefaultAsync()).TimelineComponentKey;


            result = await RLI.Common.Managers.LessonsManager.SnoozeLesson(lessId, categoryKey, userKey, snoozeLessonDate);

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SnoozeEventComponent(int evId, string snoozeEventDate)
        {

            string userKey = CurrentUser.Id;
            int result = 400;

            var categoryKey = (await db.TimelineComponents.Where(t => t.ComponentName == "Events").FirstOrDefaultAsync()).TimelineComponentKey;


            result = await RLI.Common.Managers.MoodleEventsManager.SnoozeEvent(evId, categoryKey, userKey, snoozeEventDate);

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SnoozeAssignmentComponent(int asId, string snoozeAssignmentDate)
        {

            string userKey = CurrentUser.Id;
            int result = 400;

            var categoryKey = (await db.TimelineComponents.Where(t => t.ComponentName == "Assignments").FirstOrDefaultAsync()).TimelineComponentKey;

            result = await RLI.Common.Managers.AssignmentsManager.SnoozeAssignment(asId, categoryKey, userKey, snoozeAssignmentDate);

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BookmarkSynchronousSessionsComponent(int synchronousSessionKey)
        {

            string userKey = CurrentUser.Id;
            int result = 400;

            var categoryKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;
            List<RLI.EntityFramework.EDM.TimeLineComponentMode> timeLineComponentMode = new List<RLI.EntityFramework.EDM.TimeLineComponentMode>();
            string synchronousKey = synchronousSessionKey.ToString();
            timeLineComponentMode = await db.TimeLineComponentModes.Where(t => t.EntityKey == synchronousKey && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "SynchronousSessions" && t.UserKey == userKey).ToListAsync();

            if (timeLineComponentMode.Count == 0)
            {
                result = await RLI.Common.Managers.SynchronousSessionsManager.BookmarkSession(synchronousSessionKey, categoryKey, userKey);
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
        public async Task<ActionResult> SnoozeSynchronousSessionsComponent(int syncSessionKey, string syncSessionSnoozeDate)
        {

            string userKey = CurrentUser.Id;
            int result = 400;

            var categoryKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;

            result = await RLI.Common.Managers.SynchronousSessionsManager.SnoozeSession(syncSessionKey, categoryKey, userKey, syncSessionSnoozeDate);
            return Json(result);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BookmarkEventsComponent(int eventKey)
        {

            string userKey = CurrentUser.Id;
            int result = 400;

            var categoryKey = (await db.TimelineComponents.Where(t => t.ComponentName == "Events").FirstOrDefaultAsync()).TimelineComponentKey;
            List<RLI.EntityFramework.EDM.TimeLineComponentMode> timeLineComponentMode = new List<RLI.EntityFramework.EDM.TimeLineComponentMode>();
            string eventid = eventKey.ToString();
            timeLineComponentMode = await db.TimeLineComponentModes.Where(t => t.EntityKey == eventid && t.Mode.Mode1 == "Bookmarked" && t.TimelineComponent.ComponentName == "Events" && t.UserKey == userKey).ToListAsync();

            if (timeLineComponentMode.Count == 0)
            {
                result = await RLI.Common.Managers.MoodleEventsManager.BookmarkEvent(eventKey, categoryKey, userKey);
                if (result == 200)
                {
                    DefaultHubManager hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
                    RLI.WebApplication.Managers.MoodleSignalRManager MoodleSignalRManager = hubManager.ResolveHub("MoodleSignalRManager") as RLI.WebApplication.Managers.MoodleSignalRManager;
                    MoodleSignalRManager.SendBookmarkSignal(userKey);
                }
            }

            return Json(result);
        }

        public ActionResult LaunchEGooru()
        {
            string baseUrl = "https://app.mygooru.ai/api/nucleus-auth/v1/s2s/authorize";
            string clientId = "37934dd8-5aed-46ac-89f7-5e6c503f5f17";
            string responseType = "launch_app";
            string state = "eyJhbGciOiJIUzUxMiJ9.eyJyb2xlIjoidGVhY2hlciIsImlkIjoiNzgxMTkiLCJ1c2VybmFtZSI6ImVsaXNhX2tob3VyeTFAdGMucGx1cy5lZHVjYXRpb24iLCJleHAiOjE3NjE3MjY2NzYsImlhdCI6MTc2MTY0MDIwOH0.BQq9ID8bxIc8b8VBQi2p4HHHtxnDWopNMdPsoyBQANlWlciX1xAO-OogONwV1lBoBTg7blwNkCmiQC4458Y3lQ";

            string fullUrl = string.Format("{0}?client_id={1}&response_type={2}&state={3}",
                                           baseUrl, clientId, responseType, state);

            return Content(fullUrl);
        }
    }
}