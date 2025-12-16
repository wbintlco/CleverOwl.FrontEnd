using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Objects;

namespace FrontEnd.WebApplication.Controllers
{
    [Authorize]
    public class ReportsTestController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        // GET: ReportsTest
        
        public async System.Threading.Tasks.Task<ActionResult> Index()

        {
            
            ViewBag.Schools = new SelectList(db.Schools, "SchoolKey", "SchoolName", selectedValue: default);
            ViewBag.Teachers = new SelectList(db.Teachers, "TeacherKey", "FirstName", selectedValue: default);
            ViewBag.Students = new SelectList(db.Students, "StudentKey", "FirstName", selectedValue: default);
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<ActionResult> TutorialFinished()
        {
            string userKey = User.Identity.GetUserId();
            AspNetUser aspNetUser = await db.AspNetUsers.FirstOrDefaultAsync(i => i.Id == userKey);
            aspNetUser.StatusKey = (int)RLI.Common.Enums.StatusEnum.StatusHideOverlay;
            await db.SaveChangesAsync();
            return Json("");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<ActionResult> getUserStatus()
        {
            try
            {
                String userKey = User.Identity.GetUserId();
                AspNetUser aspNetUser = await db.AspNetUsers.FirstOrDefaultAsync(i => i.Id == userKey);
                int? status = aspNetUser.StatusKey;
                return Json(status);
            }
            catch
            {
                return Json("19");
            }
        }
    }
}