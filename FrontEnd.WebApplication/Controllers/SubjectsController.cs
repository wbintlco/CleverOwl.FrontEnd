using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FrontEnd.WebApplication.Models;
using Newtonsoft.Json;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Controllers;
using RLI.WebApplication.Objects;

namespace FrontEnd.WebApplication.Controllers
{
    public class SubjectsController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        // GET: Subjects
        public async Task<ActionResult> Index()
        {
            var subjects = db.Subjects.Take(50).OrderBy(s => s.SubjectIndex);
            ViewBag.SubjectDropDown = new SelectList(db.Subjects.OrderBy(s => s.SubjectIndex), "SubjectKey", "Subject1", selectedValue: default);
            return View(await subjects.ToListAsync());
        }

   

        public async Task<ActionResult> Create()
        {
            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            ViewBag.IndexDropDown = new SelectList(db.Subjects.OrderBy(s => s.SubjectIndex), "SubjectIndex", "Subject1", selectedValue: default);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(SubjectViewModel subjectModel)
        {
            if (ModelState.IsValid)
            {

                //Add row in DataGUID table
                Guid localDomainGUID = Guid.NewGuid();
                DataGUID dataGUID = new DataGUID();
                dataGUID.DataGUID1 = localDomainGUID;
                dataGUID.SourceTable = "Subjects";
                db.DataGUIDs.Add(dataGUID);
                await db.SaveChangesAsync();

                if (subjectModel.LanguageTransaltionKeys != null)
                {
                    

                    var languageTranslationKeys = JsonConvert.DeserializeObject<List<string>>(subjectModel.LanguageTransaltionKeys);
                    var languageGradeNames = JsonConvert.DeserializeObject<List<string>>(subjectModel.SubjectNamesPerLanguage);

                    if (languageTranslationKeys.Count != 0)
                    {

                        //Add rows for every language in DataTranslation table
                        var gradeNamesIndex = 0;
                        foreach (var item in languageTranslationKeys)
                        {
                            DataTranslation dataTranslation = new DataTranslation();
                            dataTranslation.DataGUID = localDomainGUID;
                            dataTranslation.Value = languageGradeNames[gradeNamesIndex];
                            dataTranslation.LanguageKey = int.Parse(item);

                            db.DataTranslations.Add(dataTranslation);
                            await db.SaveChangesAsync();

                            gradeNamesIndex++;
                        }

                    }

                }

                await db.SaveChangesAsync();
                int? lastIndex = await db.Subjects.OrderByDescending(g => g.SubjectIndex).Select(g => g.SubjectIndex).FirstOrDefaultAsync();
                int? index = subjectModel.SubjectIndex;
                if (index == -1 || index == null)
                {
                    RLI.EntityFramework.EDM.Subject subject = new RLI.EntityFramework.EDM.Subject();
                    subject.Subject1 = subjectModel.Subject1;
                    subject.SubjectIndex = lastIndex + 1;
                    subject.LocalSubjectGUID = localDomainGUID;
                    subject.Display = subjectModel.Visibility;
                    db.Subjects.Add(subject);
                    await db.SaveChangesAsync();

                    
                }
                else
                {
                    List<RLI.EntityFramework.EDM.Subject> subjects = await db.Subjects.Where(g => g.SubjectIndex >= index).ToListAsync();
                    foreach (RLI.EntityFramework.EDM.Subject g in subjects)
                    {
                        g.SubjectIndex++;
                        await db.SaveChangesAsync();
                    }
                    RLI.EntityFramework.EDM.Subject subject = new RLI.EntityFramework.EDM.Subject();
                    subject.Subject1 = subjectModel.Subject1;
                    subject.SubjectIndex = index;
                    subject.LocalSubjectGUID = localDomainGUID;
                    subject.Display = subjectModel.Visibility;
                    db.Subjects.Add(subject);
                    await db.SaveChangesAsync();
                }

            }

            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            if (subjectModel.Continue == "true")
            {
                return View(subjectModel);
            }
            else
            {
                return RedirectToAction("Index");
            }

        }
        public async Task<ActionResult> Delete(int? id)
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            RLI.EntityFramework.EDM.Subject subject = await db.Subjects.FindAsync(id);
            Guid guid = (Guid)subject.LocalSubjectGUID;
            db.Subjects.Remove(subject);
            await db.SaveChangesAsync();
            List<DataTranslation> dataTranslations = await db.DataTranslations.Where(d => d.DataGUID == guid).ToListAsync();
            foreach (DataTranslation dt in dataTranslations)
            {
                db.DataTranslations.Remove(dt);
                await db.SaveChangesAsync();
            }
            DataGUID dataGUID = await db.DataGUIDs.Where(d => d.DataGUID1 == guid).FirstOrDefaultAsync();
            db.DataGUIDs.Remove(dataGUID);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            RLI.EntityFramework.EDM.Subject subject = await db.Subjects.FindAsync(id);
            if (subject == null)
            {
                return HttpNotFound();
            }
            ViewBag.Visibility = subject.Display;
            ViewBag.IndexDropDown = new SelectList(db.Subjects.OrderBy(s => s.SubjectIndex), "SubjectIndex", "Subject1", selectedValue: default);
            ViewBag.DataTranslations = await db.DataTranslations.Where(d => d.DataGUID == subject.LocalSubjectGUID).ToListAsync();
            var dataTranslationKeys = await db.DataTranslations.Where(d => d.DataGUID == subject.LocalSubjectGUID).Select(d => d.LanguageKey.ToString()).ToArrayAsync();
            ViewBag.DataTranslationKeys = JsonConvert.SerializeObject(dataTranslationKeys);
            SubjectViewModel subjectViewModel = new SubjectViewModel();
            subjectViewModel.Subject1 = subject.Subject1;
            subjectViewModel.LocalSubjectGUID = subject.LocalSubjectGUID;
            subjectViewModel.SubjectKey = subject.SubjectKey;
            return View(subjectViewModel);

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

        // POST: Grades/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(SubjectViewModel subjectViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (subjectViewModel.LanguageTransaltionKeys != null)
                    {
                        var languageTranslationKeys = JsonConvert.DeserializeObject<List<string>>(subjectViewModel.LanguageTransaltionKeys);
                        var languageSubjectNames = JsonConvert.DeserializeObject<List<string>>(subjectViewModel.SubjectNamesPerLanguage);


                        if (languageTranslationKeys.Count != 0)
                        {
                            var subjectNamesIndex = 0;
                            //Check if language Translation Key already exists
                            foreach (var item in languageTranslationKeys)
                            {
                                int langKey = int.Parse(item);
                                var existingTransaltion = await db.DataTranslations.Where(d => d.DataGUID == subjectViewModel.LocalSubjectGUID && d.LanguageKey == langKey).ToListAsync();
                                if (existingTransaltion.Count != 0)
                                {
                                    existingTransaltion.FirstOrDefault().Value = languageSubjectNames[subjectNamesIndex];
                                    subjectNamesIndex++;
                                    await db.SaveChangesAsync();
                                }
                                else
                                {
                                    DataTranslation newDataTranslation = new DataTranslation();
                                    newDataTranslation.DataGUID = subjectViewModel.LocalSubjectGUID;
                                    newDataTranslation.Value = languageSubjectNames[subjectNamesIndex];
                                    newDataTranslation.LanguageKey = int.Parse(item);

                                    db.DataTranslations.Add(newDataTranslation);
                                    subjectNamesIndex++;
                                    await db.SaveChangesAsync();
                                }

                            }
                        }
                    }

                    RLI.EntityFramework.EDM.Subject subject = await db.Subjects.FindAsync(subjectViewModel.SubjectKey);
                    subject.Subject1 = subjectViewModel.Subject1;
                    subject.Display = subjectViewModel.Visibility;
                    int? lastIndex = await db.Subjects.OrderByDescending(g => g.SubjectIndex).Select(g => g.SubjectIndex).FirstOrDefaultAsync();
                    int? index = subjectViewModel.SubjectIndex;
                    if (index == -1 && index != subject.SubjectIndex)
                    {
                        subject.SubjectIndex = lastIndex + 1;

                        
                    } else if (index != null && index != subject.SubjectIndex)
                    {
                        List<RLI.EntityFramework.EDM.Subject> subjects = await db.Subjects.Where(g => g.SubjectIndex >= index).ToListAsync();
                        foreach (RLI.EntityFramework.EDM.Subject g in subjects)
                        {
                            g.SubjectIndex++;
                            await db.SaveChangesAsync();
                        }
                        subject.SubjectIndex = subjectViewModel.SubjectIndex;
                    }
                    await db.SaveChangesAsync();
                }


                return RedirectToAction("Index");
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            RLI.EntityFramework.EDM.Subject subject = await db.Subjects.FindAsync(id);
            if (subject == null)
            {
                return HttpNotFound();
            }
            ViewBag.Visibility = subject.Display;
            ViewBag.IndexDropDown = new SelectList(db.Subjects, "SubjectIndex", "SubjectIndex", subject.SubjectIndex);
            ViewBag.DataTranslations = await db.DataTranslations.Where(d => d.DataGUID == subject.LocalSubjectGUID).ToListAsync();
            var dataTranslationKeys = await db.DataTranslations.Where(d => d.DataGUID == subject.LocalSubjectGUID).Select(d => d.LanguageKey.ToString()).ToArrayAsync();
            ViewBag.DataTranslationKeys = JsonConvert.SerializeObject(dataTranslationKeys);
            SubjectViewModel subjectViewModel = new SubjectViewModel();
            subjectViewModel.Subject1 = subject.Subject1;
            subjectViewModel.LocalSubjectGUID = subject.LocalSubjectGUID;
            return View(subjectViewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextOrPreviousSubjects(int skip, int? subjectKey = null, bool? visibility = null)
        {
            List<RLI.EntityFramework.EDM.Subject> subjects = new List<RLI.EntityFramework.EDM.Subject>();
            subjects = await db.Subjects.Where(l =>
           ((subjectKey != null && (l.SubjectKey == subjectKey)) || (subjectKey == null))
            && ((visibility != null && (l.Display == visibility)) || (visibility == null))
            ).OrderBy(les => les.SubjectKey).ToListAsync();

            int subjectsCount = subjects.Skip(skip * 50).Take(50).Count();
            if (subjectsCount != 0)
            {
                return PartialView("_SubjectsTable", (object)subjects.Skip(skip * 50).Take(50));
            }
            else
            {
                return Json("Empty", JsonRequestBehavior.AllowGet);
            }
        }
    }
}