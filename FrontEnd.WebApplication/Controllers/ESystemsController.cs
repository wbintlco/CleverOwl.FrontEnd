using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FrontEnd.WebApplication.Models;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Objects;

namespace FrontEnd.WebApplication.Controllers
{
    public class ESystemsController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        // GET: ESystems
        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            var eSystem = db.ESystems.Take(50);
            ViewBag.ESystemDropDown = new SelectList(db.ESystems, "ESystemKey", "ESystemName", selectedValue: default);
            return View(await eSystem.ToListAsync());
        }

        public async Task<ActionResult> Create()
        {

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ESystemViewModel eSystemModel)
        {
            if (ModelState.IsValid)
            {
                await db.SaveChangesAsync();
                ESystem eSystem = new ESystem();
                eSystem.ESystemName = eSystemModel.ESystemName;
                db.ESystems.Add(eSystem);
                await db.SaveChangesAsync();

            }

            if (eSystemModel.Continue == "true")
            {
                return View(eSystemModel);
            }
            else
            {
                return RedirectToAction("Index");
            }

        }

        public async Task<ActionResult> Edit(int? id)
        {
           
            ESystem eSystem = await db.ESystems.FindAsync(id);
            if (eSystem == null)
            {
                return HttpNotFound();
            }
            ESystemViewModel eSystemModel = new ESystemViewModel();
            eSystemModel.ESystemName = eSystem.ESystemName;
            eSystemModel.ESystemKey = eSystem.ESystemKey;
            return View(eSystemModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ESystemViewModel eSystemModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    ESystem eSystem = await db.ESystems.FindAsync(eSystemModel.ESystemKey);
                    eSystem.ESystemName = eSystemModel.ESystemName;
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
            ESystem eSystem = await db.ESystems.FindAsync(id);
            if (eSystem == null)
            {
                return HttpNotFound();
            }
            ESystemViewModel eSystemViewModel = new ESystemViewModel();
            eSystemViewModel.ESystemName = eSystem.ESystemName;
            return View(eSystemViewModel);
        }

        public async Task<ActionResult> Delete(int? id)
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            ESystem eSystem = await db.ESystems.FindAsync(id);
            db.ESystems.Remove(eSystem);
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
        public async Task<ActionResult> GetNextOrPreviousESystems(int skip, int? ESystemKey = null)
        {
            List<RLI.EntityFramework.EDM.ESystem> eSystem = new List<RLI.EntityFramework.EDM.ESystem>();
            eSystem = await db.ESystems.Where(l =>
           (ESystemKey != null && (l.ESystemKey == ESystemKey)) || (ESystemKey == null)).ToListAsync();

            int Count = eSystem.Skip(skip * 50).Take(50).Count();
            if (Count != 0)
            {
                return PartialView("_ESystemsTable", (object)eSystem.Skip(skip * 50).Take(50));
            }
            else
            {
                return Json("Empty", JsonRequestBehavior.AllowGet);
            }
        }
    }
}