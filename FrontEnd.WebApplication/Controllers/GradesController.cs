using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Objects;
using RLI.WebApplication.Controllers;
using Newtonsoft.Json;
using FrontEnd.WebApplication.Models;

namespace FrontEnd.WebApplication.Controllers
{
    public class GradesController : BaseController
    {
        private RLIEntities db = new RLIEntities();

        // GET: Grades
        public async Task<ActionResult> Index()
        {
            var grades = db.Grades.Include(g => g.DataGUID).Include(g => g.Cycle).OrderBy(g => g.GradeIndex).Take(50);
            ViewBag.GradeDropDown = new SelectList(db.Grades.OrderBy(g => g.GradeIndex), "GradeKey", "Grade1", selectedValue: default);
            ViewBag.CycleDropDown = new SelectList(db.Cycles, "CycleKey", "Cycle1", selectedValue: default);
            return View(await grades.ToListAsync());
        }

        // GET: Grades/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            RLI.EntityFramework.EDM.Grade grade = await db.Grades.FindAsync(id);
            if (grade == null)
            {
                return HttpNotFound();
            }
            ViewBag.CycleDropDown = new SelectList(db.Cycles, "CycleKey", "Cycle1", grade.CycleKey);
            ViewBag.IndexDropDown = new SelectList(db.Grades, "GradeIndex", "GradeIndex", grade.GradeIndex);
            ViewBag.Visibility = grade.Display;
            ViewBag.DataTranslations = await db.DataTranslations.Where(d => d.DataGUID == grade.LocalGradeGUID).ToListAsync();
            var dataTranslationKeys = await db.DataTranslations.Where(d => d.DataGUID == grade.LocalGradeGUID).Select(d => d.LanguageKey.ToString()).ToArrayAsync();
            ViewBag.DataTranslationKeys = JsonConvert.SerializeObject(dataTranslationKeys);
            GradeViewModel gradeViewModel = new GradeViewModel();
            gradeViewModel.Grade1 = grade.Grade1;
            gradeViewModel.CycleKey = (int)grade.CycleKey;
            gradeViewModel.LocalGradeGUID = grade.LocalGradeGUID;
            gradeViewModel.GradeIndex = grade.GradeIndex;
            return View(gradeViewModel);
        }

        // GET: Grades/Create
        public async Task<ActionResult> Create()
        {
            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            ViewBag.CycleDropDown = new SelectList(db.Cycles, "CycleKey", "Cycle1", selectedValue: default);
            ViewBag.IndexDropDown = new SelectList(db.Grades.OrderBy(g => g.GradeIndex), "GradeIndex", "Grade1", selectedValue: default);
            return View();
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
            preferedLanguagesGroup.Add("children", db.LessonTypes.Select(l => new 
            {
                id= l.IconHTML,
                text = l.IconHTML + $"<i class ='{l.IconHTML}'></i>",
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

        // POST: Grades/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(GradeViewModel gradeModel)
        {
            if (ModelState.IsValid)
            {

                //Add row in DataGUID table
                Guid localDomainGUID = Guid.NewGuid();
                DataGUID dataGUID = new DataGUID();
                dataGUID.DataGUID1 = localDomainGUID;
                dataGUID.SourceTable = "Grades";
                db.DataGUIDs.Add(dataGUID);
                await db.SaveChangesAsync();

                if (gradeModel.LanguageTransaltionKeys != null)
                {
                    

                    var languageTranslationKeys = JsonConvert.DeserializeObject<List<string>>(gradeModel.LanguageTransaltionKeys);
                    var languageGradeNames = JsonConvert.DeserializeObject<List<string>>(gradeModel.GradeNamesPerLanguage);

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
                int? lastIndex = await db.Grades.OrderByDescending(g => g.GradeIndex).Select(g => g.GradeIndex).FirstOrDefaultAsync();
                int? index = gradeModel.GradeIndex;
                if( index == -1 || index == null)
                {
                    RLI.EntityFramework.EDM.Grade grade = new RLI.EntityFramework.EDM.Grade();
                    grade.Grade1 = gradeModel.Grade1;
                    grade.CycleKey = gradeModel.CycleKey;
                    grade.GradeIndex = lastIndex + 1;
                    grade.LocalGradeGUID = localDomainGUID;
                    grade.Display = gradeModel.Visibility;
                    db.Grades.Add(grade);
                    await db.SaveChangesAsync();

                } else
                {
                    List<RLI.EntityFramework.EDM.Grade> grades = await db.Grades.Where(g => g.GradeIndex >= index).ToListAsync();
                    foreach (RLI.EntityFramework.EDM.Grade g in grades)
                    {
                        g.GradeIndex++;
                        await db.SaveChangesAsync();
                    }
                    RLI.EntityFramework.EDM.Grade grade = new RLI.EntityFramework.EDM.Grade();
                    grade.Grade1 = gradeModel.Grade1;
                    grade.CycleKey = gradeModel.CycleKey;
                    grade.GradeIndex = index;
                    grade.LocalGradeGUID = localDomainGUID;
                    grade.Display = gradeModel.Visibility;
                    db.Grades.Add(grade);
                    await db.SaveChangesAsync();
                }
                

            }

            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            ViewBag.CycleDropDown = new SelectList(db.Cycles, "CycleKey", "Cycle1", selectedValue: default);
            ViewBag.IndexDropDown = new SelectList(db.Grades.OrderBy(g => g.GradeIndex), "GradeIndex", "Grade1", selectedValue: default);
            if (gradeModel.Continue == "true")
            {
                return View(gradeModel);
            }
            else
            {
                return RedirectToAction("Index");
            }

        }

        // GET: Grades/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.LanguageKey = await TopicsController.GetLanguagesDropdownData();
            RLI.EntityFramework.EDM.Grade grade = await db.Grades.FindAsync(id);
            if (grade == null)
            {
                return HttpNotFound();
            }
            ViewBag.CycleDropDown = new SelectList(db.Cycles, "CycleKey", "Cycle1", grade.CycleKey);
            ViewBag.IndexDropDown = new SelectList(db.Grades.OrderBy(g => g.GradeIndex), "GradeIndex", "Grade1", selectedValue: default);
            ViewBag.Visibility = grade.Display;
            ViewBag.DataTranslations = await db.DataTranslations.Where(d => d.DataGUID == grade.LocalGradeGUID).ToListAsync();
            var dataTranslationKeys = await db.DataTranslations.Where(d => d.DataGUID == grade.LocalGradeGUID).Select(d => d.LanguageKey.ToString()).ToArrayAsync();
            ViewBag.DataTranslationKeys = JsonConvert.SerializeObject(dataTranslationKeys);
            GradeViewModel gradeViewModel = new GradeViewModel();
            gradeViewModel.Grade1 = grade.Grade1;
            gradeViewModel.CycleKey = (int)grade.CycleKey;
            gradeViewModel.LocalGradeGUID = grade.LocalGradeGUID;
            gradeViewModel.GradeKey = grade.GradeKey;
            return View(gradeViewModel);
            
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
        public async Task<ActionResult> Edit(GradeViewModel gradeModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (gradeModel.LanguageTransaltionKeys != null)
                    {
                        var languageTranslationKeys = JsonConvert.DeserializeObject<List<string>>(gradeModel.LanguageTransaltionKeys);
                        var languageGradeNames = JsonConvert.DeserializeObject<List<string>>(gradeModel.GradeNamesPerLanguage);


                        if (languageTranslationKeys.Count != 0)
                        {
                            var gradeNamesIndex = 0;
                            //Check if language Translation Key already exists
                            foreach (var item in languageTranslationKeys)
                            {
                                int langKey = int.Parse(item);
                                var existingTransaltion = await db.DataTranslations.Where(d => d.DataGUID == gradeModel.LocalGradeGUID && d.LanguageKey == langKey).ToListAsync();
                                if (existingTransaltion.Count != 0)
                                {
                                    existingTransaltion.FirstOrDefault().Value = languageGradeNames[gradeNamesIndex];
                                    gradeNamesIndex++;
                                    await db.SaveChangesAsync();
                                }
                                else
                                {
                                    DataTranslation newDataTranslation = new DataTranslation();
                                    newDataTranslation.DataGUID = gradeModel.LocalGradeGUID;
                                    newDataTranslation.Value = languageGradeNames[gradeNamesIndex];
                                    newDataTranslation.LanguageKey = int.Parse(item);

                                    db.DataTranslations.Add(newDataTranslation);
                                    gradeNamesIndex++;
                                    await db.SaveChangesAsync();
                                }

                            }
                        }
                    }

                    RLI.EntityFramework.EDM.Grade editedGrade = await db.Grades.FindAsync(gradeModel.GradeKey);
                    editedGrade.Grade1 = gradeModel.Grade1;
                    editedGrade.Display = gradeModel.Visibility;
                    int? lastIndex = await db.Subjects.OrderByDescending(g => g.SubjectIndex).Select(g => g.SubjectIndex).FirstOrDefaultAsync();
                    int? index = gradeModel.GradeIndex;
                    if(index == -1  && index != editedGrade.GradeIndex)
                    {
                        editedGrade.GradeIndex = lastIndex + 1;
                    }
                    else if(index != null && index != editedGrade.GradeIndex)
                    {
                        List<RLI.EntityFramework.EDM.Grade> grades = await db.Grades.Where(g => g.GradeIndex >= index).ToListAsync();
                        foreach (RLI.EntityFramework.EDM.Grade g in grades)
                        {
                            g.GradeIndex++;
                            await db.SaveChangesAsync();
                        }
                        editedGrade.GradeIndex = gradeModel.GradeIndex;
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

        // GET: Grades/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            return View();
        }

        // POST: Grades/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            RLI.EntityFramework.EDM.Grade grade = await db.Grades.FindAsync(id);
            Guid guid = (Guid)grade.LocalGradeGUID;
            db.Grades.Remove(grade);
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
        public async Task<ActionResult> GetNextOrPreviousGrades(int skip, int? gradeKey = null, int? cycleKey = null, bool? visibility = null)
        {
            List<RLI.EntityFramework.EDM.Grade> grades = new List<RLI.EntityFramework.EDM.Grade>();
            grades = await db.Grades.Where(l =>
           ((gradeKey != null && (l.GradeKey == gradeKey)) || (gradeKey == null))
            && ((cycleKey != null && (l.Cycle.CycleKey == cycleKey)) || (cycleKey == null))
            && ((visibility != null && (l.Display == visibility)) || (visibility == null))
            ).OrderBy(les => les.GradeKey).ToListAsync();
            
            int greadesCount = grades.Skip(skip * 50).Take(50).Count();
            if (greadesCount != 0)
            {
                return PartialView("_GradesTable", (object)grades.Skip(skip * 50).Take(50));
            }
            else
            {
                return Json("Empty", JsonRequestBehavior.AllowGet);
            }
        }
    }
}