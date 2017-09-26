﻿using Buy.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Buy.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        private string UserID
        {
            get
            {
                return User.Identity.GetUserId();
            }
        }

        // GET: User
        public ActionResult Index()
        {
            var user = db.Users.FirstOrDefault(s => s.Id == UserID);
            return View(user);
        }

        public ActionResult Edit()
        {
            var user = db.Users.FirstOrDefault(s => s.Id == UserID);
            return View(new UserSetting(user));
        }

        public ActionResult CustomerService()
        {
            var model = new SystemSetting()
            {
                Value = GetService(),
            };
            return View(model);
        }

        [AllowCrossSiteJson]
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Edit(ApplicationUser model)
        {
            var user = db.Users.FirstOrDefault(s => s.Id == model.Id);
            user.NickName = model.NickName;
            user.Avatar = model.Avatar;
            db.SaveChanges();
            return Json(Comm.ToJsonResult("Success", "修改成功"));
        }



        public string GetService()
        {
            return Bll.SystemSettings.CustomerService;
        }

        [AllowCrossSiteJson]
        [AllowAnonymous]
        [HttpGet]
        public ActionResult GetCustomerService()
        {
            return Json(Comm.ToJsonResult("Success", "成功", new { Data = GetService() }), JsonRequestBehavior.AllowGet);
        }

    }
}