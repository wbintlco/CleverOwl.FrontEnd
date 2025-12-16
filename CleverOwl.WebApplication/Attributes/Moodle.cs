using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using RLI.Common.Managers;

namespace CleverOwl.WebApplication.Attributes
{
    public class MoodleToken : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var getRequestToken = Task.Run(async () =>
                {
                    await MoodleManager.requestToken(filterContext.HttpContext);
                });
                getRequestToken.Wait();
            }
            catch (Exception e)
            {
                var logErrorTask = Task.Run(async () =>
                {
                    await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);
                });
                logErrorTask.Wait();
            }
        }
    }
    public class MoodleCookie : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var getRequestToken = Task.Run(async () =>
                {
                    await MoodleManager.requestCookie(filterContext.HttpContext);
                });
                getRequestToken.Wait();
            }
            catch (Exception e)
            {
                var logErrorTask = Task.Run(async () =>
                {
                    await LogManager.log(MethodBase.GetCurrentMethod().Name, e.Message);
                });
                logErrorTask.Wait();
            }
        }
    }
}