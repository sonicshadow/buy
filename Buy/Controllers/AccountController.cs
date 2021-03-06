﻿using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Buy.Models;
using System.Drawing;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Buy.Controllers
{
    [Authorize]
    public class AccountController : Controller, System.Web.SessionState.IRequiresSessionState
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private ApplicationDbContext db = new ApplicationDbContext();

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [AllowCrossSiteJson]
        public ActionResult GetUserInfo(string userId)
        {
            var user = db.Users.FirstOrDefault(s => s.Id == userId);
            var data = new UserViewModel(user);
            return Json(Comm.ToJsonResult("Success", "成功", new { Data = data }), JsonRequestBehavior.AllowGet);
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult LoginSystem(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [AllowCrossSiteJson]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            var result = await SignInManager.PasswordSignInAsync(model.UserName, model.Password, false, shouldLockout: true);
            switch (result)
            {
                case SignInStatus.Success:
                    {
                        var user = db.Users.FirstOrDefault(s => s.UserName == model.UserName || s.PhoneNumber == model.UserName);
                        return Json(Comm.ToJsonResult("Success", "登录成功", new UserViewModel(user)));
                    }
                case SignInStatus.LockedOut:
                    {
                        var user = db.Users.FirstOrDefault(s => s.UserName == model.UserName || s.PhoneNumber == model.UserName);
                        if (user.IsLocked())
                        {
                            return Json(Comm.ToJsonResult("Error", "帐号已经被冻结"));
                        }
                        return Json(Comm.ToJsonResult("Error", "因多次登录失败，帐号已经被冻结，请稍微再试"));
                    }
                case SignInStatus.RequiresVerification:
                    {
                        return Json(Comm.ToJsonResult("Error", "用户或密码为空"));
                    }
                case SignInStatus.Failure:
                default:
                    return Json(Comm.ToJsonResult("Error", "用户或密码有误"));
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // 要求用户已通过使用用户名/密码或外部登录名登录
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 以下代码可以防范双重身份验证代码遭到暴力破解攻击。
            // 如果用户输入错误代码的次数达到指定的次数，则会将
            // 该用户帐户锁定指定的时间。
            // 可以在 IdentityConfig 中配置帐户锁定设置
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "代码无效。");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [AllowCrossSiteJson]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var verCode = Bll.Accounts.VerCode(model.PhoneNumber, model.Code);
                if (!verCode.IsSuccess)
                {
                    return Json(Comm.ToJsonResult("Error", verCode.Message));
                }
                if (db.Users.Any(s => s.UserName == model.PhoneNumber))
                {
                    return Json(Comm.ToJsonResult("Error", "用户名已存在"));
                }
                var user = new ApplicationUser
                {
                    UserName = model.PhoneNumber,
                    UserType = Enums.UserType.Normal,
                    RegisterDateTime = DateTime.Now,
                    LastLoginDateTime = DateTime.Now,
                    PhoneNumber = model.PhoneNumber,
                    NickName = model.PhoneNumber,
                    PhoneNumberConfirmed = true,
                };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    user = db.Users.FirstOrDefault(s => s.UserName == model.PhoneNumber);
                    return Json(Comm.ToJsonResult("Success", "成功", new UserViewModel(user)));
                }
                return Json(Comm.ToJsonResult("Error", result.Errors.FirstOrDefault()));
            }
            return Json(Comm.ToJsonResult("Error", ModelState.FirstErrorMessage()));
        }

        public ActionResult Activation(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            var id = User.Identity.GetUserId();
            var u = db.Users.FirstOrDefault(s => s.Id == id);
            return View(u);
        }

        [HttpPost]
        [AllowAnonymous]
        [AllowCrossSiteJson]
        public ActionResult Activation(string userId, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(Comm.ToJsonResult("Error", "填写注册码"));
            }
            code = code.ToUpper();
            //允许重复码
            var registrationCodes = db.RegistrationCodes
                .FirstOrDefault(s => !s.UseTime.HasValue
                && s.Code == code);
            if (registrationCodes == null)
            {
                return Json(Comm.ToJsonResult("Error", "没有这个注册码"));
            }
            else
            {
                if (registrationCodes.UseTime.HasValue)
                {
                    return Json(Comm.ToJsonResult("Error", "注册码已激活"));
                }
            }
            registrationCodes.UseTime = DateTime.Now;
            registrationCodes.UseUser = userId;
            var user = db.Users.FirstOrDefault(s => s.Id == userId);
            user.ParentUserID = registrationCodes.OwnUser;
            user.IsActive = true;
            db.SaveChanges();
            return Json(Comm.ToJsonResult("Success", "成功"));
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [AllowCrossSiteJson]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(s => s.UserName == model.PhoneNumber);
                if (user == null)
                {
                    return Json(Comm.ToJsonResult("Error", "用户不存在"));
                }
                var verCode = Bll.Accounts.VerCode(user.PhoneNumber, model.Code);
                if (!verCode.IsSuccess)
                {
                    return Json(Comm.ToJsonResult("Error", verCode.Message));
                }
                UserManager.RemovePassword(user.Id);
                var r = UserManager.AddPassword(user.Id, model.Password);
                if (r.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    return Json(Comm.ToJsonResult("Success", "设置成功"));
                }
                else
                {
                    return Json(Comm.ToJsonResult("Error", r.Errors.FirstOrDefault()));
                }
            }
            return Json(Comm.ToJsonResult("Error", ModelState.FirstErrorMessage()));
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        public ActionResult ResetPassword()
        {
            var model = new ResetPasswordViewModel()
            {
                UserID = User.Identity.GetUserId(),
            };
            return View(model);
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowCrossSiteJson]
        [AllowAnonymous]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(s => s.Id == model.UserID);
                if (user == null)
                {
                    return Json(Comm.ToJsonResult("Error", "用户不存在"));
                }
                var result = SignInManager.PasswordSignIn(user.UserName, model.OldPassword, false, false);
                if (result == SignInStatus.Success)
                {
                    UserManager.RemovePassword(user.Id);
                    var r = UserManager.AddPassword(user.Id, model.NewPassword);
                    if (r.Succeeded)
                    {
                        return Json(Comm.ToJsonResult("Success", "设置成功"));
                    }
                    else
                    {
                        return Json(Comm.ToJsonResult("Error", r.Errors.FirstOrDefault()));
                    }
                }
                else
                {
                    return Json(Comm.ToJsonResult("Error", "原密码不正确"));
                }
            }
            return Json(Comm.ToJsonResult("Error", ModelState.FirstErrorMessage()));
        }



        [HttpPost]
        [AllowAnonymous]
        [AllowCrossSiteJson]
        public async Task<ActionResult> LoginClient(string username, string password)
        {
            var result = await SignInManager.PasswordSignInAsync(username, password, false, shouldLockout: true);
            switch (result)
            {
                case SignInStatus.Success:
                    {
                        var user = db.Users.FirstOrDefault(s => s.UserName == username || s.PhoneNumber == username);
                        if (user.UserType != Enums.UserType.Proxy)
                        {
                            return Json(Comm.ToJsonResult("PermissionDenied", "该帐号没有代理权限"));
                        }
                        var code = Comm.Random.Next(10000, 99999).ToString("x");
                        db.ClientAccessLogs.Add(new ClientAccessLog
                        {
                            Code = code,
                            IP = this.GetIPAddress(),
                            LoginDateTime = DateTime.Now,
                            UserID = user.Id
                        });
                        return Json(Comm.ToJsonResult("Success", "登录成功", new
                        {
                            ID = user.Id,
                            NickName = user.NickName,
                            Code = code,
                            PhoneNumber = user.PhoneNumber
                        }));
                    }
                case SignInStatus.LockedOut:
                    {
                        var user = db.Users.FirstOrDefault(s => s.UserName == username || s.PhoneNumber == username);
                        if (user.IsLocked())
                        {
                            return Json(Comm.ToJsonResult("LockedOut", "帐号已经被冻结"));
                        }
                        return Json(Comm.ToJsonResult("LockedOut", "因多次登录失败，帐号已经被临时冻结，请稍微再试"));
                    }
                case SignInStatus.RequiresVerification:
                    {
                        return Json(Comm.ToJsonResult("RequiresVerification", "用户或密码为空"));
                    }
                case SignInStatus.Failure:
                default:
                    return Json(Comm.ToJsonResult("Failure", "用户或密码有误"));
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [AllowCrossSiteJson]
        public ActionResult CheckClientCode(string id, string code)
        {
            var ispass = db.ClientAccessLogs
                .OrderByDescending(s => s.LoginDateTime)
                .FirstOrDefault(s => s.UserID == id)
                ?.Code == code;
            if (ispass)
            {
                return Json(Comm.ToJsonResult("Success", "成功"));
            }
            else
            {
                return Json(Comm.ToJsonResult("Error", "验证失败"));
            }

        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // 请求重定向到外部登录提供程序
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        [HttpPost]
        [AllowAnonymous]
        [AllowCrossSiteJson]
        public ActionResult SendCode(SendCodeViewModel model)
        {
            string code = new Random().Next(10000, 99999).ToString();
            string ip = Request.UserHostAddress;

            if (!ModelState.IsValid)
            {
                return Json(Comm.ToJsonResult("Error", ModelState.FirstErrorMessage()));
            }
            model.Phone = model.Phone.Trim();
            var verificationCodes = db.VerificationCodes.Where(s => s.To == model.Phone).ToList();
            int max = 5;
            if (verificationCodes.Where(s => s.CreateDate.Date == DateTime.Now.Date).Count() >= max)
            {
                return Json(Comm.ToJsonResult("Error", $"一天只能发送{max}次"));
            }
            try
            {
                ISms sms = new YunPianSms();
                var verCode = sms.Send(model.Phone, code);
                if (verCode.IsSuccess)
                {
                    db.VerificationCodes.Add(new VerificationCode
                    {
                        To = model.Phone,
                        CreateDate = DateTime.Now,
                        Code = code,
                        IP = ip
                    });
                    db.SaveChanges();
                    return Json(Comm.ToJsonResult("Success", verCode.Message));
                }
                else
                {
                    return Json(Comm.ToJsonResult("Error", verCode.Message));
                }
            }
            catch (Exception ex)
            {
                return Json(Comm.ToJsonResult("Error", ex.Message));
            }
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // 如果用户已具有登录名，则使用此外部登录提供程序将该用户登录
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // 如果用户没有帐户，则提示该用户创建帐户
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // 从外部登录提供程序获取有关用户的信息
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            var userId = User.Identity.GetUserId();
            var userType = db.Users.FirstOrDefault(s => s.Id == userId).UserType;
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            if (userType == Enums.UserType.System)
            {
                return RedirectToAction("LoginSystem", "Account");
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [AllowAnonymous]
        public ActionResult VerCode()
        {

            Session["CreateTime"] = DateTime.Now;

            //Create Bitmap object and to draw 
            Bitmap basemap = new Bitmap(200, 60);
            Graphics graph = Graphics.FromImage(basemap);
            graph.FillRectangle(new SolidBrush(Color.White), 0, 0, 200, 60);
            Font font = new Font(FontFamily.GenericSerif, 48, FontStyle.Bold, GraphicsUnit.Pixel);
            Random r = new Random();
            string letters = "ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijklmnpqrstuvwxyz0123456789";
            string letter;
            StringBuilder s = new StringBuilder();


            // Add a random five-letter 
            for (int x = 0; x < 4; x++)
            {
                letter = letters.Substring(r.Next(0, letters.Length - 1), 1);
                s.Append(letter);
                // Draw the String 
                graph.DrawString(letter, font, new SolidBrush(Color.Black), x * 38, r.Next(0, 15));
            }


            // Confusion background 
            Pen linePen = new Pen(new SolidBrush(Color.Black), 2);
            for (int x = 0; x < 10; x++)
            {
                graph.DrawLine(linePen, new Point(r.Next(0, 199), r.Next(0, 59)), new Point(r.Next(0, 199), r.Next(0, 59)));
            }

            var steam = new MemoryStream();

            // Save the picture to the output stream      
            basemap.Save(steam, System.Drawing.Imaging.ImageFormat.Gif);

            // If you do not realize the IRequiresSessionState,it will be wrong here,and it can not generate a picture also. 
            Session["ValidateCode"] = s.ToString();
            return File(steam.ToArray(), "image/gif", "code.gif");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CheckCode(string code)
        {
            if (code.ToLower() == Session["ValidateCode"].ToString().ToLower())
            {
                return Json(Comm.ToJsonResult("Success", "验证成功"));
            }
            else
            {
                return Json(Comm.ToJsonResult("Error", "验证失败"));
            }
        }


        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }


        #region 微信对接

        [HttpGet]
        [AllowAnonymous]
        public ActionResult LoginByWeiXinSilence(string state)
        {
            var p = new Dictionary<string, string>();
            p.Add("appid", WeChat.Config.AppID);
            p.Add("redirect_uri", "http://www.yumy.me/Account/LoginByWeiXin");
            p.Add("response_type", "code");
            p.Add("scope", "snsapi_base");
            p.Add("state", state);
            return Redirect($"https://open.weixin.qq.com/connect/oauth2/authorize{p.ToParam("?")}#wechat_redirect");
        }

        [AllowAnonymous]
        [AllowCrossSiteJson]
        public ActionResult LoginByWeiXin(string code, string state = null, int app = 0)
        {
            Func<string, ActionResult> error = content =>
            {
                if (app > 0)
                {
                    return Json(Comm.ToJsonResult("Error", content));
                }
                else
                {
                    return this.ToError("错误", content, Url.Action("Login", "Account"));
                }
            };
            if (string.IsNullOrWhiteSpace(code))
            {
                return error("请求有误");
            }
            WeChat.WeChatApi wechat;
            switch (app)
            {
                case 1:
                    {
                        wechat = new WeChat.WeChatApi(WeChat.Config.AppID2, WeChat.Config.AppSecret2);
                    }
                    break;
                case 0:
                default:
                    {
                        wechat = new WeChat.WeChatApi(WeChat.Config.AppID, WeChat.Config.AppSecret);
                    }
                    break;
            }
            WeChat.AccessTokenResult result;
            try
            {
                result = wechat.GetAccessTokenSns(code);
            }
            catch (Exception ex)
            {
                return this.ToError("错误", "获取微信用户失败", Url.Action("Login"));
            }

            var openID = result.OpenID;
            if (state == "openid")
            {
                Response.Cookies["WeChatOpenID"].Value = openID;
                return Json(Comm.ToJsonResult("Success", "成功", new { OpenID = openID }));
            }
            var accessToken = result.AccessToken;
            var unionid = result.UnionID;
            try
            {
                var userInfo = wechat.GetUserInfoSns(openID, accessToken);

                var user = LoginByWeiXinInfo(unionid, userInfo.HeadImgUrl, userInfo.NickName);
                if (app > 0)
                {
                    return Json(Comm.ToJsonResult("Success", "成功", new UserViewModel(user)));
                }
                string returnUrl = null;
                switch (state.ToLower())
                {
                    case null:
                    case "":
                    case "couponindex":
                        {
                            returnUrl = Url.Action("Index", "Coupon");
                        }
                        break;
                    default:
                        {
                            returnUrl = state;
                        }
                        break;
                }
                if (user.UserType == Enums.UserType.Normal && !user.IsActive)
                {
                    return RedirectToAction("Activation", "Account", new { ReturnUrl = returnUrl });
                }
                return Redirect(returnUrl);
            }
            catch (Exception)
            {
                return error("请求有误");
            }


        }

        [HttpPost]
        [AllowAnonymous]
        [AllowCrossSiteJson]
        public ActionResult LoginByWeiXinUnionID(string unionID, string avatar, string nickname, bool NeedRegister = false)
        {
            if (!ModelState.IsValid)
            {
                return Json(Comm.ToJsonResult("Error", ModelState.FirstErrorMessage()));
            }
            try
            {
                var user = db.Users.FirstOrDefault(s => s.WeChatID == unionID);
                if (user == null)
                {
                    if (NeedRegister)
                    {
                        user = LoginByWeiXinInfo(unionID, avatar, nickname);
                    }
                    else
                    {
                        return Json(Comm.ToJsonResult("NoFound", "用户不存在"));
                    }
                }
                return Json(Comm.ToJsonResult("Success", "成功", new UserViewModel(user)));
            }
            catch (Exception ex)
            {
                return Json(Comm.ToJsonResult("Error", ex.Message));
            }
        }

        public ApplicationUser LoginByWeiXinInfo(string unionID, string avatar = null, string nickname = null)
        {

            if (string.IsNullOrWhiteSpace(unionID))
            {
                throw new Exception("unionID不可为空");
            }
            var user = db.Users.FirstOrDefault(s => s.WeChatID == unionID);

            if (user == null)
            {
                user = CreateByWeChat(new WeChat.UserInfoResult()
                {
                    HeadImgUrl = avatar,
                    NickName = nickname,
                    UnionID = unionID
                });
            }
            SignInManager.SignIn(user, true, true);

            return user;
        }

        private ApplicationUser CreateByWeChat(WeChat.UserInfoResult model)
        {
            string username, nickname, avart, unionId = model.UnionID;

            nickname = model.NickName;

            avart = model.HeadImgUrl;
            if (!string.IsNullOrWhiteSpace(avart))
            {
                try
                {
                    avart = this.Download(avart);
                }
                catch (Exception)
                {
                    avart = "~/Content/Images/404/avatar.png";
                }
            }

            unionId = model.UnionID;

            ApplicationUser user = db.Users.FirstOrDefault(s => s.WeChatID == unionId);
            if (user == null)
            {
                do
                {
                    username = $"wc{DateTime.Now:yyyyMMddHHmmss}{Comm.Random.Next(1000, 9999)}";
                } while (db.Users.Any(s => s.UserName == username));
                if (string.IsNullOrWhiteSpace(nickname))
                {
                    nickname = username;
                }
                user = new ApplicationUser
                {
                    WeChatID = unionId,
                    UserName = username,
                    NickName = nickname,
                    Avatar = avart,
                    RegisterDateTime = DateTime.Now,
                    LastLoginDateTime = DateTime.Now,
                };
                var r = UserManager.Create(user);

                user = db.Users.FirstOrDefault(s => s.WeChatID == unionId);
                db.SaveChanges();
                if (!r.Succeeded)
                {
                    throw new Exception(r.Errors.FirstOrDefault());
                }
            }
            return user;
        }



        #endregion


        #region 帮助程序
        // 用于在添加外部登录名时提供 XSRF 保护
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}