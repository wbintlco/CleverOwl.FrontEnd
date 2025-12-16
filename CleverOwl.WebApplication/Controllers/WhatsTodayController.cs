using RLI.WebApplication.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CleverOwl.WebApplication.Controllers
{
    public class WhatsTodayController : BaseController
    {
        // GET: WhatsToday
        public ActionResult Index()
        {
            return View();
        }
    }
}