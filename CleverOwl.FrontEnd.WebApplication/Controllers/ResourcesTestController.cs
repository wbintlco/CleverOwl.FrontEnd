using Everest.OpenCurate.EntityFramework.EDM;
using Everest.OpenCurate.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace CleverOwl.FrontEnd.WebApplication.Controllers
{
    public class ResourcesTestController : BaseController
    {
        private OpenCurateEntities db = new OpenCurateEntities();

        // GET: Resources
        public async Task<ActionResult> Index(int? grade, int? subject, int? topic, int? tag)
        {
            //ViewBag.grade = grade;
            //ViewBag.subject = subject;
            //ViewBag.topic = topic;
            //ViewBag.tag = tag;
            var candidates = db.Candidates.Where(c =>
            ((grade != null && (c.GradeKey == grade)) || (grade == null)) &&
            ((subject != null && (c.SubjectKey == subject)) || (subject == null)) &&
            ((topic != null && (c.TopicKey == topic)) || (topic == null)) &&
             ((tag != null && c.CandidateTags.Any(t => t.TagKey == tag)) || (tag == null)))
            .OrderByDescending(c => c.PublishedDate)
            .Where(c => c.StatusKey == 3);

            return View(await candidates.ToListAsync());
        }

        // GET: Resources/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Candidate candidate = await db.Candidates.FindAsync(id);
            if (candidate == null)
            {
                return HttpNotFound();
            }
            return View(candidate);
        }

        // GET: Resources/Create
        public ActionResult Create()
        {
            ViewBag.FileKey = new SelectList(db.Files, "FileKey", "FileName");
            ViewBag.GradeKey = new SelectList(db.Grades, "GradeKey", "Grade1");
            ViewBag.LanguageKey = new SelectList(db.Languages, "LanguageKey", "Language1");
            ViewBag.StatusKey = new SelectList(db.Status, "StatusKey", "Status1");
            ViewBag.SubjectKey = new SelectList(db.Subjects, "SubjectKey", "Subject1");
            ViewBag.TopicKey = new SelectList(db.Topics, "TopicKey", "Topic1");
            return View();
        }

        // POST: Resources/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "CandidateKey,ForeignCandidateKey,SubjectKey,GradeKey,TopicKey,PublishedDate,StatusKey,Title,LanguageKey,Scope,Summary,URL,FileKey,Notes")] Candidate candidate)
        {
            if (ModelState.IsValid)
            {
                db.Candidates.Add(candidate);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.FileKey = new SelectList(db.Files, "FileKey", "FileName", candidate.FileKey);
            ViewBag.GradeKey = new SelectList(db.Grades, "GradeKey", "Grade1", candidate.GradeKey);
            ViewBag.LanguageKey = new SelectList(db.Languages, "LanguageKey", "Language1", candidate.LanguageKey);
            ViewBag.StatusKey = new SelectList(db.Status, "StatusKey", "Status1", candidate.StatusKey);
            ViewBag.SubjectKey = new SelectList(db.Subjects, "SubjectKey", "Subject1", candidate.SubjectKey);
            ViewBag.TopicKey = new SelectList(db.Topics, "TopicKey", "Topic1", candidate.TopicKey);
            return View(candidate);
        }

        // GET: Resources/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Candidate candidate = await db.Candidates.FindAsync(id);
            if (candidate == null)
            {
                return HttpNotFound();
            }
            ViewBag.FileKey = new SelectList(db.Files, "FileKey", "FileName", candidate.FileKey);
            ViewBag.GradeKey = new SelectList(db.Grades, "GradeKey", "Grade1", candidate.GradeKey);
            ViewBag.LanguageKey = new SelectList(db.Languages, "LanguageKey", "Language1", candidate.LanguageKey);
            ViewBag.StatusKey = new SelectList(db.Status, "StatusKey", "Status1", candidate.StatusKey);
            ViewBag.SubjectKey = new SelectList(db.Subjects, "SubjectKey", "Subject1", candidate.SubjectKey);
            ViewBag.TopicKey = new SelectList(db.Topics, "TopicKey", "Topic1", candidate.TopicKey);
            return View(candidate);
        }

        // POST: Resources/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "CandidateKey,ForeignCandidateKey,SubjectKey,GradeKey,TopicKey,PublishedDate,StatusKey,Title,LanguageKey,Scope,Summary,URL,FileKey,Notes")] Candidate candidate)
        {
            if (ModelState.IsValid)
            {
                db.Entry(candidate).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.FileKey = new SelectList(db.Files, "FileKey", "FileName", candidate.FileKey);
            ViewBag.GradeKey = new SelectList(db.Grades, "GradeKey", "Grade1", candidate.GradeKey);
            ViewBag.LanguageKey = new SelectList(db.Languages, "LanguageKey", "Language1", candidate.LanguageKey);
            ViewBag.StatusKey = new SelectList(db.Status, "StatusKey", "Status1", candidate.StatusKey);
            ViewBag.SubjectKey = new SelectList(db.Subjects, "SubjectKey", "Subject1", candidate.SubjectKey);
            ViewBag.TopicKey = new SelectList(db.Topics, "TopicKey", "Topic1", candidate.TopicKey);
            return View(candidate);
        }

        // GET: Resources/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Candidate candidate = await db.Candidates.FindAsync(id);
            if (candidate == null)
            {
                return HttpNotFound();
            }
            return View(candidate);
        }

        // POST: Resources/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Candidate candidate = await db.Candidates.FindAsync(id);
            db.Candidates.Remove(candidate);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
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

            var candidates = db.Candidates.Where(c => c.StatusKey == 3).Where(c =>
               c.Title.ToLower().Contains(query)
            || c.Topic.Topic1.ToLower().Contains(query)).Select(c => new
            {
                id = c.CandidateKey.ToString(),
                title = c.Title,
                text = textHtml1 + "fa fa-book-open" + textHtml2 + "Resources" + textHtml3 + c.Title + textHtml4,
                location = candidatesLink + "/" + c.CandidateKey.ToString()
            });

            var topics = db.Topics.Where(t => t.Candidates.Any(c => c.StatusKey == 3)).Where(t =>
               t.Topic1.ToLower().Contains(query)
            || t.Subject.Subject1.ToLower().Contains(query)).Select(c => new
            {
                id = c.TopicKey.ToString(),
                title = c.Topic1,
                text = textHtml1 + "fa fa-comments" + textHtml2 + "Topics" + textHtml3 + c.Topic1 + textHtml4,
                location = resourcesFilterLink + "/?topic=" + c.TopicKey.ToString()
            });

            var subjects = db.Subjects.Where(s => s.Candidates.Any(c => c.StatusKey == 3)).Where(s =>
                s.Subject1.ToLower().Contains(query)).Select(s => new
                {
                    id = s.SubjectKey.ToString(),
                    title = s.Subject1,
                    text = textHtml1 + "fab fa-discourse" + textHtml2 + "Subjects" + textHtml3 + s.Subject1 + textHtml4,
                    location = resourcesFilterLink + "/?subject=" + s.SubjectKey.ToString()
                });

            var grades = db.Grades.Where(g => g.Candidates.Any(c => c.StatusKey == 3)).Where(g =>
                  g.Grade1.ToLower().Contains(query)).Select(g => new
                  {
                      id = g.GradeKey.ToString(),
                      title = g.Grade1,
                      text = textHtml1 + "fa fa-cubes" + textHtml2 + "Grades" + textHtml3 + g.Grade1 + textHtml4,
                      location = resourcesFilterLink + "/?subject=" + g.GradeKey.ToString()
                  });

            var tags = db.Tags.Where(t => t.CandidateTags.Any(ct => ct.Candidate.StatusKey == 3)).Where(t =>
                     t.Tag1.ToLower().Contains(query)).Select(t => new
                     {
                         id = t.TagKey.ToString(),
                         title = t.Tag1,
                         text = textHtml1 + "fa fa-tags" + textHtml2 + "Tags" + textHtml3 + t.Tag1 + textHtml4,
                         location = resourcesFilterLink + "/?tag=" + t.TagKey.ToString()
                     });

            List<object> result = new List<object>();

            result.AddRange(await candidates.Distinct().Take(10).ToListAsync());
            result.AddRange(await topics.Distinct().Take(10).ToListAsync());
            result.AddRange(await subjects.Distinct().Take(10).ToListAsync());
            result.AddRange(await grades.Distinct().Take(10).ToListAsync());
            result.AddRange(await tags.Distinct().Take(10).ToListAsync());

            return Json(result);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextCandidates(int currentIndex, string gradesStr, string subjectsStr)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            List<int> gradesList = new List<int>();
            List<int> subjectsList = new List<int>();
            if(gradesStr != "")
            {
                gradesList = (List<int>)serializer.Deserialize(gradesStr, typeof(List<int>));
            }
            if(subjectsStr != "")
            {
                subjectsList = (List<int>)serializer.Deserialize(subjectsStr, typeof(List<int>));
            }
            
            int count = await db.Candidates.Where(c =>
                ((gradesList.Count != 0 && (gradesList.Contains((int)c.GradeKey))) || (gradesList.Count == 0)) &&
                ((subjectsList.Count != 0 && (subjectsList.Contains((int)c.SubjectKey))) || (subjectsList.Count == 0)))
                .OrderByDescending(c => c.PublishedDate)
                .Where(c => c.StatusKey == 3).CountAsync();
            if (currentIndex >= count)
            {
                return Json("empty");
            }


            var candidates = await db.Candidates.Where(c =>
                ((gradesList.Count != 0 && gradesList.Contains((int)c.GradeKey)) || (gradesList.Count == 0)) &&
                ((subjectsList.Count != 0 && subjectsList.Contains((int)c.SubjectKey)) || (subjectsList.Count == 0)))
                .OrderByDescending(c => c.PublishedDate)
                .Where(c => c.StatusKey == 3).Skip(currentIndex).Take(10).Select(c => new
            {
                candidateKey = c.CandidateKey,
                URL = c.URL,
                topic = c.Topic.Topic1,
                title = c.Title,
                grade = c.Grade.Grade1,
                subject = c.Subject.Subject1

            }).ToListAsync();
            return Json(candidates);



        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}