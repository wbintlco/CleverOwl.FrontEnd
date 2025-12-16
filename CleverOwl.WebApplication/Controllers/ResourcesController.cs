using Everest.OpenCurate.EntityFramework.EDM;
using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace CleverOwl.WebApplication.Controllers
{
    public class ResourcesController : BaseController
    {
        private Everest.OpenCurate.EntityFramework.EDM.OpenCurateEntities db = new OpenCurateEntities();

        // GET: Resources
        public async Task<ActionResult> Index(int? grade, int? subject, int? topic, int? tag)
        {
            var candidates = db.Candidates.Where(c =>
            ((grade != null && (c.GradeKey == grade)) || (grade == null)) &&
            ((subject != null && (c.SubjectKey == subject)) || (subject == null)) &&
            ((topic != null && (c.LearningOutcomeKey == topic)) || (topic == null)) &&
             ((tag != null && c.CandidateTags.Any(t => t.TagKey == tag)) || (tag == null)))
            .OrderByDescending(c => c.UpdatedAt)
            .Where(c => c.StatusKey == 3);

            return View(await candidates.ToListAsync());
        }

        public async Task<ActionResult> DetailsR(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            string redirectUrl = "";
            Candidate candidate = await db.Candidates.FindAsync(id);
            if (candidate == null)
            {
                return HttpNotFound();
            }

            redirectUrl = "~/Resource/" + id + "/" + Regex.Replace(candidate.Title.Trim(), @"[^a-zA-Z0-9^\u0600-\u06FF]+", "-");

            return Redirect(redirectUrl);
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

            ViewBag.candidateId = candidate.CandidateKey;
            var topicsFound = candidate.CandidateTags.Where(t => t.Tag.TagTypeKey == 2);
            string topicsFoundText = "";
            foreach (var topic in topicsFound)
            {
                for (int i = 0; i <= topic.Weight; i++)
                {
                    topicsFoundText += topic.Tag.Tag1 + " ";
                }
            }

            ViewBag.topicsFound = topicsFoundText;

            var entitiesFound = candidate.CandidateTags.Where(t => t.Tag.TagTypeKey == 3);
            List<Dictionary<string, object>> entitiesFoundDict = new List<Dictionary<string, object>>();
            foreach (var entity in entitiesFound)
            {
                Dictionary<string, object> entityFound = new Dictionary<string, object>();
                entityFound.Add("name", entity.Tag.Tag1);
                entityFound.Add("value", entity.Weight);

                entitiesFoundDict.Add(entityFound);
            }

            //ViewBag.FileKey = await CheckCandidateScreenshot(candidate.CandidateKey);

            ViewBag.entitiesFoundDict = entitiesFoundDict;
            return View(candidate);
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
            || c.LearningOutcome.LearningOutcome1.ToLower().Contains(query)).Select(c => new
            {
                id = c.CandidateKey.ToString(),
                title = c.Title,
                text = textHtml1 + "fa fa-book-open" + textHtml2 + "Resources" + textHtml3 + c.Title + textHtml4,
                external = true,
                location = c.URL
            });

            var topics = db.LearningOutcomes.Where(t => t.Candidates.Any(c => c.StatusKey == 3)).Where(t =>
               t.LearningOutcome1.ToLower().Contains(query)
            || t.Subject.Subject1.ToLower().Contains(query)).Select(c => new
            {
                id = c.LearningOutcomeKey.ToString(),
                title = c.LearningOutcome1,
                text = textHtml1 + "fa fa-comments" + textHtml2 + "Topics" + textHtml3 + c.LearningOutcome1 + textHtml4,
                location = resourcesFilterLink + "/?topic=" + c.LearningOutcomeKey.ToString()
            });

            var subjects = db.Subjects.Where(s => s.Candidates.Any(c => c.StatusKey == 3)).Where(s =>
                s.Subject1.ToLower().Contains(query)).Select(s => new
                {
                    id = s.SubjectKey.ToString(),
                    title = s.Subject1,
                    text = textHtml1 + "fab fa-discourse" + textHtml2 + "Subjects" + textHtml3 + s.Subject1 + textHtml4,
                    location = resourcesFilterLink + "/?subject=" + s.SubjectKey.ToString()
                });

            //var grades = db.Grades.Where(g => g.Candidates.Any(c => c.StatusKey == 3)).Where(g =>
            //      g.Grade1.ToLower().Contains(query)).Select(g => new
            //      {
            //          id = g.GradeKey.ToString(),
            //          title = g.Grade1,
            //          text = textHtml1 + "fa fa-cubes" + textHtml2 + "Grades" + textHtml3 + g.Grade1 + textHtml4,
            //          location = resourcesFilterLink + "/?subject=" + g.GradeKey.ToString()
            //      });

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
            //result.AddRange(await grades.Distinct().Take(10).ToListAsync());
            //  result.AddRange(await tags.Distinct().Take(10).ToListAsync());

            TempData["ContentBrowserSearchQuery"] = query;

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextCandidates(int currentIndex, string gradesStr, string subjectsStr)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            List<int> gradesList = new List<int>();
            List<int> subjectsList = new List<int>();
            if (gradesStr != "")
            {
                gradesList = (List<int>)serializer.Deserialize(gradesStr, typeof(List<int>));
            }
            if (subjectsStr != "")
            {
                subjectsList = (List<int>)serializer.Deserialize(subjectsStr, typeof(List<int>));
            }

            IQueryable<Candidate> candidates = db.Candidates;

            //string contentBrowserSearchQuery = (string)TempData.Peek("ContentBrowserSearchQuery");
            //if (!string.IsNullOrEmpty(contentBrowserSearchQuery))
            //{
            //    candidates = db.CandidateTags.Where(ct => ct.Tag.Tag1.ToLower().Contains(contentBrowserSearchQuery)).Select(ct => ct.Candidate).Distinct();
            //}

            int count = await candidates.Where(c =>
                ((gradesList.Count != 0 && (gradesList.Contains((int)c.GradeKey))) || (gradesList.Count == 0)) &&
                ((subjectsList.Count != 0 && (subjectsList.Contains((int)c.SubjectKey))) || (subjectsList.Count == 0)))
                .OrderByDescending(c => c.PublishedDate)
                .Where(c => c.StatusKey == 3).CountAsync();
            if (currentIndex >= count)
            {
                return Json("404");
            }

            var returnedCandidates = await candidates.Where(c =>
                ((gradesList.Count != 0 && gradesList.Contains((int)c.GradeKey)) || (gradesList.Count == 0)) &&
                ((subjectsList.Count != 0 && subjectsList.Contains((int)c.SubjectKey)) || (subjectsList.Count == 0)))
                .OrderByDescending(c => c.PublishedDate)
                .Where(c => c.StatusKey == 3).Skip(currentIndex).Take(10).Select(c => new
                {
                    subjectKey = c.SubjectKey,
                    gradekey = c.GradeKey,
                    candidateKey = c.CandidateKey,
                    URL = c.URL,
                    topic = c.LearningOutcome.LearningOutcome1,
                    title = c.Title,
                    grade = c.Grade.Grade1,
                    subject = c.Subject.Subject1

                }).ToListAsync();
            //var candidatesGroupedByGrades = returnedCandidates.GroupBy(g => g.gradekey).ToList();

            //var gradesNumbers = returnedCandidates.GroupBy(
            //    p => p.gradekey,
            //    (key, value) => new
            //    {
            //        GradeKey = key,
            //        Count = value.Count()
            //    }).ToList();
            
           
            
            //var subjectsNumbers = returnedCandidates.GroupBy(
            //    p => p.subjectKey,
            //    (key, value) => new {
            //        GradeKey = key,
            //        Count = value.Count()
            //    }).ToList();
            //ViewBag.CandidatesGroupedByGradesKeyCount = gradesNumbers;
            //ViewBag.CandidatesGroupedBySubjectsKeyCount = subjectsNumbers;


            //var gradesPartial = RenderViewToString("~/Views/Resources/_Grades.cshtml", returnedCandidates.Select(x => x.grade).ToList()); /*PartialView("~/Views/Resources/_Grades.cshtml", returnedCandidates.Select(x => x.grade).ToList());*/


            //var subjectPartial = (PartialView("~/Views/Resources/_Subjects.cshtml"), returnedCandidates.Select(x => x.subject).ToList());
            //Dictionary<string, Object> partial = new Dictionary<string, Object>();

            //partial.Add("subjectsPartial", subjectPartial.ToString());
            //partial.Add("candidates",  returnedCandidates);

            return Json(returnedCandidates);
        }


        //public static string RenderPartialToString(string controlName, List<Grade> viewData)
        //{
        //    ViewPage viewPage = new ViewPage() { ViewContext = new ViewContext() };

        //    viewPage.ViewData = new ViewDataDictionary(viewData);
        //    viewPage.Controls.Add(viewPage.LoadControl(controlName));

        //    StringBuilder sb = new StringBuilder();
        //    using (StringWriter sw = new StringWriter(sb))
        //    {
        //        using (System.Web.UI.HtmlTextWriter tw = new HtmlTextWriter(sw))
        //        {
        //            viewPage.RenderControl(tw);
        //        }
        //    }

        //    return sb.ToString();
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetRelatedSubjects(string gradesKeys, string subjectsKey)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            List<int> gradesList = new List<int>();
            List<int> subjectsList = new List<int>();
            if (gradesKeys != "")
            {
                gradesList = (List<int>)serializer.Deserialize(gradesKeys, typeof(List<int>));
            }
            if (subjectsKey != "")
            {
                subjectsList = (List<int>)serializer.Deserialize(subjectsKey, typeof(List<int>));
            }
            List<Everest.OpenCurate.EntityFramework.EDM.Subject> subjectsRelatedToGradesSelected = await db.Candidates.Where(c =>
                ((gradesList.Count != 0 && gradesList.Contains((int)c.GradeKey)) || (gradesList.Count == 0)) &&
                ((subjectsList.Count != 0 && subjectsList.Contains((int)c.SubjectKey)) || (subjectsList.Count == 0)))
                .Where(c => c.StatusKey == 3).Select(c => c.Subject).ToListAsync();
            if (subjectsRelatedToGradesSelected.Count() == 0 || subjectsRelatedToGradesSelected == null)
            {
                return Json(404);
            }
            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Resources", "Index", CurrentLanguageIdentifier);
            return PartialView("~/Views/Resources/_Subjects.cshtml", subjectsRelatedToGradesSelected);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetRelatedGrades(string subjectsKeys, string gradesKey)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            List<int> subjectsList = new List<int>();
            List<int> gradesList = new List<int>();
            if (subjectsKeys != "")
            {
                subjectsList = (List<int>)serializer.Deserialize(subjectsKeys, typeof(List<int>));
            }
            if (gradesKey != "")
            {
                gradesList = (List<int>)serializer.Deserialize(gradesKey, typeof(List<int>));
            }
            List<Everest.OpenCurate.EntityFramework.EDM.Grade> gradesRelatedToSubjectsSelected = await db.Candidates.Where(c =>
                ((gradesList.Count != 0 && gradesList.Contains((int)c.GradeKey)) || (gradesList.Count == 0)) &&
                ((subjectsList.Count != 0 && subjectsList.Contains((int)c.SubjectKey)) || (subjectsList.Count == 0)))
                .Where(c => c.StatusKey == 3).Select(c => c.Grade).ToListAsync();
            if (gradesRelatedToSubjectsSelected.Count() == 0 || gradesRelatedToSubjectsSelected == null)
            {
                return Json(404);
            }
            ViewBag.Locale = await RLI.Common.Managers.UtilitiesManager.GetLocalisationPerPage("Resources", "Index", CurrentLanguageIdentifier);
            return PartialView("~/Views/Resources/_Grades.cshtml", gradesRelatedToSubjectsSelected);
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