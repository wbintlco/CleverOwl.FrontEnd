using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RLI.Common.DataObjects;
using RLI.Common.Managers;
using RLI.Common.Managers.MoodleManagers.Database;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Attributes;
using RLI.WebApplication.Models;
using RLI.WebApplication.Objects;
using RLI.WebApplication.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Assignment = RLI.EntityFramework.EDM.Assignment;
using AssignmentViewModel = RLI.Common.DataObjects.AssignmentViewModel;
namespace CleverOwl.WebApplication.Controllers
{
    public class MemoryPostedFile : HttpPostedFileBase
    {
        private readonly byte[] fileBytes;

        public MemoryPostedFile(byte[] fileBytes, string fileName = null)
        {
            this.fileBytes = fileBytes;
            this.FileName = fileName;
            this.InputStream = new MemoryStream(fileBytes);
        }

        public override int ContentLength => fileBytes.Length;

        public override string FileName { get; }

        public override Stream InputStream { get; }
    }



    [MoodleToken]
    [Authorize]
    public class AssignmentsController : BaseController
    {

        public static List<(long AssignmentId, decimal? Grade, string Status, decimal? MaxGrade)> GetGradesAndStatuses(List<long> assignmentIds, long studentId)
        {
            using (moodleEntities dbm = new moodleEntities())
            {
                var result = (from ag in dbm.mdl_assign_grades
                              join a in dbm.mdl_assign on ag.assignment equals a.id
                              where assignmentIds.Contains(ag.assignment) && ag.userid == studentId
                              join sub in dbm.mdl_assign_submission
                                  on new { ag.assignment, ag.userid } equals new { sub.assignment, sub.userid } into subs
                              from sub in subs.DefaultIfEmpty()
                              select new
                              {
                                  AssignmentId = ag.assignment,
                                  Grade = ag.grade,
                                  Status = sub != null ? sub.status : null,
                                  MaxGrade = (decimal?)a.grade  // Explicit cast to decimal?
                              })
                             .ToList()
                             .Select(x => (x.AssignmentId, x.Grade, x.Status, x.MaxGrade))
                             .ToList();

                return result;
            }
        }


        public async Task<ActionResult> Index(dynamic httpContext)
        {
            List<RLI.EntityFramework.EDM.Assignment> assignments = new List<RLI.EntityFramework.EDM.Assignment>();
            List<RLI.EntityFramework.EDM.Assignment> assignments2 = new List<RLI.EntityFramework.EDM.Assignment>();
            List<Models.AssignmentCardViewModel> assignmentCardViewModel = new List<Models.AssignmentCardViewModel>();
            var remoteId = "";
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
            {
                ViewBag.httpContext = HttpContext;
                string userkey = CurrentUser.Id;
                var currentStudent = await db.AspNetUsers.Where(a => a.Id == CurrentUser.Id).ToListAsync();
                var student = currentStudent.FirstOrDefault().Student;
                remoteId = currentStudent.FirstOrDefault().RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == userkey).OrderByDescending(r => r.RemoteAuthenticationKey).FirstOrDefault().EsystemRemoteId;
                var studentAssTimeline = await db.Timelines.Where(ut => ut.UserKey == CurrentUser.Id).Select(ass => ass.TimeLineEntityKey).ToListAsync();

                assignments = await db.Assignments.Where(a => a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && studentAssTimeline.Contains(a.AssignmentKey)).OrderByDescending(a => a.TimeModifed).Take(10).ToListAsync();
                assignments2 = await db.Assignments.Where(a => a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && studentAssTimeline.Contains(a.AssignmentKey)).OrderByDescending(a => a.TimeModifed).ToListAsync();
            }
            else
            {
                assignments = await db.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey).OrderByDescending(a => a.TimeModifed).Take(10).ToListAsync();
                assignments2 = await db.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey).OrderByDescending(a => a.TimeModifed).ToListAsync();
            }


            using (moodleEntities dbm = new moodleEntities())
            {
                var assignmentIds = assignments
    .Where(a => a.AssignmentId.HasValue)  // Filter out null AssignmentIds
    .Select(a => (long)a.AssignmentId.Value)  // Convert int? to long
    .ToList();
                long studentId = 0;
                List<(long AssignmentId, decimal? Grade, string Status, decimal? MaxGrade)> gradesAndStatuses = null;
                List<(long AssignmentId, int SubmissionCount)> submissionCounts = null;
                List<(long AssignmentId, int StudentCount)> studentCounts = null;

                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
                {
                    if (!long.TryParse(remoteId, out studentId))
                    {
                        // If conversion fails, handle it here, for example:
                        throw new ArgumentException("Invalid student ID format.");
                    }
                    gradesAndStatuses = MoodleManager.GetGradesAndStatuses(assignmentIds, studentId);
                }

                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher")
                {
                    submissionCounts = MoodleManager.GetSubmissionCounts(assignmentIds);
                    //  var submissionCounts = GetSubmissionCounts3(assignmentIds);
                    studentCounts = MoodleManager.GetStudentCounts(assignmentIds);
                }



                foreach (RLI.EntityFramework.EDM.Assignment a in assignments)
                {
                    Models.AssignmentCardViewModel currentAssignmentCardViewModel = new Models.AssignmentCardViewModel();

                    currentAssignmentCardViewModel.AssignmentId = a.AssignmentId;
                    currentAssignmentCardViewModel.AssignmentKey = a.AssignmentKey;
                    currentAssignmentCardViewModel.AssignmentTitle = a.AssignmentTitle;
                    currentAssignmentCardViewModel.CourseId = a.CourseId;
                    currentAssignmentCardViewModel.CourseLongName = a.CourseLongName;
                    currentAssignmentCardViewModel.CourseModuleID = a.CourseModuleID;
                    currentAssignmentCardViewModel.CourseTitle = a.CourseTitle;
                    currentAssignmentCardViewModel.CreatedAt = a.CreatedAt;
                    currentAssignmentCardViewModel.Description = a.Description;
                    currentAssignmentCardViewModel.DueDate = a.DueDate;
                    currentAssignmentCardViewModel.SchoolSubjectTeacherGrade = a.SchoolSubjectTeacherGrade;
                    currentAssignmentCardViewModel.SchoolSubjectTeacherGradeKey = a.SchoolSubjectTeacherGradeKey;
                    currentAssignmentCardViewModel.TimeModifed = a.TimeModifed;
                    try
                    {
                        // var gradingSummary = await RLI.Common.Managers.SubmissionsManager.GetAssignGradingSummary(a.AssignmentId.ToString(), HttpContext);

                        //currentAssignmentCardViewModel.Submissions = RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher"? gradingSummary.SubmissionsSubmittedCount.ToString(): "0";

                        // currentAssignmentCardViewModel.participantCount = RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher"? gradingSummary.ParticipantCount.ToString(): "0";

                        if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher")
                        {
                            var submissionResult = submissionCounts.FirstOrDefault(q => q.AssignmentId == a.AssignmentId);

                            var participantResult = studentCounts.FirstOrDefault(q => q.AssignmentId == a.AssignmentId);

                            currentAssignmentCardViewModel.Submissions = submissionResult.AssignmentId != 0
    ? submissionResult.SubmissionCount.ToString()
    : "0";

                            currentAssignmentCardViewModel.participantCount = participantResult.AssignmentId != 0
     ? participantResult.StudentCount.ToString()
     : "0";
                        }
                        else
                        {
                            currentAssignmentCardViewModel.Submissions = "0";
                            currentAssignmentCardViewModel.participantCount = "0";
                        }


                    }
                    catch (Exception e)
                    {
                        currentAssignmentCardViewModel.Submissions = "error";
                    }

                    currentAssignmentCardViewModel.Grade = "";
                    currentAssignmentCardViewModel.Status = "";
                    RLI.Common.DataObjects.SubmissionDetailsViewModel submissionDetailsViewModel = new RLI.Common.DataObjects.SubmissionDetailsViewModel();

                    //if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
                    //{
                    //    submissionDetailsViewModel = await SubmissionsManager.GetAssignSubmissionDetails(a.AssignmentId.ToString(), remoteId, HttpContext);
                    //    if (submissionDetailsViewModel != null)
                    //    {
                    //        currentAssignmentCardViewModel.Grade = submissionDetailsViewModel != null && submissionDetailsViewModel.Grade == null ? "" : submissionDetailsViewModel.Grade;
                    //        currentAssignmentCardViewModel.Status = submissionDetailsViewModel != null && submissionDetailsViewModel.GradingStatus == null ? "" : submissionDetailsViewModel.GradingStatus;
                    //    }
                    //}

                    if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
                    {
                        var gs = gradesAndStatuses.FirstOrDefault(x => x.AssignmentId == a.AssignmentId);

                        if (gs != default)
                        {
                            // Format grade as "75.00 / 100.00"
                            if (gs.Grade.HasValue && gs.MaxGrade.HasValue)
                            {
                                currentAssignmentCardViewModel.Grade =
                                    $"{gs.Grade.Value.ToString("0.00")}&nbsp;/&nbsp;{gs.MaxGrade.Value.ToString("0.00")}";
                            }
                            else
                            {
                                currentAssignmentCardViewModel.Grade = "";
                            }

                            // Standardize status to either "graded" or "notgraded"
                            currentAssignmentCardViewModel.Status = gs.Grade.HasValue ? "graded" : "notgraded";
                        }
                        else
                        {
                            currentAssignmentCardViewModel.Grade = "";
                            currentAssignmentCardViewModel.Status = "notgraded"; // Default to notgraded if no record exists
                        }
                    }

                    assignmentCardViewModel.Add(currentAssignmentCardViewModel);
                }


            }
            //assignmentCardViewModel = assignments.Select(a => new Models.AssignmentCardViewModel
            //{
            //    AssignmentId = a.AssignmentId,
            //    AssignmentKey = a.AssignmentKey,
            //    AssignmentTitle = a.AssignmentTitle,
            //    CourseId = a.CourseId,
            //    CourseLongName = a.CourseLongName,
            //    CourseModuleID = a.CourseModuleID,
            //    CourseTitle = a.CourseTitle,
            //    CreatedAt = a.CreatedAt,
            //    Description = a.Description,
            //    DueDate = a.DueDate,
            //    SchoolSubjectTeacherGrade = a.SchoolSubjectTeacherGrade,
            //    SchoolSubjectTeacherGradeKey = a.SchoolSubjectTeacherGradeKey,
            //    TimeModifed = a.TimeModifed,
            //    Submissions = RLI.Common.Managers.SubmissionsManager.GetAssignGradingSummary(a.AssignmentId.ToString(), HttpContext).Result.SubmissionsSubmittedCount.ToString()
            //}).ToList();

            List<RLI.EntityFramework.EDM.Grade> grades = assignments2.Select(a => a.SchoolSubjectTeacherGrade.Grade).Distinct().ToList();
            //   List<RLI.EntityFramework.EDM.Grade> grades2 = assignments2.Select(a => a.SchoolSubjectTeacherGrade.Grade).Distinct().ToList();
            List<RLI.EntityFramework.EDM.Subject> subjects = assignments2.Select(a => a.SchoolSubjectTeacherGrade.Subject).Distinct().ToList();
            //   List<RLI.EntityFramework.EDM.Subject> subjects2 = assignments2.Select(a => a.SchoolSubjectTeacherGrade.Subject).Distinct().ToList();
            var dueDates = assignments2.Where(a => a.DueDate != null).Select(a => a.DueDate).Distinct().Select(d => new
            {
                date1 = UtilitiesManager.GetFieldLabel(Locale, d.Value.DayOfWeek.ToString()) + ", " + UtilitiesManager.GetFieldLabel(Locale, d.Value.ToString("MMMM")) + " " + d.Value.Day + ", " + d.Value.Year,
                dateKey = d.Value.ToLongDateString()
            }).ToList();
            //var dueDates2 = assignments2.Where(a => a.DueDate != null).Select(a => a.DueDate).Distinct().Select(d => new
            //{
            //    date1 = UtilitiesManager.GetFieldLabel(Locale, d.Value.DayOfWeek.ToString()) + ", " + UtilitiesManager.GetFieldLabel(Locale, d.Value.ToString("MMMM")) + " " + d.Value.Day + ", " + d.Value.Year,
            //    dateKey = d.Value.ToLongDateString()
            //}).ToList();

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
            ViewBag.CurrentLanguageIdentifier = CurrentLanguageIdentifier;
            ViewBag.DueDates = new SelectList(dueDates, "dateKey", "date1");

            return View(assignmentCardViewModel.Take(10));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AssignmentsFilter(string query = null, int? records = null, int? gradeKey = null, int? subjectKey = null, DateTime? dueDate = null)
        {
            try
            {


                int numberOfRecords = (int)(records == null ? 10 : records);
                List<Assignment> assignments = new List<Assignment>();
                List<Assignment> assignmentstudent = new List<Assignment>();
                var currentuser = await db.AspNetUsers.Where(a => a.Id == CurrentUser.Id).FirstOrDefaultAsync();
                int? key = 0;
                int? schoolkey = 0;
                //DateTime? dueDateValue = new DateTime();
                //if (dueDate!=null && dueDate != "")
                //{
                //    dueDateValue = DateTime.ParseExact(dueDate.ToString(), "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);
                //}
                IQueryable<Assignment> userAssignments;

                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
                {
                    //userAssignments = db.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey).OrderByDescending(a => a.TimeModifed);

                    var currentStudent = await db.AspNetUsers.Where(a => a.Id == CurrentUser.Id).ToListAsync();
                    var student = currentStudent.FirstOrDefault().Student;
                    var studentAssTimeline = await db.Timelines.Where(ut => ut.UserKey == CurrentUser.Id).Select(ass => ass.TimeLineEntityKey).ToListAsync();

                    assignmentstudent = await db.Assignments.Where(a => a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && studentAssTimeline.Contains(a.AssignmentKey)).OrderByDescending(a => a.TimeModifed).ToListAsync();

                }
                else
                {
                    // var currentStudent = await db.AspNetUsers.Where(a => a.Id == CurrentUser.Id).FirstOrDefaultAsync();
                    //     var teach = await db.SchoolSubjectTeacherGrades.Where(t => t.TeacherKey == currentStudent.TeacherKey).FirstOrDefaultAsync();
                    //     var anystudent = await db.Students.Where(s => s.SchoolKey == teach.SchoolKey && s.GradeKey == teach.GradeKey).FirstOrDefaultAsync();
                    // var student = anystudent;
                    ////     var student = currentStudent.FirstOrDefault().Student;
                    //     var studentAssTimeline = await db.Timelines.Where(ut => ut.UserKey == CurrentUser.Id).Select(ass => ass.TimeLineEntityKey).ToListAsync();

                    // userAssignments =  db.Assignments.Where(a => a.SchoolSubjectTeacherGrade.SchoolKey == student.SchoolKey && a.SchoolSubjectTeacherGrade.GradeKey == student.GradeKey && studentAssTimeline.Contains(a.AssignmentKey)).OrderByDescending(a => a.TimeModifed);


                    assignmentstudent = await db.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == CurrentUser.TeacherKey).OrderByDescending(a => a.TimeModifed).ToListAsync();
                }

                //    assignments = await userAssignments.Where(a =>
                //  ((gradeKey != null && (a.SchoolSubjectTeacherGrade.Grade.GradeKey == gradeKey)) || (gradeKey == null))
                //  && ((subjectKey != null && (a.SchoolSubjectTeacherGrade.Subject.SubjectKey == subjectKey)) || (subjectKey == null))
                //  && ((dueDate != null && (EntityFunctions.TruncateTime(a.DueDate) == EntityFunctions.TruncateTime(dueDate))) || (dueDate == null))
                //  //&& ((dueDate != "" && (a.DueDate == dueDateValue)) || (dueDate == ""))
                //  && ((query != null && (a.AssignmentTitle.Contains(query))) || (query == null))
                //).Take(numberOfRecords).ToListAsync();


                var queryableAssignments = assignmentstudent.AsQueryable();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    queryableAssignments = queryableAssignments
                       .Where(a => a.AssignmentTitle.ToLower().Contains(query.ToLower()));
                }

                if (gradeKey != null)
                {
                    queryableAssignments = queryableAssignments
                        .Where(a => a.SchoolSubjectTeacherGrade.GradeKey == gradeKey);
                }

                if (subjectKey != null)
                {
                    queryableAssignments = queryableAssignments
                        .Where(a => a.SchoolSubjectTeacherGrade.SubjectKey == subjectKey);
                }

                if (dueDate != null)
                {
                    var dateOnly = dueDate.Value.Date;
                    queryableAssignments = queryableAssignments
                        .Where(a => a.DueDate.HasValue && a.DueDate.Value.Date == dateOnly);
                }


                // Now finally fetch the list
                assignments = queryableAssignments
     .OrderBy(a => a.SchoolSubjectTeacherGrade.GradeKey)
     .Take(numberOfRecords)
     .ToList();  // synchronous

                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Assignments", "Index", CurrentLanguageIdentifier);
                if (assignments.Count() == 0)
                {
                    return PartialView("_EmptyAssignments");
                }
                string userkey = CurrentUser.Id;
                List<Models.AssignmentCardViewModel> assignmentCardViewModel = new List<Models.AssignmentCardViewModel>();
                //  string remoteId = CurrentUser.RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle"&&r.Userkey==userkey).OrderByDescending(r => r.RemoteAuthenticationKey).FirstOrDefault().EsystemRemoteId;

                string remoteId = await db.RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == userkey).OrderByDescending(r => r.RemoteAuthenticationKey).Select(s => s.EsystemRemoteId).FirstOrDefaultAsync();


                using (moodleEntities dbm = new moodleEntities())
                {
                    var assignmentIds = assignments
        .Where(a => a.AssignmentId.HasValue)
        .Select(a => (long)a.AssignmentId.Value)
        .ToList();

                    //var assignmentIds = new List<long> { 6503L, 6492L, 5962L };
                    // var assignmentIds2 = new List<int> { 6503, 6492, 5962 };
                    long studentId = 0;
                    List<(long AssignmentId, decimal? Grade, string Status, decimal? MaxGrade)> gradesAndStatuses = null;
                    List<(long AssignmentId, int SubmissionCount)> submissionCounts = null;
                    List<(long AssignmentId, int StudentCount)> studentCounts = null;

                    if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
                    {
                        if (!long.TryParse(remoteId, out studentId))
                        {
                            // If conversion fails, handle it here, for example:
                            throw new ArgumentException("Invalid student ID format.");
                        }
                        gradesAndStatuses = MoodleManager.GetGradesAndStatuses(assignmentIds, studentId);
                    }

                    if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher")
                    {
                        submissionCounts = MoodleManager.GetSubmissionCounts(assignmentIds);
                        //  var submissionCounts = GetSubmissionCounts3(assignmentIds);
                        studentCounts = MoodleManager.GetStudentCounts(assignmentIds);
                    }



                    foreach (RLI.EntityFramework.EDM.Assignment a in assignments)
                    {
                        Models.AssignmentCardViewModel currentAssignmentCardViewModel = new Models.AssignmentCardViewModel();

                        currentAssignmentCardViewModel.AssignmentId = a.AssignmentId;
                        currentAssignmentCardViewModel.AssignmentKey = a.AssignmentKey;
                        currentAssignmentCardViewModel.AssignmentTitle = a.AssignmentTitle;
                        currentAssignmentCardViewModel.CourseId = a.CourseId;
                        currentAssignmentCardViewModel.CourseLongName = a.CourseLongName;
                        currentAssignmentCardViewModel.CourseModuleID = a.CourseModuleID;
                        currentAssignmentCardViewModel.CourseTitle = a.CourseTitle;
                        currentAssignmentCardViewModel.CreatedAt = a.CreatedAt;
                        currentAssignmentCardViewModel.Description = a.Description;
                        currentAssignmentCardViewModel.DueDate = a.DueDate;
                        currentAssignmentCardViewModel.SchoolSubjectTeacherGrade = a.SchoolSubjectTeacherGrade;
                        currentAssignmentCardViewModel.SchoolSubjectTeacherGradeKey = a.SchoolSubjectTeacherGradeKey;
                        currentAssignmentCardViewModel.TimeModifed = a.TimeModifed;
                        try
                        {
                            //  currentAssignmentCardViewModel.Submissions = RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher" ? (await RLI.Common.Managers.SubmissionsManager.GetAssignGradingSummary(a.AssignmentId.ToString(), HttpContext)).SubmissionsSubmittedCount.ToString() : "0";

                            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Teacher")
                            {
                                var submissionResult = submissionCounts.FirstOrDefault(q => q.AssignmentId == a.AssignmentId);

                                var participantResult = studentCounts.FirstOrDefault(q => q.AssignmentId == a.AssignmentId);

                                currentAssignmentCardViewModel.Submissions = submissionResult.AssignmentId != 0
        ? submissionResult.SubmissionCount.ToString()
        : "0";

                                currentAssignmentCardViewModel.participantCount = participantResult.AssignmentId != 0
         ? participantResult.StudentCount.ToString()
         : "0";
                            }
                            else
                            {
                                currentAssignmentCardViewModel.Submissions = "0";
                                currentAssignmentCardViewModel.participantCount = "0";
                            }
                        }
                        catch (Exception e)
                        {
                            currentAssignmentCardViewModel.Submissions = "error";
                        }

                        currentAssignmentCardViewModel.Grade = "";
                        currentAssignmentCardViewModel.Status = "";
                        RLI.Common.DataObjects.SubmissionDetailsViewModel submissionDetailsViewModel = new RLI.Common.DataObjects.SubmissionDetailsViewModel();
                        //if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
                        //{
                        //    submissionDetailsViewModel = await SubmissionsManager.GetAssignSubmissionDetails(a.AssignmentId.ToString(), remoteId, HttpContext);
                        //    if (submissionDetailsViewModel != null)
                        //    {
                        //        currentAssignmentCardViewModel.Grade = submissionDetailsViewModel != null && submissionDetailsViewModel.Grade == null ? "" : submissionDetailsViewModel.Grade;
                        //        currentAssignmentCardViewModel.Status = submissionDetailsViewModel != null && submissionDetailsViewModel.GradingStatus == null ? "" : submissionDetailsViewModel.GradingStatus;
                        //    }
                        //}
                        if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
                        {
                            var gs = gradesAndStatuses.FirstOrDefault(x => x.AssignmentId == a.AssignmentId);

                            if (gs != default)
                            {
                                // Format grade as "75.00 / 100.00"
                                if (gs.Grade.HasValue && gs.MaxGrade.HasValue)
                                {
                                    currentAssignmentCardViewModel.Grade =
                                        $"{gs.Grade.Value.ToString("0.00")}&nbsp;/&nbsp;{gs.MaxGrade.Value.ToString("0.00")}";
                                }
                                else
                                {
                                    currentAssignmentCardViewModel.Grade = "";
                                }

                                // Standardize status to either "graded" or "notgraded"
                                currentAssignmentCardViewModel.Status = gs.Grade.HasValue ? "graded" : "notgraded";
                            }
                            else
                            {
                                currentAssignmentCardViewModel.Grade = "";
                                currentAssignmentCardViewModel.Status = "notgraded"; // Default to notgraded if no record exists
                            }
                        }
                        assignmentCardViewModel.Add(currentAssignmentCardViewModel);
                    }

                }
                return PartialView("_AssignmentCard", assignmentCardViewModel.Take(50));
            }
            catch (Exception e)
            {
                string t = e.ToString();
                return PartialView("_AssignmentCard", "");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [MoodleToken]
        [Authorization(Roles = "Teacher")]
        public async Task<ActionResult> DeleteAssignment(int assignmentId, int courseId, int courseModule)
        {
            DeleteAssignmentViewModel deleteAssignmentViewModel = new DeleteAssignmentViewModel();
            deleteAssignmentViewModel.AssignmentId = assignmentId;
            deleteAssignmentViewModel.CourseId = courseId;
            deleteAssignmentViewModel.CourseModule = courseModule;
            try
            {
                string deleteResult = await RLI.Common.Managers.MoodleManager.DeleteAssignment(HttpContext, deleteAssignmentViewModel);
            }
            catch (Exception e)
            {
                return Json(404);
            }

            return Json(200);
        }

        public async Task<ActionResult> IndexTest()
        {
            return View();
        }

        [MoodleToken]
        [MoodleCookie]
        [MoodleAdmin]
        [Authorization(Roles = "Administrator,Teacher")]
        public async Task<ActionResult> Create(int? assigId, int? lessonId, string lessonName = null, string studentsListSelected = null, int? gradeKey = null, int? schoolKey = null)
        {
            List<string> studentsListSelectedFromResources = new List<string>();
            if (studentsListSelected != null)
            {
                studentsListSelectedFromResources = JsonConvert.DeserializeObject<List<string>>(studentsListSelected);
            }
            List<RLI.EntityFramework.EDM.Grade> gradesSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey != null).Select(sstg => sstg.Grade).Distinct().ToListAsync();
            List<Subject> subjectsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.SubjectKey != null).Select(sstg => sstg.Subject).Distinct().ToListAsync();
            List<School> schoolsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.SchoolKey != null).Select(sstg => sstg.School).Distinct().ToListAsync();

            ViewBag.assigId = assigId;
            ViewBag.isAutoCorrected = "False";
            if (lessonId != null)
            {
                int id = (int)lessonId;
                ViewBag.LessonId = id;
                ViewBag.LessonText = lessonName;
                var currentLesson = await db.Lessons.FindAsync(lessonId);

                if (currentLesson.ESystem.ESystemName == "Moodle" && (currentLesson.LessonType.LessonType1 == "Interactive" || currentLesson.LessonType.LessonType1 == "Scorm"))
                {
                    ViewBag.isAutoCorrected = "True";
                }

            }
            if (assigId == null)
            {
                //List<MoodleUserCourseModel> userCourseList = await RLI.Common.Managers.AssignmentsManager.GetUserCourses(CurrentUser.Id, HttpContext);
                // ViewBag.UserCoursesDropDown = new SelectList(userCourseList, "id", "displayname");
                ViewBag.Grades = new SelectList(gradesSstg.OrderBy(g => g.GradeIndex).Select(g => new
                {
                    GradeKey = g.GradeKey,
                    DefaultGrade1 = g.Grade1,
                    Grade1 = CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault() == null ? g.Grade1 : CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    GradeIndex = g.GradeIndex,
                    LocalGradeGUID = g.LocalGradeGUID
                }), "GradeKey", "Grade1");
                ViewBag.Subjects = new SelectList(subjectsSstg.OrderBy(s => s.SubjectIndex).Select(g => new
                {
                    SubjectKey = g.SubjectKey,
                    DefaultSubject1 = g.Subject1,
                    Subject1 = CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault() == null ? g.Subject1 : CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    SubjectIndex = g.SubjectIndex,
                    LocalSubjectGUID = g.LocalSubjectGUID,
                    Chapters = g.Chapters
                }), "SubjectKey", "Subject1");

                int firstSchoolKey = schoolsSstg.OrderBy(s => s.SchoolKey).Select(s => s.SchoolKey).FirstOrDefault();
                ViewBag.Schools = new SelectList(schoolsSstg, "SchoolKey", "SchoolName", firstSchoolKey);

                string currentDateTime = DateTime.Now.ToString("dd/MM/yyyy");

                AssigViewModel assignmentViewModel = new AssigViewModel();



                assignmentViewModel.GradeToPass = 50;
                assignmentViewModel.IsCreatePage = true;
                assignmentViewModel.FileTypes = new List<string>();
                assignmentViewModel.MaxSubSize = 100000000;
                assignmentViewModel.MaxFilesNumber = 20;
                assignmentViewModel.EnableWordLimit = false;
                assignmentViewModel.WordLimit = null;
                assignmentViewModel.FilesSubmission = true;
                assignmentViewModel.OnlineText = true;
                assignmentViewModel.RemindMeToGradeBy = "";
                assignmentViewModel.DueDate = "";
                assignmentViewModel.AllowSubmissionFrom = currentDateTime;
                assignmentViewModel.Attachements = UtilitiesManager.RandomNumericString(6);
                assignmentViewModel.DescAttachementsId = UtilitiesManager.RandomNumericString(6);
                assignmentViewModel.AssignmetName = "";
                assignmentViewModel.Description = "";
                assignmentViewModel.GradeKey = ((gradeKey == null ? 0 : (int)gradeKey));
                assignmentViewModel.SubjectKey = 0;
                assignmentViewModel.SchoolKey = ((schoolKey == null ? 0 : (int)schoolKey));
                assignmentViewModel.HideFromStudents = false;
                assignmentViewModel.IntroAttachments = new List<RLI.Common.DataObjects.Introattachment>();
                if (studentsListSelected == null)
                {
                    assignmentViewModel.SelectedStudentsList = new List<string>();
                }
                else
                {
                    assignmentViewModel.SelectedStudentsList = studentsListSelectedFromResources;
                }
                assignmentViewModel.StudentsList = new List<Models.StudentsEnrolledViewModel>();
                return View(assignmentViewModel);
            }
            else
            {
                RLI.EntityFramework.EDM.Assignment assignment1 = await db.Assignments.FindAsync(assigId);
                ViewBag.Grades = new SelectList(gradesSstg.OrderBy(g => g.GradeIndex).Select(g => new
                {
                    GradeKey = g.GradeKey,
                    DefaultGrade1 = g.Grade1,
                    Grade1 = CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    GradeIndex = g.GradeIndex,
                    LocalGradeGUID = g.LocalGradeGUID
                }), "GradeKey", "Grade1", assignment1.SchoolSubjectTeacherGrade.GradeKey);
                ViewBag.Subjects = new SelectList(subjectsSstg.OrderBy(s => s.SubjectIndex).Select(g => new
                {
                    SubjectKey = g.SubjectKey,
                    DefaultSubject1 = g.Subject1,
                    Subject1 = CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    SubjectIndex = g.SubjectIndex,
                    LocalSubjectGUID = g.LocalSubjectGUID,
                    Chapters = g.Chapters
                }), "SubjectKey", "Subject1", assignment1.SchoolSubjectTeacherGrade.SubjectKey);

                ViewBag.Schools = new SelectList(schoolsSstg, "SchoolKey", "SchoolName", assignment1.SchoolSubjectTeacherGrade.SchoolKey);

                RLI.Common.DataObjects.CourseModuleResultModel assignInfoByCMId = await MoodleManager.GetAssignmentInfoByCMId((int)assignment1.CourseModuleID, false, HttpContext);
                RLI.Common.DataObjects.Assignment assignment2 = await MoodleManager.GetAssignmentInfoByAssId((int)assignment1.CourseId, (int)assignment1.AssignmentId, HttpContext);
                RLI.Common.DataObjects.Assignment assignment = GetAssignmentWithConfigs((int)assignment1.AssignmentId);
                string json1 = JsonConvert.SerializeObject(assignment, Formatting.Indented);
                string json2 = JsonConvert.SerializeObject(assignment2, Formatting.Indented);

                List<Config> configs = assignment.configs;
                var online = configs.Where(c => c.plugin == "onlinetext");
                var file = configs.Where(c => c.plugin == "file");
                string onlineEn = online.Where(c => c.name == "enabled").Select(c => c.value).FirstOrDefault();

                bool onlineEnabled = onlineEn == "1" ? true : false;
                string wordLimitString = configs.Where(c => c.name == "wordlimit").Select(c => c.value).FirstOrDefault();
                int wordLimit = 0;
                if (wordLimitString != null)
                {
                    wordLimit = int.Parse(wordLimitString);
                }

                string wordLimitEnString = configs.Where(c => c.name == "wordlimitenabled").Select(c => c.value).FirstOrDefault();
                int wordLimitEn = 0;
                if (wordLimitEnString != null)
                {
                    wordLimitEn = int.Parse(wordLimitEnString);
                }
                bool wordLimitEnabled = wordLimitEn == 1 ? true : false;
                string fileEn = file.Where(c => c.name == "enabled").Select(c => c.value).FirstOrDefault();
                bool fileEnabled = fileEn == "1" ? true : false;
                string maxFile = file.Where(c => c.name == "maxfilesubmissions").Select(c => c.value).FirstOrDefault();
                int maxFileSub = 0;
                if (maxFile != null)
                {
                    maxFileSub = int.Parse(maxFile);
                }

                string maxSubBytesString = file.Where(c => c.name == "maxsubmissionsizebytes").Select(c => c.value).FirstOrDefault();
                int maxSubBytes = 0;
                if (maxSubBytesString != null)
                {
                    maxSubBytes = int.Parse(file.Where(c => c.name == "maxsubmissionsizebytes").Select(c => c.value).FirstOrDefault());
                }
                string fileTypesList = file.Where(c => c.name == "filetypeslist").Select(c => c.value).FirstOrDefault();

                string attachmentsID = await RLI.Common.Managers.AssignmentsManager.GetIntroAttachments(HttpContext, assignment1.CourseModuleID.ToString());
                string descAttachmentsID = await RLI.Common.Managers.AssignmentsManager.GetIntroDesc(HttpContext, assignment1.CourseModuleID.ToString());
              //  descAttachmentsID = "98596667";
                Dictionary<string, object> fileextensions = new Dictionary<string, object>();
                string[] parentExtensions = { };

                List<string> selectedStudentsEnrolled = await GetStudentsEnrolledSelected(assignment1.CourseId, assignment1.CourseModuleID);
                List<CleverOwl.WebApplication.Models.StudentsEnrolledViewModel> allStudentsEnrolled = await GetAllStudentsEnrolled((int)assignment1.SchoolSubjectTeacherGrade.SchoolKey, (int)assignment1.SchoolSubjectTeacherGrade.GradeKey, (int)assignment1.SchoolSubjectTeacherGrade.SubjectKey);

                //mdl_grade_items gradeToPassMoodle = new mdl_grade_items();
                //using (moodleEntities moodleDB = new moodleEntities())
                //{
                //    gradeToPassMoodle = await moodleDB.mdl_grade_items.Where(gp => gp.iteminstance == assigId).FirstOrDefaultAsync();
                //    if (gradeToPassMoodle != null)
                //    {
                //        assignmentViewModel.GradeToPass = Convert.ToInt32(gradeToPassMoodle.gradepass);
                //    }
                //}

                AssigViewModel assignmentViewModel = new AssigViewModel();
                assignmentViewModel.IsCreatePage = false;
              //  assignmentViewModel.GradeToPass = (int)Convert.ToDecimal(assignInfoByCMId.cm.gradepass);
                assignmentViewModel.GradeToPass = (int)assignment.grade;
                assignmentViewModel.FileTypesStr = fileTypesList;
                assignmentViewModel.MaxSubSize = maxSubBytes;
                assignmentViewModel.MaxFilesNumber = maxFileSub;
                assignmentViewModel.EnableWordLimit = wordLimitEnabled;
                assignmentViewModel.WordLimit = wordLimit;
                assignmentViewModel.FilesSubmission = fileEnabled;
                assignmentViewModel.OnlineText = onlineEnabled;
                assignmentViewModel.RemindMeToGradeBy = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.gradingduedate).ToString("dd/MM/yyyy");
                assignmentViewModel.DueDate = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.duedate).ToString("dd/MM/yyyy");
                assignmentViewModel.AllowSubmissionFrom = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.allowsubmissionsfromdate).ToString("dd/MM/yyyy");
                assignmentViewModel.Attachements = attachmentsID;
                assignmentViewModel.DescAttachementsId = descAttachmentsID;
                assignmentViewModel.AssignmetName = assignment.name;
                assignmentViewModel.Description = assignment.intro.Replace("/webservice", "");
                assignmentViewModel.GradeKey = (int)assignment1.SchoolSubjectTeacherGrade.GradeKey;
                assignmentViewModel.SubjectKey = (int)assignment1.SchoolSubjectTeacherGrade.SubjectKey;
                assignmentViewModel.SchoolKey = (int)assignment1.SchoolSubjectTeacherGrade.SchoolKey;
                assignmentViewModel.HideFromStudents = false;
                assignmentViewModel.CourseModule = assignment1.CourseModuleID;
                assignmentViewModel.IntroAttachments = assignment.introattachments;
                assignmentViewModel.SelectedStudentsList = (selectedStudentsEnrolled == null ? new List<string>() : selectedStudentsEnrolled);
                assignmentViewModel.StudentsList = allStudentsEnrolled;

                ViewBag.assigmentID = assignment.id;
                ViewBag.assignmnentCourseId = assignment.course;
                ViewBag.assignmnentCourseModuleId = assignment.cmid;
                return View(assignmentViewModel);
            }
        }

        public static int? GetGradeToPass(int courseModuleId)
        {
            using (var db = new moodleEntities())
            {
                // Get the 'assign' module id
                var assignModuleId = db.mdl_modules
                                       .Where(m => m.name == "assign")
                                       .Select(m => m.id)
                                       .FirstOrDefault();

                // Join course_modules with assign
                var gradeQuery = from cm in db.mdl_course_modules
                                 join a in db.mdl_assign
                                     on cm.instance equals a.id
                                 where cm.id == courseModuleId && cm.module == assignModuleId
                                 select a.grade;

                var gradeValue = gradeQuery.FirstOrDefault();

                // grade in mdl_assign is long, convert safely to int
                return (int?)gradeValue;
            }
        }


        public static RLI.Common.DataObjects.Assignment GetAssignmentWithConfigs(int assignmentId)
        {
            using (var db = new moodleEntities())
            {
                var assignModuleId = db.mdl_modules
                                       .Where(m => m.name == "assign")
                                       .Select(m => m.id)
                                       .FirstOrDefault();

                // Query assignment + course module + plugin configs
                var query = from a in db.mdl_assign
                            join c in db.mdl_course_modules
                                on a.id equals c.instance
                            where a.id == assignmentId && c.module == assignModuleId
                            select new
                            {
                                a,
                                cmid = c.id,
                                configs = (from apc in db.mdl_assign_plugin_config
                                           where apc.assignment == a.id
                                           select new Config
                                           {
                                               plugin = apc.plugin,
                                               subtype = apc.subtype,
                                               name = apc.name,
                                               value = apc.value
                                           }).ToList()
                            };

                var result = query.FirstOrDefault();
                if (result == null) return null;

                // Map to your Assignment object
                var assignmentObj = new RLI.Common.DataObjects.Assignment
                {
                    id = (int)result.a.id,
                    course = (int)result.a.course,
                    cmid = (int)result.cmid,
                    name = result.a.name,
                    intro = result.a.intro,
                    introformat = result.a.introformat,
                    nosubmissions = result.a.nosubmissions,
                    submissiondrafts = result.a.submissiondrafts,
                    sendnotifications = result.a.sendnotifications,
                    sendlatenotifications = result.a.sendlatenotifications,
                    sendstudentnotifications = result.a.sendstudentnotifications,
                    duedate = (int)result.a.duedate,
                    allowsubmissionsfromdate = (int)result.a.allowsubmissionsfromdate,
                    grade = (int)result.a.grade,
                    timemodified = (int)result.a.timemodified,
                    completionsubmit = (int)result.a.completionsubmit,
                    cutoffdate = (int)result.a.cutoffdate,
                    gradingduedate = (int)result.a.gradingduedate,
                    teamsubmission = result.a.teamsubmission,
                    requireallteammemberssubmit = result.a.requireallteammemberssubmit,
                    teamsubmissiongroupingid = (int)result.a.teamsubmissiongroupingid,
                    blindmarking = result.a.blindmarking,
                    hidegrader = result.a.hidegrader,
                    revealidentities = result.a.revealidentities,
                    attemptreopenmethod = result.a.attemptreopenmethod.ToString(),
                    maxattempts = result.a.maxattempts,
                    markingworkflow = result.a.markingworkflow,
                    markingallocation = result.a.markingallocation,
                    requiresubmissionstatement = result.a.requiresubmissionstatement,
                    preventsubmissionnotingroup = result.a.preventsubmissionnotingroup,
                    configs = result.configs
                };

                return assignmentObj;
            }
        }


        [MoodleToken]
        [MoodleCookie]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UploadRecordingFile(string fileBase64, int attachmentDescId)
        {
            try
            {
                string[] base64Array = fileBase64.Split(',');
                byte[] tab;
                Guid guid1;

                string base64 = base64Array[1];
                tab = Convert.FromBase64String(base64);
                guid1 = Guid.NewGuid();
                HttpPostedFileBase objFile = (HttpPostedFileBase)new MemoryPostedFile(tab, guid1.ToString());

                FileUploadModel fileUploadModel = new FileUploadModel();
                fileUploadModel.file = objFile;
                fileUploadModel.itemid = attachmentDescId.ToString();
                fileUploadModel.title = objFile.FileName;

                string result = await RLI.Common.Managers.MoodleManager.UploadFile(HttpContext, fileUploadModel);

                string userKey = CurrentUser.Id;
                RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == userKey).FirstOrDefaultAsync();
                string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;

                string userToken = await MoodleManager.requestTokenByMoodleUserId(currentMoodleUserId, HttpContext, userKey);

                DescriptionFileViewModel descriptionFileViewModel = JsonConvert.DeserializeObject<DescriptionFileViewModel>(result);
                descriptionFileViewModel.url = descriptionFileViewModel.url + "?token=" + userToken;
                return Json(descriptionFileViewModel);
            }
            catch (Exception e)
            {
                return Json(500);
            }
        }

        public async Task<ActionResult> Details()
        {
            return View();
        }
        public async Task<ActionResult> DueDateExtension()
        {
            return View();
        }

        public async Task<ActionResult> CreateTest()
        {
            return View();
        }

        [MoodleCookie]
        [HttpPost]
        public async Task<ActionResult> UploadFile(HttpPostedFileBase file, int attachmentId)
        {
            try
            {
                Stream imagePDF = PDFManager.GeneratePDF(file.FileName, file.InputStream);

                FileUploadModel fileUploadModel = new FileUploadModel();
                if (imagePDF != null)
                {
                    fileUploadModel.FileStream = imagePDF;
                    string filename = file.FileName.Split('.')[0];
                    fileUploadModel.FileName = filename + ".pdf";
                }
                fileUploadModel.file = file;
                fileUploadModel.itemid = attachmentId.ToString();
                fileUploadModel.title = file.FileName;
                await RLI.Common.Managers.MoodleManager.UploadFile(HttpContext, fileUploadModel);
                return Json("success");
            }
            catch (Exception e)
            {
                string error = e.Message.ToString();
                if (error == "Parameter is not valid.")
                {
                    FileUploadModel fileUploadModel = new FileUploadModel();
                    fileUploadModel.file = file;
                    fileUploadModel.itemid = attachmentId.ToString();
                    fileUploadModel.title = file.FileName;
                    string result = await RLI.Common.Managers.MoodleManager.UploadFile(HttpContext, fileUploadModel);
                    return Json("200");
                }
                else
                {
                    await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                    return Json("error");
                }
            }

        }

        [MoodleCookie]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UploadDescFile(HttpPostedFileBase file, int attachmentId)
        {
            string result = string.Empty;
            try
            {
                FileUploadModel fileUploadModel = new FileUploadModel();
                fileUploadModel.file = file;
                fileUploadModel.itemid = attachmentId.ToString();
                fileUploadModel.title = file.FileName;
                result = await RLI.Common.Managers.MoodleManager.UploadFile(HttpContext, fileUploadModel);
                return Json(result);
            }
            catch (Exception e)
            {
                return Json("error");
            }

        }

        [MoodleCookie]
        [HttpPost]
        [Authorization(Roles = "Teacher")]
        public async Task<ActionResult> DeleteFile(string fileName, int attachmentId)
        {
            try
            {
                string result = await RLI.Common.Managers.MoodleManager.DeleteAttachementFiles(HttpContext, attachmentId, fileName);
                return Json("success");
            }
            catch (Exception e)
            {
                return Json("error");
            }
        }
        [MoodleCookie]
        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> SubmitAssignment(string AssigData)
        {
            try
            {


                JavaScriptSerializer serializer = new JavaScriptSerializer();
                AssigViewModel assig = (RLI.WebApplication.Models.AssigViewModel)serializer.Deserialize(AssigData, typeof(AssigViewModel));

                DateTime dueDate = DateTime.ParseExact(assig.DueDate, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);
                DateTime allowSubDate = DateTime.ParseExact(assig.AllowSubmissionFrom, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);
                DateTime remindToGradeDate = DateTime.ParseExact(assig.RemindMeToGradeBy, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);
                string userKey = CurrentUser.Id;
                List<MoodleUserCourseModel> userCourseList = await RLI.Common.Managers.AssignmentsManager.GetUserCourses(userKey, HttpContext);

                var Sstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey).ToListAsync();
                int MoodleCourseId = (int)Sstg.Where(s => s.SchoolKey == assig.SchoolKey && s.SubjectKey == assig.SubjectKey && s.GradeKey == assig.GradeKey).Select(s => s.MoodleId).FirstOrDefault();
                int courseId = userCourseList.Where(c => c.id == MoodleCourseId).Select(c => c.id).FirstOrDefault();

                if (assig.SelectedStudentsList.Count() == 0)
                {
                    return Json(700);
                }

                AssignmentViewModel assigmnetViewModel = new AssignmentViewModel();
                assigmnetViewModel.CourseId = courseId;
                assigmnetViewModel.AssignmetName = assig.AssignmetName;
                assigmnetViewModel.Description = Encoding.UTF8.GetString(Convert.FromBase64String(assig.Description));
                assigmnetViewModel.AllowSubmissionFrom = allowSubDate;
                assigmnetViewModel.DueDate = dueDate;
                assigmnetViewModel.RemindMeToGradeBy = remindToGradeDate;
                assigmnetViewModel.OnlineText = assig.OnlineText;
                assigmnetViewModel.FilesSubmission = assig.FilesSubmission;
                assigmnetViewModel.WordLimit = assig.WordLimit;
                assigmnetViewModel.MaxFilesNumber = assig.MaxFilesNumber;
                assigmnetViewModel.MaxSubSize = assig.MaxSubSize;
                assigmnetViewModel.FileTypes = assig.FileTypes;
                assigmnetViewModel.GradeToPass = assig.GradeToPass;
                assigmnetViewModel.Attachements = assig.Attachements;
                assigmnetViewModel.DescriptionItemId = assig.DescAttachementsId;
                assigmnetViewModel.HideFromStudents = assig.HideFromStudents;
                assigmnetViewModel.EnableWordLimit = assig.EnableWordLimit;
                assigmnetViewModel.GradeKey = assig.GradeKey;
                assigmnetViewModel.SubjectKey = assig.SubjectKey;
                assigmnetViewModel.SchoolKey = assig.SchoolKey;
                assigmnetViewModel.UserKey = userKey;
                assigmnetViewModel.StudentsList = assig.SelectedStudentsList;
                assigmnetViewModel.SelectedLessons = (List<int?>)serializer.Deserialize(assig.SelectedLessons, typeof(List<int?>));


                var testAssign = await AssignmentsManager.CreateAssignment(assigmnetViewModel, HttpContext);
                if (!string.IsNullOrEmpty(testAssign))
                {
                    return Json(200);
                }
                else
                {
                    return Json(400);
                }
            }
            catch (Exception e)
            {
                return Json(e);
            }


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetGrades(int SchoolId)
        {
            List<RLI.EntityFramework.EDM.Grade> gradesSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.SchoolKey == SchoolId && sstg.GradeKey != null).Select(sstg => sstg.Grade).Distinct().ToListAsync();
            var grades = gradesSstg.OrderBy(g => g.GradeIndex).Select(g => new
            {
                GradeKey = g.GradeKey,
                DefaultGrade1 = g.Grade1,
                Grade1 = CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value == null ? g.Grade1 : CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                GradeIndex = g.GradeIndex,
                LocalGradeGUID = g.LocalGradeGUID
            }).Select(g => new
            {
                id = g.GradeKey,
                text = g.Grade1,

            }).ToList();
            return Json(grades);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetSubjects(int SchoolId, int gradeId)
        {
            List<RLI.EntityFramework.EDM.Subject> subjectsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.SchoolKey == SchoolId && sstg.GradeKey == gradeId && sstg.SubjectKey != null).Select(sstg => sstg.Subject).Distinct().ToListAsync();
            var grades = subjectsSstg.OrderBy(s => s.SubjectIndex).Select(g => new
            {
                SubjectKey = g.SubjectKey,
                DefaultSubject1 = g.Subject1,
                Subject1 = CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                SubjectIndex = g.SubjectIndex,
                LocalSubjectGUID = g.LocalSubjectGUID,
                Chapters = g.Chapters
            }).Select(g => new
            {
                id = g.SubjectKey,
                text = g.Subject1,

            }).ToList();
            return Json(grades);
        }

        [MoodleCookie]
        [MoodleToken]
        [HttpPost]
        [Authorization(Roles = "Teacher")]
        public async Task<ActionResult> EditAssignment(string EditAssigData, int assKey)
        {
            try
            {

                Assignment assignment1 = await db.Assignments.FindAsync(assKey);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                AssigViewModel assig = (AssigViewModel)serializer.Deserialize(EditAssigData, typeof(AssigViewModel));

                DateTime dueDate = DateTime.ParseExact(assig.DueDate, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);
                DateTime allowSubDate = DateTime.ParseExact(assig.AllowSubmissionFrom, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);
                DateTime remindToGradeDate = DateTime.ParseExact(assig.RemindMeToGradeBy, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);
                string cleanedInput = assig.SelectedLessons.Trim('[', ']');
                string[] stringArray = cleanedInput.Split(',');

                // Step 2: Remove double quotes and convert to integers
                List<int?> selectedLessonsInt = stringArray
                    .Select(s =>
                    {
                        if (int.TryParse(s.Trim('"').Trim(), out int result))
                            return (int?)result;  // Return nullable int if parsing succeeds
                        return (int?)null;  // Return null if parsing fails
                    })
                    .ToList();
                AssignmentViewModel assigmnetViewModel = new AssignmentViewModel();
                assigmnetViewModel.CourseId = (int)assignment1.CourseId;
                assigmnetViewModel.AssignmetName = assig.AssignmetName;
                assigmnetViewModel.CourseModule = assignment1.CourseModuleID;
                //assigmnetViewModel.Description = Encoding.UTF8.GetString(Convert.FromBase64String(assig.Description));
                assigmnetViewModel.Description = Encoding.UTF8.GetString(Convert.FromBase64String(assig.Description));
                assigmnetViewModel.AllowSubmissionFrom = allowSubDate;
                assigmnetViewModel.DueDate = dueDate;
                assigmnetViewModel.RemindMeToGradeBy = remindToGradeDate;
                assigmnetViewModel.OnlineText = assig.OnlineText;
                assigmnetViewModel.FilesSubmission = assig.FilesSubmission;
                assigmnetViewModel.WordLimit = assig.WordLimit;
                assigmnetViewModel.MaxFilesNumber = assig.MaxFilesNumber;
                assigmnetViewModel.MaxSubSize = assig.MaxSubSize;
                assigmnetViewModel.FileTypes = assig.FileTypes;
                assigmnetViewModel.GradeToPass = assig.GradeToPass;
                assigmnetViewModel.Attachements = assig.Attachements;
                assigmnetViewModel.DescriptionItemId = assig.DescAttachementsId;
                assigmnetViewModel.HideFromStudents = assig.HideFromStudents;
                assigmnetViewModel.EnableWordLimit = assig.EnableWordLimit;
                assigmnetViewModel.GradeKey = (int)assignment1.SchoolSubjectTeacherGrade.GradeKey;
                assigmnetViewModel.SubjectKey = (int)assignment1.SchoolSubjectTeacherGrade.SubjectKey;
                assigmnetViewModel.SchoolKey = (int)assignment1.SchoolSubjectTeacherGrade.SchoolKey;
                assigmnetViewModel.Instance = assignment1.AssignmentId;
                assigmnetViewModel.Update = assignment1.CourseModuleID;
                assigmnetViewModel.AssignmentType = (RLI.Common.Enums.AssignmentTypeEnum)assignment1.AssignmentTypeKey;
                assigmnetViewModel.SelectedLessons = selectedLessonsInt;
                assigmnetViewModel.StudentsList = assig.SelectedStudentsList;
                var testAssign = await AssignmentsManager.EditAssignment(assigmnetViewModel, HttpContext);
                if (!string.IsNullOrEmpty(testAssign))
                {
                    return Json(200);
                }
                else
                {
                    return Json(400);
                }

            }
            catch (Exception e)
            {
                return Json(e);
            }


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Search(string query)
        {
            if (query.Length < 3)
                return new HttpStatusCodeResult(HttpStatusCode.NoContent);
            //                                                            

            string textHtml1 = "<h6><i class='m-menu__link-icon ";
            string textHtml2 = " mr-1' style='font-size:2.5rem;'></i><small class='text-muted mr-4 ml-1'>In&nbsp;";
            string textHtml3 = "</small>&nbsp;<span style='font-size:1.5rem'>";
            string textHtml4 = "</span></h6>";

            string candidatesLink = Url.Action("Details", "Resources");
            string resourcesFilterLink = Url.Action("Index", "Resources");

            var lessons = db.Lessons.Where(c => c.Status.Status1 == "Approved" && c.ESystem.ESystemName != "Moodle").Where(c =>
                c.LessonName.ToLower().Contains(query)
             || c.Topic.Topic1.ToLower().Contains(query)
             || c.LessonObjectives.FirstOrDefault().Objective.Objective1.ToLower().Contains(query)
             || c.Topic.ChaptersTopics.FirstOrDefault().Chapter.Grade.Grade1.ToLower().Contains(query)).Select(c => new
             {
                 gradeIndex = c.Topic.ChaptersTopics.FirstOrDefault().Chapter.Grade.GradeIndex,
                 id = c.LessonKey.ToString(),
                 title = c.LessonName == null ? c.Topic.Topic1 == null ? c.LessonObjectives.FirstOrDefault().Objective.Objective1 : c.Topic.Topic1 : c.LessonName,
                 text = textHtml1 + "fa fa-book-open" + textHtml2 + "Resources" + textHtml3 + (c.LessonName == null ? c.Topic.Topic1 == null ? c.LessonObjectives.FirstOrDefault().Objective.Objective1 : c.Topic.Topic1 : c.LessonName) + textHtml4 + " " + c.ESystem.ESystemName,
                 // location = candidatesLink + "/" + c.LessonKey.ToString()
             });

            // var lessonObjectives = db.LessonObjectives.Where(t =>
            //    t.Objective.Objective1.ToLower().Contains(query)
            //).Select(c => new
            // {
            //     id = c.LessonKey.ToString(),
            //     title = c.Lesson.LessonName,
            //     text = textHtml1 + "fa fa-comments" + textHtml2 + "Topics" + textHtml3 + c.Lesson.LessonName + textHtml4,
            //     location = resourcesFilterLink + "/?topic=" + c.LessonKey.ToString()
            // });

            var subjects = db.Subjects.Where(s =>
                s.Subject1.ToLower().Contains(query)).Select(s => new
                {
                    id = s.SubjectKey.ToString(),
                    title = s.Subject1,
                    text = textHtml1 + "fab fa-discourse" + textHtml2 + "Subjects" + textHtml3 + s.Subject1 + textHtml4,
                    location = resourcesFilterLink + "/?subject=" + s.SubjectKey.ToString()
                });

            var grades = db.Grades.Where(g =>
                  g.Grade1.ToLower().Contains(query)).Select(g => new
                  {
                      id = g.GradeKey.ToString(),
                      title = g.Grade1,
                      text = textHtml1 + "fa fa-cubes" + textHtml2 + "Grades" + textHtml3 + g.Grade1 + textHtml4,
                      location = resourcesFilterLink + "/?subject=" + g.GradeKey.ToString()
                  });



            List<object> result = new List<object>();

            result.AddRange(await lessons.Distinct().OrderBy(c => c.gradeIndex).Take(20).ToListAsync());
            // result.AddRange(await lessonObjectives.Distinct().Take(10).ToListAsync());
            //result.AddRange(await subjects.Distinct().Take(10).ToListAsync());
            //result.AddRange(await grades.Distinct().Take(10).ToListAsync());

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SearchAutocorrectedLessons(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                return new HttpStatusCodeResult(HttpStatusCode.NoContent);

            query = query.Trim().ToLowerInvariant();

            var user = CurrentUser;
            int? schoolKey = null;
            int partnerTypeKey = 2;

            var sstg = await db.SchoolSubjectTeacherGrades
                .FirstOrDefaultAsync(t => t.TeacherKey == user.TeacherKey);
            schoolKey = sstg?.SchoolKey;

            string providersParamFinal = null;

            // Get eSystem keys for school
            if (schoolKey.HasValue)
            {
                var schoolEsystems = await db.SchoolsEsystems
                    .Where(s => s.SchoolKey == schoolKey &&
                                s.PartnerTypeKey == partnerTypeKey &&
                                (s.StatusKey == (int)RLI.Common.Enums.StatusEnum.StatusAssigned ||
                                 s.StatusKey == (int)RLI.Common.Enums.StatusEnum.StatusPendingToBeAssigned))
                    .ToListAsync();

                var esystemKeys = schoolEsystems
                    .Where(e => e.EsystemKey.HasValue)
                    .Select(e => e.EsystemKey.Value);

                if (esystemKeys.Any())
                    providersParamFinal = string.Join(",", esystemKeys);
            }

            // 🧠 Now call your stored procedure (from EF model)
            string LibType = "1"; // or "2" etc. depending on what type you want
            List<GetDigitalLibrary_Result> lessons = db
                .GetDigitalLibrary(null, null, null, null, null, 4, providersParamFinal, null, LibType)
                .ToList();

            // 🔍 Apply the text filter (because SP doesn’t handle it)
            var filteredLessons = lessons
                .Where(c =>
                    (c.lessonName != null && c.lessonName.ToLower().Contains(query)) ||
                    (c.Topic != null && c.Topic.ToLower().Contains(query)) )
                .OrderBy(c => c.GradeIndex)
                .Take(20)
                .ToList();

            // 🎨 Build your frontend text (same as before)
            string textHtml1 = "<h6><i class='m-menu__link-icon fa fa-book-open mr-1' style='font-size:2.5rem;'></i>";
            string textHtml2 = "<small class='text-muted mr-4 ml-1'>In&nbsp;Resources</small>&nbsp;";
            string textHtml3 = "<span style='font-size:1.5rem'>";
            string textHtml4 = "</span></h6>";

            string resourcesFilterLink = Url.Action("Index", "Resources");

            var result = filteredLessons.Select(c => new
            {
                id = c.LessonKey.ToString(),
                title = c.lessonName ?? c.Topic ?? c.ChapterName ?? c.Subject ?? c.Grade,
                text = textHtml1 + textHtml2 + textHtml3 + (c.lessonName ?? c.Topic ?? c.ChapterName ?? c.Subject ?? c.Grade) + textHtml4,
                location = resourcesFilterLink + "/?lesson=" + c.LessonKey
            }).ToList();

            return Json(result);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> InsertLessonCard(string lessonCardId)
        {
            try
            {
                var lessonCardIds = JsonConvert.DeserializeObject<List<int>>(lessonCardId);
                var selectedLesson = await db.Lessons.Where(l => lessonCardIds.Contains(l.LessonKey)).ToListAsync();
                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Library", "Index", CurrentLanguageIdentifier);
                return PartialView("~/Views/Library/_LessonsGrid.cshtml", selectedLesson);
            }
            catch (Exception e)
            {
                return Json(404);
            }



        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetStudentsEnrolled(int SchoolKey, int SubjectKey, int GradeKey)
        {
            List<CleverOwl.WebApplication.Models.StudentsEnrolledViewModel> studentsEnrolled = await GetAllStudentsEnrolled(SchoolKey, GradeKey, SubjectKey);
            if (studentsEnrolled == null)
            {
                return Json("404");
            }
            return Json(studentsEnrolled);
        }
        public async Task<List<string>> GetStudentsEnrolledSelected(int? courseId, int? courseModelId)
        {
            string groups = await MoodleManager.GetCourseGroups(courseId.ToString(), HttpContext);
            List<CourseGroupsModel> courseGroups = JsonConvert.DeserializeObject<List<CourseGroupsModel>>(groups);
            string availabilityConditionsHtmlEncode = await RLI.Common.Managers.AssignmentsManager.GetAvailabilityConditionsJson(HttpContext, courseModelId.ToString());
            if (availabilityConditionsHtmlEncode == "" || availabilityConditionsHtmlEncode == null)
            {
                return null;
            }
            string availabilityConditions = HttpUtility.HtmlDecode(availabilityConditionsHtmlEncode);
            Availabilityconditionsjson availabilityConditionsList = JsonConvert.DeserializeObject<Availabilityconditionsjson>(availabilityConditions);
            List<int> conditionsIdList = availabilityConditionsList.c.Select(i => i.id).ToList();
            List<string> studentsEnrolled = courseGroups.Where(g => conditionsIdList.Contains(g.id)).Select(g => g.idnumber).ToList();
            return studentsEnrolled;
        }

        public async Task<List<CleverOwl.WebApplication.Models.StudentsEnrolledViewModel>> GetAllStudentsEnrolled(int schoolkey, int gradeKey, int SubjectKey)
        {
            List<CleverOwl.WebApplication.Models.StudentsEnrolledViewModel> studentsEnrolled;
            int? sstgKey = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.SchoolKey == schoolkey && sstg.SubjectKey == SubjectKey && sstg.GradeKey == gradeKey && sstg.MoodleId != null).Select(sstg => sstg.SchoolSubjectTeacherGradeKey).FirstOrDefaultAsync();
            if (sstgKey == null)
            {
                return null;
            }
            //Dictionary<int?, string> studentsEnrolled = await db.StudentEnrolments.Where(s => s.SchoolSubjectTeacherGradeKey == sstgKey && s.StatusKey == 2).Distinct().ToDictionaryAsync(s => s.StudentKey, s => s.Student.FirstName + " " + s.Student.MiddleName + " " + s.Student.LastName);
            studentsEnrolled = await db.StudentEnrolments.Where(s => s.SchoolSubjectTeacherGradeKey == sstgKey && s.StatusKey == 2).Select(s => new CleverOwl.WebApplication.Models.StudentsEnrolledViewModel()
            {
                StudentKey = s.StudentKey.ToString(),
                StudentName = s.Student.FirstName + " " + s.Student.MiddleName + " " + s.Student.LastName,
            }).Distinct().ToListAsync();

            if (studentsEnrolled.Count() == 0)
            {
                return null;
            }
            return studentsEnrolled;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GetCheckedStudentsOnCreating(List<int?> studentKeys)
        {
            var checkedStudentsInfo = new List<CleverOwl.WebApplication.Models.CheckedStudentInfo>();
            if (studentKeys.Count() > 0)
            {
                var checkedStudents = db.AspNetUsers.Where(s => studentKeys.Contains(s.Studentkey)).ToList();
                foreach (var checkedStudent in checkedStudents)
                {
                    var studentInfo = new CleverOwl.WebApplication.Models.CheckedStudentInfo
                    {
                        id = checkedStudent.Studentkey.ToString(),
                        username = checkedStudent.UserName,
                        metadata = new Dictionary<string, string>
                {
                    { "firstName", checkedStudent.FirstName },
                    { "lastName", checkedStudent.LastName }
                }
                    };
                    checkedStudentsInfo.Add(studentInfo);
                }

            }

            return Json(checkedStudentsInfo, JsonRequestBehavior.AllowGet);
        }


        [MoodleToken]
        [MoodleCookie]
        [MoodleAdmin]
        [Authorization(Roles = "Administrator,Teacher")]
        public async Task<ActionResult> Reassign(int? assigId, int? lessonId, string lessonName = null, string studentsListSelected = null, int? gradeKey = null, int? schoolKey = null)
        {
            List<string> studentsListSelectedFromResources = new List<string>();
            if (studentsListSelected != null)
            {
                studentsListSelectedFromResources = JsonConvert.DeserializeObject<List<string>>(studentsListSelected);
            }
            List<RLI.EntityFramework.EDM.Grade> gradesSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.GradeKey != null).Select(sstg => sstg.Grade).Distinct().ToListAsync();
            List<Subject> subjectsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.SubjectKey != null).Select(sstg => sstg.Subject).Distinct().ToListAsync();
            List<School> schoolsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey && sstg.SchoolKey != null).Select(sstg => sstg.School).Distinct().ToListAsync();

            ViewBag.assigId = assigId;
            ViewBag.isAutoCorrected = "False";
            if (lessonId != null)
            {
                int id = (int)lessonId;
                ViewBag.LessonId = id;
                ViewBag.LessonText = lessonName;
                var currentLesson = await db.Lessons.FindAsync(lessonId);

                if (currentLesson.ESystem.ESystemName == "Moodle" && (currentLesson.LessonType.LessonType1 == "Interactive" || currentLesson.LessonType.LessonType1 == "Scorm"))
                {
                    ViewBag.isAutoCorrected = "True";
                }

            }
            if (assigId == null)
            {
                //List<MoodleUserCourseModel> userCourseList = await RLI.Common.Managers.AssignmentsManager.GetUserCourses(CurrentUser.Id, HttpContext);
                // ViewBag.UserCoursesDropDown = new SelectList(userCourseList, "id", "displayname");
                ViewBag.Grades = new SelectList(gradesSstg.OrderBy(g => g.GradeIndex).Select(g => new
                {
                    GradeKey = g.GradeKey,
                    DefaultGrade1 = g.Grade1,
                    Grade1 = CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault() == null ? g.Grade1 : CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    GradeIndex = g.GradeIndex,
                    LocalGradeGUID = g.LocalGradeGUID
                }), "GradeKey", "Grade1");
                ViewBag.Subjects = new SelectList(subjectsSstg.OrderBy(s => s.SubjectIndex).Select(g => new
                {
                    SubjectKey = g.SubjectKey,
                    DefaultSubject1 = g.Subject1,
                    Subject1 = CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault() == null ? g.Subject1 : CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    SubjectIndex = g.SubjectIndex,
                    LocalSubjectGUID = g.LocalSubjectGUID,
                    Chapters = g.Chapters
                }), "SubjectKey", "Subject1");

                int firstSchoolKey = schoolsSstg.OrderBy(s => s.SchoolKey).Select(s => s.SchoolKey).FirstOrDefault();
                ViewBag.Schools = new SelectList(schoolsSstg, "SchoolKey", "SchoolName", firstSchoolKey);

                string currentDateTime = DateTime.Now.ToString("dd/MM/yyyy");

                AssigViewModel assignmentViewModel = new AssigViewModel();



                assignmentViewModel.GradeToPass = 50;
                assignmentViewModel.IsCreatePage = true;
                assignmentViewModel.FileTypes = new List<string>();
                assignmentViewModel.MaxSubSize = 100000000;
                assignmentViewModel.MaxFilesNumber = 20;
                assignmentViewModel.EnableWordLimit = false;
                assignmentViewModel.WordLimit = null;
                assignmentViewModel.FilesSubmission = true;
                assignmentViewModel.OnlineText = true;
                assignmentViewModel.RemindMeToGradeBy = "";
                assignmentViewModel.DueDate = "";
                assignmentViewModel.AllowSubmissionFrom = currentDateTime;
                assignmentViewModel.Attachements = UtilitiesManager.RandomNumericString(6);
                assignmentViewModel.DescAttachementsId = UtilitiesManager.RandomNumericString(6);
                assignmentViewModel.AssignmetName = "";
                assignmentViewModel.Description = "";
                assignmentViewModel.GradeKey = ((gradeKey == null ? 0 : (int)gradeKey));
                assignmentViewModel.SubjectKey = 0;
                assignmentViewModel.SchoolKey = ((schoolKey == null ? 0 : (int)schoolKey));
                assignmentViewModel.HideFromStudents = false;
                assignmentViewModel.IntroAttachments = new List<RLI.Common.DataObjects.Introattachment>();
                if (studentsListSelected == null)
                {
                    assignmentViewModel.SelectedStudentsList = new List<string>();
                }
                else
                {
                    assignmentViewModel.SelectedStudentsList = studentsListSelectedFromResources;
                }
                assignmentViewModel.StudentsList = new List<Models.StudentsEnrolledViewModel>();
                return View(assignmentViewModel);
            }
            else
            {
                RLI.EntityFramework.EDM.Assignment assignment1 = await db.Assignments.FindAsync(assigId);
                ViewBag.Grades = new SelectList(gradesSstg.OrderBy(g => g.GradeIndex).Select(g => new
                {
                    GradeKey = g.GradeKey,
                    DefaultGrade1 = g.Grade1,
                    Grade1 = CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    GradeIndex = g.GradeIndex,
                    LocalGradeGUID = g.LocalGradeGUID
                }), "GradeKey", "Grade1", assignment1.SchoolSubjectTeacherGrade.GradeKey);
                ViewBag.OldGradeKey = assignment1.SchoolSubjectTeacherGrade.GradeKey;
                ViewBag.Subjects = new SelectList(subjectsSstg.OrderBy(s => s.SubjectIndex).Select(g => new
                {
                    SubjectKey = g.SubjectKey,
                    DefaultSubject1 = g.Subject1,
                    Subject1 = CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    SubjectIndex = g.SubjectIndex,
                    LocalSubjectGUID = g.LocalSubjectGUID,
                    Chapters = g.Chapters
                }), "SubjectKey", "Subject1", assignment1.SchoolSubjectTeacherGrade.SubjectKey);

                ViewBag.Schools = new SelectList(schoolsSstg, "SchoolKey", "SchoolName", assignment1.SchoolSubjectTeacherGrade.SchoolKey);

                RLI.Common.DataObjects.CourseModuleResultModel assignInfoByCMId = await MoodleManager.GetAssignmentInfoByCMId((int)assignment1.CourseModuleID, false, HttpContext);
                RLI.Common.DataObjects.Assignment assignment = await MoodleManager.GetAssignmentInfoByAssId((int)assignment1.CourseId, (int)assignment1.AssignmentId, HttpContext);
                List<Config> configs = assignment.configs;
                var online = configs.Where(c => c.plugin == "onlinetext");
                var file = configs.Where(c => c.plugin == "file");
                string onlineEn = online.Where(c => c.name == "enabled").Select(c => c.value).FirstOrDefault();

                bool onlineEnabled = onlineEn == "1" ? true : false;
                string wordLimitString = configs.Where(c => c.name == "wordlimit").Select(c => c.value).FirstOrDefault();
                int wordLimit = 0;
                if (wordLimitString != null)
                {
                    wordLimit = int.Parse(wordLimitString);
                }

                string wordLimitEnString = configs.Where(c => c.name == "wordlimitenabled").Select(c => c.value).FirstOrDefault();
                int wordLimitEn = 0;
                if (wordLimitEnString != null)
                {
                    wordLimitEn = int.Parse(wordLimitEnString);
                }
                bool wordLimitEnabled = wordLimitEn == 1 ? true : false;
                string fileEn = file.Where(c => c.name == "enabled").Select(c => c.value).FirstOrDefault();
                bool fileEnabled = fileEn == "1" ? true : false;
                string maxFile = file.Where(c => c.name == "maxfilesubmissions").Select(c => c.value).FirstOrDefault();
                int maxFileSub = 0;
                if (maxFile != null)
                {
                    maxFileSub = int.Parse(maxFile);
                }

                string maxSubBytesString = file.Where(c => c.name == "maxsubmissionsizebytes").Select(c => c.value).FirstOrDefault();
                int maxSubBytes = 0;
                if (maxSubBytesString != null)
                {
                    maxSubBytes = int.Parse(file.Where(c => c.name == "maxsubmissionsizebytes").Select(c => c.value).FirstOrDefault());
                }
                string fileTypesList = file.Where(c => c.name == "filetypeslist").Select(c => c.value).FirstOrDefault();

                string attachmentsID = await RLI.Common.Managers.AssignmentsManager.GetIntroAttachments(HttpContext, assignment1.CourseModuleID.ToString());
                string descAttachmentsID = await RLI.Common.Managers.AssignmentsManager.GetIntroDesc(HttpContext, assignment1.CourseModuleID.ToString());

                Dictionary<string, object> fileextensions = new Dictionary<string, object>();
                string[] parentExtensions = { };

                List<string> selectedStudentsEnrolled = await GetStudentsEnrolledSelected(assignment1.CourseId, assignment1.CourseModuleID);
                List<CleverOwl.WebApplication.Models.StudentsEnrolledViewModel> allStudentsEnrolled = await GetAllStudentsEnrolled((int)assignment1.SchoolSubjectTeacherGrade.SchoolKey, (int)assignment1.SchoolSubjectTeacherGrade.GradeKey, (int)assignment1.SchoolSubjectTeacherGrade.SubjectKey);

                //mdl_grade_items gradeToPassMoodle = new mdl_grade_items();
                //using (moodleEntities moodleDB = new moodleEntities())
                //{
                //    gradeToPassMoodle = await moodleDB.mdl_grade_items.Where(gp => gp.iteminstance == assigId).FirstOrDefaultAsync();
                //    if (gradeToPassMoodle != null)
                //    {
                //        assignmentViewModel.GradeToPass = Convert.ToInt32(gradeToPassMoodle.gradepass);
                //    }
                //}

                AssigViewModel assignmentViewModel = new AssigViewModel();
                assignmentViewModel.IsCreatePage = false;
                assignmentViewModel.GradeToPass = (int)Convert.ToDecimal(assignInfoByCMId.cm.gradepass);
                assignmentViewModel.FileTypesStr = fileTypesList;
                assignmentViewModel.MaxSubSize = maxSubBytes;
                assignmentViewModel.MaxFilesNumber = maxFileSub;
                assignmentViewModel.EnableWordLimit = wordLimitEnabled;
                assignmentViewModel.WordLimit = wordLimit;
                assignmentViewModel.FilesSubmission = fileEnabled;
                assignmentViewModel.OnlineText = onlineEnabled;
                assignmentViewModel.RemindMeToGradeBy = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.gradingduedate).ToString("dd/MM/yyyy");
                assignmentViewModel.DueDate = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.duedate).ToString("dd/MM/yyyy");
                assignmentViewModel.AllowSubmissionFrom = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.allowsubmissionsfromdate).ToString("dd/MM/yyyy");
                assignmentViewModel.Attachements = attachmentsID;
                assignmentViewModel.DescAttachementsId = descAttachmentsID;
                assignmentViewModel.AssignmetName = assignment.name;
                assignmentViewModel.Description = assignment.intro.Replace("/webservice", "");
                assignmentViewModel.GradeKey = (int)assignment1.SchoolSubjectTeacherGrade.GradeKey;
                assignmentViewModel.SubjectKey = (int)assignment1.SchoolSubjectTeacherGrade.SubjectKey;
                assignmentViewModel.SchoolKey = (int)assignment1.SchoolSubjectTeacherGrade.SchoolKey;
                assignmentViewModel.HideFromStudents = false;
                assignmentViewModel.CourseModule = assignment1.CourseModuleID;
                assignmentViewModel.IntroAttachments = assignment.introattachments;
                assignmentViewModel.SelectedStudentsList = (selectedStudentsEnrolled == null ? new List<string>() : selectedStudentsEnrolled);
                assignmentViewModel.StudentsList = allStudentsEnrolled;

                ViewBag.assigmentID = assignment.id;
                ViewBag.assignmnentCourseId = assignment.course;
                ViewBag.assignmnentCourseModuleId = assignment.cmid;
                return View(assignmentViewModel);
            }
        }
    }
}