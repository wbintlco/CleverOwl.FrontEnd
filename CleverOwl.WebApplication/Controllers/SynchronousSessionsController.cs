using CleverOwl.WebApplication.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using RLI.Common.DataObjects;
using RLI.Common.DataObjects.SynchronousSessions;
using RLI.Common.Globals;
using RLI.Common.Managers;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Models;
using RLI.WebApplication.Objects;
using RLI.WebApplication.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static RLI.WebApplication.Controllers.LibraryController;

namespace CleverOwl.WebApplication.Controllers
{
    [Authorize]
    [Authorization(Roles = "Administrator,Teacher,Student,ContentManager")]
    public class SynchronousSessionsController : BaseController
    {
        const int DEFAULT_LOAD_COUNT = 10;

        // GET: SynchronousSessions
        public async Task<ActionResult> Index()
        {

            var synchronousSessions = db.SynchronousSessions.Where(ss => ss.SessionDate >= DateTime.Now && ss.CreatedByUserKey == CurrentUser.Id).OrderBy(ss => ss.SessionDate).Take(DEFAULT_LOAD_COUNT / 2);
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
            {
                var synchronousSessionsIds = await db.Timelines.Where(t => t.TimelineComponent.ComponentName == "SynchronousSessions" && t.UserKey == CurrentUser.Id && t.EntitySource.Value == "Internal").Select(t => t.TimeLineEntityKey).ToArrayAsync();
                var studentsSynchronousSessions = db.SynchronousSessions.Where(ss => synchronousSessionsIds.Contains(ss.SynchronousSessionKey) && ss.SessionDate >= DateTime.Now).OrderByDescending(ss => ss.SessionDate).Take(DEFAULT_LOAD_COUNT / 2);
                synchronousSessions = synchronousSessions.Union(studentsSynchronousSessions);
            }
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
            {
                var synchronousSessionsIds = await db.Timelines.Where(t => t.TimelineComponent.ComponentName == "SynchronousSessions" && t.UserKey == CurrentUser.Id && t.EntitySource.Value == "Internal").Select(t => t.TimeLineEntityKey).ToArrayAsync();
                synchronousSessions = db.SynchronousSessions.Where(ss => synchronousSessionsIds.Contains(ss.SynchronousSessionKey) && ss.SessionDate >= DateTime.Now).OrderByDescending(ss => ss.SessionDate).Take(DEFAULT_LOAD_COUNT);
            }


            List<RLI.EntityFramework.EDM.Grade> grades = await synchronousSessions.Select(a => a.SchoolSubjectTeacherGrade.Grade).Distinct().ToListAsync();
            List<RLI.EntityFramework.EDM.Subject> subjects = await synchronousSessions.Select(a => a.SchoolSubjectTeacherGrade.Subject).Distinct().ToListAsync();
            var sessionDates = await synchronousSessions.Select(a => a.SessionDate).Distinct().ToListAsync();

            ViewBag.Grades = new SelectList(grades.OrderBy(g => g.GradeIndex).Where(g => g.Grade1 != "Training").Select(g => new RLI.WebApplication.Objects.Grade
            {
                GradeKey = g.GradeKey,
                DefaultGrade1 = g.Grade1,
                Grade1 = CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                GradeIndex = g.GradeIndex,
                LocalGradeGUID = g.LocalGradeGUID

            }), "GradeKey", "Grade1");
            ViewBag.Subjects = new SelectList(subjects.OrderBy(s => s.SubjectIndex).Where(s => s.Display == true && s.Subject1 != "Training").Select(g => new RLI.WebApplication.Objects.Subjects
            {
                SubjectKey = g.SubjectKey,
                DefaultSubject1 = g.Subject1,
                Subject1 = CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                SubjectIndex = g.SubjectIndex,
                LocalSubjectGUID = g.LocalSubjectGUID,
                Chapters = g.Chapters

            }), "SubjectKey", "Subject1");

            ViewBag.DueDates = new SelectList(sessionDates.Select(d => new
            {
                date1 = UtilitiesManager.GetFieldLabel(Locale, d.DayOfWeek.ToString()) + ", " + UtilitiesManager.GetFieldLabel(Locale, d.ToString("MMMM")) + " " + d.Day + ", " + d.Year,
                dateKey = d.ToLongDateString()
            }), "dateKey", "date1");

            ViewBag.CurrentLanguageIdentifier = CurrentLanguageIdentifier;
            return View(await synchronousSessions.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Search(string query)
        {
            if (query.Length < 3)
                return new HttpStatusCodeResult(HttpStatusCode.NoContent);

            List<object> result = new List<object>();

            string searchQuery = query.ToLower();

            try
            {
                var synchronousSessions = db.SynchronousSessions.Where(ss => ss.CreatedByUserKey == CurrentUser.Id).Where(ss =>
                   ss.Topic.ToLower().Contains(searchQuery)
                || ss.Descriprion.ToLower().Contains(searchQuery)).Select(ss => ss.Topic).Distinct().Select(ss => new
                {
                    id = ss,
                    title = ss,
                    text = "<h5 class='m-auto d-block'>" + ss + "</h5>"
                });

                result.AddRange(await synchronousSessions.Take(10).ToListAsync());

            }
            catch (Exception e)
            {
                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.ToString());

                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Filter(string query = null, int? records = null, int? gradeKey = null, int? subjectKey = null, DateTime? sessionDate = null)
        {
            int numberOfRecords = (int)(records == null ? DEFAULT_LOAD_COUNT : records);

            var synchronousSessions = db.SynchronousSessions.Where(ss => ss.SessionDate >= DateTime.Now && ss.CreatedByUserKey == CurrentUser.Id).OrderBy(a => a.SessionDate);

            List<SynchronousSession> synchronousSessionsFiltered = await synchronousSessions.Where(a =>
          ((gradeKey != null && (a.SchoolSubjectTeacherGrade.Grade.GradeKey == gradeKey)) || (gradeKey == null))
          && ((subjectKey != null && (a.SchoolSubjectTeacherGrade.Subject.SubjectKey == subjectKey)) || (subjectKey == null))
          && ((sessionDate != null && (EntityFunctions.TruncateTime(a.SessionDate) == EntityFunctions.TruncateTime(sessionDate))) || (sessionDate == null))
          && ((query != null && (a.Topic.Contains(query) || a.Descriprion.Contains(query))) || (query == null))).Take(numberOfRecords).ToListAsync();

            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("SynchronousSessions", "Index", CurrentLanguageIdentifier);
            return PartialView("_SynchronousSessionsGrid", synchronousSessionsFiltered);
        }

        // GET: SynchronousSessions/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SynchronousSession synchronousSession = await db.SynchronousSessions.FindAsync(id);
            if (synchronousSession == null)
            {
                return HttpNotFound();
            }
            return View(synchronousSession);
        }

        // GET: SynchronousSessions/Create
        [Authorization(Roles = "Teacher")]
        public async Task<ActionResult> Create()
        {
            string userKey = CurrentUser.Id;

            var user = await db.AspNetUsers.FindAsync(userKey);
            School school = null;
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
            {
                school = user.Teacher.SchoolSubjectTeacherGrades.FirstOrDefault().School;
            }
            else if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
            {
                school = user.Student.School;
                ViewBag.CurrentStudentGrade = user.Student.Grade;
            }

            ViewBag.CurrentTeacherSchool = school;
            if (!await SynchronousSessionsManager.VerifyAccessToThirdParty(CurrentUser))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: SynchronousSessions/SessionSetup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SessionSetup(SynchronousSessionsSetupViewModel model)
        {
            if (model == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    List<SynchronousSession> session = await SynchronousSessionsManager.CheckSessionSchedulingValidity(model.SessionDate.Value, model.MeetingDuration.Value, CurrentUser);
                    if (session.Count > 0)
                    {
                        TempData["sessionTopic"] = session.ElementAt(0).Topic;
                        TempData["date"] = model.SessionDate.Value;
                        TempData["duration"] = model.MeetingDuration.Value;
                        return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
                    }

                    SynchronousSession synchronousSession = new SynchronousSession();

                    synchronousSession.Topic = model.Topic;
                    synchronousSession.Descriprion = model.Descriprion;
                    synchronousSession.SessionDate = model.SessionDate.Value;
                    synchronousSession.MeetingDuration = model.MeetingDuration.Value;

                    synchronousSession.CreatedByUserKey = CurrentUser.Id;
                    synchronousSession.CreationDate = DateTime.Now;
                    synchronousSession.EsystemKey = GSuiteManager.GSuiteKey;
                    synchronousSession.TimeZone = Configuration.TIME_ZONE;

                    TempData["SynchronousSession"] = synchronousSession;

                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                catch (Exception e)
                {
                    await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);

                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetConflicTopicName()
        {

            List<SynchronousSession> session = await SynchronousSessionsManager.CheckSessionSchedulingValidity((DateTime)TempData["date"], (double)TempData["duration"], CurrentUser);
            if (session.Count() > 0)
            {
                return Json(session.ElementAt(0).Topic);
            }
            else
            {
                return Json("404");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> LookupTopics(string query, string grade, int? subject, int type = 1)
        {

            int?[] grade_idArray = new int?[] { };
            if (grade != null)
            {
                grade_idArray = JsonConvert.DeserializeObject<List<int?>>(grade).ToArray();
            }

            ChapterType chapterType = type == 1 ? ChapterType.ChapterTypeDigitalLibrary : ChapterType.ChapterTypeRecordedSession;

            var chapters = db.Chapters.Where(c => c.ChapterTypeKey == (int)chapterType &&
            ((!string.IsNullOrEmpty(query) && ((CurrentLanguageIdentifier == 0 ? c.ChapterName : c.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value).Contains(query))) || (string.IsNullOrEmpty(query))) &&
            ((grade != null && (grade_idArray.Contains(c.GradeKey))) || (grade == null)) &&
            ((subject != null && (c.SubjectKey == subject)) || (subject == null)) &&
            c.ChaptersTopics.Select(ct => ct.Topic).SelectMany(t => t.Lessons).Where(l => l.StatusKey == 5).Count() != 0
            ).Distinct().OrderBy(c => c.ChapterIndex).Select(c => new
            {
                grade = c.GradeKey,
                subject = c.SubjectKey,
                gradeName = CurrentLanguageIdentifier == 0 ? c.Grade.Grade1 : c.Grade.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                subjectName = CurrentLanguageIdentifier == 0 ? c.Subject.Subject1 : c.Subject.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                id = c.ChapterKey.ToString(),
                title = CurrentLanguageIdentifier == 0 ? c.ChapterName : c.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                text = c.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault() == null ? c.ChapterName : c.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                gradeIndex = c.Grade.GradeIndex,
                chapterIndex = c.ChapterIndex
            }).Distinct();

            var groupedChapters = await chapters.GroupBy(c =>
                new
                {
                    c.grade,
                    c.subject,
                    c.gradeName,
                    c.subjectName,
                    c.gradeIndex

                },
                (key, value) => new
                {
                    id = key.grade + "-" + key.subject,
                    index = key.gradeIndex,
                    text = "<h4 style='margin-bottom:0px !important'>" + key.gradeName + (key.subject == null ? "" : ", " + key.subjectName) + "</h4>",
                    children = value.OrderBy(c => c.chapterIndex).ToList()
                }).OrderBy(k => k.index).ToListAsync();

            return Json(groupedChapters);
        }

        // POST: SynchronousSessions/SessionAudience
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SessionAudience(SynchronousSessionsAudienceViewModel model)
        {
            if (model == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    SchoolSubjectTeacherGrade schoolSubjectTeacherGrade = new SchoolSubjectTeacherGrade();
                    if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                    {
                        schoolSubjectTeacherGrade = await db.SchoolSubjectTeacherGrades.FirstOrDefaultAsync(sstg => sstg.SchoolKey == model.SchoolKey && sstg.SubjectKey == model.SubjectKey && sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey == model.GradeKey);
                    }
                    else if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
                    {
                        var studentSstg = await db.StudentEnrolments.Where(se => se.StudentKey == CurrentUser.Studentkey).Select(se => se.SchoolSubjectTeacherGrade).ToListAsync();
                        schoolSubjectTeacherGrade = studentSstg.FirstOrDefault(sstg => sstg.SchoolKey == model.SchoolKey && sstg.SubjectKey == model.SubjectKey && sstg.GradeKey == model.GradeKey);
                    }

                    if (schoolSubjectTeacherGrade == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    TempData["synchronousSessionsAudienceViewModel"] = model;
                    TempData["schoolSubjectTeacherGrade"] = schoolSubjectTeacherGrade;

                    List<Student> allStudentsInClass = await db.Students.Where(s => s.SchoolKey == schoolSubjectTeacherGrade.SchoolKey && s.GradeKey == schoolSubjectTeacherGrade.GradeKey && s.AspNetUsers.Count != 0).ToListAsync();

                    if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
                    {
                        allStudentsInClass = allStudentsInClass.Where(s => s.StudentKey != CurrentUser.Studentkey).ToList();
                        ViewBag.ClassTeacher = schoolSubjectTeacherGrade.Teacher;
                    }

                    List<StudentsEnrolledViewModel> studentsEnrolledList = new List<StudentsEnrolledViewModel>();
                    foreach (var student in allStudentsInClass)
                    {
                        StudentsEnrolledViewModel studentEnrolled = new StudentsEnrolledViewModel();
                        studentEnrolled.StudentKey = student.StudentKey.ToString();
                        studentEnrolled.StudentName = $"{student.FirstName} {student.LastName}";

                        studentsEnrolledList.Add(studentEnrolled);
                    }

                    SynchronousSessionSelectAudienceViewModel synchronousSessionSelectAudienceViewModel = new RLI.WebApplication.Models.SynchronousSessionSelectAudienceViewModel();
                    synchronousSessionSelectAudienceViewModel.StudentsEnrolled = studentsEnrolledList;
                    synchronousSessionSelectAudienceViewModel.SelectedStudentsEnrolled = studentsEnrolledList;

                    ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("SynchronousSessions", "Create", CurrentLanguageIdentifier);

                    return PartialView("_SynchronousSessionSelectAudience", synchronousSessionSelectAudienceViewModel);
                }
                catch (Exception e)
                {
                    await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);

                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
        }

        // POST: SynchronousSessions/SessionAudience
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SessionSelectAudience(SelectedAudienceFormViewModel model)
        {

            if (ModelState.IsValid)
            {
                try
                {

                    SynchronousSession synchronousSession = (SynchronousSession)TempData["SynchronousSession"];
                    if (synchronousSession == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    SchoolSubjectTeacherGrade schoolSubjectTeacherGrade = (SchoolSubjectTeacherGrade)TempData["schoolSubjectTeacherGrade"];
                    if (schoolSubjectTeacherGrade == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    SynchronousSessionsAudienceViewModel synchronousSessionsAudienceViewModel = (SynchronousSessionsAudienceViewModel)TempData["synchronousSessionsAudienceViewModel"];
                    if (synchronousSessionsAudienceViewModel == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    Session session = SynchronousSessionsManager.CreateEvent(synchronousSession.Topic, synchronousSession.Descriprion, synchronousSession.SessionDate, synchronousSession.MeetingDuration, CurrentUser, synchronousSessionsAudienceViewModel.SchoolKey.Value);
                    if (session == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    synchronousSession.MeetingURL = session.Link;
                    synchronousSession.SchoolSubjectTeacherGradeKey = schoolSubjectTeacherGrade.SchoolSubjectTeacherGradeKey;
                    synchronousSession.GoogleRemoteID = session.Id;
                    synchronousSession.ChapterKey = synchronousSessionsAudienceViewModel.ChapterKey;

                    db.SynchronousSessions.Add(synchronousSession);

                    await db.SaveChangesAsync();

                    Timeline timelineObject = new Timeline();
                    timelineObject.CreatedByUserKey = CurrentUser.Id;
                    timelineObject.UserKey = CurrentUser.Id;
                    timelineObject.Description = synchronousSession.Descriprion;
                    timelineObject.CreatedAt = synchronousSession.CreationDate;
                    timelineObject.TimeLineEntityKey = synchronousSession.SynchronousSessionKey;
                    timelineObject.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;
                    timelineObject.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "Internal").FirstOrDefaultAsync()).EntitySourceKey;

                    await TimelineManager.InsertTimelineData(timelineObject);
                    await db.SaveChangesAsync();

                    CreateCalendarEventModel createCalendarEventModel = new CreateCalendarEventModel();
                    createCalendarEventModel.courseid = 0;
                    createCalendarEventModel.name = synchronousSession.Topic;
                    createCalendarEventModel.description = synchronousSession.Descriprion + " You can join using this link: " + session.Link;
                    //createCalendarEventModel.eventtype = "SyncSession";
                    createCalendarEventModel.timestart = UtilitiesManager.ConvertToUnixTimestamp(synchronousSession.SessionDate);
                    createCalendarEventModel.timeduration = (int)(synchronousSession.MeetingDuration * 3600);//in seconds

                    var calendar = await CalendarManager.CreateEvent(createCalendarEventModel, HttpContext);

                    var calendarEvents = calendar.events;
                    if (calendarEvents.Count != 0)
                    {
                        synchronousSession.MoodleEventId = calendarEvents.FirstOrDefault().id;
                        await db.SaveChangesAsync();
                    }

                    //var allStudentsInClass = await db.Students.Where(s => s.SchoolKey == schoolSubjectTeacherGrade.SchoolKey && s.GradeKey == schoolSubjectTeacherGrade.GradeKey && s.AspNetUsers.Count != 0).ToListAsync();

                    foreach (var student in model.studentsSelected) //allStudentsInClass
                    {
                        int studentId = Convert.ToInt32(student);
                        var studentUser = await db.AspNetUsers.Where(su => su.Studentkey == studentId).FirstOrDefaultAsync();
                        if (studentUser == null)
                        {
                            await LogManager.log(MethodBase.GetCurrentMethod().Name, "Couldn't find student with id: " + student);
                            continue;
                        }
                        var moodleStudent = await db.RemoteAuthentications.Where(ra => ra.ESystem.ESystemName == "Moodle" && ra.Userkey == studentUser.Id).FirstOrDefaultAsync();
                        if (moodleStudent == null)
                        {
                            await LogManager.log(MethodBase.GetCurrentMethod().Name, "Couldn't find student with moodle user. (studentId) " + student);
                            continue;
                        }

                        try
                        {
                            string moodleStudentId = moodleStudent.EsystemRemoteId;
                            string userKey = moodleStudent.Userkey;
                            string token = await MoodleManager.requestTokenByMoodleUserId(moodleStudentId, HttpContext, userKey);

                            Timeline newTimeline = new Timeline();

                            newTimeline.TimeLineEntityKey = synchronousSession.SynchronousSessionKey;
                            newTimeline.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;
                            newTimeline.CreatedAt = DateTime.Now;
                            string studentUserKey = studentUser.Id;
                            newTimeline.UserKey = studentUserKey;
                            newTimeline.CreatedByUserKey = CurrentUser.Id;
                            newTimeline.Description = synchronousSession.Descriprion;
                            newTimeline.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "Internal").FirstOrDefaultAsync()).EntitySourceKey;

                            bool insertStudentTimline = await TimelineManager.InsertTimelineData(newTimeline);

                            if (insertStudentTimline)
                            {
                                CreateCalendarEventModel createCalendarEventModelStudent = new CreateCalendarEventModel();
                                createCalendarEventModelStudent.courseid = 0;
                                createCalendarEventModelStudent.name = synchronousSession.Topic;
                                createCalendarEventModelStudent.description = synchronousSession.Descriprion + " You can join using this link: " + session.Link;
                                //createCalendarEventModel.eventtype = "SyncSession";
                                createCalendarEventModelStudent.timestart = UtilitiesManager.ConvertToUnixTimestamp(synchronousSession.SessionDate);
                                createCalendarEventModelStudent.timeduration = (int)(synchronousSession.MeetingDuration * 3600);//in seconds

                                var calendarStudent = await CalendarManager.CreateEvent(createCalendarEventModelStudent, HttpContext, true, token);
                            }
                        }
                        catch (Exception e)
                        {
                            await LogManager.log(MethodBase.GetCurrentMethod().Name, e.ToString());
                            continue;
                        }

                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(model.selectTeacher))
                        {


                            int teacherId = Convert.ToInt32(model.selectTeacher);
                            var teacherUser = await db.AspNetUsers.Where(su => su.TeacherKey == teacherId).FirstOrDefaultAsync();
                            if (teacherUser == null)
                            {
                                await LogManager.log(MethodBase.GetCurrentMethod().Name, "Couldn't find teacher with id: " + teacherId);

                            }
                            var moodleTeacher = await db.RemoteAuthentications.Where(ra => ra.ESystem.ESystemName == "Moodle" && ra.Userkey == teacherUser.Id).FirstOrDefaultAsync();
                            if (moodleTeacher == null)
                            {
                                await LogManager.log(MethodBase.GetCurrentMethod().Name, "Couldn't find teacher with moodle user. (teacherId) " + teacherId);

                            }
                            string moodleTeacherId = moodleTeacher.EsystemRemoteId;
                            string token = await MoodleManager.requestTokenByMoodleUserId(moodleTeacherId, HttpContext);

                            Timeline newTimeline = new Timeline();

                            newTimeline.TimeLineEntityKey = synchronousSession.SynchronousSessionKey;
                            newTimeline.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;
                            newTimeline.CreatedAt = DateTime.Now;
                            string teacherUserKey = teacherUser.Id;
                            newTimeline.UserKey = teacherUserKey;
                            newTimeline.CreatedByUserKey = CurrentUser.Id;
                            newTimeline.Description = synchronousSession.Descriprion;
                            newTimeline.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "Internal").FirstOrDefaultAsync()).EntitySourceKey;

                            bool insertStudentTimline = await TimelineManager.InsertTimelineData(newTimeline);

                            if (insertStudentTimline)
                            {
                                CreateCalendarEventModel createCalendarEventModelTeacher = new CreateCalendarEventModel();
                                createCalendarEventModelTeacher.courseid = 0;
                                createCalendarEventModelTeacher.name = synchronousSession.Topic;
                                createCalendarEventModelTeacher.description = synchronousSession.Descriprion + " You can join using this link: " + session.Link;
                                //createCalendarEventModel.eventtype = "SyncSession";
                                createCalendarEventModelTeacher.timestart = UtilitiesManager.ConvertToUnixTimestamp(synchronousSession.SessionDate);
                                createCalendarEventModelTeacher.timeduration = (int)(synchronousSession.MeetingDuration * 3600);//in seconds

                                var calendarStudent = await CalendarManager.CreateEvent(createCalendarEventModelTeacher, HttpContext, true, token);
                            }


                        }
                        else
                        {
                            string sessionKey = Convert.ToString(synchronousSession.SynchronousSessionKey);
                            var teacherUser = await db.AspNetUsers.FirstOrDefaultAsync(t => t.TeacherKey == schoolSubjectTeacherGrade.TeacherKey);
                            if (teacherUser != null)
                            {
                                RLI.WebApplication.Controllers.MoodleSignalRController moodleSignalRController = new RLI.WebApplication.Controllers.MoodleSignalRController();
                                var notifyTeacher = await moodleSignalRController.NotifyTeacherAboutStudentsSynchronousSession(CurrentUser.Id, teacherUser.Id, sessionKey);
                            }
                            else
                            {
                                await LogManager.log(MethodBase.GetCurrentMethod().Name, $"Couldn't find the teacher with teacherKey {schoolSubjectTeacherGrade.TeacherKey}");
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        await LogManager.log(MethodBase.GetCurrentMethod().Name, e.ToString());
                    }

                    return Json(new { MeetingUrl = session.Link });
                }
                catch (Exception e)
                {
                    await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);

                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetSchools(string query = null)
        {
            List<School> schoolsSstg = new List<School>();
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
            {

#warning retrieving schools, grades, subjects for user should be moved to a manager
                schoolsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.SchoolKey != null).Select(sstg => sstg.School).Distinct().ToListAsync();
            }
            else if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
            {
                var studentSstg = await db.StudentEnrolments.Where(se => se.StudentKey == CurrentUser.Studentkey).Select(se => se.SchoolSubjectTeacherGrade).ToListAsync();
                schoolsSstg = studentSstg.Select(sstg => sstg.School).Distinct().ToList();
            }

            var schoolsSelectList = schoolsSstg.Select(sstg => new
            {
                id = sstg.SchoolKey,
                text = sstg.SchoolName
            }).ToList();

            return Json(schoolsSelectList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetGrades(string query = null, int schoolKey = 0)
        {
            List<RLI.EntityFramework.EDM.Grade> gradesSstg = new List<RLI.EntityFramework.EDM.Grade>();
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
            {
                gradesSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.SchoolKey == schoolKey && sstg.GradeKey != null).Select(sstg => sstg.Grade).Distinct().ToListAsync();
            }
            else if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
            {
                var studentSstg = await db.StudentEnrolments.Where(se => se.StudentKey == CurrentUser.Studentkey).Select(se => se.SchoolSubjectTeacherGrade).ToListAsync();
                gradesSstg = studentSstg.Select(sstg => sstg.Grade).Distinct().ToList();
            }

            var grades = gradesSstg.OrderBy(g => g.GradeIndex).Select(g => new
            {
                GradeKey = g.GradeKey,
                DefaultGrade1 = g.Grade1,
                Grade1 = CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                GradeIndex = g.GradeIndex,
                LocalGradeGUID = g.LocalGradeGUID
            });

            var gradesSelectList = grades.Select(g => new
            {
                id = g.GradeKey,
                text = g.Grade1
            }).ToList();

            return Json(gradesSelectList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetSubjects(string query = null, int schoolKey = 0, int gradeKey = 0)
        {
            List<Subject> subjectsSstg = new List<Subject>();
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
            {
                subjectsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.SchoolKey == schoolKey && sstg.GradeKey == gradeKey && sstg.SubjectKey != null).Select(sstg => sstg.Subject).Distinct().ToListAsync();
            }
            else if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
            {
                var studentSstg = await db.StudentEnrolments.Where(se => se.StudentKey == CurrentUser.Studentkey).Select(se => se.SchoolSubjectTeacherGrade).ToListAsync();
                subjectsSstg = studentSstg.Where(sstg => sstg.SchoolKey == schoolKey && sstg.GradeKey == gradeKey && sstg.SubjectKey != null).Select(sstg => sstg.Subject).Distinct().ToList();
            }

            var subjects = subjectsSstg.OrderBy(s => s.SubjectIndex).Select(g => new
            {
                SubjectKey = g.SubjectKey,
                DefaultSubject1 = g.Subject1,
                Subject1 = CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                SubjectIndex = g.SubjectIndex,
                LocalSubjectGUID = g.LocalSubjectGUID,
                Chapters = g.Chapters
            });

            var subjectsSelectList = subjects.Select(s => new
            {
                id = s.SubjectKey,
                text = s.Subject1
            }).ToList();

            return Json(subjectsSelectList);
        }

        // GET: SynchronousSessions/Edit/5
        [Authorization(Roles = "Teacher")]
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SynchronousSession synchronousSession = await db.SynchronousSessions.FindAsync(id);
            if (synchronousSession == null)
            {
                return HttpNotFound();
            }

            SynchronousSessionsSetupViewModel synchronousSessionsSetupViewModel = new SynchronousSessionsSetupViewModel();
            synchronousSessionsSetupViewModel.Key = synchronousSession.SynchronousSessionKey;
            synchronousSessionsSetupViewModel.Topic = synchronousSession.Topic;
            synchronousSessionsSetupViewModel.Descriprion = synchronousSession.Descriprion;
            synchronousSessionsSetupViewModel.SessionDate = synchronousSession.SessionDate;
            synchronousSessionsSetupViewModel.MeetingDuration = synchronousSession.MeetingDuration;
            synchronousSessionsSetupViewModel.GoogleCalendarEventId = synchronousSession.GoogleRemoteID;

            ViewBag.SynchronousSessionsSetupViewModel = synchronousSessionsSetupViewModel;

            SynchronousSessionsAudienceViewModel synchronousSessionsAudienceViewModel = new SynchronousSessionsAudienceViewModel();
            synchronousSessionsAudienceViewModel.SchoolKey = synchronousSession.SchoolSubjectTeacherGrade.SchoolKey;
            synchronousSessionsAudienceViewModel.SchoolDisplayName = synchronousSession.SchoolSubjectTeacherGrade.School.SchoolName;
            synchronousSessionsAudienceViewModel.GradeKey = synchronousSession.SchoolSubjectTeacherGrade.GradeKey;
            synchronousSessionsAudienceViewModel.GradeDisplayName = synchronousSession.SchoolSubjectTeacherGrade.Grade.Grade1;
            synchronousSessionsAudienceViewModel.SubjectKey = synchronousSession.SchoolSubjectTeacherGrade.SubjectKey;
            synchronousSessionsAudienceViewModel.SubjectDisplayName = synchronousSession.SchoolSubjectTeacherGrade.Subject.Subject1;
            synchronousSessionsAudienceViewModel.ChapterKey = synchronousSession.ChapterKey;

            ViewBag.synchronousSessionsAudienceViewModel = synchronousSessionsAudienceViewModel;

            SynchronousSessionSelectAudienceViewModel synchronousSessionSelectAudienceViewModel = new SynchronousSessionSelectAudienceViewModel();

            var allStudentsInClass = await db.Students.Where(s => s.SchoolKey == synchronousSession.SchoolSubjectTeacherGrade.SchoolKey && s.GradeKey == synchronousSession.SchoolSubjectTeacherGrade.GradeKey && s.AspNetUsers.Count != 0).ToListAsync();

            List<StudentsEnrolledViewModel> studentsEnrolledList = new List<StudentsEnrolledViewModel>();
            foreach (var student in allStudentsInClass)
            {
                StudentsEnrolledViewModel studentEnrolled = new StudentsEnrolledViewModel();
                studentEnrolled.StudentKey = student.StudentKey.ToString();
                studentEnrolled.StudentName = $"{student.FirstName} {student.LastName}";

                studentsEnrolledList.Add(studentEnrolled);
            }
            synchronousSessionSelectAudienceViewModel.StudentsEnrolled = studentsEnrolledList;

            var sessionTimelineStudents = await db.Timelines.Where(ts => ts.TimeLineEntityKey == synchronousSession.SynchronousSessionKey && ts.UserKey != synchronousSession.CreatedByUserKey).ToListAsync();

            List<StudentsEnrolledViewModel> selectedStudentsEnrolledList = new List<StudentsEnrolledViewModel>();
            foreach (var student in sessionTimelineStudents)
            {
                StudentsEnrolledViewModel selectedStudentEnrolled = new StudentsEnrolledViewModel();

                if (student.AspNetUser1.Studentkey != null)
                {
                    selectedStudentEnrolled.StudentKey = student.AspNetUser1.Studentkey.ToString();

                    selectedStudentEnrolled.StudentName = $"{student.AspNetUser1.Student.FirstName} {student.AspNetUser1.Student.LastName}";

                    selectedStudentsEnrolledList.Add(selectedStudentEnrolled);
                }
                
            }
            synchronousSessionSelectAudienceViewModel.SelectedStudentsEnrolled = selectedStudentsEnrolledList;

            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
            {
                allStudentsInClass = allStudentsInClass.Where(s => s.StudentKey != CurrentUser.Studentkey).ToList();
                ViewBag.ClassTeacher = synchronousSession.SchoolSubjectTeacherGrade.Teacher;

                var teacherUser = await db.AspNetUsers.FirstOrDefaultAsync(tu => tu.TeacherKey == synchronousSession.SchoolSubjectTeacherGrade.TeacherKey);
                var teacherSelected = await db.Timelines.FirstOrDefaultAsync(tt => tt.UserKey == teacherUser.Id && tt.TimeLineEntityKey == synchronousSession.SynchronousSessionKey);
                if (teacherSelected != null)
                {
                    ViewBag.ClassTeacherSelected = teacherSelected;
                }
            }

            ViewBag.SynchronousSessionsSelectAudienceViewModel = synchronousSessionSelectAudienceViewModel;

            return View();
        }

        // POST: SynchronousSessions/SessionEditSetup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SessionEditSetup(SynchronousSessionsSetupViewModel model)
        {
            if (model == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    SynchronousSession synchronousSession = await db.SynchronousSessions.FindAsync(model.Key);
                    if (synchronousSession == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    List<SynchronousSession> session = await SynchronousSessionsManager.CheckSessionSchedulingValidity(model.SessionDate.Value, model.MeetingDuration.Value, CurrentUser, synchronousSession.SynchronousSessionKey);
                    if (session.Count > 0)
                    {
                        TempData["sessionTopic"] = session.ElementAt(0).Topic;
                        return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
                    }

                    synchronousSession.Topic = model.Topic;
                    synchronousSession.Descriprion = model.Descriprion;
                    synchronousSession.SessionDate = model.SessionDate.Value;
                    synchronousSession.MeetingDuration = model.MeetingDuration.Value;

                    synchronousSession.CreatedByUserKey = CurrentUser.Id;
                    synchronousSession.CreationDate = DateTime.Now;
                    synchronousSession.EsystemKey = GSuiteManager.GSuiteKey;
                    synchronousSession.TimeZone = Configuration.TIME_ZONE;

                    TempData["SynchronousSessionEdit"] = synchronousSession;

                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                catch (Exception e)
                {
                    await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);

                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
        }

        // POST: SynchronousSessions/SessionEditAudience
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SessionEditAudience(SynchronousSessionsAudienceViewModel model)
        {
            if (model == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    SynchronousSession synchronousSession = (SynchronousSession)TempData["SynchronousSessionEdit"];
                    if (synchronousSession == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    if (synchronousSession.GoogleRemoteID == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    SchoolSubjectTeacherGrade schoolSubjectTeacherGrade = new SchoolSubjectTeacherGrade();
                    if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                    {
                        schoolSubjectTeacherGrade = await db.SchoolSubjectTeacherGrades.FirstOrDefaultAsync(sstg => sstg.SchoolKey == model.SchoolKey && sstg.SubjectKey == model.SubjectKey && sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey == model.GradeKey);
                    }
                    else if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Student")
                    {
                        var studentSstg = await db.StudentEnrolments.Where(se => se.StudentKey == CurrentUser.Studentkey).Select(se => se.SchoolSubjectTeacherGrade).ToListAsync();
                        schoolSubjectTeacherGrade = studentSstg.FirstOrDefault(sstg => sstg.SchoolKey == model.SchoolKey && sstg.SubjectKey == model.SubjectKey && sstg.GradeKey == model.GradeKey);
                    }

                    if (schoolSubjectTeacherGrade == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    TempData["synchronousSessionsAudienceViewModelEdit"] = model;
                    TempData["schoolSubjectTeacherGradeEdit"] = schoolSubjectTeacherGrade;
                    TempData["SynchronousSessionEdit2"] = synchronousSession;


                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                catch (Exception e)
                {
                    await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);

                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
        }

        // POST: SynchronousSessions/SessionAudience
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SessionEditSelectAudience(SelectedAudienceFormViewModel model)
        {

            if (ModelState.IsValid)
            {
                try
                {

                    SynchronousSession synchronousSession = (SynchronousSession)TempData["SynchronousSessionEdit2"];
                    if (synchronousSession == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    SchoolSubjectTeacherGrade schoolSubjectTeacherGrade = (SchoolSubjectTeacherGrade)TempData["schoolSubjectTeacherGradeEdit"];
                    if (schoolSubjectTeacherGrade == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    SynchronousSessionsAudienceViewModel synchronousSessionsAudienceViewModel = (SynchronousSessionsAudienceViewModel)TempData["synchronousSessionsAudienceViewModelEdit"];
                    if (synchronousSessionsAudienceViewModel == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    Session session = SynchronousSessionsManager.EditEvent(synchronousSession.GoogleRemoteID, synchronousSession.Topic, synchronousSession.Descriprion, synchronousSession.SessionDate, synchronousSession.MeetingDuration, CurrentUser, synchronousSessionsAudienceViewModel.SchoolKey.Value);
                    if (session == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    synchronousSession.MeetingURL = session.Link;
                    synchronousSession.SchoolSubjectTeacherGradeKey = schoolSubjectTeacherGrade.SchoolSubjectTeacherGradeKey;
                    synchronousSession.ChapterKey = synchronousSessionsAudienceViewModel.ChapterKey;
                    db.Entry(synchronousSession).State = EntityState.Modified;

                    await db.SaveChangesAsync();

                    //Timeline timelineObject = new Timeline();
                    //timelineObject.CreatedByUserKey = CurrentUser.Id;
                    //timelineObject.UserKey = CurrentUser.Id;
                    //timelineObject.Description = synchronousSession.Descriprion;
                    //timelineObject.CreatedAt = synchronousSession.CreationDate;
                    //timelineObject.TimeLineEntityKey = synchronousSession.SynchronousSessionKey;
                    //timelineObject.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;
                    //timelineObject.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "Internal").FirstOrDefaultAsync()).EntitySourceKey;

                    //await TimelineManager.InsertTimelineData(timelineObject);
                    //await db.SaveChangesAsync();

                    //CreateCalendarEventModel createCalendarEventModel = new CreateCalendarEventModel();
                    //createCalendarEventModel.courseid = 0;
                    //createCalendarEventModel.name = synchronousSession.Topic;
                    //createCalendarEventModel.description = synchronousSession.Descriprion + " You can join using this link: " + session.Link;
                    ////createCalendarEventModel.eventtype = "SyncSession";
                    //createCalendarEventModel.timestart = UtilitiesManager.ConvertToUnixTimestamp(synchronousSession.SessionDate);
                    //createCalendarEventModel.timeduration = (int)(synchronousSession.MeetingDuration * 3600);//in seconds

                    //var calendar = await CalendarManager.CreateEvent(createCalendarEventModel, HttpContext);

                    //var calendarEvents = calendar.events;
                    //if (calendarEvents.Count != 0)
                    //{
                    //    synchronousSession.MoodleEventId = calendarEvents.FirstOrDefault().id;
                    //    await db.SaveChangesAsync();
                    //}

                    //var allStudentsInClass = await db.Students.Where(s => s.SchoolKey == schoolSubjectTeacherGrade.SchoolKey && s.GradeKey == schoolSubjectTeacherGrade.GradeKey && s.AspNetUsers.Count != 0).ToListAsync();

                    var sessionTimelineStudents = await db.Timelines.Where(ts => ts.TimeLineEntityKey == synchronousSession.SynchronousSessionKey && ts.UserKey != synchronousSession.CreatedByUserKey).ToListAsync();

                    foreach (var studentTimeline in sessionTimelineStudents)
                    {
                        db.Timelines.Remove(studentTimeline);
                        await db.SaveChangesAsync();
                    }

                    //bool calendar = await CalendarManager.DeleteEvent(deleteCalendarEventModel, HttpContext);
                    //if (!calendar)
                    //{
                    //    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    //}

                    foreach (var student in model.studentsSelected) //allStudentsInClass
                    {
                        int studentId = Convert.ToInt32(student);
                        var studentUser = await db.AspNetUsers.Where(su => su.Studentkey == studentId).FirstOrDefaultAsync();
                        if (studentUser == null)
                        {
                            await LogManager.log(MethodBase.GetCurrentMethod().Name, "Couldn't find student with id: " + student);
                            continue;
                        }
                        var moodleStudent = await db.RemoteAuthentications.Where(ra => ra.ESystem.ESystemName == "Moodle" && ra.Userkey == studentUser.Id).FirstOrDefaultAsync();
                        if (studentUser == null)
                        {
                            await LogManager.log(MethodBase.GetCurrentMethod().Name, "Couldn't find student with moodle user. (studentId) " + student);
                            continue;
                        }
                        string moodleStudentId = moodleStudent.EsystemRemoteId;
                        string userKey = moodleStudent.Userkey;
                        string token = await MoodleManager.requestTokenByMoodleUserId(moodleStudentId, HttpContext,userKey);

                        Timeline newTimeline = new Timeline();

                        newTimeline.TimeLineEntityKey = synchronousSession.SynchronousSessionKey;
                        newTimeline.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;
                        newTimeline.CreatedAt = DateTime.Now;
                        string studentUserKey = studentUser.Id;
                        newTimeline.UserKey = studentUserKey;
                        newTimeline.CreatedByUserKey = CurrentUser.Id;
                        newTimeline.Description = synchronousSession.Descriprion;
                        newTimeline.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "Internal").FirstOrDefaultAsync()).EntitySourceKey;

                        bool insertStudentTimline = await TimelineManager.InsertTimelineData(newTimeline);

                        if (insertStudentTimline)
                        {
                            CreateCalendarEventModel createCalendarEventModelStudent = new CreateCalendarEventModel();
                            createCalendarEventModelStudent.courseid = 0;
                            createCalendarEventModelStudent.name = synchronousSession.Topic;
                            createCalendarEventModelStudent.description = synchronousSession.Descriprion + " You can join using this link: " + session.Link;
                            //createCalendarEventModel.eventtype = "SyncSession";
                            createCalendarEventModelStudent.timestart = UtilitiesManager.ConvertToUnixTimestamp(synchronousSession.SessionDate);
                            createCalendarEventModelStudent.timeduration = (int)(synchronousSession.MeetingDuration * 3600);//in seconds

                            var calendarStudent = await CalendarManager.CreateEvent(createCalendarEventModelStudent, HttpContext, true, token);
                        }


                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(model.selectTeacher))
                        {


                            int teacherId = Convert.ToInt32(model.selectTeacher);
                            var teacherUser = await db.AspNetUsers.Where(su => su.TeacherKey == teacherId).FirstOrDefaultAsync();
                            if (teacherUser == null)
                            {
                                await LogManager.log(MethodBase.GetCurrentMethod().Name, "Couldn't find teacher with id: " + teacherId);

                            }
                            var moodleTeacher = await db.RemoteAuthentications.Where(ra => ra.ESystem.ESystemName == "Moodle" && ra.Userkey == teacherUser.Id).FirstOrDefaultAsync();
                            if (moodleTeacher == null)
                            {
                                await LogManager.log(MethodBase.GetCurrentMethod().Name, "Couldn't find teacher with moodle user. (teacherId) " + teacherId);

                            }
                            string moodleTeacherId = moodleTeacher.EsystemRemoteId;
                            string token = await MoodleManager.requestTokenByMoodleUserId(moodleTeacherId, HttpContext);

                            Timeline newTimeline = new Timeline();

                            newTimeline.TimeLineEntityKey = synchronousSession.SynchronousSessionKey;
                            newTimeline.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;
                            newTimeline.CreatedAt = DateTime.Now;
                            string teacherUserKey = teacherUser.Id;
                            newTimeline.UserKey = teacherUserKey;
                            newTimeline.CreatedByUserKey = CurrentUser.Id;
                            newTimeline.Description = synchronousSession.Descriprion;
                            newTimeline.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "Internal").FirstOrDefaultAsync()).EntitySourceKey;

                            bool insertStudentTimline = await TimelineManager.InsertTimelineData(newTimeline);

                            if (insertStudentTimline)
                            {
                                CreateCalendarEventModel createCalendarEventModelTeacher = new CreateCalendarEventModel();
                                createCalendarEventModelTeacher.courseid = 0;
                                createCalendarEventModelTeacher.name = synchronousSession.Topic;
                                createCalendarEventModelTeacher.description = synchronousSession.Descriprion + " You can join using this link: " + session.Link;
                                //createCalendarEventModel.eventtype = "SyncSession";
                                createCalendarEventModelTeacher.timestart = UtilitiesManager.ConvertToUnixTimestamp(synchronousSession.SessionDate);
                                createCalendarEventModelTeacher.timeduration = (int)(synchronousSession.MeetingDuration * 3600);//in seconds

                                var calendarStudent = await CalendarManager.CreateEvent(createCalendarEventModelTeacher, HttpContext, true, token);
                            }


                        }
                        else
                        {
                            string sessionKey = Convert.ToString(synchronousSession.SynchronousSessionKey);
                            var teacherUser = await db.AspNetUsers.FirstOrDefaultAsync(t => t.TeacherKey == schoolSubjectTeacherGrade.TeacherKey);
                            if (teacherUser != null)
                            {
                                RLI.WebApplication.Controllers.MoodleSignalRController moodleSignalRController = new RLI.WebApplication.Controllers.MoodleSignalRController();
                                var notifyTeacher = await moodleSignalRController.NotifyTeacherAboutStudentsSynchronousSession(CurrentUser.Id, teacherUser.Id, sessionKey);
                            }
                            else
                            {
                                await LogManager.log(MethodBase.GetCurrentMethod().Name, $"Couldn't find the teacher with teacherKey {schoolSubjectTeacherGrade.TeacherKey}");
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        await LogManager.log(MethodBase.GetCurrentMethod().Name, e.ToString());
                    }

                    return Json(new { MeetingUrl = session.Link });
                }
                catch (Exception e)
                {
                    await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);

                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
        }

        // POST: SynchronousSessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorization(Roles = "Teacher")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
            SynchronousSession synchronousSession = await db.SynchronousSessions.FindAsync(id);
            if (synchronousSession == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
            try
            {
                var deleteResponse = SynchronousSessionsManager.DeleteEvent(synchronousSession.GoogleRemoteID, CurrentUser, synchronousSession.SchoolSubjectTeacherGrade.SchoolKey.Value);
                if (!deleteResponse)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }

                DeleteCalendarEventModel deleteCalendarEventModel = new DeleteCalendarEventModel();
                deleteCalendarEventModel.eventid = (int)synchronousSession.MoodleEventId;
                deleteCalendarEventModel.repeat = 0;
                var timelineCalendarItem = await db.Timelines.Where(t => t.TimeLineEntityKey == deleteCalendarEventModel.eventid).FirstOrDefaultAsync();
                if (timelineCalendarItem != null)
                {
                    db.Timelines.Remove(timelineCalendarItem);
                    await db.SaveChangesAsync();
                }

                bool calendar = await CalendarManager.DeleteEvent(deleteCalendarEventModel, HttpContext);
                if (!calendar)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }

            }
            catch (Exception e)
            {
                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);
            }
            try
            {
                //shit.. to be replaced soon
                int timelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;
                //
                // int timelineKey = await TimelineManager.RetrieveTimeLineKey(synchronousSession.SynchronousSessionKey, timelineComponentKey, CurrentUser.Id);
                var syncSessionsInTimeline = await db.Timelines.Where(t => t.TimeLineEntityKey == synchronousSession.SynchronousSessionKey && t.TimelineComponent.ComponentName == "SynchronousSessions").ToListAsync();
                if (syncSessionsInTimeline.Count != 0)
                {
                    foreach (var syncSession in syncSessionsInTimeline)
                    {
                        await TimelineManager.RemoveTimelineData(syncSession.TimelineKey);
                    }
                }

                db.SynchronousSessions.Remove(synchronousSession);
                await db.SaveChangesAsync();

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);

                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
        }
    }
}