using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Everest.OpenCurate.WebApplication.Objects;
using RLI.EntityFramework.EDM;

namespace CleverOwl.FrontEnd.WebApplication.Controllers
{
    public class AspNetRolesController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        // GET: AspNetRoles
        public async Task<ActionResult> Index()
        {
            List<AspNetRole> aspNetRoles = await db.AspNetRoles.Take(50).ToListAsync();
            ViewBag.NamesDropDown = new SelectList(db.AspNetRoles, "Id", "Name", selectedValue: default);
            ViewBag.RolesDropDown = new SelectList(db.RoleTypes, "RoleTypeKey", "RoleType1", selectedValue: default);
            return View(aspNetRoles);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetNextOrPreviousStatus(int skip, string ID, int? roleTypeKey = null)
        {
            List<AspNetRole> aspNetRoles = new List<AspNetRole>();
            aspNetRoles = await db.AspNetRoles.Where(l =>
           ((ID != "" && (l.Id == ID)) || (ID == "")) &&
           ((roleTypeKey != null && (l.RoleType.RoleTypeKey == roleTypeKey)) || (roleTypeKey == null))).ToListAsync();

            int Count = aspNetRoles.Skip(skip * 50).Take(50).Count();
            if (Count != 0)
            {
                return PartialView("_AspNetRoleTable", (object)aspNetRoles.Skip(skip * 50).Take(50));
            }
            else
            {
                return Json("Empty", JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> Create()
        {
            ViewBag.RolesTypeDropDown = new SelectList(db.RoleTypes, "RoleTypeKey", "RoleType1", selectedValue: default);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AspNetRoleViewModel aspRoleViewModelModel)
        {
            if (ModelState.IsValid)
            {
                Guid aspNetRoleGuid = Guid.NewGuid();
                DataGUID dataGUID = new DataGUID();
                dataGUID.DataGUID1 = aspNetRoleGuid;
                dataGUID.SourceTable = "AspNetRoles";
                db.DataGUIDs.Add(dataGUID);
                await db.SaveChangesAsync();

                AspNetRole aspNetRole = new AspNetRole();
                aspNetRole.Id = aspNetRoleGuid.ToString();
                aspNetRole.Name = aspRoleViewModelModel.Name;
                aspNetRole.RoleTypeKey = aspRoleViewModelModel.RoleType;
                db.AspNetRoles.Add(aspNetRole);
                await db.SaveChangesAsync();

            }

            if (aspRoleViewModelModel.Continue == "true")
            {
                return View(aspRoleViewModelModel);
            }
            else
            {
                return RedirectToAction("Index");
            }

        }
        public async Task<ActionResult> Edit(string id)
        {

            AspNetRole aspNetRole = await db.AspNetRoles.FindAsync(id);
            ViewBag.RoleTypesDropDown = new SelectList(db.RoleTypes, "RoleTypeKey", "RoleType1", aspNetRole.RoleTypeKey);
            if (aspNetRole == null)
            {
                return HttpNotFound();
            }
            AspNetRoleViewModel roleModel = new AspNetRoleViewModel();
            roleModel.Name = aspNetRole.Name;
            roleModel.RoleType = aspNetRole.RoleTypeKey;
            roleModel.ID = aspNetRole.Id;
            return View(roleModel);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(AspNetRoleViewModel roleModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    AspNetRole aspNetRole = await db.AspNetRoles.FindAsync(roleModel.ID);
                    aspNetRole.Name = roleModel.Name;
                    aspNetRole.RoleTypeKey = roleModel.RoleType;
                    await db.SaveChangesAsync();
                }


                return RedirectToAction("Index");
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public async Task<ActionResult> Details(string id)
        {
            if (id == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AspNetRole aspNetRole = await db.AspNetRoles.FindAsync(id);
            if (aspNetRole == null)
            {
                return HttpNotFound();
            }
            ViewBag.RoleTypesDropDown = new SelectList(db.RoleTypes, "RoleTypeKey", "RoleType1", aspNetRole.RoleTypeKey);
            AspNetRoleViewModel roleModel = new AspNetRoleViewModel();
            roleModel.Name = aspNetRole.Name;
            roleModel.RoleType = aspNetRole.RoleTypeKey;
            return View(roleModel);
        }
        public async Task<ActionResult> Delete(string id)
        {
            AspNetRoleViewModel roleModel = new AspNetRoleViewModel();
            roleModel.ID = id;
            return View(roleModel);
        }

        [HttpPost]
        public async Task<ActionResult> Delete1(AspNetRoleViewModel roleModel)
        {
            AspNetRole aspNetRole = await db.AspNetRoles.FindAsync(roleModel.ID);
            db.AspNetRoles.Remove(aspNetRole);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}