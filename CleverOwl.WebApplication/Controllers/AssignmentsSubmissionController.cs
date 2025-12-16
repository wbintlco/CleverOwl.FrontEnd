using CleverOwl.WebApplication.Attributes;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using RLI.Common.DataObjects;
using RLI.Common.Managers;
using RLI.Common.Managers.MoodleManagers.Database;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Models;
using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Assignment = RLI.EntityFramework.EDM.Assignment;

namespace CleverOwl.WebApplication.Controllers
{
    [Authorize]
    public class AssignmentsSubmissionController : BaseController
    {
        // GET: AssignmentsSubmission
        [MoodleToken]
        [MoodleCookie]
        [MoodleAdmin]
        public async Task<ActionResult> Index(int assignmentKey = 17208)
        {
            try
            {
                await MoodleManager.requestToken(HttpContext);
                ViewBag.StudentToken = MoodleManager.retrieveToken(HttpContext);
            }
            catch (Exception e)
            {

            }


            Assignment assignment1 = await db.Assignments.FindAsync(assignmentKey);
            AssigViewModel assignmentViewModel = new AssigViewModel();
            try
            {
                int? teacherKey = assignment1.SchoolSubjectTeacherGrade.TeacherKey;
                string userKey = CurrentUser.Id;
                int? LKey = await db.AssignmentLessons.Where(a => a.AssignmentKey == assignmentKey && a.Lesson.ESystemKey == 3021).Select(l => l.LessonKey).FirstOrDefaultAsync();
                try
                {
                    if (LKey != null)
                    {
                        SynconizeLesson(LKey, userKey, assignmentKey);
                    }
                }
                catch (Exception e)
                {

                }
                RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == userKey).FirstOrDefaultAsync();
                string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;
                RLI.Common.DataObjects.Assignment assignment = null;
                try
                {
                    assignment = await MoodleManager.GetAssignmentInfoByAssId((int)assignment1.CourseId, (int)assignment1.AssignmentId, HttpContext);

                }
                catch (Exception e)
                {
                    await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                    return RedirectToAction("ForbiddenAssignment");
                }
                RLI.Common.DataObjects.SubmissionDetailsViewModel submissionDetailsViewModel = new RLI.Common.DataObjects.SubmissionDetailsViewModel();

                //This code is written because in Moodle the first time we call this function it generates an error
                bool getAssignSubmissionDetails = true;

                try
                {
                    submissionDetailsViewModel = await SubmissionsManager.GetAssignSubmissionDetails(assignment1.AssignmentId.ToString(), currentMoodleUserId, HttpContext);
                }
                catch (Exception e)
                {
                    getAssignSubmissionDetails = false;
                }

                if (getAssignSubmissionDetails == false)
                {
                    submissionDetailsViewModel = await SubmissionsManager.GetAssignSubmissionDetails(assignment1.AssignmentId.ToString(), currentMoodleUserId, HttpContext);
                }
                ///////
                if (submissionDetailsViewModel.SubmissionFile == null)
                {
                    submissionDetailsViewModel.SubmissionFile = new List<AssignSubmissionStatusResult.File>();
                }

                List<StudentAssignmentReportDrillDown> studentAssignmentReportDrillDown = await SubmissionsManager.GetAssignmentAutoCorrectedReport(assignment1.AssignmentKey, (int)remoteAuthentication.AspNetUser.Studentkey, HttpContext);
                var scormPagesInfo = studentAssignmentReportDrillDown.Select(s => new SubmissionDetailsViewModel.ScormPagesInfo
                {
                    PageNumber = s.PageNumber.ToString(),
                    PageTitle = s.PageTitle,
                    PageGrade = s.Score.ToString() + "%",
                    ErrorCount = s.ErrorCount.ToString(),
                    MistakeCount = s.MistakeCount.ToString(),
                    CheckCount = s.CheckCount.ToString()
                }).ToList();

                submissionDetailsViewModel.scormPagesInfo = scormPagesInfo;
                ViewBag.SubmissionDetails = submissionDetailsViewModel;
                int? studentkey = CurrentUser.Studentkey;
                ViewBag.AutoCorrectedGraded = await db.StudentAssignmentReports.Where(s => s.AssignmentKey == assignmentKey && s.StudentKey == studentkey).Select(a => a.AutoCorrectedGrade).FirstOrDefaultAsync();
                int assignmentIdInt = (int)assignment1.AssignmentId;
                ViewBag.CourseModuleId = (await db.Assignments.Where(a => a.AssignmentId == assignmentIdInt).FirstOrDefaultAsync()).CourseModuleID;

                ViewBag.SubmissionId = submissionDetailsViewModel.SubmissionId;
                ViewBag.AssignmentId = assignment1.AssignmentId;
                ViewBag.AssignmentKey = assignmentKey;
                ViewBag.AssignmentCMID = assignment1.CourseModuleID;
                ViewBag.TeacherKey = teacherKey;
                ViewBag.AssignmentName = assignment1.AssignmentTitle;
                ViewBag.GradeKey = assignment1.SchoolSubjectTeacherGrade.GradeKey;
                ViewBag.SubjectKey = assignment1.SchoolSubjectTeacherGrade.SubjectKey;
                var gradedByUser = assignment1.SchoolSubjectTeacherGrade.Teacher.AspNetUsers.FirstOrDefault();
                ViewBag.GradedBy = gradedByUser.FirstName + " " + gradedByUser.LastName;
                var lessonKey = db.AssignmentLessons.Where(al => al.AssignmentKey == assignmentKey).Select(l => l.LessonKey).FirstOrDefault();
                List<bool?> lessonsKey = db.AssignmentLessons.Where(al => al.AssignmentKey == assignmentKey).Select(l => l.Lesson.IsAutoCorrected).ToList();
                if (lessonsKey.Any(x => x == true))
                {
                    ViewBag.IsAutoCorrectedOrNot = true;
                }
                else
                {
                    ViewBag.IsAutoCorrectedOrNot = false;
                }
                    ViewBag.LessonKey = lessonKey;
                string LessonURL = db.Lessons.Where(l => l.LessonKey == lessonKey).Select(l => l.LessonURL).FirstOrDefault();
                string[] urlId = null;
                try
                {
                     urlId = LessonURL.Split('=');
                }
                catch (Exception e)
                {

                }
               
                int courseId = 0;
                bool courseIdParseSuccess = false;
                try
                {
                     courseIdParseSuccess = int.TryParse(urlId[1], out courseId);
                }
                catch
                {
                    courseIdParseSuccess = false;
                }
                
                if (courseIdParseSuccess)
                {
                    ViewBag.CourseId = courseId;
                }
                List<RLI.Common.DataObjects.Config> configs = assignment.configs;

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

                string fileEn = file.Where(c => c.name == "enabled").Select(c => c.value).FirstOrDefault();
                bool fileEnabled = fileEn == "1" ? true : false;
                assignmentViewModel.WordLimit = wordLimit;
                assignmentViewModel.FilesSubmission = fileEnabled;
                assignmentViewModel.OnlineText = onlineEnabled;
                //assignmentViewModel.RemindMeToGradeBy = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.gradingduedate).ToString("dd/MM/yyyy");
                assignmentViewModel.DueDate = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.duedate).ToString("dd/MM/yyyy");
                //assignmentViewModel.AllowSubmissionFrom = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.allowsubmissionsfromdate).ToString("dd/MM/yyyy");
                if (submissionDetailsViewModel.SubmissionStatus == "draft" && fileEnabled && submissionDetailsViewModel.Locked == false)
                {
                    string filesAttachementId=null;
                    try
                    {
                         filesAttachementId = await SubmissionsManager.GetFilesFileManager(HttpContext, assignment1.CourseModuleID.ToString());
                    }
                    catch (Exception e)
                    {
                        await LogManager.log(UtilitiesManager.GetCurrentMethodName(), "filesAttachementId is not working" + e.ToString());
                    }
                    assignmentViewModel.Attachements = filesAttachementId;
                }
                else
                {
                    assignmentViewModel.Attachements = UtilitiesManager.RandomNumericString(6).Replace('0', '7');
                }

                assignmentViewModel.DescAttachementsId = UtilitiesManager.RandomNumericString(6).Replace('0', '7');
                assignmentViewModel.AssignmetName = assignment.name;
                assignmentViewModel.Description = assignment.intro.Replace("/webservice", "");

                assignmentViewModel.IntroAttachments = assignment.introattachments;
                assignmentViewModel.FeedbackComments = submissionDetailsViewModel.FeedbackComments == null ? "" : submissionDetailsViewModel.FeedbackComments.FirstOrDefault();
                List<QualitativeFeedback_Result> qualitativeFeedback_Result = await SubmissionsManager.GetQualitativeFeeedback(assignment1.AssignmentKey, (int)remoteAuthentication.AspNetUser.Studentkey);
                ViewBag.qualitativeFeedback_Result = qualitativeFeedback_Result;

                DateTime today = DateTime.Now.Date;
                int? EsystemMoodle = RLI.Common.Utilities.EnumExtensions.GetESystemKey(RLI.Common.Enums.ESystemEnum.Moodle);
                int? lessonTypeScorm = RLI.Common.Utilities.EnumExtensions.GetLessonTypeKey(RLI.Common.Enums.LessonType.Interactive);
                var studentenrolments = await db.StudentEnrolments.Where(se => se.EsystemKey == EsystemMoodle && se.StudentKey == remoteAuthentication.AspNetUser.Studentkey).ToListAsync();

                int?[] sstgKey = studentenrolments.Select(s => s.SchoolSubjectTeacherGradeKey).Cast<int?>().ToArray();
                var getAssignmentsAutoccrectedLessons = await db.AssignmentLessons.Where(a => a.AssignmentKey == assignment1.AssignmentKey).FirstOrDefaultAsync();
                int userMoodleIDInt = int.Parse(currentMoodleUserId);
                string lessUrl = "";
                bool webhookScormTypeExists = false;
                if (getAssignmentsAutoccrectedLessons != null)
                {
                    lessUrl = getAssignmentsAutoccrectedLessons.Lesson.LessonURL;
                    webhookScormTypeExists = db.WebhookQueues.Any(w => lessUrl.Contains(w.MoodleCourseID.ToString()) && w.MoodleUserID == userMoodleIDInt && w.EventType == "Scorm");
                }

                var studentsAssignmetReport = await db.StudentAssignmentReports.Where(a => a.AssignmentKey == assignment1.AssignmentKey && a.AutoCorrectedGrade != null && a.StudentKey == CurrentUser.Studentkey).FirstOrDefaultAsync();
                ViewBag.autoCorrectedGrade = "0";
                if (studentsAssignmetReport != null)
                {
                    ViewBag.autoCorrectedGrade = Math.Round((double)studentsAssignmetReport.AutoCorrectedGrade * 100, 2).ToString() + "%";
                }
                ViewBag.webhookScormTypeExists = webhookScormTypeExists;
                return View(assignmentViewModel);
            }
            catch (Exception e)
            {
                await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                return View(assignmentViewModel);
            }

        }

        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> DeleteSubmission()
        {
            return Json("");
        }

        public async Task<ActionResult> ForbiddenAssignment()
        {
            return View();
        }

        [MoodleToken]
        [MoodleAdmin]
        [MoodleCookie]
        [HttpPost]
        public async Task<ActionResult> SubmitAssignment(int assignmentKey, string assignmentId, string fileAttachementsId, string descAttachementsId, string text, int teacherKey, bool isAutoCorrected, bool isFileEnabled)
        {
            try
            {
                string userKey = CurrentUser.Id;
                RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == userKey).FirstOrDefaultAsync();
                string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;
                int studentUserId = int.Parse(currentMoodleUserId);

                SaveAssignmentSubmissionModel saveAssignmentSubmissionModel = new SaveAssignmentSubmissionModel();
                saveAssignmentSubmissionModel.AssignmentId = assignmentId;
                if (isFileEnabled)
                {
                    saveAssignmentSubmissionModel.FilesFileManager = fileAttachementsId;
                }
                saveAssignmentSubmissionModel.ItemId = descAttachementsId;
                saveAssignmentSubmissionModel.Text = Encoding.UTF8.GetString(Convert.FromBase64String(text));

                bool success;

                if (isAutoCorrected)
                {
                    SaveAssignmentSubmissionModel saveAssignmentSubmissionModelAutoCorrected = new SaveAssignmentSubmissionModel();
                    saveAssignmentSubmissionModelAutoCorrected.AssignmentId = assignmentId;
                    success = await SubmissionsManager.SaveAutoCorrectedSubmission(saveAssignmentSubmissionModelAutoCorrected, studentUserId, teacherKey, assignmentKey, HttpContext);
                }
                else
                {
                    success = await SubmissionsManager.SaveSubmission(saveAssignmentSubmissionModel, teacherKey, studentUserId, assignmentKey, HttpContext);
                }

                if (success)
                {
                    Assignment assignment1 = await db.Assignments.FindAsync(assignmentKey);

                    RLI.Common.DataObjects.SubmissionDetailsViewModel submissionDetailsViewModel = await SubmissionsManager.GetAssignSubmissionDetails(assignment1.AssignmentId.ToString(), currentMoodleUserId, HttpContext);

                    ViewBag.AssignmentKey = assignment1.AssignmentKey;
                    ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("AssignmentsSubmission", "Index", CurrentLanguageIdentifier);
                    return PartialView("~/Views/AssignmentsSubmission/_SubmissionAssignment.cshtml", submissionDetailsViewModel);
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
        [MoodleCookie]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UploadDescFile(string fileBase64, int attachmentDescId)
        {
            try
            {
                string[] base64Array = fileBase64.Split(',');
                string base64 = base64Array[1];
                byte[] tab = Convert.FromBase64String(base64);
                Guid guid1 = Guid.NewGuid();

                HttpPostedFileBase objFile = (HttpPostedFileBase)new MemoryPostedFile(tab, guid1.ToString());

                FileUploadModel fileUploadModel = new FileUploadModel();
                fileUploadModel.file = objFile;
                fileUploadModel.itemid = attachmentDescId.ToString();
                fileUploadModel.title = objFile.FileName;

                string result = await RLI.Common.Managers.MoodleManager.UploadFile(HttpContext, fileUploadModel);

                string userKey = CurrentUser.Id;
                RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == userKey).FirstOrDefaultAsync();
                string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;

                string userToken = await MoodleManager.requestTokenByMoodleUserId(currentMoodleUserId, HttpContext,userKey);

                DescriptionFileViewModel descriptionFileViewModel = JsonConvert.DeserializeObject<DescriptionFileViewModel>(result);
                descriptionFileViewModel.url = descriptionFileViewModel.url + "?token=" + userToken;
                return Json(descriptionFileViewModel);
            }
            catch (Exception e)
            {
                return Json(500);
            }

        }

        [MoodleToken]
        [MoodleAdmin]
        [MoodleCookie]
        [HttpPost]
        public async Task<ActionResult> SaveDraft(string assignmentKeyD, string assignmentIdD, string fileAttachementsIdD, string descAttachementsIdD, string textD, bool isFileSub)
        {
            SaveAssignmentSubmissionModel saveAssignmentSubmissionModel = new SaveAssignmentSubmissionModel();
            saveAssignmentSubmissionModel.AssignmentId = assignmentIdD;
            if (isFileSub)
            {
                saveAssignmentSubmissionModel.FilesFileManager = fileAttachementsIdD;
            }
            saveAssignmentSubmissionModel.ItemId = descAttachementsIdD;
            saveAssignmentSubmissionModel.Text = Encoding.UTF8.GetString(Convert.FromBase64String(textD)); ;

            Assignment assignment1 = await db.Assignments.FindAsync(int.Parse(assignmentKeyD));
            string userKey = CurrentUser.Id;
            RemoteAuthentication remoteAuthentication = await db.RemoteAuthentications.Where(r => r.ESystem.ESystemName == "Moodle" && r.Userkey == userKey).FirstOrDefaultAsync();
            string currentMoodleUserId = remoteAuthentication.EsystemRemoteId;

            bool success = await SubmissionsManager.SaveSubmissionDraft(saveAssignmentSubmissionModel, currentMoodleUserId, HttpContext);
            if (success)
            {
                RLI.Common.DataObjects.SubmissionDetailsViewModel submissionDetailsViewModel = await SubmissionsManager.GetAssignSubmissionDetails(assignment1.AssignmentId.ToString(), currentMoodleUserId, HttpContext);
                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("AssignmentsSubmission", "Index", CurrentLanguageIdentifier);
                return PartialView("~/Views/AssignmentsSubmission/_SubmissionAssignment.cshtml", submissionDetailsViewModel);
            }
            else
            {
                return Json("500");
            }
        }
        [MoodleToken]
        [MoodleAdmin]
        [HttpPost]
        public async Task<ActionResult> AddStudentComment(string commentValue, int submID, int assignmentCmI)
        {
            try
            {
                List<AddCommentResultModel> addCommentResultModel = await SubmissionsManager.AddSubmissionComment(assignmentCmI, submID, commentValue, HttpContext);
                Dictionary<string, string> commentsData = new Dictionary<string, string>();
                commentsData.Add("comment", HttpUtility.HtmlDecode(addCommentResultModel.FirstOrDefault().content));
                commentsData.Add("commentId", addCommentResultModel.FirstOrDefault().id.ToString());

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
        public async Task<ActionResult> DeleteStudentComment(int commentId)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> InsertLessonCard(string lessonCardId , int? cardAssignmentKey = null)
        {
            try
            {
                var lessonCardIds = JsonConvert.DeserializeObject<List<int>>(lessonCardId);
                var selectedLesson = await db.Lessons.Where(l => lessonCardIds.Contains(l.LessonKey)).ToListAsync();
                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Library", "Index", CurrentLanguageIdentifier);
                ViewBag.AssignmentKey = cardAssignmentKey;
                return PartialView("~/Views/Library/_LessonsGrid.cshtml", selectedLesson);
            }
            catch (Exception e)
            {
                await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                return Json(404);
            }
        }
        [HttpPost]
        [MoodleCookie]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UploadFile(HttpPostedFileBase file, string attachmentId)
        {
            try
            {
                Stream imagePDF = PDFManager.GeneratePDF(file.FileName, file.InputStream);

                FileUploadModel fileUploadModel = new FileUploadModel();
                string filename = "";
                if (imagePDF != null)
                {
                    fileUploadModel.FileStream = imagePDF;
                    filename = file.FileName.Split('.')[0];
                    fileUploadModel.FileName = filename + ".pdf";
                }
                fileUploadModel.file = file;
                fileUploadModel.itemid = attachmentId.ToString();
                fileUploadModel.title = file.FileName;
                string result = await RLI.Common.Managers.MoodleManager.UploadFile(HttpContext, fileUploadModel);
                return Json("200");
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
                    return Json("200");
                }
            }

        }
        [HttpPost]
        [MoodleCookie]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UploadFileFromAjax(HttpPostedFileBase file1, string attachmentId1)
        {
            try
            {
                Stream imagePDF = PDFManager.GeneratePDF(file1.FileName, file1.InputStream);

                FileUploadModel fileUploadModel = new FileUploadModel();
                if (imagePDF != null)
                {
                    fileUploadModel.FileStream = imagePDF;
                    string filename = file1.FileName.Split('.')[0];
                    fileUploadModel.FileName = filename + ".pdf";
                }
                fileUploadModel.file = file1;
                fileUploadModel.itemid = attachmentId1.ToString();
                fileUploadModel.title = file1.FileName;
                string result = await RLI.Common.Managers.MoodleManager.UploadFile(HttpContext, fileUploadModel);
                return Json("200");
            }
            catch (Exception e)
            {
                string error = e.Message.ToString();
                if (error == "Parameter is not valid.")
                {
                    FileUploadModel fileUploadModel = new FileUploadModel();
                    fileUploadModel.file = file1;
                    fileUploadModel.itemid = attachmentId1.ToString();
                    fileUploadModel.title = file1.FileName;
                    string result = await RLI.Common.Managers.MoodleManager.UploadFile(HttpContext, fileUploadModel);
                    return Json("200");
                }
                else
                {
                    await LogManager.log(UtilitiesManager.GetCurrentMethodName(), $"{UtilitiesManager.GetCurrentMethodName()}: " + e.ToString());
                    return Json("200");
                }
            }

        }

        [MoodleCookie]
        [HttpPost]
        public async Task<ActionResult> DeleteFile(string fileName, int attachmentId)
        {
            try
            {
                string result = await RLI.Common.Managers.MoodleManager.DeleteAttachementFiles(HttpContext, attachmentId, fileName);
                if (result != "false")
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
        public static void SynconizeLesson(int? lessonkey, string currentUserMainID, int assignmentKey)
        {
            using (RLIEntities db = new RLIEntities())
            {
                string EsystemRemoteID = db.RemoteAuthentications.Where(r => r.Userkey == currentUserMainID && r.ESystemKey == (int)RLI.Common.Enums.ESystemEnum.Moodle).Select(r => r.EsystemRemoteId).FirstOrDefault();
                var studentKey = db.RemoteAuthentications.Where(r => r.Userkey == currentUserMainID && r.ESystemKey == (int)RLI.Common.Enums.ESystemEnum.Moodle).Select(a => a.AspNetUser.Studentkey).FirstOrDefault();
                var lesson = db.Lessons.Where(l => l.LessonKey == lessonkey).FirstOrDefault();
                string LessonUrl = lesson.LessonURL;
                int lessonKey = lesson.LessonKey;
                var result = LessonUrl.Substring(LessonUrl.LastIndexOf('=') + 1);
                var resultInt = int.Parse(result);
                RLI.Common.Managers.MoodleManager.studentReportSyncronization(resultInt, EsystemRemoteID, assignmentKey);
                using (moodleEntities dba = new moodleEntities())
                {
                    int? assignmentID = db.Assignments.Where(a => a.AssignmentKey == assignmentKey).Select(a => a.AssignmentId).FirstOrDefault();
                    long userId = long.Parse(EsystemRemoteID);
                    var finalGrade = dba.mdl_assign_grades.Where(a => a.assignment == assignmentID && a.userid == userId).Select(g => g.grade).FirstOrDefault();
                    var studentAssignmentReports = db.StudentAssignmentReports.Where(a => a.AssignmentKey == assignmentKey && a.StudentKey== studentKey).FirstOrDefault();
                    if (studentAssignmentReports != null && finalGrade != null)
                    {
                        var finalGradeString = finalGrade.ToString();
                        var finalGradeDouble = double.Parse(finalGradeString);
                        if (studentAssignmentReports != null)
                        {
                            studentAssignmentReports.FinalGrade = finalGradeDouble;
                        }
                                           
                    }
                    db.SaveChanges();
                }
            }

            //lesson.IsNotSyncronised = true;
            //    db.SaveChanges();
        }
        public static void SynconizeAssignment(int assigId ,string userId ,int? StudentKey, int assignmentKey)
        {
            long userIdlong = long.Parse(userId);
            using (RLIEntities db = new RLIEntities())
            {
                var studentAssignmentReportToDelete = db.StudentAssignmentReports.Where(s => s.StudentKey == StudentKey && s.AssignmentKey == assignmentKey).FirstOrDefault();
                if(studentAssignmentReportToDelete != null)
                {
                    db.StudentAssignmentReports.Remove(studentAssignmentReportToDelete);
                    db.SaveChanges();
                }

                using (moodleEntities dbm = new moodleEntities())
                {
                  var timestamp =   dbm.mdl_assign_submission.Where(u => u.userid == userIdlong && u.assignment == assigId).Select(t => t.timemodified).FirstOrDefault();
                    var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                    StudentAssignmentReport studentAssignmentReport = new StudentAssignmentReport();
                    studentAssignmentReport.StudentKey = StudentKey;
                    studentAssignmentReport.AssignmentKey = assignmentKey;
                    studentAssignmentReport.SubmissionDate = dateTime;
                    studentAssignmentReport.FinalGrade = dbm.mdl_assign_grades.Where(u=>u.userid == userIdlong && u.assignment == assigId).Select(g=> (double?)g.grade).FirstOrDefault();
                    db.StudentAssignmentReports.Add(studentAssignmentReport);
                    db.SaveChanges();
                }
                
            }
        }
            }
}