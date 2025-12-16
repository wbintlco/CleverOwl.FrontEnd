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
    public class CyclesController : BaseController
    {
        
        private RLIEntities db = new RLIEntities();
        // GET: Cycle
        public async Task<ActionResult> Index()
        {
            
            var cycles = db.Cycles.Take(50);
            ViewBag.CycleDropDown = new SelectList(db.Cycles, "CycleKey", "Cycle1", selectedValue: default);
            return View(await cycles.ToListAsync());
        }

        public async Task<ActionResult> Create()
        {
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CycleViewModel cycleModel)
        {
            if (ModelState.IsValid)
            {
                await db.SaveChangesAsync();
                RLI.EntityFramework.EDM.Cycle cycle = new RLI.EntityFramework.EDM.Cycle();
                cycle.Cycle1 = cycleModel.Cycle1;
                db.Cycles.Add(cycle);
                await db.SaveChangesAsync();

            }

            if (cycleModel.Continue == "true")
            {
                return View(cycleModel);
            }
            else
            {
                return RedirectToAction("Index");
            }

        }

        public async Task<ActionResult> Edit(int? id)
        {
            
            RLI.EntityFramework.EDM.Cycle cycle = await db.Cycles.FindAsync(id);
            if (cycle == null)
            {
                return HttpNotFound();
            }
            CycleViewModel cycleViewModel = new CycleViewModel();
            cycleViewModel.Cycle1 = cycle.Cycle1;
            cycleViewModel.CycleKey = cycle.CycleKey;
            return View(cycleViewModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(CycleViewModel cycleViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    RLI.EntityFramework.EDM.Cycle editedCycle = await db.Cycles.FindAsync(cycleViewModel.CycleKey);
                    editedCycle.Cycle1 = cycleViewModel.Cycle1;
                    await db.SaveChangesAsync();
                }


                return RedirectToAction("Index");
            }
            catch
            {
                return HttpNotFound();
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextOrPreviousCycles(int skip, int? cycleKey = null)
        {
            List<RLI.EntityFramework.EDM.Cycle> cycles = new List<RLI.EntityFramework.EDM.Cycle>();
            cycles = await db.Cycles.Where(l =>
           (cycleKey != null && (l.CycleKey == cycleKey)) || (cycleKey == null)).OrderBy(l => l.CycleKey).ToListAsync();

            int cyclesCount = cycles.Skip(skip * 50).Take(50).Count();
            if (cyclesCount != 0)
            {
                return PartialView("_CyclesTable", (object)cycles.Skip(skip * 50).Take(50));
            }
            else
            {
                return Json("Empty", JsonRequestBehavior.AllowGet);
            }
        }
        public async Task<ActionResult> Delete(int? id)
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            RLI.EntityFramework.EDM.Cycle cycle = await db.Cycles.FindAsync(id);
            db.Cycles.Remove(cycle);
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

        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RLI.EntityFramework.EDM.Cycle cycle = await db.Cycles.FindAsync(id);
            if (cycle == null)
            {
                return HttpNotFound();
            }
            CycleViewModel cycleViewModel = new CycleViewModel();
            cycleViewModel.Cycle1 = cycle.Cycle1;
            return View(cycleViewModel);
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
    }
}