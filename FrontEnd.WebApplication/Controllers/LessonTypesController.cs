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
    public class LessonTypesController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        // GET: LessonTypes
        public async Task<ActionResult> Index()
        {
            var lessonTypes = db.LessonTypes.Take(50);
            ViewBag.LessonTypesDropDown = new SelectList(db.LessonTypes, "LessonTypeKey", "LessonType1", selectedValue: default);
            ViewBag.IconHTMLDropDown = await db.LessonTypes.Select(l => l.IconHTML).Distinct().ToListAsync();
            return View(await lessonTypes.ToListAsync());
        }

        public async Task<ActionResult> Create()
        {

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(LessonTypeViewModel lessonType)
        {
            if (ModelState.IsValid)
            {
                await db.SaveChangesAsync();
                RLI.EntityFramework.EDM.LessonType lessons = new RLI.EntityFramework.EDM.LessonType();
                lessons.LessonType1 = lessonType.LessonType1;
                lessons.IconHTML = lessonType.IconHTML;
                db.LessonTypes.Add(lessons);
                await db.SaveChangesAsync();

            }

            if (lessonType.Continue == "true")
            {
                return View(lessonType);
            }
            else
            {
                return RedirectToAction("Index");
            }

        }

        public async Task<ActionResult> Edit(int? id)
        {

            RLI.EntityFramework.EDM.LessonType lesson = await db.LessonTypes.FindAsync(id);
            if (lesson == null)
            {
                return HttpNotFound();
            }
            LessonTypeViewModel lessonTypeViewModel = new LessonTypeViewModel();
            lessonTypeViewModel.LessonType1 = lesson.LessonType1;
            lessonTypeViewModel.LessonTypeKey= lesson.LessonTypeKey;
            lessonTypeViewModel.IconHTML = lesson.IconHTML;
            return View(lessonTypeViewModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(LessonTypeViewModel lessonTypeModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    RLI.EntityFramework.EDM.LessonType lesson = await db.LessonTypes.FindAsync(lessonTypeModel.LessonTypeKey);
                    lesson.LessonType1 = lessonTypeModel.LessonType1;
                    lesson.IconHTML = lessonTypeModel.IconHTML;
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
            RLI.EntityFramework.EDM.LessonType lesson = await db.LessonTypes.FindAsync(id);
            if (lesson == null)
            {
                return HttpNotFound();
            }
            LessonTypeViewModel lessonTypeViewModel = new LessonTypeViewModel();
            lessonTypeViewModel.LessonType1 = lesson.LessonType1;
            lessonTypeViewModel.IconHTML = lesson.IconHTML;
            return View(lessonTypeViewModel);
        }
        public async Task<ActionResult> Delete(int? id)
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            RLI.EntityFramework.EDM.LessonType lesson = await db.LessonTypes.FindAsync(id);
            db.LessonTypes.Remove(lesson);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextOrPreviousLessonTypes(int skip, int? lessonTypeKey = null, string iconHtml = null)
        {
            List<RLI.EntityFramework.EDM.LessonType> LessonTypes = new List<RLI.EntityFramework.EDM.LessonType>();
            LessonTypes = await db.LessonTypes.Where(l =>
           ((lessonTypeKey != null && (l.LessonTypeKey == lessonTypeKey)) || (lessonTypeKey == null))
           && ((iconHtml != null && (l.IconHTML == iconHtml)) || (iconHtml == ""))).OrderBy(l => l.LessonTypeKey).ToListAsync();

            int cyclesCount = LessonTypes.Skip(skip * 50).Take(50).Count();
            if (cyclesCount != 0)
            {
                return PartialView("_LessonTypesTable", (object)LessonTypes.Skip(skip * 50).Take(50));
            }
            else
            {
                return Json("Empty", JsonRequestBehavior.AllowGet);
            }
        }
    }
}