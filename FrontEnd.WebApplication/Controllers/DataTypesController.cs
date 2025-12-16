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
    public class DataTypesController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        // GET: DataTypes
        public async Task<ActionResult> Index()
        {
            var dataTypes = await db.DataTypes.Take(50).ToListAsync();
            ViewBag.DataTypeDropDown = new SelectList(db.DataTypes, "DataTypeKey", "DataType1", selectedValue: default);
            return View(dataTypes);
        }

        public async Task<ActionResult> Create()
        {

            return View();
        }

        public async Task<ActionResult> Edit(int? id)
        {

            DataType dataType = await db.DataTypes.FindAsync(id);
            if (dataType == null)
            {
                return HttpNotFound();
            }
            DataTypeViewModel dataTypeModel = new DataTypeViewModel();
            dataTypeModel.DataType1 = dataType.DataType1;
            dataTypeModel.DataTypeKey = dataType.DataTypeKey;
            return View(dataTypeModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(DataTypeViewModel dataTypeModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    DataType dataType = await db.DataTypes.FindAsync(dataTypeModel.DataTypeKey);
                    dataType.DataType1 = dataTypeModel.DataType1;
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
        public async Task<ActionResult> Create(DataTypeViewModel dataTypeModel)
        {
            if (ModelState.IsValid)
            {
                await db.SaveChangesAsync();
                DataType dataType = new DataType();
                dataType.DataType1 = dataTypeModel.DataType1;
                db.DataTypes.Add(dataType);
                await db.SaveChangesAsync();

            }

            if (dataTypeModel.Continue == "true")
            {
                return View(dataTypeModel);
            }
            else
            {
                return RedirectToAction("Index");
            }

        }

        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DataType dataType = await db.DataTypes.FindAsync(id);
            if (dataType == null)
            {
                return HttpNotFound();
            }
            DataTypeViewModel dataTypeModel = new DataTypeViewModel();
            dataTypeModel.DataType1 = dataType.DataType1;
            return View(dataTypeModel);
        }

        public async Task<ActionResult> Delete(int? id)
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            DataType dataType = await db.DataTypes.FindAsync(id);
            db.DataTypes.Remove(dataType);
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
        public async Task<ActionResult> GetNextOrPreviousDataTypes(int skip, int? DataTypeKey = null)
        {
            List<DataType> dataType = new List<DataType>();
            dataType = await db.DataTypes.Where(l =>
           (DataTypeKey != null && (l.DataTypeKey == DataTypeKey)) || (DataTypeKey == null)).ToListAsync();

            int Count = dataType.Skip(skip * 50).Take(50).Count();
            if (Count != 0)
            {
                return PartialView("_DataTypesTable", (object)dataType.Skip(skip * 50).Take(50));
            }
            else
            {
                return Json("Empty", JsonRequestBehavior.AllowGet);
            }
        }
    }
}