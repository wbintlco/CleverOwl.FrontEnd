using Microsoft.AspNetCore.Http;
using RLI.WebApplication.Objects;
using RLI.Common.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RLI.WebApplication.Attributes;
using Newtonsoft.Json;
using RLI.Common.Managers;
using System.Threading.Tasks;
using System.Data.Entity;
using RLI.EntityFramework.EDM;
using Assignment = RLI.Common.DataObjects.Assignment;
using Microsoft.AspNet.SignalR.Json;

namespace CleverOwl.WebApplication.Controllers
{
    [Authorize]
    public class StudentsSubmissionPageController : BaseController
    {
        [MoodleToken]
        [Authorize]
        // GET: StudentsSubmissionPage
        public async Task<ActionResult> Index(string assignmnentId = null)
        {
            if (assignmnentId == null) 
            {
                return HttpNotFound();
            }
            int? assigId = int.Parse(assignmnentId);
            List<GetAssignmentSubmissionsResultModel> assignmentSubmissions = await RLI.Common.Managers.SubmissionsManager.GetAssignSubmissions(assignmnentId, HttpContext);
            List<GetAssignmentParticipantsListResultModel> assignmentNotViewedStudents = await RLI.Common.Managers.SubmissionsManager.GetAssignNotViewedParticipants(assignmnentId, HttpContext);
            List<GetAssignmentSubmissionsResultModel> assignmentNotViewedList = new List<GetAssignmentSubmissionsResultModel>();
            if (assignmentNotViewedStudents.Count() > 0)
            {
                foreach (var student in assignmentNotViewedStudents)
                {
                    GetAssignmentSubmissionsResultModel assignmentNotViewed = new GetAssignmentSubmissionsResultModel();

                    assignmentNotViewed.AssignmentId = (int)assigId;
                    assignmentNotViewed.FullName = student.fullname;
                    assignmentNotViewed.Status = "notviewed";//this is a status we created and is not from moodle.
                    assignmentNotViewed.AssignmentId = student.id;

                    assignmentNotViewedList.Add(assignmentNotViewed);
                }

                assignmentSubmissions.AddRange(assignmentNotViewedList);
            }
            int courseId = (int)await db.Assignments.Where(a => a.AssignmentId == assigId).Select(a => a.CourseId).FirstOrDefaultAsync();
            RLI.Common.DataObjects.Assignment assignment = await MoodleManager.GetAssignmentInfoByAssId((int)courseId, (int)assigId, HttpContext);
            List<RLI.Common.DataObjects.Config> configs = assignment.configs;

            var online = configs.Where(c => c.plugin == "onlinetext");
            var file = configs.Where(c => c.plugin == "file");

            string fileEn = file.Where(c => c.name == "enabled").Select(c => c.value).FirstOrDefault();
            bool fileEnabled = fileEn == "1" ? true : false;

            string onlineEn = online.Where(c => c.name == "enabled").Select(c => c.value).FirstOrDefault();
            bool onlineEnabled = onlineEn == "1" ? true : false;

            ViewBag.assignmnentId = assignmnentId;
            string assigName = await db.Assignments.Where(a => a.AssignmentId == assigId).Select(a => a.AssignmentTitle).FirstOrDefaultAsync();
            ViewBag.AssignmentName = assigName;
            var assignment1 = await db.Assignments.Where(a => a.AssignmentId == assigId).FirstOrDefaultAsync();
            ViewBag.GradeKey = assignment1.SchoolSubjectTeacherGrade.GradeKey;
            ViewBag.SubjectKey = assignment1.SchoolSubjectTeacherGrade.SubjectKey;
            ViewBag.AssignmentName = assignment1.AssignmentTitle;
            return View(assignmentSubmissions);
        }

        [MoodleToken]
        [HttpPost]
        [ValidateAntiForgeryToken]
        // GET: StudentsSubmissionPage
        public async Task<ActionResult> SeachStudentsSubmissions(string assignmnentId = "437", string studentFullName = null, int? records = null)
        {
            List<GetAssignmentSubmissionsResultModel> assignmentSubmissions = await RLI.Common.Managers.SubmissionsManager.GetAssignSubmissions(assignmnentId, HttpContext);
            assignmentSubmissions = assignmentSubmissions.Where(a => a.FullName.ToLower().Contains(studentFullName)).Take((int)records).ToList();

            return PartialView("_StudentSubmissionsGrid", assignmentSubmissions);
        }

        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> StudentSubmissionDetails(string userId, string assignmentId, bool isGraded, int index, int submissionsSize, string submissionStatus)
        {
            SubmissionDetailsViewModel submissionDetailsViewModel = await SubmissionsManager.GetAssignSubmissionDetails(assignmentId, userId, HttpContext);
            AspNetUser aspNetUser = (await db.RemoteAuthentications.Where(r => r.EsystemRemoteId == userId).OrderByDescending(r=>r.RemoteAuthenticationKey).FirstOrDefaultAsync()).AspNetUser;
            string fullName = aspNetUser.FirstName + " " + aspNetUser.LastName;
            ViewBag.IsGraded = isGraded;
            ViewBag.Index = index;
            ViewBag.assignmentId = assignmentId;
            int assignmentIdInt = int.Parse(assignmentId);
            RLI.EntityFramework.EDM.Assignment assignment = await db.Assignments.Where(a => a.AssignmentId == assignmentIdInt).FirstOrDefaultAsync();
            List<StudentAssignmentReportDrillDown> studentAssignmentReportDrillDown = await SubmissionsManager.GetAssignmentAutoCorrectedReport(assignment.AssignmentKey, (int)aspNetUser.Studentkey, HttpContext);
            if (studentAssignmentReportDrillDown.Count() > 0)
            {
                var StudentAssignmentReportKey = studentAssignmentReportDrillDown.FirstOrDefault().StudentAssignmentReportKey;
                double ? totalscore = db.StudentAssignmentReports.Where(s => s.StudentAssignmentReportKey == StudentAssignmentReportKey).Select(s=>s.AutoCorrectedGrade).FirstOrDefault();
                ViewBag.TotalScore = totalscore.HasValue ? Math.Round(totalscore.Value, 2) : 0.0;
            }
            else
            {
                ViewBag.TotalScore = 0;
            }
            ViewBag.GradeKey = assignment.SchoolSubjectTeacherGrade.GradeKey;
            ViewBag.SubjectKey = assignment.SchoolSubjectTeacherGrade.SubjectKey;
            List<int?> lessonKey =await db.AssignmentLessons.Where(al => al.AssignmentKey == assignment.AssignmentKey).Select(l => l.LessonKey).ToListAsync();
            string lessonKeyJson = JsonConvert.SerializeObject(lessonKey);
            ViewBag.LessonKey = lessonKeyJson;
            var scormPagesInfo = studentAssignmentReportDrillDown.Select(s => new SubmissionDetailsViewModel.ScormPagesInfo
            {
                PageNumber = s.PageNumber.ToString(),
                PageTitle = s.PageTitle,
                PageGrade = s.Score.ToString()+"%",
                ErrorCount = s.ErrorCount.ToString(),
                MistakeCount = s.MistakeCount.ToString(),
                CheckCount = s.CheckCount.ToString()
            }).ToList();

            ViewBag.courseModuleId = assignment.CourseModuleID;
            ViewBag.AssignmentName = assignment.AssignmentTitle;
            ViewBag.userId = userId;
            ViewBag.fullName = fullName;
            ViewBag.SubmissionId = submissionDetailsViewModel.SubmissionId;

            ViewBag.SubmissionsSize = submissionsSize;
            ViewBag.SubmissionStatus = submissionStatus;

            int assigId = int.Parse(assignmentId);
            int courseId = (int)await db.Assignments.Where(a => a.AssignmentId == assigId).Select(a => a.CourseId).FirstOrDefaultAsync();
            int assignmentKey = (int)await db.Assignments.Where(a => a.AssignmentId == assigId).Select(a => a.AssignmentKey).FirstOrDefaultAsync();
            RLI.Common.DataObjects.Assignment assignment1 = await MoodleManager.GetAssignmentInfoByAssId((int)courseId, (int)assigId, HttpContext);
            List<RLI.Common.DataObjects.Config> configs = assignment1.configs;

            var online = configs.Where(c => c.plugin == "onlinetext");
            var file = configs.Where(c => c.plugin == "file");

            string fileEn = file.Where(c => c.name == "enabled").Select(c => c.value).FirstOrDefault();
            bool fileEnabled = fileEn == "1" ? true : false;

            string onlineEn = online.Where(c => c.name == "enabled").Select(c => c.value).FirstOrDefault();
            bool onlineEnabled = onlineEn == "1" ? true : false;

            submissionDetailsViewModel.FileSubmissionsEnabled = fileEnabled;
            submissionDetailsViewModel.OnlineTextEnabled = onlineEnabled;
            //ViewBag.SubmissionDate = assignment1.DueDate;
            submissionDetailsViewModel.scormPagesInfo = scormPagesInfo;
            int? LKey = await db.AssignmentLessons.Where(a => a.AssignmentKey == assignmentKey && a.Lesson.ESystemKey == 3021).Select(l => l.LessonKey).FirstOrDefaultAsync();
            string userKey = await db.RemoteAuthentications.Where(r => r.EsystemRemoteId == userId).OrderByDescending(r => r.RemoteAuthenticationKey).Select(u=>u.Userkey).FirstOrDefaultAsync();
            int? studentKey = await db.AspNetUsers.Where(i => i.Id == userKey).Select(s => s.Studentkey).FirstOrDefaultAsync();
            if(LKey != null)
            {
                CleverOwl.WebApplication.Controllers.AssignmentsSubmissionController.SynconizeLesson(LKey, userKey, assignmentKey);
            }
            else
            {
                CleverOwl.WebApplication.Controllers.AssignmentsSubmissionController.SynconizeAssignment(assigId, userId, studentKey, assignmentKey);
            }
            List<QualitativeFeedback_Result> qualitativeFeedback_Result = await SubmissionsManager.GetQualitativeFeeedback(assignmentKey, aspNetUser.Studentkey);
            ViewBag.qualitativeFeedback_Result = qualitativeFeedback_Result;
            return View(submissionDetailsViewModel);
        }
        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> GetNextNotGradedSubmission(string assigId, int currentIndex, string submissionStatus, string gradingStatus)
        {
            try
            {
                List<GetAssignmentSubmissionsResultModel> assignmentSubmissions = await SubmissionsManager.GetAssignSubmissions(assigId, HttpContext);
                List<GetAssignmentSubmissionsResultModel> submissions = new List<GetAssignmentSubmissionsResultModel>();
                if (submissionStatus == "submitted")
                {
                    submissions = assignmentSubmissions.Where(s => s.Status == submissionStatus && s.GradingStatus == gradingStatus).ToList();
                }
                else
                {
                    submissions = assignmentSubmissions.Where(s => s.Status == submissionStatus).ToList();
                }

                GetAssignmentSubmissionsResultModel nextSubmission = submissions.ElementAt(currentIndex + 1);
                SubmissionDetailsViewModel submissionDetails = await SubmissionsManager.GetAssignSubmissionDetails(nextSubmission.AssignmentId.ToString(), nextSubmission.UserId.ToString(), HttpContext);
                string fullName = nextSubmission.FullName;
                ViewBag.nextFullName = fullName;
                ViewBag.userId = nextSubmission.UserId.ToString();
                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("StudentsSubmissionPage", "StudentSubmissionDetails", CurrentLanguageIdentifier);
                
                int assignmentKey = (int)await db.Assignments.Where(a => a.AssignmentId == nextSubmission.AssignmentId).Select(a => a.AssignmentKey).FirstOrDefaultAsync();
                string userIdInMoodle = nextSubmission.UserId.ToString();
                AspNetUser aspNetUser = (await db.RemoteAuthentications.Where(r => r.EsystemRemoteId == userIdInMoodle && r.ESystem.ESystemName=="Moodle").OrderByDescending(r => r.RemoteAuthenticationKey).FirstOrDefaultAsync()).AspNetUser;

                List<QualitativeFeedback_Result> qualitativeFeedback_Result = await SubmissionsManager.GetQualitativeFeeedback(assignmentKey, aspNetUser.Studentkey);
                ViewBag.qualitativeFeedback_Result = qualitativeFeedback_Result;

                List<StudentAssignmentReportDrillDown> studentAssignmentReportDrillDown = await SubmissionsManager.GetAssignmentAutoCorrectedReport(assignmentKey, (int)aspNetUser.Studentkey, HttpContext);
                var scormPagesInfo = studentAssignmentReportDrillDown.Select(s => new SubmissionDetailsViewModel.ScormPagesInfo
                {
                    PageNumber = s.PageNumber.ToString(),
                    PageTitle = s.PageTitle,
                    PageGrade = s.Score.ToString() + "%",
                    ErrorCount = s.ErrorCount.ToString(),
                    MistakeCount = s.MistakeCount.ToString(),
                    CheckCount = s.CheckCount.ToString()
                }).ToList();

                submissionDetails.scormPagesInfo = scormPagesInfo;
                return PartialView("~/Views/StudentsSubmissionPage/_SubmissionDetails.cshtml", submissionDetails);
            }
            catch (Exception e)
            {
                await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                return Json("500");
            }
        }
        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> GetPreviousNotGradedSubmission(string assigID, int curIndex, string subStatus, string gradingStatus2)
        {
            try
            {
                List<GetAssignmentSubmissionsResultModel> assignmentSubmissions = await SubmissionsManager.GetAssignSubmissions(assigID, HttpContext);
                List<GetAssignmentSubmissionsResultModel> submissions = new List<GetAssignmentSubmissionsResultModel>();
                if (subStatus == "submitted")
                {
                    submissions = assignmentSubmissions.Where(s => s.Status == subStatus && s.GradingStatus == gradingStatus2).ToList();
                }
                else
                {
                    submissions = assignmentSubmissions.Where(s => s.Status == subStatus).ToList();
                }
                GetAssignmentSubmissionsResultModel previousSubmission = submissions.ElementAt(curIndex - 1);
                SubmissionDetailsViewModel submissionDetails = await SubmissionsManager.GetAssignSubmissionDetails(previousSubmission.AssignmentId.ToString(), previousSubmission.UserId.ToString(), HttpContext);
                string fullName = previousSubmission.FullName;
                ViewBag.nextFullName = fullName;
                ViewBag.userId = previousSubmission.UserId.ToString();
                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("StudentsSubmissionPage", "StudentSubmissionDetails", CurrentLanguageIdentifier);
                int assignmentKey = (int)await db.Assignments.Where(a => a.AssignmentId == previousSubmission.AssignmentId).Select(a => a.AssignmentKey).FirstOrDefaultAsync();
                string userIdInMoodle = previousSubmission.UserId.ToString();
                AspNetUser aspNetUser = (await db.RemoteAuthentications.Where(r => r.EsystemRemoteId == userIdInMoodle && r.ESystem.ESystemName == "Moodle").OrderByDescending(r => r.RemoteAuthenticationKey).FirstOrDefaultAsync()).AspNetUser;

                List<QualitativeFeedback_Result> qualitativeFeedback_Result = await SubmissionsManager.GetQualitativeFeeedback(assignmentKey, aspNetUser.Studentkey);
                ViewBag.qualitativeFeedback_Result = qualitativeFeedback_Result;
                List<StudentAssignmentReportDrillDown> studentAssignmentReportDrillDown = await SubmissionsManager.GetAssignmentAutoCorrectedReport(assignmentKey, (int)aspNetUser.Studentkey, HttpContext);
                var scormPagesInfo = studentAssignmentReportDrillDown.Select(s => new SubmissionDetailsViewModel.ScormPagesInfo
                {
                    PageNumber = s.PageNumber.ToString(),
                    PageTitle = s.PageTitle,
                    PageGrade = s.Score.ToString() + "%",
                    ErrorCount = s.ErrorCount.ToString(),
                    MistakeCount = s.MistakeCount.ToString(),
                    CheckCount = s.CheckCount.ToString()
                }).ToList();

                submissionDetails.scormPagesInfo = scormPagesInfo;
                return PartialView("~/Views/StudentsSubmissionPage/_SubmissionDetails.cshtml", submissionDetails);
            }
            catch (Exception e)
            {
                await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                return Json("500");
            }
        }
        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> PreventSubmissionChanges(string preventAssigId, string preventUserId)
        {
            try
            {
                bool success = await SubmissionsManager.LockSubmission(preventAssigId, preventUserId, HttpContext);
                if (success == true)
                {
                    return Json("200");
                }
                return Json("500");
            }
            catch (Exception e)
            {
                await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                return Json("500");
            }

        }


        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> AllowSubmissionChanges(string allowPreventAssigId, string allowPreventUserId)
        {
            try
            {
                bool success = await SubmissionsManager.UnlockSubmission(allowPreventAssigId, allowPreventUserId, HttpContext);
                if (success == true)
                {
                    return Json("200");
                }
                return Json("500");
            }
            catch (Exception e)
            {
                await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                return Json("500");
            }

        }

        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> AddTeacherComment(string commentValue, int submID, int assignmentCmI)
        {
            try
            {
                List<AddCommentResultModel> addCommentResultModel = await SubmissionsManager.AddSubmissionComment(assignmentCmI, submID, commentValue, HttpContext);
                Dictionary<string, string> commentsData = new Dictionary<string, string>();

                commentsData.Add("comment", HttpUtility.HtmlDecode(addCommentResultModel.FirstOrDefault().content));
                commentsData.Add("commentId", addCommentResultModel.FirstOrDefault().id.ToString());
                commentsData.Add("timecreated", RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(addCommentResultModel.FirstOrDefault().timecreated).ToString());

                return Json(commentsData);
            }
            catch (Exception e)
            {
                await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                return Json("500");
            }

        }

        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> DeleteTeacherComment(int commentId)
        {
            try
            {
                bool deleteComment = await SubmissionsManager.DeleteSubmissionComment(commentId, HttpContext);

                if (deleteComment == true)
                {
                    return Json("200");
                }
                else
                {
                    return Json("500");
                }
            }
            catch (Exception e)
            {
                await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                return Json("500");
            }

        }

        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> AutoCorrectedGrading(string assId, string IdofUser, string grade)
        {
            try
            {
                var result = await MoodleManager.SaveAssignmentGrade(assId, IdofUser, grade, HttpContext);
                if (result == "null")
                {
                    return Json("200");
                }
                else
                {
                    return Json("500");
                }
            }
            catch (Exception e)
            {
                await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                return Json("500");
            }
        }
        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> GrantExtension(string assignmentId, string userId, string date)
        {
            try
            {
                string dateToParse = date + " 11:59 PM";
                DateTime grantExtensionDate = DateTime.ParseExact(dateToParse, "dd/MM/yyyy hh:mm tt", null, System.Globalization.DateTimeStyles.None);
                double unixTimestamp = UtilitiesManager.ConvertToUnixTimestamp(grantExtensionDate);
                bool subManager = await SubmissionsManager.GrantExtension(assignmentId, userId, unixTimestamp.ToString(), HttpContext);
                if (subManager == true)
                {
                    return Json("200");
                }
                else
                {
                    return Json("500");
                }

            }
            catch (Exception e)
            {
                await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                return Json("500");
            }

        }
        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public bool AssignmentSynchronisation(int? assignmnentKey = null)
        {
            bool exists = false;
            var currentUser = ViewBag.CurrentUser;
            string id = currentUser.Id;
            var userId = db.RemoteAuthentications.Where(u => u.Userkey == id && u.ESystemKey== 3021).Select(e => e.EsystemRemoteId).FirstOrDefault();
            int? assignmentId = db.Assignments.Where(a => a.AssignmentKey == assignmnentKey).Select(a => a.AssignmentId).FirstOrDefault();
            int courseId = (int)db.Assignments.Where(a => a.AssignmentId == assignmentId).Select(a => a.CourseId).FirstOrDefault();
            int assignmentKey = (int) db.Assignments.Where(a => a.AssignmentId == assignmentId).Select(a => a.AssignmentKey).FirstOrDefault();
            int? LKey =  db.AssignmentLessons.Where(a => a.AssignmentKey == assignmentKey && a.Lesson.ESystemKey == 3021).Select(l => l.LessonKey).FirstOrDefault();
            string userKey =  db.RemoteAuthentications.Where(r => r.EsystemRemoteId == userId).OrderByDescending(r => r.RemoteAuthenticationKey).Select(u => u.Userkey).FirstOrDefault();
            int? studentKey =  db.AspNetUsers.Where(i => i.Id == userKey).Select(s => s.Studentkey).FirstOrDefault();
            if (LKey != null)
            {
                CleverOwl.WebApplication.Controllers.AssignmentsSubmissionController.SynconizeLesson(LKey, userKey, assignmentKey);
            }
            else
            {
                CleverOwl.WebApplication.Controllers.AssignmentsSubmissionController.SynconizeAssignment((int)assignmentId, userId, studentKey, assignmentKey);
            }
            var StudentAssignmentReportAvailibility = db.StudentAssignmentReports.Where(s => s.AssignmentKey == assignmnentKey).FirstOrDefault();
            if (StudentAssignmentReportAvailibility != null)
            {
                exists = true;
            }
            return exists;
        }
        }
}