using RLI.EntityFramework.EDM;
using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CleverOwl.WebApplication.Controllers
{
    [Authorize]
    public class TestLessonsPlansController : BaseController
    {
        const int DEFAULT_LOAD_COUNT = 10;
        // GET: TestLessonsPlans
        public async Task<ActionResult> Index()
        {
            var synchronousSessions = db.SynchronousSessions.Where(ss => ss.SessionDate >= DateTime.Now && ss.CreatedByUserKey == CurrentUser.Id).OrderBy(ss => ss.SessionDate).Take(DEFAULT_LOAD_COUNT);
            List<RLI.EntityFramework.EDM.Grade> grades = await synchronousSessions.Select(a => a.SchoolSubjectTeacherGrade.Grade).Distinct().ToListAsync();
            List<RLI.EntityFramework.EDM.Subject> subjects = await synchronousSessions.Select(a => a.SchoolSubjectTeacherGrade.Subject).Distinct().ToListAsync();
            List<RLI.EntityFramework.EDM.Chapter> chapters = await synchronousSessions.Select(a => a.Chapter).Distinct().ToListAsync();
            List<RLI.EntityFramework.EDM.School> schools = await db.Schools.Distinct().ToListAsync();
              
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

            ViewBag.Chapters = new SelectList(chapters, "ChapterKey", "ChapterName");
            ViewBag.Schools = new SelectList(schools,"SchoolKey","SchoolName");


            return View();
        }

 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetChapters(string query = null, int subjectKey = 0)
        {
            var synchronousSessions = db.SynchronousSessions.Where(ss => ss.SessionDate >= DateTime.Now && ss.CreatedByUserKey == CurrentUser.Id).OrderBy(ss => ss.SessionDate).Take(DEFAULT_LOAD_COUNT);


            Subject subject = await synchronousSessions.Where(s => s.SchoolSubjectTeacherGrade.SubjectKey == subjectKey).Select(s=>s.SchoolSubjectTeacherGrade.Subject).FirstOrDefaultAsync();
            List<Chapter> chapterss =  subject.Chapters.ToList();
            var chapters = chapterss.Select(g => new
            {
                ChapterKey = g.ChapterKey,
                ChapterName=g.ChapterName
                
            }).Distinct().Take(DEFAULT_LOAD_COUNT).ToList();

            var chaptersSelectList = chapters.Select(s => new
            {
                id = s.ChapterKey,
                text = s.ChapterName
            }).ToList();

            return Json(chaptersSelectList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetTopics(string query = null, int chapterKey = 0)
        {
            
            var synchronousSessions = db.SynchronousSessions.Where(ss => ss.SessionDate >= DateTime.Now && ss.CreatedByUserKey == CurrentUser.Id).OrderBy(ss => ss.SessionDate).Take(DEFAULT_LOAD_COUNT);
            Chapter chapter = await db.Chapters.Where(s => s.ChapterKey == chapterKey).FirstOrDefaultAsync();
            List<ChaptersTopic> chapterTopics = chapter.ChaptersTopics.ToList();
            var topics = chapterTopics.Select(g => new
            {
                id = g.TopicKey,
                text = g.Topic.Topic1
            }).ToList();

          

            return Json(topics);
        }


    }
}