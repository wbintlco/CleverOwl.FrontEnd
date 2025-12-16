
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using CleverOwl.WebApplication.Attributes;
using CleverOwl.WebApplication.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using RLI.Common.DataObjects;
using RLI.Common.Managers;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Objects;

namespace CleverOwl.WebApplication.Controllers
{
    //public class MemoryPostedFile : HttpPostedFileBase
    //{
    //    private readonly byte[] fileBytes;

    //    public MemoryPostedFile(byte[] fileBytes, string fileName = null)
    //    {
    //        this.fileBytes = fileBytes;
    //        this.FileName = fileName;
    //        this.InputStream = new MemoryStream(fileBytes);
    //    }

    //    public override int ContentLength => fileBytes.Length;

    //    public override string FileName { get; }

    //    public override Stream InputStream { get; }
    //}
    public class AssignmentsTestController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        public async Task<ActionResult> Index(dynamic httpContext)
        {
            List<RLI.EntityFramework.EDM.Assignment> assignments = new List<RLI.EntityFramework.EDM.Assignment>();
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name == "Student")
            {
                var currentStudent = await db.AspNetUsers.Where(a => a.Id == "fea3608b-d002-45d4-ac1c-42f7f5a79a84").ToListAsync();
                var student = currentStudent.FirstOrDefault().Student;

                //var hashd=db.AspNetUsers.Where(a=>a)
                //assignments = await db.Assignments.Where(a => a.GradeKey == student.GradeKey && a.AspNetUser.TeacherKey).OrderByDescending(a => a.TimeModifed).Take(50).ToListAsync();
            }

            assignments = await db.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == 45290).OrderByDescending(a => a.TimeModifed).Take(50).ToListAsync();

            List<RLI.EntityFramework.EDM.Grade> Grades = assignments.Select(a => a.SchoolSubjectTeacherGrade.Grade).Distinct().ToList();
            List<RLI.EntityFramework.EDM.Subject> Subjects = assignments.Select(a => a.SchoolSubjectTeacherGrade.Subject).Distinct().ToList();
            var dueDates = assignments.Where(a => a.DueDate != null).Select(a => a.DueDate).Distinct().Select(d => new
            {
                date1 = d.Value.ToLongDateString(),
                dateKey = d.Value.ToLongDateString()
            }).ToList();

            ViewBag.Grades = new SelectList(Grades.OrderBy(g => g.GradeIndex).Where(g => g.Grade1 != "Training").Select(g => new 
            {
                GradeKey = g.GradeKey,
                DefaultGrade1 = g.Grade1,
                Grade1 = CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                GradeIndex = g.GradeIndex,
                LocalGradeGUID = g.LocalGradeGUID

            }), "GradeKey", "Grade1");
            ViewBag.Subjects = new SelectList(Subjects.OrderBy(s => s.SubjectIndex).Where(s => s.Display == true && s.Subject1 != "Training").Select(g => new
            {
                SubjectKey = g.SubjectKey,
                DefaultSubject1 = g.Subject1,
                Subject1 = CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                SubjectIndex = g.SubjectIndex,
                LocalSubjectGUID = g.LocalSubjectGUID,
                Chapters = g.Chapters

            }), "SubjectKey", "Subject1");

            ViewBag.DueDates = new SelectList(dueDates, "dateKey", "date1");
            return View(assignments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AssignmentsFilter(string query = null, int? records = null, int? gradeKey = null, int? subjectKey = null, DateTime? dueDate = null)
        {
            int numberOfRecords = (int)(records == null ? 50 : records);
            List<RLI.EntityFramework.EDM.Assignment> assignments = new List<RLI.EntityFramework.EDM.Assignment>();
            var userAssignments = db.Assignments.Where(a => a.SchoolSubjectTeacherGrade.TeacherKey == 45290).OrderByDescending(a => a.TimeModifed);

            assignments = await userAssignments.Where(a =>
          ((gradeKey != null && (a.SchoolSubjectTeacherGrade.Grade.GradeKey == gradeKey)) || (gradeKey == null))
          && ((subjectKey != null && (a.SchoolSubjectTeacherGrade.Subject.SubjectKey == subjectKey)) || (subjectKey == null))
          && ((dueDate != null && (a.DueDate == dueDate)) || (dueDate == null))
          && ((query != null && (a.AssignmentTitle.Contains(query))) || (query == null))
        ).Take(numberOfRecords).ToListAsync();

            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Assignments", "Index", CurrentLanguageIdentifier);
            return PartialView("_AssignmentCard", assignments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [MoodleToken]
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
        public async Task<ActionResult> Create(int? assigId)
        {
           
            //assigId = 7072;
            List<RLI.EntityFramework.EDM.Grade> gradesSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == 45290).Select(sstg => sstg.Grade).Distinct().ToListAsync();
            List<Subject> subjectsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == 45290).Select(sstg => sstg.Subject).Distinct().ToListAsync();
            List<School> schoolsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == 45290).Select(sstg => sstg.School).Distinct().ToListAsync();
            ViewBag.assigId = assigId;
            
            if (assigId == null)
            {
                //List<MoodleUserCourseModel> userCourseList = await RLI.Common.Managers.AssignmentsManager.GetUserCourses("fda21393-9f36-4b49-94fd-5dbd98918714", HttpContext);
                //ViewBag.UserCoursesDropDown = new SelectList(userCourseList, "id", "displayname");

                ViewBag.Grades = new SelectList(gradesSstg.OrderBy(g => g.GradeIndex).Where(g => g.Grade1 != "Training").Select(g => new
                {
                    GradeKey = g.GradeKey,
                    DefaultGrade1 = g.Grade1,
                    Grade1 = CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    GradeIndex = g.GradeIndex,
                    LocalGradeGUID = g.LocalGradeGUID
                }), "GradeKey", "Grade1");
                ViewBag.Subjects = new SelectList(subjectsSstg.OrderBy(s => s.SubjectIndex).Where(s => s.Display == true && s.Subject1 != "Training").Select(g => new
                {
                    SubjectKey = g.SubjectKey,
                    DefaultSubject1 = g.Subject1,
                    Subject1 = CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    SubjectIndex = g.SubjectIndex,
                    LocalSubjectGUID = g.LocalSubjectGUID,
                    Chapters = g.Chapters
                }), "SubjectKey", "Subject1");
                ViewBag.Schools = new SelectList(schoolsSstg, "SchoolKey", "SchoolName");

                AssigViewModel assignmentViewModel = new AssigViewModel();
                assignmentViewModel.IsCreatePage = true;
                assignmentViewModel.GradeToPass = 50;
                assignmentViewModel.FileTypes = new List<string>();
                assignmentViewModel.MaxSubSize = 100000000;
                assignmentViewModel.MaxFilesNumber = 20;
                assignmentViewModel.EnableWordLimit = false;
                assignmentViewModel.WordLimit = null;
                assignmentViewModel.FilesSubmission = true;
                assignmentViewModel.OnlineText = true;
                assignmentViewModel.RemindMeToGradeBy = "";
                assignmentViewModel.DueDate = "";
                assignmentViewModel.AllowSubmissionFrom = "";
                assignmentViewModel.Attachements = UtilitiesManager.RandomNumericString(6);
                assignmentViewModel.DescAttachementsId = UtilitiesManager.RandomNumericString(6);
                assignmentViewModel.AssignmetName = "";
                assignmentViewModel.Description = "";
                assignmentViewModel.GradeKey = 0;
                assignmentViewModel.SubjectKey = 0;
                assignmentViewModel.SchoolKey = 0;
                assignmentViewModel.HideFromStudents = false;
                assignmentViewModel.IntroAttachments = new List<RLI.Common.DataObjects.Introattachment>();

                return View(assignmentViewModel);
            }
            else
            {
                RLI.EntityFramework.EDM.Assignment assignment1 = await db.Assignments.FindAsync(assigId);
                //List<MoodleUserCourseModel> userCourseList = await RLI.Common.Managers.AssignmentsManager.GetUserCourses("fda21393-9f36-4b49-94fd-5dbd98918714", HttpContext);
                //ViewBag.UserCoursesDropDown = new SelectList(userCourseList, "id", "displayname", assignment1.CourseId);
                ViewBag.Grades = new SelectList(gradesSstg.OrderBy(g => g.GradeIndex).Where(g => g.Grade1 != "Training").Select(g => new
                {
                    GradeKey = g.GradeKey,
                    DefaultGrade1 = g.Grade1,
                    Grade1 = CurrentLanguageIdentifier == 0 ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    GradeIndex = g.GradeIndex,
                    LocalGradeGUID = g.LocalGradeGUID
                }), "GradeKey", "Grade1", assignment1.SchoolSubjectTeacherGrade.GradeKey);
                ViewBag.Subjects = new SelectList(subjectsSstg.OrderBy(s => s.SubjectIndex).Where(s => s.Display == true && s.Subject1 != "Training").Select(g => new
                {
                    SubjectKey = g.SubjectKey,
                    DefaultSubject1 = g.Subject1,
                    Subject1 = CurrentLanguageIdentifier == 0 ? g.Subject1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
                    SubjectIndex = g.SubjectIndex,
                    LocalSubjectGUID = g.LocalSubjectGUID,
                    Chapters = g.Chapters
                }), "SubjectKey", "Subject1", assignment1.SchoolSubjectTeacherGrade.SubjectKey);

                ViewBag.Schools = new SelectList(schoolsSstg, "SchoolKey", "SchoolName", assignment1.SchoolSubjectTeacherGrade.SchoolKey);


                RLI.Common.DataObjects.Assignment assignment = await MoodleManager.GetAssignmentInfoByAssId((int)assignment1.CourseId, (int)assignment1.AssignmentId, HttpContext);
                List<Config> configs = assignment.configs;
                var online = configs.Where(c => c.plugin == "onlinetext");
                var file = configs.Where(c => c.plugin == "file");
                string onlineEn = online.Where(c => c.name == "enabled").Select(c => c.value).FirstOrDefault();
                bool onlineEnabled = onlineEn == "1" ? true : false;
                int wordLimit = int.Parse(configs.Where(c => c.name == "wordlimit").Select(c => c.value).FirstOrDefault());
                int wordLimitEn = int.Parse(configs.Where(c => c.name == "wordlimitenabled").Select(c => c.value).FirstOrDefault());
                bool wordLimitEnabled = wordLimitEn == 1 ? true : false;
                string fileEn = file.Where(c => c.name == "enabled").Select(c => c.value).FirstOrDefault();
                bool fileEnabled = fileEn == "1" ? true : false;
                int maxFileSub = int.Parse(file.Where(c => c.name == "maxfilesubmissions").Select(c => c.value).FirstOrDefault());
                int maxSubBytes = int.Parse(file.Where(c => c.name == "maxsubmissionsizebytes").Select(c => c.value).FirstOrDefault());
                string fileTypesList = file.Where(c => c.name == "filetypeslist").Select(c => c.value).FirstOrDefault();

                string attachmentsID = await RLI.Common.Managers.AssignmentsManager.GetIntroAttachments(HttpContext, assignment1.CourseModuleID.ToString());
                string descAttachmentsID = await RLI.Common.Managers.AssignmentsManager.GetIntroDesc(HttpContext, assignment1.CourseModuleID.ToString());
                
                Dictionary<string, object> fileextensions = new Dictionary<string, object>();
                string[] parentExtensions = { };


                AssigViewModel assignmentViewModel = new AssigViewModel();
                assignmentViewModel.IsCreatePage = false;
                assignmentViewModel.GradeToPass = 50;
                assignmentViewModel.FileTypesStr = fileTypesList;
                assignmentViewModel.MaxSubSize = maxSubBytes;
                assignmentViewModel.MaxFilesNumber = maxFileSub;
                assignmentViewModel.EnableWordLimit = wordLimitEnabled;
                assignmentViewModel.WordLimit = wordLimit;
                assignmentViewModel.FilesSubmission = fileEnabled;
                assignmentViewModel.OnlineText = onlineEnabled;
                assignmentViewModel.RemindMeToGradeBy = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.gradingduedate).ToString("dd/mm/yyyy - HH:ii P");
                assignmentViewModel.DueDate = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.duedate).ToString("dd/mm/yyyy - HH:ii P");
                assignmentViewModel.AllowSubmissionFrom = RLI.Common.Managers.UtilitiesManager.ConvertFromUnixTimestamp(assignment.allowsubmissionsfromdate).ToString("dd/mm/yyyy - HH:ii P");
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
                return View(assignmentViewModel);
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
                
                FileUploadModel fileUploadModel = new FileUploadModel();
                fileUploadModel.file = file;
                fileUploadModel.itemid = attachmentId.ToString();
                fileUploadModel.title = file.FileName;
                await RLI.Common.Managers.MoodleManager.UploadFile(HttpContext, fileUploadModel);
                return Json("success");
            }
            catch (Exception e)
            {
                return Json("error");
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
                string base64 = base64Array[1];
                byte[] tab = Convert.FromBase64String(base64);
                //FileStream fileStrem = await ByteArrayToFile("filename", ret);
                //MemoryStream memStream = new MemoryStream(tab);
                Guid guid1 = Guid.NewGuid();
                
                HttpPostedFileBase objFile = (HttpPostedFileBase)new MemoryPostedFile(tab, guid1.ToString());
                
                FileUploadModel fileUploadModel = new FileUploadModel();
                fileUploadModel.file = objFile;
                fileUploadModel.itemid = attachmentDescId.ToString();
                fileUploadModel.title = objFile.FileName;

                string result = await RLI.Common.Managers.MoodleManager.UploadFile(HttpContext, fileUploadModel);
                DescriptionFileViewModel descriptionFileViewModel = JsonConvert.DeserializeObject<DescriptionFileViewModel>(result);
                return Json(descriptionFileViewModel);
            }
            catch (Exception e)
            {
                return Json(500);
            }

        }

     
        [MoodleCookie]
        [HttpPost]
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
        [HttpPost]
        public async Task<ActionResult> SubmitAssignment(string AssigData)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                AssigViewModel assig = (AssigViewModel)serializer.Deserialize(AssigData, typeof(AssigViewModel));

                string[] dueDateAr = assig.DueDate.Split('-');
                string dueSateStr = dueDateAr[0].Trim() + " " + dueDateAr[1].Trim();
                DateTime dueDate = DateTime.ParseExact(dueSateStr, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);



                string[] allowSubDateAr = assig.AllowSubmissionFrom.Split('-');
                string allowSubDateStr = allowSubDateAr[0].Trim() + " " + allowSubDateAr[1].Trim();
                DateTime allowSubDate = DateTime.ParseExact(allowSubDateStr, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);

                string[] remindToGradeDateAr = assig.RemindMeToGradeBy.Split('-');
                string remindToGradeDateStr = remindToGradeDateAr[0].Trim() + " " + remindToGradeDateAr[1].Trim();
                DateTime remindToGradeDate = DateTime.ParseExact(remindToGradeDateStr, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);

                List<MoodleUserCourseModel> userCourseList = await RLI.Common.Managers.AssignmentsManager.GetUserCourses("fda21393-9f36-4b49-94fd-5dbd98918714", HttpContext);

                string userKey = CurrentUser.Id;

                List<RLI.EntityFramework.EDM.Grade> gradesSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == 45290).Select(sstg => sstg.Grade).Distinct().ToListAsync();
                List<Subject> subjectsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == 45290).Select(sstg => sstg.Subject).Distinct().ToListAsync();
                List<School> schoolsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == 45290).Select(sstg => sstg.School).Distinct().ToListAsync();
                string grade = gradesSstg.Where(g => g.GradeKey == assig.GradeKey).Select(g => g.Grade1).FirstOrDefault().Trim();
                string subject = subjectsSstg.Where(s => s.SubjectKey == assig.SubjectKey).Select(s => s.Subject1).FirstOrDefault().Trim();
                string school = schoolsSstg.Where(s => s.SchoolKey == assig.SchoolKey).Select(s => s.SchoolName).FirstOrDefault().Trim();
                string courseValue = subject + "," + grade + "," + school;
                int courseId = userCourseList.Where(c => c.displayname == courseValue).Select(c => c.id).FirstOrDefault();

                AssignmentViewModel assigmnetViewModel = new AssignmentViewModel();
                assigmnetViewModel.AssignmetName = assig.AssignmetName;
                assigmnetViewModel.CourseId = courseId;
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
                assigmnetViewModel.UserKey = "fda21393-9f36-4b49-94fd-5dbd98918714";

                var testAssign = await AssignmentsManager.CreateAssignment(assigmnetViewModel, HttpContext);
                return Json(200);
            }
            catch (Exception e)
            {
                return Json(e);
            }


        }

        [MoodleCookie]
        [MoodleToken]
        [HttpPost]
        public async Task<ActionResult> EditAssignment(string EditAssigData, int assKey)
        {
            try
            {

                RLI.EntityFramework.EDM.Assignment assignment1 = await db.Assignments.FindAsync(assKey); ;
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                AssigViewModel assig = (AssigViewModel)serializer.Deserialize(EditAssigData, typeof(AssigViewModel));

                string[] dueDateAr = assig.DueDate.Split('-');
                string dueSateStr = dueDateAr[0].Trim() + " " + dueDateAr[1].Trim();
                DateTime dueDate = DateTime.ParseExact(dueSateStr, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);



                string[] allowSubDateAr = assig.AllowSubmissionFrom.Split('-');
                string allowSubDateStr = allowSubDateAr[0].Trim() + " " + allowSubDateAr[1].Trim();
                DateTime allowSubDate = DateTime.ParseExact(allowSubDateStr, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);

                string[] remindToGradeDateAr = assig.RemindMeToGradeBy.Split('-');
                string remindToGradeDateStr = remindToGradeDateAr[0].Trim() + " " + remindToGradeDateAr[1].Trim();
                DateTime remindToGradeDate = DateTime.ParseExact(remindToGradeDateStr, "dd/MM/yyyy hh:mm tt", null, DateTimeStyles.None);

                AssignmentViewModel assigmnetViewModel = new AssignmentViewModel();
                assigmnetViewModel.CourseId = (int)assignment1.CourseId;
                assigmnetViewModel.AssignmetName = assig.AssignmetName;
                assigmnetViewModel.CourseModule = assignment1.CourseModuleID;
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
                var testAssign = await AssignmentsManager.EditAssignment(assigmnetViewModel, HttpContext);
                return Json(200);
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
            List<RLI.EntityFramework.EDM.Grade> gradesSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == 45290 && sstg.SchoolKey == SchoolId).Select(sstg => sstg.Grade).Distinct().ToListAsync();
            var grades = gradesSstg.Select(g => new
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
            List<RLI.EntityFramework.EDM.Subject> subjectsSstg = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == 45290 && sstg.SchoolKey == SchoolId && sstg.GradeKey == gradeId).Select(sstg => sstg.Subject).Distinct().ToListAsync();
            var grades = subjectsSstg.Select(g => new
            {
                id = g.SubjectKey,
                text = g.Subject1,

            }).ToList();
            return Json(grades);
        }
    }
}