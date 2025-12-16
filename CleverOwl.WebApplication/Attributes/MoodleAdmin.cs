using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using RLI.Common.Managers;

namespace CleverOwl.WebApplication.Attributes
{
    public class MoodleAdmin : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var adminTokenTask = Task.Run(async () =>
            {
                await MoodleManager.requestAdminToken(filterContext.HttpContext);
            });
            adminTokenTask.Wait();
        }
    }
}