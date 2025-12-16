using CleverOwl.WebApplication.Attributes;
using Microsoft.AspNet.Identity;
using RLI.Common.Managers;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using File = RLI.EntityFramework.EDM.File;

namespace CleverOwl.WebApplication.Controllers
{
    [Authorize]
    public class LessonsBankController : BaseController
    {
        //// GET: LessonBuilderManagement
        //public ActionResult Index()
        //{
        //    return View();
        //}

        // GET: LessonBuilderManagement AdaptLessons
        private const int TAKE_LESSON = 9;
        public async Task<ActionResult> AdaptLessons(string fromAdapt=null)
        {

            ViewBag.fromAdapt = fromAdapt;
            await LogManager.log("AdaptLessons", "AdaptLessons Page!!!");
            List<TeachersFile> teachersFile = new List<TeachersFile>();
            int pendingStatus = (int)RLI.Common.Enums.StatusEnum.StatusPending;

            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
            {
                string userKey = CurrentUser.Id;
                var teacher = await db.AspNetUsers.Where(u => u.Id == userKey).FirstOrDefaultAsync();
                int? teacherKey = teacher.TeacherKey;
                teachersFile = await db.TeachersFiles.Where(tf => tf.Teacherkey == teacherKey).OrderByDescending(t => t.File.CreationDate).ThenBy(t => t.File.MetaData).ThenBy(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Take(TAKE_LESSON).ToListAsync();//&& tf.File.StatusKey == pendingStatus

                ViewBag.GradesDroDown = new SelectList(db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey).Select(sstg => sstg.Grade).Distinct(), "GradeKey", "Grade1");
                ViewBag.Schools = new SelectList(db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey).Select(sstg => sstg.School).Distinct(), "SchoolKey", "SchoolName", db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey).Select(sstg => sstg.School).Distinct().FirstOrDefault().SchoolKey);
            }
            else
            {
                teachersFile = await db.TeachersFiles.Where(tf => tf.File.StatusKey == pendingStatus).OrderByDescending(t => t.File.CreationDate).ThenBy(t => t.File.MetaData).ThenBy(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Take(TAKE_LESSON).ToListAsync();
            }

            await LogManager.log("AdaptLessons", "teachersFile lonq!!!" + teachersFile.Count);
            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("LessonBuilderManagement", "AdaptLessons", CurrentLanguageIdentifier);
            ViewBag.Status = new SelectList(teachersFile.Select(tf =>tf.File.Status).Distinct(), "StatusKey", "Status1");
            ViewBag.Dates = new SelectList(teachersFile.Select(tf => tf.File).Distinct(), "CreationDate", "CreationDate");

            return View(teachersFile);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> UrlFileUpload(string fileUrl)
        {
            string scormUrl = "";
            try
            {
                string uploadsFolderPath = System.IO.Path.GetTempPath();
                //string uploadsFolderPath = RLI.Common.Globals.Configuration.STORAGE_SCORMS_FOLDER;
                
                if (!Directory.Exists(uploadsFolderPath))
                {
                    Directory.CreateDirectory(uploadsFolderPath);
                }
                string nameOfFile = fileUrl.Split('/').Last().ToString().Replace(".zip","");
                string uniqueFileName = DateTime.Now.Ticks.ToString() + "_" + nameOfFile + ".zip";
                string uploadedFilePath = uploadsFolderPath + uniqueFileName;

                WebClient webClient = new WebClient();
                webClient.DownloadFile(fileUrl, uploadedFilePath);

                var folderName = await ExtractFilesFromZipped(uniqueFileName, uploadsFolderPath);
                    scormUrl = await ObjectFromFile(folderName);
                    if (scormUrl == "")
                    {
                        Directory.Delete(uploadsFolderPath + folderName, true);
                        System.IO.File.Delete(uploadedFilePath);
                    }
                    else
                    {
                        Dictionary<string, string> fileNameDictionary = new Dictionary<string, string>();
                        fileNameDictionary.Add("fileName", uniqueFileName);
                        fileNameDictionary.Add("folderName", folderName);
                        return Json(fileNameDictionary);
                    }
            }
            catch (Exception e)
            {
                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);
            }
            return Json(scormUrl);
        }


        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> UploadSCORMFiles(HttpPostedFileBase file)
        {
            string scormUrl = "";
            string uploadsFolderPath = System.IO.Path.GetTempPath();

            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            try
            {
                string fileName = System.IO.Path.GetFileName((DateTime.Now.Ticks.ToString() + "_" + file.FileName).Replace(" ", ""));
                string uploadedFilePath = uploadsFolderPath + fileName;
                file.SaveAs(uploadedFilePath);
                var folderName = await ExtractFilesFromZipped(fileName, uploadsFolderPath);
                scormUrl = await ObjectFromFile(folderName);
                if (scormUrl == "")
                {
                    Directory.Delete(uploadsFolderPath + folderName,true);
                    System.IO.File.Delete(uploadedFilePath);
                }
                else
                {
                    Dictionary<string, string> fileNameDictionary = new Dictionary<string, string>();
                    fileNameDictionary.Add("fileName", fileName);
                    fileNameDictionary.Add("folderName", folderName);
                    return Json(fileNameDictionary);
                }
            }
            catch (Exception e)
            {
                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);
            }
            return Json(scormUrl);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> SaveFile(string f_name,string folder_name,string less_name)
        {
            try
            {
                string uploadsFolderPath = System.IO.Path.GetTempPath();
                string newUploadsFolderPath = RLI.Common.Globals.Configuration.STORAGE_SCORMS_FOLDER;

                if (!Directory.Exists(newUploadsFolderPath))
                {
                    Directory.CreateDirectory(newUploadsFolderPath);
                }

                System.IO.File.Copy(System.IO.Path.Combine(uploadsFolderPath, f_name), System.IO.Path.Combine(newUploadsFolderPath, f_name), true);
                var folderName = await ExtractFilesFromZipped(f_name, newUploadsFolderPath);
                if (!string.IsNullOrEmpty(folder_name))
                {
                    Directory.Delete(uploadsFolderPath + folder_name, true);
                }
                System.IO.File.Delete(uploadsFolderPath+f_name);

                string userKey = CurrentUser.Id;
                RLI.EntityFramework.EDM.File newFile = new RLI.EntityFramework.EDM.File();
                Guid newFileGUID = Guid.NewGuid();

                newFile.FileName = f_name;
                newFile.FileKey = newFileGUID;
                newFile.MetaData = less_name;
                newFile.MimeType = "application/zip";
                newFile.FileTypeKey = (int)RLI.Common.Enums.FileTypeEnum.FileTypeZip;
                newFile.StatusKey = (int)RLI.Common.Enums.StatusEnum.StatusPending;
                int dataTypeKey = (await db.DataTypes.Where(d => d.DataType1 == "Lesson").FirstOrDefaultAsync()).DataTypeKey;
                newFile.DataTypeKey = dataTypeKey;
                newFile.CreationDate = DateTime.Now;

                db.Files.Add(newFile);
                await db.SaveChangesAsync();

                TeachersFile teacherFile = new TeachersFile();

                teacherFile.FileKey = newFileGUID;
                teacherFile.Teacherkey = (await db.AspNetUsers.FindAsync(userKey)).TeacherKey;

                db.TeachersFiles.Add(teacherFile);
                await db.SaveChangesAsync();

                ExtendedData extendedFileData = new ExtendedData();

                extendedFileData.FileKey = newFileGUID;
                extendedFileData.FileName = f_name;
               // extendedFileData.FilePath = newUploadsFolderPath + folderName;
                extendedFileData.FilePath = newUploadsFolderPath + folderName +"/"+ (string)TempData["ScormUrl"];
                extendedFileData.HTML = "";
                extendedFileData.DataSourceColumns = "";

                db.ExtendedDatas.Add(extendedFileData);
                await db.SaveChangesAsync();
            }
            catch(Exception e)
            {
                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);
            }
           
            return Json(200);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> DeleteFile(string file_name, string fol_name)
        {
            try
            {
                string uploadsFolderPath = System.IO.Path.GetTempPath();
                Directory.Delete(uploadsFolderPath + fol_name, true);
                System.IO.File.Delete(uploadsFolderPath + file_name);
            }
            catch (Exception e)
            {
                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);
            }

            return Json(200);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> DeleteFileFromLessonBank(int teachersFileKey)
        {
            try
            {
                string uploadsFolderPath = RLI.Common.Globals.Configuration.STORAGE_SCORMS_FOLDER;
                var teachersFile = await db.TeachersFiles.FindAsync(teachersFileKey);
                var fileKey = teachersFile.FileKey;
                var file = await db.Files.FindAsync(fileKey);
                var extendedDate = file.ExtendedDatas.FirstOrDefault();
                var folderNameLenght = extendedDate.FilePath.Split('\\').Length;
                var folderName = extendedDate.FilePath.Split('\\')[5];//5 is the index of the folder in the path split by \. //folderNameLenght-2

                if (!string.IsNullOrEmpty(folderName))
                {
                    Directory.Delete(uploadsFolderPath + folderName, true);
                }
                System.IO.File.Delete(uploadsFolderPath + file.FileName);

                db.TeachersFiles.Remove(teachersFile);
                await db.SaveChangesAsync();

                db.Files.Remove(file);
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);

                return Json(400);
            }

            return Json(200);
        }

        public async Task<string> ObjectFromFile(string folderName)
        {
            // Load the XML file from our project directory
            var currentDirectory = System.IO.Path.GetTempPath() + folderName;
            string[] xmlFilesPath = Directory.GetFiles(currentDirectory, "*.xml", SearchOption.AllDirectories);//Server.MapPath()

            foreach (var filePath in xmlFilesPath)
            {
                if (filePath.Contains("imsmanifest.xml"))
                {

                    XElement xmlFile = XElement.Load(filePath);//load and search
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filePath);

                    //Display all the book titles.
                   // XmlNodeList elemList = doc.GetElementsByTagName("resource");
                    XmlNodeList elemList = doc.GetElementsByTagName("file");
                    for (int i = 0; i < elemList.Count; i++)
                    {
                      //  var url = elemList[i].FirstChild.Attributes.GetNamedItem("href").InnerText;
                        var url = elemList[i].Attributes.GetNamedItem("href").InnerText;
                        if (url.Contains("index.html")) // || url.Contains("index_lms.html")
                        {
                            string fullUrl = currentDirectory + "/" + url;
                            TempData["ScormFullUrl"] = fullUrl;
                            TempData["ScormUrl"] = url;
                            return fullUrl;
                        }
                        
                        //var elementInnerXml = elemList[i].InnerXml;
                        //Console.WriteLine(elemList[i].InnerXml);
                    }


                    //string xmlFileString = xmlFile.ToString();//maybe to be used

                    var xmlFileContent = xmlFile.Descendants("page");//.Where(x => x.Attribute("name").Value != "header" && x.Attribute("name").Value != "footer" && x.Attribute("name").Value != "Lesson Report")
                    //var xmlFileAssets = xmlFile.Descendants("asset");


                    //var xmlFileParentAssets = xmlFile.Descendants("style");//maybe to be used


                    foreach (var xElement in xmlFileContent)
                    {
                        //DEV_SCORM_Pages pageAttr = new DEV_SCORM_Pages();
                        //pageAttr.PageId = Regex.Unescape(xElement.Attribute("id").Value.ToString());
                        //pageAttr.Href = Regex.Unescape(xElement.Attribute("href").Value.ToString());
                        //pageAttr.Name = Regex.Unescape(xElement.Attribute("name").Value.ToString());
                        //pageAttr.Preview = Regex.Unescape(xElement.Attribute("preview").Value.ToString());
                        //pageAttr.Reportable = Regex.Unescape(xElement.Attribute("reportable").Value.ToString());
                        //pageAttr.modulesMaxScore = xElement.Attribute("modulesMaxScore") != null ? Regex.Unescape(xElement.Attribute("modulesMaxScore").Value.ToString()) : "";
                        //pageAttr.pageWeight = xElement.Attribute("pageWeight") != null ? Regex.Unescape(xElement.Attribute("pageWeight").Value.ToString()) : "";
                        //pageAttr.XmlString = Regex.Unescape(xElement.ToString());
                        //pageAttr.LessonKey = lessonDBObject.LessonKey;

                        //Pages.Add(pageAttr);
                        //db.DEV_SCORM_Pages.Add(pageAttr);

                        var pagesDirectory = filePath.Replace("main.xml", "");//Globals.Configuration.SCORM_UPLOADS_FOLDER + folderName + @"\pages\";
                        var pagePath = pagesDirectory + xElement.Attribute("href").Value.ToString();

                        XElement xmlPage = XElement.Load(pagePath);
                        string xmlPageString = xmlPage.ToString();


                        //foreach (var asset in xmlFileAssets)
                        //{
                        //    DEV_SCORM_Assets pageAsset = new DEV_SCORM_Assets();
                        //    if (xmlPageString.Contains(asset.Attribute("href").Value.ToString()))
                        //    {
                        //        pageAsset.PageId = Regex.Unescape(xElement.Attribute("id").Value.ToString());
                        //        pageAsset.Href = Regex.Unescape(asset.Attribute("href").Value.ToString());
                        //        pageAsset.FileName = Regex.Unescape(asset.Attribute("fileName").Value.ToString());
                        //        pageAsset.ContentType = Regex.Unescape(asset.Attribute("contentType").Value.ToString());
                        //        pageAsset.Title = Regex.Unescape(asset.Attribute("title").Value.ToString());
                        //        pageAsset.Type = Regex.Unescape(asset.Attribute("type").Value.ToString());
                        //        pageAsset.XmlString = Regex.Unescape(asset.ToString());

                        //        Assets.Add(pageAsset);
                        //        db.DEV_SCORM_Assets.Add(pageAsset);
                        //    }

                        //}

                    }

                }
                else
                {
                    return "";
                }

            }

            return "";
        }

        public async Task<string> ExtractFilesFromZipped(string zipName,string startFolderPath)
        {
            string folderName = @"extractedFiles" + DateTime.Now.Ticks.ToString();
            string extractPath = startFolderPath + folderName;
            ZipFile.ExtractToDirectory(startFolderPath + zipName, extractPath);
            return folderName;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SortByAlphabet(string alphabet)
        {
            List<TeachersFile> teachersFile = new List<TeachersFile>();
            string userKey = CurrentUser.Id;
            var teacher = await db.AspNetUsers.Where(u => u.Id == userKey).FirstOrDefaultAsync();
           
            if (alphabet == "AZ")
            {
                teachersFile = await db.TeachersFiles.Where(tf => tf.Teacherkey == teacher.TeacherKey).OrderByDescending(t => t.File.MetaData).ThenByDescending(t => t.File.FileName).ThenByDescending(t => t.File.CreationDate).ThenBy(t => t.File.StatusKey).Take(TAKE_LESSON).ToListAsync();
            }
            else
            {
                teachersFile = await db.TeachersFiles.Where(tf => tf.Teacherkey == teacher.TeacherKey).OrderBy(t => t.File.MetaData).ThenBy(t => t.File.FileName).ThenByDescending(t => t.File.CreationDate).ThenBy(t => t.File.StatusKey).Take(TAKE_LESSON).ToListAsync();
            }

            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("LessonBuilderManagement", "AdaptLessons", CurrentLanguageIdentifier);
            return PartialView("~/Views/LessonsBank/_AuthoringLessonCard.cshtml", teachersFile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SortByStatus(string statusIndex)
        {
            List<TeachersFile> teachersFile = new List<TeachersFile>();
            string userKey = CurrentUser.Id;
            var teacher = await db.AspNetUsers.Where(u => u.Id == userKey).FirstOrDefaultAsync();
            int pendingStatus = (int)RLI.Common.Enums.StatusEnum.StatusPending;
            int finalStatus = (int)RLI.Common.Enums.StatusEnum.StatusApproved;

            if (statusIndex == "Pending")
            {
                teachersFile = await db.TeachersFiles.Where(tf => tf.Teacherkey == teacher.TeacherKey).OrderByDescending(t => t.File.StatusKey == pendingStatus).ThenByDescending(t => t.File.CreationDate).ThenByDescending(t => t.File.MetaData).ThenByDescending(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Take(TAKE_LESSON).ToListAsync();
            }
            else
            {
                teachersFile = await db.TeachersFiles.Where(tf => tf.Teacherkey == teacher.TeacherKey).OrderByDescending(t => t.File.StatusKey == finalStatus).ThenByDescending(t => t.File.CreationDate).ThenByDescending(t => t.File.MetaData).ThenByDescending(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Take(TAKE_LESSON).ToListAsync();
            }

            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("LessonBuilderManagement", "AdaptLessons", CurrentLanguageIdentifier);
            return PartialView("~/Views/LessonsBank/_AuthoringLessonCard.cshtml", teachersFile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SortByDate(string dateIndex)
        {
            List<TeachersFile> teachersFile = new List<TeachersFile>();
            string userKey = CurrentUser.Id;
            var teacher = await db.AspNetUsers.Where(u => u.Id == userKey).FirstOrDefaultAsync();
            
            if (dateIndex == "Asc")
            {
                teachersFile = await db.TeachersFiles.Where(tf => tf.Teacherkey == teacher.TeacherKey).OrderBy(t => t.File.CreationDate).ThenBy(t => t.File.MetaData).ThenBy(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Take(TAKE_LESSON).ToListAsync();
            }
            else
            {
                teachersFile = await db.TeachersFiles.Where(tf => tf.Teacherkey == teacher.TeacherKey).OrderByDescending(t => t.File.CreationDate).ThenBy(t => t.File.MetaData).ThenBy(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Take(TAKE_LESSON).ToListAsync();
            }

            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("LessonBuilderManagement", "AdaptLessons", CurrentLanguageIdentifier);
            return PartialView("~/Views/LessonsBank/_AuthoringLessonCard.cshtml", teachersFile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Search(string query)
        {
            if (query.Length < 3)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NoContent);
            }
            int pendingStatus = (int)RLI.Common.Enums.StatusEnum.StatusPending;
            List<object> result = new List<object>();
            string searchQuery = query.ToLower();
            try
            {
                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                {
                    string userKey = CurrentUser.Id;
                    var teacher = await db.AspNetUsers.Where(u => u.Id == userKey).FirstOrDefaultAsync();
                    int? teacherKey = teacher.TeacherKey;
                    var lessons = await db.TeachersFiles.Where(tf => (tf.Teacherkey == teacherKey) && ((tf.File.FileName.Contains(searchQuery))|| tf.File.MetaData.Contains(searchQuery))).Distinct().Select(ss => new
                    {
                        id = ss.File.MetaData==null?ss.File.FileName: ss.File.MetaData,
                        title = ss.File.MetaData == null ? ss.File.FileName.Replace("-", " ").Replace(".zip", "") : ss.File.MetaData,
                        text = "<h5 class='m-auto d-block'>" + (ss.File.MetaData == null ? ss.File.FileName.Replace("-", " ").Replace(".zip", "") : ss.File.MetaData) + "</h5>"
                    }).ToListAsync();
                    result.AddRange(lessons.Take(10));

                }
                else
                {
                    var lessons = db.TeachersFiles.Where(tf => (tf.File.StatusKey == pendingStatus) && ((tf.File.FileName.Contains(searchQuery))|| tf.File.MetaData.Contains(searchQuery))).Distinct().Select(ss => new
                    {
                        id = ss.File.MetaData == null ? ss.File.FileName : ss.File.MetaData,
                        title = ss.File.MetaData == null ? ss.File.FileName : ss.File.MetaData,
                        text = "<h5 class='m-auto d-block'>" + (ss.File.MetaData == null ? ss.File.FileName.Replace("-", " ").Replace(".zip", "") : ss.File.MetaData) + "</h5>"
                    });
                    result.AddRange(await lessons.Take(10).ToListAsync());
                }

            }
            catch (Exception e)
            {
                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.ToString());

                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            return Json(result);
        }

        // GET: LessonBuilderManagement LessonMapping
        public async Task<ActionResult> LessonMapping(Guid file)
        {
            ViewBag.fileKey = file;
            List<RLI.EntityFramework.EDM.Grade> grades = new List<RLI.EntityFramework.EDM.Grade>();
            List<RLI.EntityFramework.EDM.Subject> subjects = new List<RLI.EntityFramework.EDM.Subject>();
            if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
            {
                grades = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey).Select(sstg => sstg.Grade).ToListAsync();
                subjects = await db.SchoolSubjectTeacherGrades.Where(sstg => sstg.TeacherKey == CurrentUser.TeacherKey).Select(sstg => sstg.Subject).ToListAsync();
            }
            else
            {
                grades = await db.Grades.ToListAsync();
                subjects = await db.Subjects.ToListAsync();
            }
            ViewBag.GradeKey = new SelectList(grades.OrderBy(g => g.GradeIndex).Select(g => new
            {
                GradeKey = g.GradeKey,
                DefaultGrade1 = g.Grade1,
                Grade1 = CurrentLanguageIdentifier == null ? g.Grade1 : g.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
            }), "GradeKey", "Grade1");
            ViewBag.SubjectKey = new SelectList(subjects.OrderBy(s => s.SubjectIndex).Select(s => new
            {
                SubjectKey = s.SubjectKey,
                DefaultGrade1 = s.Subject1,
                Subject1 = CurrentLanguageIdentifier == null ? s.Subject1 : s.DataGUID.DataTranslations.Where(dt => dt.LanguageKey == CurrentLanguageIdentifier).FirstOrDefault().Value,
            }), "SubjectKey", "Subject1");
            ViewBag.ChapterTypeKey = new SelectList(db.ChaptersTypes, "ChapterTypeKey", "ChapterType");

            List<Chapter> chapters = new List<Chapter>();
            ViewBag.ChapterKey = new SelectList(chapters, "ChapterKey", "ChapterName");

            //var ProvidersDropDown = (List<Dictionary<string, string>>)HttpContext.Items["ProvidersDropDown"];
            //var providers = ProvidersDropDown.Select(c => new
            //{
            //    id = c["ESystemKey"],
            //    ESystemIndex = int.Parse(c["ESystemIndex"]),
            //    text = c["ESystemName"],
            //}).OrderBy(c => c.ESystemIndex);

            ViewBag.ESystemKey = new SelectList(db.ESystemScoped, "id", "text");

            ViewBag.LessonTypeKey = new SelectList(db.LessonTypes, "LessonTypeKey", "LessonType1");

            List<Topic> topics = new List<Topic>();
            ViewBag.TopicKey = new SelectList(topics, "TopicKey", "Topic1");

            ViewBag.Languages = await db.Languages.OrderBy(l => l.LanguageDisplayKey).Where(l => l.LanguageDisplayKey == 4).ToListAsync();
            ViewBag.LanguageKey = new SelectList(db.Languages.OrderBy(g => g.Indexx).Where(l => l.LanguageKey != 2), "LanguageKey", "Language1");

            return View();
        }

        // GET: LessonBuilderManagement AdaptLessons
        public async Task<ActionResult> ViewAdaptLesson()
        {



            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> MapLessons(Guid fileKey, int topicKey)
        {
            File currentFile = await db.Files.Where(f => f.FileKey == fileKey).FirstOrDefaultAsync();

            currentFile.StatusKey = (int)RLI.Common.Enums.StatusEnum.StatusApproved;

            db.Entry(currentFile).State = EntityState.Modified;

            string fileName = currentFile.ExtendedDatas.FirstOrDefault().FileName;
            string fileDescription = currentFile.ExtendedDatas.FirstOrDefault().HTML;
            var lessonUrl = Url.Action("Index", "ScormPlayer", new { fileName = currentFile.ExtendedDatas.FirstOrDefault().FilePath });
            Lesson lesson = new Lesson();
            lesson.LessonName = fileName;
            lesson.Remark = fileDescription;
            lesson.LessonURL = lessonUrl;
            lesson.TopicKey = topicKey;
            lesson.LessonTypeKey = (int)RLI.Common.Enums.LessonType.Scorm;
            lesson.LanguageKey = db.Topics.Where(t => t.TopicKey == topicKey).FirstOrDefault().LanguageKey;
            lesson.ESystemKey = (await db.ESystems.Where(e => e.ESystemName == "Authoring").FirstOrDefaultAsync()).ESystemKey;
            lesson.FileKey = fileKey;
            lesson.StatusKey = (int)RLI.Common.Enums.StatusEnum.StatusPending;

            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
            return Json("Success");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> LessonsFilter(string query, int? records)
        {
            int numberOfRecords = (int)(records == null ? 50 : records);
            List<TeachersFile> lessons = new List<TeachersFile>();
            if (query == "")
            {
                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                {

                    string userKey = CurrentUser.Id;
                    var teacher = await db.AspNetUsers.Where(u => u.Id == userKey).FirstOrDefaultAsync();
                    int? teacherKey = teacher.TeacherKey;
                    lessons = await db.TeachersFiles.Where(t => t.Teacherkey == teacherKey).OrderByDescending(t => t.File.CreationDate).ThenBy(t => t.File.MetaData).ThenBy(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Take(TAKE_LESSON).ToListAsync();
                }
                else
                {
                    int pendingStatus = (int)RLI.Common.Enums.StatusEnum.StatusPending;
                    lessons = await db.TeachersFiles.Where(tf => tf.File.StatusKey == pendingStatus).OrderByDescending(t => t.File.CreationDate).ThenBy(t => t.File.MetaData).ThenBy(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Take(TAKE_LESSON).ToListAsync();
                }

            }
            else
            {
                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                {
                    string userKey = CurrentUser.Id;
                    var teacher = await db.AspNetUsers.Where(u => u.Id == userKey).FirstOrDefaultAsync();
                    int? teacherKey = teacher.TeacherKey;
                    lessons = await db.TeachersFiles.Where(t => ((t.File.FileName.Contains(query)) || t.File.MetaData.Contains(query)) && (t.Teacherkey == teacherKey)).OrderByDescending(t => t.File.CreationDate).ThenBy(t => t.File.MetaData).ThenBy(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Take(numberOfRecords).ToListAsync();
                }
                else
                {
                    int pendingStatus = (int)RLI.Common.Enums.StatusEnum.StatusPending;
                    lessons = await db.TeachersFiles.Where(t => ((t.File.FileName.Contains(query)) || t.File.MetaData.Contains(query)) && (t.File.StatusKey == pendingStatus)).OrderByDescending(t => t.File.CreationDate).ThenBy(t => t.File.MetaData).ThenBy(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Take(numberOfRecords).ToListAsync();
                }
            }
            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("LessonBuilderManagement", "AdaptLessons", CurrentLanguageIdentifier);
            return PartialView("~/Views/LessonsBank/_AuthoringLessonCard.cshtml", lessons);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextLessons(int skip)
        {
            try
            {
                List<TeachersFile> lessons = new List<TeachersFile>();

                ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("LessonBuilderManagement", "AdaptLessons", CurrentLanguageIdentifier);
                if (RLI.Common.Managers.UserManager.GetUserRole(CurrentUser.Id).Name=="Teacher")
                {
                    string userKey = CurrentUser.Id;
                    var teacher = await db.AspNetUsers.Where(u => u.Id == userKey).FirstOrDefaultAsync();
                    int? teacherKey = teacher.TeacherKey;
                    lessons = await db.TeachersFiles.Where(t => (t.Teacherkey == teacherKey)).OrderByDescending(t => t.File.CreationDate).ThenBy(t => t.File.MetaData).ThenBy(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Skip(skip * TAKE_LESSON).Take(TAKE_LESSON).ToListAsync();
                }
                else
                {
                    int pendingStatus = (int)RLI.Common.Enums.StatusEnum.StatusPending;
                    lessons = await db.TeachersFiles.Where(tf => tf.File.StatusKey == pendingStatus).OrderByDescending(t => t.File.CreationDate).ThenBy(t => t.File.MetaData).ThenBy(t => t.File.FileName).ThenBy(t => t.File.StatusKey).Skip(skip * TAKE_LESSON).Take(TAKE_LESSON).ToListAsync();
                }
                if (lessons.Count() == 0)
                {
                    return Json(404);
                }
                return PartialView("~/Views/LessonsBank/_AuthoringLessonCard.cshtml", lessons);
            }
            catch (Exception e)
            {
                return Json("error");
            }

        }
    }
}