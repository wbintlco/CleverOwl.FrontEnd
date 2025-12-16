using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using CleverOwl.WebApplication.Models;
using RLI.EntityFramework.EDM;
using RLI.WebApplication.Objects;
using System.Data.Entity;
using System.Threading.Tasks;
using RLI.Common.Managers;
using System.Reflection;

namespace CleverOwl.WebApplication.Controllers
{
    [Authorize]
    public class LocalisationsController : BaseController
    {
        private RLIEntities db = new RLIEntities();
        // GET: Localisations
        public ActionResult Index()
        {
            return View();
        }
        public async System.Threading.Tasks.Task<ActionResult> LocalisationEdit()
        {
            ViewBag.Controllers = db.LanguagesContents.OrderBy(l => l.Controller).Select(l => l.Controller).Distinct();
            return View();

        }
        public async System.Threading.Tasks.Task<ActionResult> JExcelLocalisationViewPending()
        {
            ViewBag.Controllers = await db.InterfaceLocalisations.Where(i => i.StatusKey == 4).Select(i => i.Controller).Distinct().ToListAsync();
            return View();

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<ActionResult> GetLanguagesSuggested(string jsonOfData)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            List<LanguageSuggested> languagesSuggested = (List<LanguageSuggested>)serializer.Deserialize(jsonOfData, typeof(List<LanguageSuggested>));
            List<LanguageSuggested> result = new List<LanguageSuggested>();

            try
            {

                List<LanguageModel> languageModel = await db.LanguagesContents.Select(l => new LanguageModel()
                {
                    LanguagesContentKey = (int)l.LanguageKey,
                    FieldKey = l.FieldKey,
                    FieldValue = l.FieldValue,
                    Controller = l.Controller,
                    Action = l.Action,
                    LanguageKey = l.LanguageKey,
                    Field = l.Field,
                    //Field1 = l.Field1,
                    //Field2 = l.Field2,
                    Language = l.Language,
                    //Language1 = l.Language1,
                    //Language2 = l.Language2

                }).ToListAsync();
                foreach (LanguageSuggested languageSuggested in languagesSuggested)
                {

                    LanguageSuggested newLanguageSuggested = new LanguageSuggested();
                    newLanguageSuggested.fieldKey = languageSuggested.fieldKey;
                    newLanguageSuggested.english = languageSuggested.english;

                    string key = languageModel.Where(l => l.LanguageKey == 4 && l.FieldValue.ToString().ToLower().Trim() == languageSuggested.english.ToLower().Trim()).Select(l => l.Field.Field1).FirstOrDefault();
                    if (languageSuggested.arabic == "true")
                    {
                        string arabic = await db.LanguagesContents.Where(l => l.LanguageKey == 3 && l.Field.Field1 == key).Select(l => l.FieldValue).FirstOrDefaultAsync();
                        newLanguageSuggested.arabic = arabic;
                    }
                    if (languageSuggested.french == "true")
                    {
                        string french = await db.LanguagesContents.Where(l => l.LanguageKey == 62 && l.Field.Field1 == key).Select(l => l.FieldValue).FirstOrDefaultAsync();
                        newLanguageSuggested.french = french;
                    }
                    result.Add(newLanguageSuggested);
                }
            }
            catch (Exception e)
            {
                return Json("error");
            }

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<ActionResult> GetLanguagesTranslated(string dataToTranslate)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            List<LanguageSuggested> languagesSuggested = (List<LanguageSuggested>)serializer.Deserialize(dataToTranslate, typeof(List<LanguageSuggested>));
            List<LanguageSuggested> result = new List<LanguageSuggested>();

            try
            {

                foreach (LanguageSuggested languageSuggested in languagesSuggested)
                {

                    LanguageSuggested newLanguageSuggested = new LanguageSuggested();

                    if (languageSuggested.arabic == "true")
                    {
                        string arabic = RLI.Common.Managers.UtilitiesManager.TranslateText(languageSuggested.english, "en", "ar");
                        newLanguageSuggested.arabic = arabic;
                    }
                    if (languageSuggested.french == "true")
                    {
                        string french = RLI.Common.Managers.UtilitiesManager.TranslateText(languageSuggested.english, "en", "fr");
                        newLanguageSuggested.french = french;
                    }
                    result.Add(newLanguageSuggested);
                }
            }
            catch (Exception e)
            {
                return Json("error");
            }

            return Json(result);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<JsonResult> SaveFile(string jsonData)
        {

            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                List<LocalosationViewModel> localosationViewModelsList = (List<LocalosationViewModel>)serializer.Deserialize(jsonData, typeof(List<LocalosationViewModel>));
                foreach (LocalosationViewModel l in localosationViewModelsList)
                {
                    if (l.english == null || l.arabic == null || l.french == null || l.fieldKey == null)
                    {
                        continue;
                    }
                    if (l.english == "" || l.arabic == "" || l.french == "" || l.fieldKey == "")
                    {
                        continue;
                    }
                    InterfaceLocalisation interfaceLocalisation = new InterfaceLocalisation();
                    if (l.controller.Trim() == "")
                    {
                        interfaceLocalisation.Controller = null;
                    }
                    else
                    {
                        interfaceLocalisation.Controller = l.controller.Trim();
                    }
                    if (l.action.Trim() == "")
                    {
                        interfaceLocalisation.Action = null;
                    }
                    else
                    {
                        interfaceLocalisation.Action = l.action.Trim();
                    }

                    interfaceLocalisation.English = l.english.Trim();
                    interfaceLocalisation.Arabic = l.arabic.Trim();
                    interfaceLocalisation.French = l.french.Trim();
                    interfaceLocalisation.FieldKey = l.fieldKey.Trim();
                    interfaceLocalisation.StatusKey = 4;
                    db.InterfaceLocalisations.Add(interfaceLocalisation);
                    await db.SaveChangesAsync();

                }

                return Json(200);
            }
            catch (Exception e)
            {

                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.ToString());
            }

            return Json(400);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetLanguageContents(int? language, string controller, string action)
        {
            if (controller == "" && action == "")
            {
                controller = null;
                action = null;
            }
            var languageContents = await db.LanguagesContents.Where(l =>
            ((language == null && l.LanguageKey != 2) || (l.LanguageKey == language)) &&
            ((controller != "" && (l.Controller == controller)) || (controller == "")) &&
            ((action != "" && (l.Action == action)) || (action == ""))
            ).Select(l => new
            {
                LanguagesContentKey = l.LanguagesContentKey,
                Controller = l.Controller,
                Action = l.Action,
                FieldValue = l.FieldValue,
                FieldKey = l.Field.Field1,
                Language = l.LanguageKey
            }).ToListAsync();
            return Json(languageContents);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<ActionResult> GetActionsDropDownPerController(string valueSelected)
        {
            List<string> actions = await db.LanguagesContents.OrderBy(l => l.Action)
            .Where(l => l.Controller == valueSelected)
            .Select(l => l.Action).Distinct().ToListAsync();
            return Json(actions);

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<ActionResult> GetControllersDropDownPerAction(string valueSelectedAction)
        {
            List<string> controllers = await db.LanguagesContents.OrderBy(l => l.Controller)
            .Where(l => l.Action == valueSelectedAction)
            .Select(l => l.Controller).Distinct().ToListAsync();
            return Json(controllers);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<JsonResult> EditLocalisation(string jsonData)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                List<LanguageContentEdit> languageContentEditList = (List<LanguageContentEdit>)serializer.Deserialize(jsonData, typeof(List<LanguageContentEdit>));
                foreach (LanguageContentEdit languageContentEdit in languageContentEditList)
                {
                    try
                    {
                        LanguagesContent languageContent = await db.LanguagesContents
                       .FindAsync(languageContentEdit.LanguagesContentKey);

                        if (languageContent != null)
                        {
                            languageContent.FieldValue = System.Net.WebUtility.UrlDecode(languageContentEdit.FieldValue.Trim());
                            await db.SaveChangesAsync();
                        }
                    }
                    catch (Exception e)
                    {

                    }
                }

                return Json("success");
            }
            catch (Exception e)
            {
                return Json("error");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<JsonResult> DeleteLocalisation(string dataToDelete)
        {

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            List<LanguageContentEdit> languageContentEditList = (List<LanguageContentEdit>)serializer.Deserialize(dataToDelete, typeof(List<LanguageContentEdit>));
            int[] languageContentKeys = languageContentEditList.Select(l => l.LanguagesContentKey).ToArray();
            int?[] fieldKeys = await db.LanguagesContents.Where(l => languageContentKeys.Contains(l.LanguagesContentKey)).Select(l => l.FieldKey).Distinct().ToArrayAsync();
            List<LanguagesContent> contentsToDelete = await db.LanguagesContents.Where(l => fieldKeys.Contains(l.FieldKey)).ToListAsync();
            List<Field> fields = await db.Fields.Where(l => fieldKeys.Contains(l.FieldKey)).ToListAsync();
            foreach (LanguagesContent contentToDelete in contentsToDelete)
            {
                db.LanguagesContents.Remove(contentToDelete);
                await db.SaveChangesAsync();
            }
            foreach (Field field in fields)
            {
                db.Fields.Remove(field);
                await db.SaveChangesAsync();
            }
            return Json(200);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<JsonResult> GetActionAndController(string data)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                List<LanguageContentExcel> languageContentExcelList = (List<LanguageContentExcel>)serializer.Deserialize(data, typeof(List<LanguageContentExcel>));
                List<LanguageContentEdit> result = new List<LanguageContentEdit>();

                foreach (LanguageContentExcel lan in languageContentExcelList)
                {
                    LanguageContentEdit newLanguageContent = new LanguageContentEdit();
                    newLanguageContent.LanguagesContentKey = int.Parse(lan.LanguagesContentKey);
                    newLanguageContent.FieldKey = lan.Field;
                    newLanguageContent.Language = lan.Language;
                    int languageKey = int.Parse(lan.LanguagesContentKey);
                    string controller = await db.LanguagesContents.Where(l => l.LanguagesContentKey == languageKey).Select(l => l.Controller).FirstOrDefaultAsync();
                    string action = await db.LanguagesContents.Where(l => l.LanguagesContentKey == languageKey).Select(l => l.Action).FirstOrDefaultAsync();
                    newLanguageContent.Controller = controller;
                    newLanguageContent.Action = action;
                    newLanguageContent.FieldValue = System.Net.WebUtility.UrlDecode(lan.FieldValue.Trim());
                    result.Add(newLanguageContent);
                }
                return Json(result);
            }
            catch (Exception e)
            {
                return Json("error");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<ActionResult> GetPendingView()
        {
            var data = await db.InterfaceLocalisations.Where(i => i.StatusKey == 4).Select(i => new
            {
                Controller = i.Controller,
                Action = i.Action,
                FieldKey = i.FieldKey,
                English = i.English,
                Arabic = i.Arabic,
                French = i.French,
                InterfaceLocalisationKey = i.InterfaceLocalisationKey
            }).ToListAsync();
            return Json(data);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<ActionResult> GetActionsPendingDropDownPerController(string valueSelected)
        {
            List<string> actions = await db.InterfaceLocalisations
            .Where(l => l.Controller == valueSelected && l.StatusKey == 4)
            .Select(l => l.Action).Distinct().ToListAsync();
            return Json(actions);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetViewPendingContents(string controller, string action)
        {
            if (controller == "" && action == "")
            {
                controller = null;
                action = null;
            }
            var languageContents = await db.InterfaceLocalisations.Where(l =>
            ((controller != "" && (l.Controller == controller)) || (controller == ""))
            && ((action != "" && (l.Action == action)) || (action == ""))
            && l.StatusKey == 4)
            .Select(i => new
            {
                Controller = i.Controller,
                Action = i.Action,
                FieldKey = i.FieldKey,
                English = i.English,
                Arabic = i.Arabic,
                French = i.French,
                InterfaceLocalisationKey = i.InterfaceLocalisationKey
            }).ToListAsync();
            return Json(languageContents);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<JsonResult> SaveViewPending(string jsonData)
        {

            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                List<LocalosationViewModel> localosationViewModelsList = (List<LocalosationViewModel>)serializer.Deserialize(jsonData, typeof(List<LocalosationViewModel>));

                foreach (LocalosationViewModel l in localosationViewModelsList)
                {
                    InterfaceLocalisation interfaceLocalisation = await db.InterfaceLocalisations.FindAsync(l.interfaceLocalisationKey);
                    if (interfaceLocalisation.Arabic != l.arabic || interfaceLocalisation.French != l.french
                        || interfaceLocalisation.English != l.english || interfaceLocalisation.FieldKey != l.fieldKey
                        || interfaceLocalisation.Action != l.action || interfaceLocalisation.Controller != l.controller)
                    {
                        if (l.action == "")
                        {
                            interfaceLocalisation.Action = null;
                        }
                        else
                        {
                            interfaceLocalisation.Action = l.action;
                        }
                        if (l.controller == "")
                        {
                            interfaceLocalisation.Controller = null;
                        }
                        else
                        {
                            interfaceLocalisation.Controller = l.controller;
                        }

                        interfaceLocalisation.FieldKey = l.fieldKey;
                        interfaceLocalisation.Arabic = l.arabic;
                        interfaceLocalisation.English = l.english;
                        interfaceLocalisation.French = l.french;
                        await db.SaveChangesAsync();
                    }
                }

                return Json(200);
            }
            catch (Exception e)
            {

                await LogManager.log(MethodBase.GetCurrentMethod().Name, e.ToString());
            }

            return Json(400);
        }
    }
    
}