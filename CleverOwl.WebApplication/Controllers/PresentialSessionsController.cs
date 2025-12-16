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
    [Authorization(Roles = "Administrator,Teacher,Student")]
    public class PresentialSessionsController : BaseController
    {
        const int DEFAULT_LOAD_COUNT = 10;
        // GET: PresentialSessions
        public ActionResult Index()
        {
            List<PresentialSessionsDetails> PresentialSessionsDetailsList = new List<PresentialSessionsDetails>();
            var PresentialSessions = db.PresentialSessions.OrderByDescending(ss => ss.SessionDate).ToList();
            foreach(var item in PresentialSessions)
            {
                PresentialSessionsDetails PresentialSessionsDetails = new PresentialSessionsDetails();
                PresentialSessionsDetails.Grade = item.SchoolSubjectTeacherGrade.Grade.Grade1;
                PresentialSessionsDetails.Subject = item.SchoolSubjectTeacherGrade.Subject.Subject1;
                if (item.ChapterKey != null)
                {
                    PresentialSessionsDetails.Chapter = item.Chapter.ChapterName;
                }
                if (item.TopicKey != null)
                {
                    PresentialSessionsDetails.Topic = item.Topic.Topic1;
                }

                PresentialSessionsDetails.Teacher = item.SchoolSubjectTeacherGrade.Teacher.FirstName +" "+item.SchoolSubjectTeacherGrade.Teacher.LastName;
                PresentialSessionsDetails.Title = item.Title;
                PresentialSessionsDetails.SessionDate = item.SessionDate;
                PresentialSessionsDetails.PresentialSessionKey = item.PresentialSessionKey;
                PresentialSessionsDetailsList.Add(PresentialSessionsDetails);
            }
            List<RLI.EntityFramework.EDM.Grade> grades =  PresentialSessions.Select(a => a.SchoolSubjectTeacherGrade.Grade).Distinct().ToList();
            List<RLI.EntityFramework.EDM.Subject> subjects = PresentialSessions.Select(a => a.SchoolSubjectTeacherGrade.Subject).Distinct().ToList();
           var sessionDates = PresentialSessions.Select(a => a.SessionDate).Distinct().ToList();

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
            return View(PresentialSessionsDetailsList);
        }
        public async Task<ActionResult> Attendance(int id)
        {
            PresentialSession presentialSession = await db.PresentialSessions.FindAsync(id);
            if (presentialSession == null)
            {
                return new HttpNotFoundResult();
            }
            var GradeKey = presentialSession.SchoolSubjectTeacherGrade.GradeKey;
            var schoolKey = presentialSession.SchoolSubjectTeacherGrade.SchoolKey;
            List<AspNetUser> AspNetUser = db.AspNetUsers.Where(a => a.Student.SchoolKey == schoolKey && a.Student.GradeKey == GradeKey).ToList();
            List<string> presentUserKeys = db.Attendances.Where(a=>a.PresentialSessionKey == id).Select(a => a.AttendedUserKey).ToList();
            List<Student> absentAspNetUsers = AspNetUser.Where(u => !presentUserKeys.Contains(u.Id)).Select(s => s.Student).ToList();
            ViewBag.Presentialkey = id;
            return View(absentAspNetUsers);
        }

        public async Task<ActionResult> AttendanceDetails(int id)
        {
            var PresentialSessions = db.PresentialSessions.Where(p=>p.PresentialSessionKey== id).FirstOrDefault();
            SchoolSubjectTeacherGrade SchoolSubjectTeacherGrade = PresentialSessions.SchoolSubjectTeacherGrade;
            ViewBag.SessionTitle = PresentialSessions.Title;
            var students = db.Students.Where(s => s.SchoolKey == SchoolSubjectTeacherGrade.SchoolKey && s.GradeKey == SchoolSubjectTeacherGrade.GradeKey).ToList();
            List<AttendanceDetailsViewModel>attendance = db.Attendances.Where(a => a.PresentialSessionKey == id)
                    .Select(a => new AttendanceDetailsViewModel
                    {
                        FirstName = a.AspNetUser.FirstName,
                        MiddleName = a.AspNetUser.Student.MiddleName,
                        LastName = a.AspNetUser.LastName,
                        AttendedOrNot = "YES"
                    })
                    .ToList();

            List<AspNetUser> AspNetUser = db.AspNetUsers.Where(a => a.Student.SchoolKey == SchoolSubjectTeacherGrade.SchoolKey && a.Student.GradeKey == SchoolSubjectTeacherGrade.GradeKey).ToList();
            List<string> presentUserKeys = db.Attendances.Select(a => a.AttendedUserKey).ToList();
            List<Student> absentAspNetUsers = AspNetUser.Where(u => !presentUserKeys.Contains(u.Id)).Select(s => s.Student).ToList();
            List<AttendanceDetailsViewModel> absentUsers = absentAspNetUsers.Select(a=> new AttendanceDetailsViewModel
                {
                        FirstName = a.FirstName,
                        MiddleName = a.MiddleName,
                        LastName = a.LastName,
                        AttendedOrNot = "NO"
                }).ToList();
            List<AttendanceDetailsViewModel> attendanceDetails = attendance.Concat(absentUsers).ToList();
            return View(attendanceDetails);
        }
            [HttpPost]
        [ValidateAntiForgeryToken]
        public void SaveAttendance(int PresentialSessionKey,int studentId)
        {
            var userkey = db.AspNetUsers.Where(a => a.Studentkey == studentId).Select(u=>u.Id).FirstOrDefault();

            Attendance Attendance = new Attendance();
            Attendance.PresentialSessionKey = PresentialSessionKey;
            Attendance.AttendedAt = DateTime.Now;
            Attendance.AttendedUserKey = userkey;
            db.Attendances.Add(Attendance);
             db.SaveChanges();
        }
            [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SessionSetup(PresentialSessionsSetupViewModel model)
        {
            if (model == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    List<PresentialSession> session = await SynchronousSessionsManager.CheckPresentialSessionSchedulingValidity(model.SessionDate.Value, model.MeetingDuration.Value, CurrentUser);
                    if (session.Count > 0)
                    {
                        TempData["sessionTopic"] = session.ElementAt(0).Topic;
                        TempData["date"] = model.SessionDate.Value;
                        TempData["duration"] = model.MeetingDuration.Value;
                        return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
                    }

                    PresentialSession PresentialSession = new PresentialSession();

                    PresentialSession.Title = model.Topic;
                    PresentialSession.Descriprion = model.Descriprion;
                    PresentialSession.SessionDate = model.SessionDate.Value;
                    PresentialSession.MeetingDuration = model.MeetingDuration.Value;

                    PresentialSession.CreatedByUserKey = CurrentUser.Id;
                    PresentialSession.CreationDate = DateTime.Now;
                    PresentialSession.EsystemKey = 8;
                    PresentialSession.TimeZone = Configuration.TIME_ZONE;

                    TempData["PresentialSession"] = PresentialSession;

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
        public async Task<ActionResult> SessionAudience(PresentialSessionsAudienceViewModel model)
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

                    PresentialSessionSelectAudienceViewModelcs PresentialSessionSelectAudienceViewModelcs = new RLI.WebApplication.Models.PresentialSessionSelectAudienceViewModelcs();
                    PresentialSessionSelectAudienceViewModelcs.StudentsEnrolled = studentsEnrolledList;
                    PresentialSessionSelectAudienceViewModelcs.SelectedStudentsEnrolled = studentsEnrolledList;

                    ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("SynchronousSessions", "Create", CurrentLanguageIdentifier);

                    //start level3-------------------------------------------------------------------------------------------------------------------
                    PresentialSession PresentialSession = (PresentialSession)TempData["PresentialSession"];
                    if (PresentialSession == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    if (schoolSubjectTeacherGrade == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    PresentialSessionsAudienceViewModel PresentialSessionsAudienceViewModel = model;
                    if (PresentialSessionsAudienceViewModel == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    Session session = SynchronousSessionsManager.CreateEvent(PresentialSession.Title, PresentialSession.Descriprion, PresentialSession.SessionDate, PresentialSession.MeetingDuration, CurrentUser, PresentialSessionsAudienceViewModel.SchoolKey.Value);
                    if (session == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }
                    PresentialSession.MeetingURL = session.Link;
                    PresentialSession.SchoolSubjectTeacherGradeKey = schoolSubjectTeacherGrade.SchoolSubjectTeacherGradeKey;
                    PresentialSession.GoogleRemoteID = session.Id;
                    PresentialSession.ChapterKey = PresentialSessionsAudienceViewModel.ChapterKey;
                    PresentialSession.TopicKey = PresentialSessionsAudienceViewModel.TopicKey;

                    db.PresentialSessions.Add(PresentialSession);

                    await db.SaveChangesAsync();
                    int PresentialSessionKey = PresentialSession.PresentialSessionKey;
                    Timeline timelineObject = new Timeline();
                    timelineObject.CreatedByUserKey = CurrentUser.Id;
                    timelineObject.UserKey = CurrentUser.Id;
                    timelineObject.Description = PresentialSession.Descriprion;
                    timelineObject.CreatedAt = PresentialSession.CreationDate;
                    timelineObject.TimeLineEntityKey = PresentialSession.PresentialSessionKey;
                    timelineObject.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;
                    timelineObject.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "Internal").FirstOrDefaultAsync()).EntitySourceKey;

                    await TimelineManager.InsertTimelineData(timelineObject);
                    await db.SaveChangesAsync();

                    CreateCalendarEventModel createCalendarEventModel = new CreateCalendarEventModel();
                    createCalendarEventModel.courseid = 0;
                    createCalendarEventModel.name = PresentialSession.Title;
                    createCalendarEventModel.description = PresentialSession.Descriprion + " You can join using this link: " + session.Link;
                    //createCalendarEventModel.eventtype = "SyncSession";
                    createCalendarEventModel.timestart = UtilitiesManager.ConvertToUnixTimestamp(PresentialSession.SessionDate);
                    createCalendarEventModel.timeduration = (int)(PresentialSession.MeetingDuration * 3600);//in seconds

                    var calendar = await CalendarManager.CreateEvent(createCalendarEventModel, HttpContext);

                    var calendarEvents = calendar.events;
                    if (calendarEvents.Count != 0)
                    {
                        PresentialSession.MoodleEventId = calendarEvents.FirstOrDefault().id;
                        await db.SaveChangesAsync();
                    }

                    //var allStudentsInClass = await db.Students.Where(s => s.SchoolKey == schoolSubjectTeacherGrade.SchoolKey && s.GradeKey == schoolSubjectTeacherGrade.GradeKey && s.AspNetUsers.Count != 0).ToListAsync();

                    foreach (var student in studentsEnrolledList) //allStudentsInClass
                    {
                        int studentId = Convert.ToInt32(student.StudentKey);
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
                            string token = await MoodleManager.requestTokenByMoodleUserId(moodleStudentId, HttpContext);

                            Timeline newTimeline = new Timeline();

                            newTimeline.TimeLineEntityKey = PresentialSession.PresentialSessionKey;
                            newTimeline.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;
                            newTimeline.CreatedAt = DateTime.Now;
                            string studentUserKey = studentUser.Id;
                            newTimeline.UserKey = studentUserKey;
                            newTimeline.CreatedByUserKey = CurrentUser.Id;
                            newTimeline.Description = PresentialSession.Descriprion;
                            newTimeline.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "Internal").FirstOrDefaultAsync()).EntitySourceKey;

                            bool insertStudentTimline = await TimelineManager.InsertTimelineData(newTimeline);

                            if (insertStudentTimline)
                            {
                                CreateCalendarEventModel createCalendarEventModelStudent = new CreateCalendarEventModel();
                                createCalendarEventModelStudent.courseid = 0;
                                createCalendarEventModelStudent.name = PresentialSession.Title;
                                createCalendarEventModelStudent.description = PresentialSession.Descriprion + " You can join using this link: " + session.Link;
                                //createCalendarEventModel.eventtype = "SyncSession";
                                createCalendarEventModelStudent.timestart = UtilitiesManager.ConvertToUnixTimestamp(PresentialSession.SessionDate);
                                createCalendarEventModelStudent.timeduration = (int)(PresentialSession.MeetingDuration * 3600);//in seconds

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
                        if (!string.IsNullOrEmpty(CurrentUser.Id))
                        {


                            int teacherId = Convert.ToInt32(CurrentUser.Id);
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

                            newTimeline.TimeLineEntityKey = PresentialSession.PresentialSessionKey;
                            newTimeline.TimelineComponentKey = (await db.TimelineComponents.Where(t => t.ComponentName == "SynchronousSessions").FirstOrDefaultAsync()).TimelineComponentKey;
                            newTimeline.CreatedAt = DateTime.Now;
                            string teacherUserKey = teacherUser.Id;
                            newTimeline.UserKey = teacherUserKey;
                            newTimeline.CreatedByUserKey = CurrentUser.Id;
                            newTimeline.Description = PresentialSession.Descriprion;
                            newTimeline.EntitySourceKey = (await db.EntitySources.Where(t => t.Value == "Internal").FirstOrDefaultAsync()).EntitySourceKey;

                            bool insertStudentTimline = await TimelineManager.InsertTimelineData(newTimeline);

                            if (insertStudentTimline)
                            {
                                CreateCalendarEventModel createCalendarEventModelTeacher = new CreateCalendarEventModel();
                                createCalendarEventModelTeacher.courseid = 0;
                                createCalendarEventModelTeacher.name = PresentialSession.Title;
                                createCalendarEventModelTeacher.description = PresentialSession.Descriprion + " You can join using this link: " + session.Link;
                                //createCalendarEventModel.eventtype = "SyncSession";
                                createCalendarEventModelTeacher.timestart = UtilitiesManager.ConvertToUnixTimestamp(PresentialSession.SessionDate);
                                createCalendarEventModelTeacher.timeduration = (int)(PresentialSession.MeetingDuration * 3600);//in seconds

                                var calendarStudent = await CalendarManager.CreateEvent(createCalendarEventModelTeacher, HttpContext, true, token);
                            }


                        }
                        else
                        {
                            string sessionKey = Convert.ToString(PresentialSession.PresentialSessionKey);
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

                    return Json(new { PresentialSessionKey = PresentialSessionKey }); ;

                    return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
                }
                catch (Exception e)
                {
                    await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);

                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Filter(string query = null, int? records = null, int? gradeKey = null, int? subjectKey = null, DateTime? sessionDate = null)
        {
            db.Configuration.ProxyCreationEnabled = false;
            int numberOfRecords = (int)(records == null ? DEFAULT_LOAD_COUNT : records);

            var PresentialSessions = db.PresentialSessions.Where(ss => ss.CreatedByUserKey == CurrentUser.Id).OrderBy(a => a.SessionDate);

            List<PresentialSessionsDetails> PresentialSessionsFiltered = await PresentialSessions.Where(a =>
          ((gradeKey != null && (a.SchoolSubjectTeacherGrade.Grade.GradeKey == gradeKey)) || (gradeKey == null))
          && ((subjectKey != null && (a.SchoolSubjectTeacherGrade.Subject.SubjectKey == subjectKey)) || (subjectKey == null))
          && ((sessionDate != null && (EntityFunctions.TruncateTime(a.SessionDate) == EntityFunctions.TruncateTime(sessionDate))) || (sessionDate == null))
          && ((query != null && (a.Title.Contains(query) || a.Descriprion.Contains(query))) || (query == null))).Take(numberOfRecords)
                  .Select(a => new PresentialSessionsDetails
                  {
                      PresentialSessionKey = a.PresentialSessionKey,
                      Title = a.Title,
                      SessionDate = a.SessionDate,
                      Grade = a.SchoolSubjectTeacherGrade.Grade.Grade1, // Adjust according to the actual property
                      Subject = a.SchoolSubjectTeacherGrade.Subject.Subject1, // Adjust according to the actual property
                      Chapter = a.Chapter.ChapterName, // Adjust according to the actual property
                      Topic = a.Topic.Topic1, // Adjust according to the actual property
                      Teacher = a.SchoolSubjectTeacherGrade.Teacher.FirstName +" "+a.SchoolSubjectTeacherGrade.Teacher.LastName // Adjust according to the actual property
                  })
        .ToListAsync();

            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("PresentialSessions", "Index", CurrentLanguageIdentifier);
            return PartialView("_PresentialSessionsTable", PresentialSessionsFiltered);
            
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
                var PresentialSessions = db.PresentialSessions.Where(ps => ps.CreatedByUserKey == CurrentUser.Id).Where(ps =>
                   ps.Title.ToLower().Contains(searchQuery)
                || ps.Descriprion.ToLower().Contains(searchQuery)).Select(ps => ps.Topic).Distinct().Select(ps => new
                {
                    id = ps,
                    title = ps,
                    text = "<h5 class='m-auto d-block'>" + ps + "</h5>"
                });

                result.AddRange(await PresentialSessions.Take(10).ToListAsync());

            }
            catch (Exception e)
            {
                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.ToString());

                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            return Json(result);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GetTopics(string query, int? Chapter)
        {
            var topics = db.ChaptersTopics.Where(c => c.ChapterKey == Chapter).Select(t => t.Topic).ToList();
            var topicsSelectList = topics.Select(s => new
            {
                id = s.TopicKey,
                text = s.Topic1
            }).ToList();

            return Json(topicsSelectList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> LookupTopics(string query, string grade, int? subject, int type = 5)
        {

            int?[] grade_idArray = new int?[] { };
            if (grade != null)
            {
                grade_idArray = JsonConvert.DeserializeObject<List<int?>>(grade).ToArray();
            }

            ChapterType chapterType;
            if (type == 1)
            {
                chapterType = ChapterType.ChapterTypeDigitalLibrary;
            }
            else if (type == 5)
            {
                chapterType = ChapterType.ChapterTypeNERDCCurriculum;
            }
            else
            {
                chapterType = ChapterType.ChapterTypeRecordedSession;
            }

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
        public async Task<ActionResult> SessionSelectAudience(SelectedAudienceFormViewModel model)
        {

            if (ModelState.IsValid)
            {
                try
                {

                    SynchronousSession synchronousSession = (SynchronousSession)TempData["PresentialSession"];
                    if (synchronousSession == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    SchoolSubjectTeacherGrade schoolSubjectTeacherGrade = (SchoolSubjectTeacherGrade)TempData["PresentialschoolSubjectTeacherGrade"];
                    if (schoolSubjectTeacherGrade == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }

                    SynchronousSessionsAudienceViewModel synchronousSessionsAudienceViewModel = (SynchronousSessionsAudienceViewModel)TempData["PresentialSessionsAudienceViewModel"];
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
                            string token = await MoodleManager.requestTokenByMoodleUserId(moodleStudentId, HttpContext);

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
        public ActionResult GetCurricullumGrades()
        {
            var grades = db.Grades.Where(g => g.GradeTypeKey == 1).ToList();
            var gradesSelectList = grades.Select(s => new
            {
                id = s.GradeKey,
                text = s.Grade1
            }).ToList();
            return Json(gradesSelectList);
        }
    }
}