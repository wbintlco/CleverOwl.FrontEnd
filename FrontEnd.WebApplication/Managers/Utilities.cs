using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using RLI.Common.Managers;
using RLI.EntityFramework.EDM;

namespace FrontEnd.WebApplication.Managers
{
    public class Utilities
    {
        public static async Task<Dictionary<string, string>> GetLocalisationPerPage(string controller, string action)
        {
            Dictionary<string, string> Locale = new Dictionary<string, string>();
            int currentLanguageIdentifier = 4;
            
                try
                {
                    int languageCookieValue = 4;
                    bool cookieLanguageIsValid = false;
                    var checkLanguageTask = Task.Run(async () =>
                    {
                        using (RLIEntities db = new RLIEntities())
                        {
                            cookieLanguageIsValid = await db.Languages.AnyAsync(l => l.LanguageKey == languageCookieValue);
                        }
                    });
                    checkLanguageTask.Wait();
                    currentLanguageIdentifier = cookieLanguageIsValid ? languageCookieValue : currentLanguageIdentifier;
                }
                catch (Exception e)
                {
                    var logErrorTask = Task.Run(async () =>
                    {
                        await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);
                    });
                    logErrorTask.Wait();
                }
            
          
            using (RLIEntities db = new RLIEntities())
            {
                try
                {
                    Locale = await db.LanguagesContents.Where(lc => (lc.LanguageKey == currentLanguageIdentifier) && ((lc.Controller == controller && (lc.Action == action)) || (lc.Controller == controller && (lc.Action == action)) || (lc.Controller == null && lc.Action == null))).ToDictionaryAsync(lc => lc.Field.Field1, lc => lc.FieldValue);
                }
                catch (Exception e)
                {
                    Locale = new Dictionary<string, string>();
                    var logErrorTask = Task.Run(async () =>
                    {
                        await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);
                    });
                    logErrorTask.Wait();
                }
            }
            return Locale;
        }
    }
}