﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Buy.Models;
using Microsoft.AspNet.Identity;

namespace Buy.Controllers
{
    public class FavoriteController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();

        private string UserID
        {

            get
            {
                return User.Identity.GetUserId();
            }
        }

        // GET: Favorite
        [Authorize]
        public ActionResult Coupon(string platforms, int page = 1)
        {
            var list = GetCoupon(UserID, platforms);
            var paged = list.OrderByDescending(s => s.CreateDateTime).ToPagedList(page);
            return View(paged);
        }

        [Authorize]
        public ActionResult LocalCoupon(int page = 1)
        {
            var list = GetLocalCoupon(UserID);
            var paged = list.OrderByDescending(s => s.Favorite.CreateDateTime).ToPagedList(page);
            return View(paged);
        }

        [HttpGet]
        [AllowCrossSiteJson]
        //收藏
        public ActionResult GetCouponFavorite(string userId, string platforms, int page = 1)
        {
            var list = GetCoupon(userId, platforms);
            var paged = list.OrderByDescending(s => s.CreateDateTime).ToPagedList(page);
            var model = paged.Select(s => new Models.ActionCell.CouponCell(s));
            return Json(Comm.ToJsonResultForPagedList(paged, model), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [AllowCrossSiteJson]
        //卡包
        public ActionResult GetLocalCouponFavorite(string userId, int page = 1)
        {
            var list = GetLocalCoupon(userId);
            var paged = list.OrderByDescending(s => s.Favorite.CreateDateTime).ToPagedList(page);
            var model = paged.Select(s => new Models.ActionCell.FavoriteLocalCouponCell(s.Favorite, new Models.ActionCell.LocalCouponCell(s.LocalCoupon)));
            return Json(Comm.ToJsonResultForPagedList(paged, model), JsonRequestBehavior.AllowGet);
        }

        public IQueryable<CouponUserViewModel> GetCoupon(string userId, string platforms)
        {
            var ps = platforms.SplitToArray<Enums.CouponPlatform>();
            if (ps == null || ps.Count <= 0)
            {
                ps = new List<Enums.CouponPlatform>();
                ps.Add(Enums.CouponPlatform.TaoBao);
                ps.Add(Enums.CouponPlatform.TMall);
            }
            else
            {
                if (ps.Any(s => s == Enums.CouponPlatform.TaoBao || s == Enums.CouponPlatform.TMall))
                {
                    ps.Add(Enums.CouponPlatform.TaoBao);
                    ps.Add(Enums.CouponPlatform.TMall);
                }
            }

            string couponUserID = Bll.Accounts.GetCouponUserID(userId);
            IQueryable<CouponUserViewModel> list;
            if (!string.IsNullOrWhiteSpace(couponUserID))
            {
                list = (from f in db.Favorites
                        join c in db.Coupons on f.CouponID equals c.ID into fc
                        from coupons in fc.DefaultIfEmpty()
                        from u in db.CouponUsers
                        where f.Type == Enums.FavoriteType.Coupon && f.UserID == userId &&
                         u.CouponID == coupons.ID && u.UserID == couponUserID && ps.Contains(coupons.Platform)
                        select coupons)
                       .Select(s => new CouponUserViewModel()
                       {
                           Commission = s.Commission,
                           CommissionRate = s.CommissionRate,
                           CreateDateTime = s.CreateDateTime,
                           DataJson = s.DataJson,
                           EndDateTime = s.EndDateTime,
                           Image = s.Image,
                           Left = s.Left,
                           ID = s.ID,
                           Name = s.Name,
                           OriginalPrice = s.OriginalPrice,
                           PCouponID = s.PCouponID,
                           Platform = s.Platform,
                           PLink = s.PLink,
                           Price = s.Price,
                           ProductID = s.ProductID,
                           ProductType = s.ProductType,
                           Sales = s.Sales,
                           ShopName = s.ShopName,
                           StartDateTime = s.StartDateTime,
                           Subtitle = s.Subtitle,
                           Total = s.Total,
                           TypeID = s.TypeID,
                           UrlLisr = s.UrlLisr,
                           Value = s.Value
                       });
            }
            else
            {
                list = (from f in db.Favorites
                        join c in db.Coupons on f.CouponID equals c.ID into fc
                        from coupons in fc.DefaultIfEmpty()
                        where f.Type == Enums.FavoriteType.Coupon && f.UserID == userId && ps.Contains(coupons.Platform)
                        select coupons)
                           .Select(s =>
                          new CouponQuery
                          {
                              CreateDateTime = s.CreateDateTime,
                              DataJson = s.DataJson,
                              Discount = s.OriginalPrice - s.Price,
                              DiscountRate = (s.OriginalPrice - s.Price) / s.OriginalPrice,
                              EndDateTime = s.EndDateTime,
                              Commission = s.Commission,
                              CommissionRate = s.CommissionRate,
                              ID = s.ID,
                              Image = s.Image,
                              Link = null,
                              Name = s.Name,
                              OriginalPrice = s.OriginalPrice,
                              Platform = s.Platform,
                              Price = s.Price,
                              ProductID = s.ProductID,
                              ProductType = s.ProductType,
                              Sales = s.Sales,
                              ShopName = s.ShopName,
                              StartDateTime = s.StartDateTime,
                              Subtitle = s.Subtitle,
                              Type = s.Type,
                              TypeID = s.TypeID,
                              Value = s.Value,
                              UserID = null,
                          });
            }
            return list;
        }


        public IQueryable<FavoriteLocalCouponList> GetLocalCoupon(string userId)
        {
            var localCoupons = from f in db.Favorites
                               from localCoupon in db.LocalCoupons
                               where f.Type == Enums.FavoriteType.LocalCoupon && f.UserID == userId && f.CouponID == localCoupon.ID
                               select new FavoriteLocalCouponList
                               {
                                   Favorite = f,
                                   LocalCoupon = new LocalCouponList()
                                   {
                                       ID = localCoupon.ID,
                                       ShopID = localCoupon.ShopID,
                                       Remark = localCoupon.Remark,
                                       Commission = localCoupon.Commission,
                                       CreateDateTime = localCoupon.CreateDateTime,
                                       EndDateTime = localCoupon.EndDateTime,
                                       Image = localCoupon.Image,
                                       IsFavorite = true,
                                       Name = localCoupon.Name,
                                       Price = localCoupon.Price,
                                       Shop = localCoupon.Shop,
                                   }
                               };
            return localCoupons;
        }

        // POST: Favorite/Create
        [HttpPost]
        [AllowCrossSiteJson]
        public ActionResult Create(Favorite model)
        {
            model.UserID = UserID == null ? model.UserID : UserID;
            if (model.UserID == null)
            {
                return Json(Comm.ToJsonResult("Error", "没有登录"));
            }
            var favorites = db.Favorites.Where(s => s.UserID == model.UserID &&
                s.CouponID == model.CouponID && s.Type == model.Type);
            if (favorites.Count() > 0)
            {
                return Json(Comm.ToJsonResult("Error", "已经收藏了"));
            }
            model.CreateDateTime = DateTime.Now;
            db.Favorites.Add(model);
            db.SaveChanges();
            return Json(Comm.ToJsonResult("Success", "成功", model.ID));
        }

        // POST: Favorite/Delete/5
        [HttpPost]
        [AllowCrossSiteJson]
        public ActionResult Delete(int id)
        {
            var favorite = db.Favorites.FirstOrDefault(s => s.ID == id);
            if (favorite == null)
            {
                return Json(Comm.ToJsonResult("Error", "没有收藏券"));
            }
            db.Favorites.Remove(favorite);
            db.SaveChanges();
            return Json(Comm.ToJsonResult("Success", "成功"));
        }
    }
}