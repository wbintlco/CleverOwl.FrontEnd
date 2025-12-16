using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FrontEnd.WebApplication.Models;
using Newtonsoft.Json;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Controllers;
using RLI.WebApplication.Models;
using RLI.WebApplication.Objects;

namespace FrontEnd.WebApplication.Controllers
{
    public class DomainsController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        // GET: Domians
        public async Task<ActionResult> Index()
        {
            List<Domain> domains = await db.Domains.ToListAsync();
            ViewBag.Grades = new SelectList(db.Grades.OrderBy(g => g.GradeIndex), "GradeKey", "Grade1", selectedValue: default);
            ViewBag.Subjects = new SelectList(db.Subjects.OrderBy(s => s.SubjectIndex), "SubjectKey", "Subject1", selectedValue: default);
            ViewBag.DomainsName = new SelectList(db.Domains.OrderBy(d => d.Domain1), "DomainKey", "Domain1", selectedValue: default);
            ViewBag.Status = new SelectList(db.Status.OrderBy(s => s.StatusKey), "StatusKey", "Status1", selectedValue: default);
            return View(domains);
        }

        // GET: Domians/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            Domain domain = await db.Domains.FindAsync(id);

            ViewBag.GradeKey = new SelectList(db.Grades, "GradeKey", "Grade1", domain.GradeKey);
            ViewBag.SubjectKey = new SelectList(db.Subjects, "SubjectKey", "Subject1", domain.SubjectKey);

            ViewBag.DataTranslations = await db.DataTranslations.Where(d => d.DataGUID == domain.LocalDomainGUID).ToListAsync();
            var dataTranslationKeys = await db.DataTranslations.Where(d => d.DataGUID == domain.LocalDomainGUID).Select(d => d.LanguageKey.ToString()).ToArrayAsync();
            ViewBag.DataTranslationKeys = JsonConvert.SerializeObject(dataTranslationKeys);

            DomainViewModel domainViewModel = new DomainViewModel();
            domainViewModel.Domain1 = domain.Domain1;
            domainViewModel.DomainKey = domain.DomainKey;
            domainViewModel.GradeKey = domain.GradeKey;
            domainViewModel.SubjectKey = domain.SubjectKey;
            domainViewModel.LocalDomainGUID = domain.LocalDomainGUID;
            domainViewModel.StatusKey = domain.StatusKey;
            return View(domainViewModel);
        }

        // GET: Domians/Create
        public async Task<ActionResult> Create()
        {
            ViewBag.GradeKey = new SelectList(db.Grades.OrderBy(g => g.GradeIndex), "GradeKey", "Grade1", selectedValue: default);
            ViewBag.SubjectKey = new SelectList(db.Subjects.OrderBy(s => s.SubjectIndex), "SubjectKey", "Subject1", selectedValue: default);
            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CollectLanguageTranslations(string langKeys = null, string domainNames = null)
        {
            TempData["langKeys"] = langKeys;
            TempData["domainNames"] = domainNames;

            return Json("", JsonRequestBehavior.AllowGet);
        }
        public static async Task<List<object>> GetLanguagesDropdownData()
        {
            RLIEntities db = new RLIEntities();
            List<object> filteredLanguages = new List<object>();

            List<Language> preferedLanguages = new List<Language>();
            List<Language> otherLanguages = new List<Language>();
            preferedLanguages = await db.Languages.Where(l => l.LanguageKey == 3 || l.LanguageKey == 4 || l.LanguageKey == 62).OrderBy(l => l.Language1).ToListAsync();
            otherLanguages = await db.Languages.Where(l => l.LanguageKey != 3 && l.LanguageKey != 4 && l.LanguageKey != 62 && l.LanguageDisplayKey == 4 && l.LanguageKey != 2).OrderBy(l => l.Language1).ToListAsync();


            Dictionary<string, object> preferedLanguagesGroup = new Dictionary<string, object>();
            Dictionary<string, object> otherLanguagesGroup = new Dictionary<string, object>();

            preferedLanguagesGroup.Add("text", "Common Languages");
            preferedLanguagesGroup.Add("children", preferedLanguages.Select(p => new
            {
                id = p.LanguageKey,
                text = p.Language1
            }));

            otherLanguagesGroup.Add("text", "Other Languages");
            otherLanguagesGroup.Add("children", otherLanguages.Select(p => new
            {
                id = p.LanguageKey,
                text = p.Language1
            }));

            filteredLanguages.Add(preferedLanguagesGroup);
            filteredLanguages.Add(otherLanguagesGroup);
            return filteredLanguages;
        }

        // POST: Domians/Create
        [HttpPost]
        public async Task<ActionResult> Create(DomainViewModel domainModel)
        {
            if (ModelState.IsValid)
            {

                //Add row in DataGUID table
                Guid localDomainGUID = Guid.NewGuid();
                DataGUID dataGUID = new DataGUID();
                dataGUID.DataGUID1 = localDomainGUID;
                dataGUID.SourceTable = "Domains";
                db.DataGUIDs.Add(dataGUID);
                await db.SaveChangesAsync();

                if (domainModel.LanguageTransaltionKeys != null)
                {
                    //var languageTranslationKeys = JsonConvert.DeserializeObject<List<string>>(TempData["langKeys"].ToString());
                    //var languageChapterNames = JsonConvert.DeserializeObject<List<string>>(TempData["domainNames"].ToString());

                    var languageTranslationKeys = JsonConvert.DeserializeObject<List<string>>(domainModel.LanguageTransaltionKeys);
                    var languageChapterNames = JsonConvert.DeserializeObject<List<string>>(domainModel.DomainNamesPerLanguage);

                    if (languageTranslationKeys.Count != 0)
                    {

                        //Add rows for every language in DataTranslation table
                        var doaminNamesIndex = 0;
                        foreach (var item in languageTranslationKeys)
                        {
                            DataTranslation dataTranslation = new DataTranslation();
                            dataTranslation.DataGUID = localDomainGUID;
                            dataTranslation.Value = languageChapterNames[doaminNamesIndex];
                            dataTranslation.LanguageKey = int.Parse(item);

                            db.DataTranslations.Add(dataTranslation);
                            await db.SaveChangesAsync();

                            doaminNamesIndex++;
                        }

                    }

                }

                //if (domainModel.DomainIndex != null)
                //{
                //    var chapters = await db.Chapters.Where(c => c.ChapterIndex >= domainModel.DomainIndex && c.GradeKey == newchapter.GradeKey && c.ChapterTypeKey == newchapter.ChapterTypeKey && c.SubjectKey == newchapter.SubjectKey).ToListAsync();
                //    foreach (var item in chapters)
                //    {
                //        item.ChapterIndex = item.ChapterIndex + 1;
                //    }
                //}
                //else
                //{
                //    var chapters = await db.Chapters.OrderBy(c => c.ChapterIndex).Where(c => c.GradeKey == newchapter.GradeKey && c.ChapterTypeKey == newchapter.ChapterTypeKey && c.SubjectKey == newchapter.SubjectKey).ToListAsync();
                //    newchapter.ChapterIndex = chapters.LastOrDefault().ChapterIndex + 1;
                //}

                await db.SaveChangesAsync();
                int lastIndex = db.Domains.Count();

                Domain domain = new Domain();
                domain.Domain1 = domainModel.Domain1;
                domain.GradeKey = domainModel.GradeKey;
                domain.SubjectKey = domainModel.SubjectKey;
                domain.DomainIndex = lastIndex + 1;
                domain.LocalDomainGUID = localDomainGUID;
                domain.StatusKey = (int)RLI.Common.Enums.StatusEnum.StatusPending;
                db.Domains.Add(domain);
                await db.SaveChangesAsync();

            }

            ViewBag.GradeKey = new SelectList(db.Grades.OrderBy(g => g.GradeIndex), "GradeKey", "Grade1", domainModel.GradeKey);
            ViewBag.LocalChapterGUID = new SelectList(db.DataGUIDs, "DataGUID1", "SourceTable", domainModel.LocalDomainGUID);
            ViewBag.SubjectKey = new SelectList(db.Subjects.OrderBy(s => s.SubjectIndex), "SubjectKey", "Subject1", domainModel.SubjectKey);
            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            if(domainModel.andContinue == "true")
            {
                return View(domainModel);
            } else
            {
                return RedirectToAction("Index");
            }
            
        }

        // GET: Domians/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            Domain domain = await db.Domains.FindAsync(id);

            ViewBag.GradeKey = new SelectList(db.Grades, "GradeKey", "Grade1", domain.GradeKey);
            ViewBag.SubjectKey = new SelectList(db.Subjects, "SubjectKey", "Subject1", domain.SubjectKey);

            ViewBag.DataTranslations = await db.DataTranslations.Where(d => d.DataGUID == domain.LocalDomainGUID).ToListAsync();
            var dataTranslationKeys = await db.DataTranslations.Where(d => d.DataGUID == domain.LocalDomainGUID).Select(d => d.LanguageKey.ToString()).ToArrayAsync();
            ViewBag.DataTranslationKeys = JsonConvert.SerializeObject(dataTranslationKeys);

            DomainViewModel domainViewModel = new DomainViewModel();
            domainViewModel.Domain1 = domain.Domain1;
            domainViewModel.DomainKey = domain.DomainKey;
            domainViewModel.GradeKey = domain.GradeKey;
            domainViewModel.SubjectKey = domain.SubjectKey;
            domainViewModel.LocalDomainGUID = domain.LocalDomainGUID;
            domainViewModel.StatusKey = domain.StatusKey;
            return View(domainViewModel);
        }

        // POST: Domians/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(DomainViewModel domainModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (domainModel.LanguageTransaltionKeys != null)
                    {
                        var languageTranslationKeys = JsonConvert.DeserializeObject<List<string>>(domainModel.LanguageTransaltionKeys);
                        var languageDomainNames = JsonConvert.DeserializeObject<List<string>>(domainModel.DomainNamesPerLanguage);


                        if (languageTranslationKeys.Count != 0)
                        {
                            var domainNamesIndex = 0;
                            //Check if language Translation Key already exists
                            foreach (var item in languageTranslationKeys)
                            {
                                int langKey = int.Parse(item);
                                var existingTransaltion = await db.DataTranslations.Where(d => d.DataGUID == domainModel.LocalDomainGUID && d.LanguageKey == langKey).ToListAsync();
                                if (existingTransaltion.Count != 0)
                                {
                                    existingTransaltion.FirstOrDefault().Value = languageDomainNames[domainNamesIndex];
                                    domainNamesIndex++;
                                    await db.SaveChangesAsync();
                                }
                                else
                                {
                                    DataTranslation newDataTranslation = new DataTranslation();
                                    newDataTranslation.DataGUID = domainModel.LocalDomainGUID;
                                    newDataTranslation.Value = languageDomainNames[domainNamesIndex];
                                    newDataTranslation.LanguageKey = int.Parse(item);

                                    db.DataTranslations.Add(newDataTranslation);
                                    domainNamesIndex++;
                                    await db.SaveChangesAsync();
                                }

                            }
                        }
                    }

                    Domain editedDomain = await db.Domains.FindAsync(domainModel.DomainKey);
                    editedDomain.Domain1 = domainModel.Domain1;
                    await db.SaveChangesAsync();
                }


                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteTranslation(int? dataTranslationKey = null)
        {
            DataTranslation dataTranslation = await db.DataTranslations.FindAsync(dataTranslationKey);
            db.DataTranslations.Remove(dataTranslation);
            await db.SaveChangesAsync();

            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ApproveDomain(int? DomId = null)
        {
            var domain = await db.Domains.Where(c => c.DomainKey == DomId).FirstOrDefaultAsync();

            domain.StatusKey = (int)RLI.Common.Enums.StatusEnum.StatusApproved;
            await db.SaveChangesAsync();
            return Json(domain.StatusKey, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RejectDomain(int? IdOfDom = null)
        {
            var domain = await db.Domains.Where(c => c.DomainKey == IdOfDom).FirstOrDefaultAsync();

            domain.StatusKey = (int)RLI.Common.Enums.StatusEnum.StatusRejected;
            await db.SaveChangesAsync();
            return Json(domain.StatusKey, JsonRequestBehavior.AllowGet);
        }

        // GET: Domians/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {

            return View();
        }

        // POST: Domians/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            Domain domain = await db.Domains.FindAsync(id);
            db.Domains.Remove(domain);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextOrPreviousDomains(int skip, int? gradeKey = null, int? subjectKey = null, int? domainName = null, int? statusKey = null)
        {

            Session["skip"] = skip;
            Session["gradeKey"] = gradeKey;
            Session["subjectKey"] = subjectKey;
            Session["chapterKey"] = domainName;
            Session["statusKey"] = statusKey;
            List<Domain> domains = new List<Domain>();
            int totalChapters = db.Domains.Where(l =>

           ((gradeKey != null && (l.GradeKey == gradeKey)) || (gradeKey == null))
            && ((subjectKey != null && (l.SubjectKey == subjectKey)) || (subjectKey == null))
            && ((domainName != null && (l.DomainKey == domainName)) || (domainName == null))
            && ((statusKey != null && (l.StatusKey == statusKey)) || (statusKey == null))
       ).Count();
            domains = await db.Domains.Where(l =>

           ((gradeKey != null && (l.GradeKey == gradeKey)) || (gradeKey == null))
            && ((subjectKey != null && (l.SubjectKey == subjectKey)) || (subjectKey == null))
            && ((domainName != null && (l.DomainKey == domainName)) || (domainName == null))
            && ((statusKey != null && (l.StatusKey == statusKey)) || (statusKey == null))
            ).OrderByDescending(l => l.DomainKey == null).ThenBy(les => les.GradeKey).ThenBy(les => les.StatusKey).ThenBy(les => les.DomainIndex).ToListAsync();//  .ThenBy(les => les.ChapterTypeKey).ThenBy(les => les.ChapterName)
            //ViewBag.Locale = await GetLocalisationpartialTable();
            //if (chapters.Count != 0)
            //{
            //    return PartialView("_ChaptersTable", (object)chapters);
            //}
            //else
            //{
            //    return Json("Empty", JsonRequestBehavior.AllowGet);
            //}
            int domainsCount = domains.Skip(skip * 50).Take(50).Count();
            if (domainsCount != 0)
            {
                return PartialView("_DomainsTable", (object)domains.Skip(skip * 50).Take(50));
            }
            else
            {
                return Json("Empty", JsonRequestBehavior.AllowGet);
            }
        }
    }
}
