using RLI.EntityFramework.EDM;
using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FrontEnd.WebApplication.Controllers
{
    public class LessonBuilderManagementTestController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        // GET: LessonBuilderManagementTest
        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
           
            return View();
        }
        public async System.Threading.Tasks.Task<ActionResult> LessonMapping()
        {


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

            ViewBag.Topics = new SelectList(db.Topics, "TopicKey", "Topic1", selectedValue: default);
            ViewBag.Lessons = new SelectList(db.Lessons, "LessonKey", "LessonType", selectedValue: default);
           // ViewBag.Languages = await db.Languages.OrderBy(l => l.LanguageDisplayKey).Where(l => l.LanguageDisplayKey == 4).ToListAsync();
            ViewBag.LanguageKey = new SelectList(db.Languages.OrderBy(g => g.Indexx).Where(l => l.LanguageKey != 2), "LanguageKey", "Language1");

            ViewBag.Providers = new SelectList(db.ESystems, "ESystemKey", "ESystemName", selectedValue: default);
            return View();
        }
        
        [HttpPost]
        public ActionResult MapLessons(int gradeKey, int subjectKey, int topicKey, int providerKey, int lessonTypeKey)
        {
            var sum = gradeKey + subjectKey + topicKey + providerKey + lessonTypeKey;
            var data = new { status = "ok", result = sum };
            return Json(data, JsonRequestBehavior.DenyGet);
        }
        [HttpPost]
        public ActionResult SaveFile(String jsonKey)
        {
            var data = new { status = "ok", result = jsonKey };
            return Json(data);
            
        }
    }
}