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
    public class StatusController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        // GET: Status
        public async Task<ActionResult> Index()
        {
            var status = db.Status.Take(50);
            ViewBag.StatusDropDown = new SelectList(db.Status, "StatusKey", "Status1", selectedValue: default);
            return View(await status.ToListAsync());
        }

        public async Task<ActionResult> Create()
        {

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(StatusViewModel statusModel)
        {
            if (ModelState.IsValid)
            {
                await db.SaveChangesAsync();
                Status status = new Status();
                status.Status1 = statusModel.Status1;
                db.Status.Add(status);
                await db.SaveChangesAsync();

            }

            if (statusModel.Continue == "true")
            {
                return View(statusModel);
            }
            else
            {
                return RedirectToAction("Index");
            }

        }

        public async Task<ActionResult> Edit(int? id)
        {

            Status status = await db.Status.FindAsync(id);
            if (status == null)
            {
                return HttpNotFound();
            }
            StatusViewModel statusModel = new StatusViewModel();
            statusModel.Status1 = status.Status1;
            statusModel.StatusKey = status.StatusKey;
            return View(statusModel);

        }

        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Status status = await db.Status.FindAsync(id);
            if (status == null)
            {
                return HttpNotFound();
            }
            StatusViewModel statusModel = new StatusViewModel();
            statusModel.Status1 = status.Status1;
            return View(statusModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(StatusViewModel statusModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Status status = await db.Status.FindAsync(statusModel.StatusKey);
                    status.Status1 = statusModel.Status1;
                    await db.SaveChangesAsync();
                }


                return RedirectToAction("Index");
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public async Task<ActionResult> Delete(int? id)
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            Status status = await db.Status.FindAsync(id);
            db.Status.Remove(status);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextOrPreviousStatus(int skip, int? StatusKey = null)
        {
            List<Status> status = new List<Status>();
            status = await db.Status.Where(l =>
           (StatusKey != null && (l.StatusKey == StatusKey)) || (StatusKey == null)).ToListAsync();

            int Count = status.Skip(skip * 50).Take(50).Count();
            if (Count != 0)
            {
                return PartialView("_StatusTable", (object)status.Skip(skip * 50).Take(50));
            }
            else
            {
                return Json("Empty", JsonRequestBehavior.AllowGet);
            }
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