  using Antlr.Runtime;
using FrontEnd.WebApplication.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using RLI.Common.Enums;
using RLI.Common.Globals;
using RLI.Common.Managers;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Models;
using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FrontEnd.WebApplication.Controllers
{
    [Authorize]

    public class CrowdContentTestController : BaseController
    {
        private RLIEntities db = new RLIEntities();

        // GET: CrowdContent
        public async Task<ActionResult> Index()
        {
            return View();
        }

        // GET: Participate


        // POST: SignCharter
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SignCharter()
        {
            AspNetUser currentUser = await db.AspNetUsers.FindAsync(CurrentUser.Id);
            if (currentUser == null)
            {
                return HttpNotFound();
            }

            currentUser.CrowdContentStatusKey = (int)StatusEnum.StatusSigned;

            db.Entry(currentUser).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        // POST: ReviewProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReviewProfile(Models.ReviewProfileViewModel model)
        {
            if (model == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            AspNetUser currentUser = await db.AspNetUsers.FindAsync(CurrentUser.Id);
            if (currentUser == null)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                Sector sector = await db.Sectors.FirstOrDefaultAsync(s => s.Sector1 == model.Sector);
                if (sector == null)
                {
                    Sector newSector = new Sector();
                    newSector.Sector1 = model.Sector;

                    db.Sectors.Add(newSector);

                    await db.SaveChangesAsync();

                    sector = newSector;
                }

                try
                {
                    currentUser.AlternativeEmail = model.AlternativeEmail;
                    currentUser.FirstName = model.FirstName;
                    currentUser.LastName = model.LastName;
                    currentUser.PhoneNumber = model.PhoneNumber;
                    currentUser.SectorKey = sector.SectorKey;
                    currentUser.PhoneNumberConfirmed = false;

                    currentUser.CrowdContentStatusKey = (int)StatusEnum.StatusProfileReviewed;

                    db.Entry(currentUser).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    ViewBag.Sector = new SelectList(db.Sectors, "Sector1", "Sector1", currentUser.SectorKey);

                    var lastSentSms = DateTime.Now - CurrentUser.LastSentSms;
                    var timeMinutes = lastSentSms == null ? Int32.MaxValue : lastSentSms.Value.Minutes;
                    int waitMinutes = Configuration.SMS_PHONE_NUMBER_VERIFICATION_RETRY_WAIT_MINUTES;
                    if (UserManager.SmsService != null && timeMinutes > waitMinutes)
                    {
                        // Generate the token and send it
                        var code = await UserManager.GenerateChangePhoneNumberTokenAsync(currentUser.Id, model.PhoneNumber);

                        var message = new IdentityMessage
                        {
                            Destination = model.PhoneNumber,
                            Body = Configuration.SMS_PHONE_NUMBER_VERIFICATION_MESSAGE_EN + code
                        };
                        await UserManager.SmsService.SendAsync(message);

                        currentUser.LastSentSms = DateTime.Now;

                        db.Entry(currentUser).State = EntityState.Modified;
                        await db.SaveChangesAsync();
                    }

                    return Json(new { MinutesFromLastSMS = timeMinutes });
                }
                catch (Exception e)
                {
                    await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);

                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }

            return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
        }

        // POST: PhoneVerified
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PhoneVerified()
        {
            AspNetUser currentUser = await db.AspNetUsers.FindAsync(CurrentUser.Id);
            if (currentUser == null)
            {
                return HttpNotFound();
            }

            currentUser.CrowdContentStatusKey = (int)StatusEnum.StatusPhoneVerified;

            db.Entry(currentUser).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        // POST: ContentContribution
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ContentContribution(Models.ContentContributionViewModel model)
        {
            AspNetUser currentUser = await db.AspNetUsers.FindAsync(CurrentUser.Id);
            if (currentUser == null)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                var contentContributions = model.ContentContribution.Select(cp => new { cp.ContributionGrade, cp.ContributionSubject }).Distinct().ToList();
                foreach (var contributionPair in contentContributions)
                {
                    ContentContributor contentContributor = new ContentContributor();
                    contentContributor.UserKey = currentUser.Id;
                    contentContributor.GradeKey = contributionPair.ContributionGrade;
                    contentContributor.SubjectKey = contributionPair.ContributionSubject;

                    db.ContentContributors.Add(contentContributor);

                    await db.SaveChangesAsync();
                }

                currentUser.CrowdContentStatusKey = (int)StatusEnum.StatusContentSpecified;

                db.Entry(currentUser).State = EntityState.Modified;
                await db.SaveChangesAsync();

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);
            }

            return new HttpStatusCodeResult(HttpStatusCode.NotFound);
        }

        // GET: Submit
        public async Task<ActionResult> Submit()
        {
            AspNetUser user = await db.AspNetUsers.FindAsync(CurrentUser.Id);
            if (user == null)
            {
                return HttpNotFound();
            }
            ViewBag.GradeKey = new SelectList(db.Grades.OrderBy(g => g.GradeIndex).Select(g => new
            {
                GradeKey = g.GradeKey,
                DefaultGrade1 = g.Grade1,
                Grade1 = CurrentLanguageIdentifier == null ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
            }), "GradeKey", "Grade1");
            ViewBag.SubjectKey = new SelectList(db.Subjects.OrderBy(s => s.SubjectIndex).Select(s => new
            {
                SubjectKey = s.SubjectKey,
                DefaultGrade1 = s.Subject1,
                Subject1 = CurrentLanguageIdentifier == null ? s.Subject1 : s.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
            }), "SubjectKey", "Subject1");

            ViewBag.TopicKey = new SelectList(new List<Topic>(), "TopicKey", "Topic1", selectedValue: default);
            ViewBag.ChapterKey = new SelectList(new List<Chapter>(), "ChapterKey", "ChapterName", selectedValue: default);
            ViewBag.LessonType = new SelectList(db.LessonTypes, "LessonTypeKey", "LessonType1", selectedValue: default);
            ViewBag.ProviderKey = new SelectList(db.ESystems, "ESystemKey", "ESystemName",selectedValue:default);
            ViewBag.FileType = new SelectList(db.FileTypes, "FileTypeKey", "FileType1", selectedValue: default);
            ViewBag.LibraryKey = new SelectList(db.ChaptersTypes, "ChapterTypeKey", "ChapterType", selectedValue: default);


            return View();
        }

        // POST: ContentSpecifications

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ContentSpecifications()
        {
            AspNetUser currentUser = await db.AspNetUsers.FindAsync(CurrentUser.Id);
            if (currentUser == null)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {

            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        // POST: CurriculumMapping
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CurriculumMapping()
        {
            AspNetUser currentUser = await db.AspNetUsers.FindAsync(CurrentUser.Id);
            if (currentUser == null)
            {
                return HttpNotFound();
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        // POST: CurriculumMapping
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ContentUpload(RLI.WebApplication.Models.ContentSpecificationsViewModel contentSpecifications)
        {
            if (contentSpecifications == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            AspNetUser currentUser = await db.AspNetUsers.FindAsync(CurrentUser.Id);
            if (currentUser == null)
            {
                return HttpNotFound();
            }

            
                if (contentSpecifications.AcknowledgeAccuracyInformation)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                else
                {
                    return Json("AcknowledgementNotFound");
                }
            

            
        }

        //public async Task<ActionResult> ContentCurriculumUpload(ContentCurriculumViewModel contentCurriculum)
        //{
        //    if (contentCurriculum == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //    AspNetUser currentUser = await db.AspNetUsers.FindAsync(CurrentUser.Id);
        //    if (currentUser == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        if (contentCurriculum.AcknowledgeAccuracyInformation)
        //        {
        //            return new HttpStatusCodeResult(HttpStatusCode.OK);
        //        }
        //        else
        //        {
        //            return Json("AcknowledgementNotFound2");
        //        }
        //    }

        //    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
        //}
        //public async Task<ActionResult> ContentFileUpload(ContentFileUploadViewModel contentFile)
        //{
        //    if (contentFile == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //    AspNetUser currentUser = await db.AspNetUsers.FindAsync(CurrentUser.Id);
        //    if (currentUser == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.OK);
        //    }

        //    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
        //}
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UploadFile(HttpPostedFileBase file)
        {
            Stream stream = file.InputStream;
            BinaryReader binaryReader = new System.IO.BinaryReader(stream);
            Byte[] bytes = binaryReader.ReadBytes((Int32)stream.Length);
            string base64String = Convert.ToBase64String(bytes, 0, bytes.Length);
            TempData["UploadedFile"] = file;
            TempData["UploadedFileBase64"] = base64String;
            return Json("");
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<JsonResult> SaveContent(CrowdContentViewModel crowd)
        //{
        //    HttpPostedFileBase file = (HttpPostedFileBase)TempData["UploadedFile"];
        //    string fileBase64 = (string)TempData["UploadedFileBase64"];
        //    if (file == null || fileBase64 == null)
        //    {
        //        return Json(-1);
        //    }
        //    string uploadsFolderPath = RLI.Common.Globals.Configuration.STORAGE_CROWDCONTENT_FOLDER;
        //    if (!Directory.Exists(uploadsFolderPath))
        //    {
        //        Directory.CreateDirectory(uploadsFolderPath);
        //    }
        //    RLI.EntityFramework.EDM.File newFile = null;

        //    newFile = new RLI.EntityFramework.EDM.File();
        //    newFile.FileKey = Guid.NewGuid();
        //    newFile.CreationDate = DateTime.Now;
        //    newFile.FileName = file.FileName;
        //    newFile.MimeType = file.ContentType;
        //    newFile.FileTypeKey = crowd.FileType;
        //    newFile.DataTypeKey = (int)RLI.Common.Enums.DataTypeEnum.DataTypeLesson;
        //    newFile.StatusKey = (int)RLI.Common.Enums.StatusEnum.StatusPending;
        //    //newFile.MetaData =;

        //    db.Files.Add(newFile);
        //    await db.SaveChangesAsync();


        //    RLI.EntityFramework.EDM.File currentFile = await db.Files.Where(f => f.FileKey == newFile.FileKey).FirstOrDefaultAsync();
        //    currentFile.StatusKey = (int)RLI.Common.Enums.StatusEnum.StatusApproved;
        //    db.Entry(currentFile).State = EntityState.Modified;


        //    Lesson lesson = new Lesson();
        //    lesson.LessonName = crowd.LessonName;
        //    lesson.Remark = crowd.Remark;
        //    lesson.LessonURL = null;
        //    lesson.TopicKey = crowd.Topic;
        //    lesson.LessonTypeKey = (int)RLI.Common.Enums.LessonType.Scorm;
        //    lesson.LanguageKey = db.Topics.Where(t => t.TopicKey == crowd.Topic).FirstOrDefault().LanguageKey;
        //    lesson.ESystemKey = 17;
        //    lesson.FileKey = newFile.FileKey;
        //    lesson.StatusKey = (int)RLI.Common.Enums.StatusEnum.StatusPending;
        //    db.Lessons.Add(lesson);
        //    await db.SaveChangesAsync();
        //    return Json("Success");
        //}
    }
}