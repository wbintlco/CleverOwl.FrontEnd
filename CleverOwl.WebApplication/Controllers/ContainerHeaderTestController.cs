using CleverOwl.WebApplication.Models;
using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace CleverOwl.WebApplication.Controllers
{
    public class ContainerHeaderTestController : BaseController
    {
        // GET: ContainerHeaderTest
        public ActionResult Index()
        {
            string title = "Title Test";
            string iconSource = "~/Views/SharedSvgs/_AssignmentsIcon.cshtml";
            string iconType = "url";
            bool containsSearchBar = true;
            bool containsActionBtn = true;
            string actionBtnName = "Create";
            string actionBtnUrl = Url.Action("Create", "Assignments");
            List<string> routeTitle = new List<string>();
            List<string> routePath = new List<string>();
            routeTitle.Add("title test");
            routePath.Add(Url.Action("Index", "ContainerHeaderTest"));
            ContainerHeaderComponentsModel containerHeader= GetContainerHeader(title, iconSource, iconType, containsSearchBar, containsActionBtn, actionBtnName, actionBtnUrl, routeTitle, routePath);
            ViewBag.ContainerHeaderModel = containerHeader;
            return View();
        }

        public ContainerHeaderComponentsModel GetContainerHeader(string title, string iconSource, string iconType, bool containsSearchBar, bool containsActionBtn, string actionBtnName, string actionBtnUrl, List<string> routeTitle, List<string> routePath)
        {
            ContainerHeaderComponentsModel containerHeader = new ContainerHeaderComponentsModel();
            List<Route> routes = new List<Route>();
            if (routeTitle.Count() == routePath.Count())
            {
                for (int i = 0; i < routeTitle.Count(); i++)
                {
                    Route route = new Route();
                    route.Title = routeTitle.ElementAt(i);
                    route.Path = routePath.ElementAt(i);
                    routes.Add(route);
                }
            }
            containerHeader.Routes = routes;
            containerHeader.TitleName = title;
            containerHeader.IconSource = iconSource;
            containerHeader.IconType = iconType;
            containerHeader.ContainsSearchBar = containsSearchBar;
            containerHeader.ContainsActionButton = containsActionBtn;
            containerHeader.ActionButtonName = actionBtnName;
            containerHeader.ActionButtonUrl = actionBtnUrl;

            return containerHeader;
        }
    }
}