﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Buy.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Buy.Bll
{
    public static class SystemSettings
    {
        static SystemSettings()
        {
            Init();
            Load();
        }

        private static void Load()
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                var systems = db.SystemSettings.ToList();
                foreach (var item in systems)
                {
                    switch (item.Key)
                    {
                        default:
                            break;
                    }
                    var tptType = db.CouponTypes.ToList();
                    _couponType = new ObservableCollection<Models.CouponType>(tptType);
                    _couponType.CollectionChanged += _couponType_CollectionChanged;

                }
            }
        }


        private static void _couponType_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        {
                            foreach (Models.CouponType item in e.NewItems)
                            {
                                db.CouponTypes.Attach(item);
                                db.Entry(item).State = System.Data.Entity.EntityState.Added;
                            }
                            db.SaveChanges();
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        {
                            var temp = new List<Models.CouponType>();
                            foreach (Models.CouponType item in e.OldItems)
                            {

                                temp.Add(item);
                            }
                            var ids = temp.Select(s => (int?)s.ID).ToList();
                            var items = db.Coupons.Where(s => ids.Contains(s.TypeID)).ToList();
                            if (items.Count > 0)
                            {
                                foreach (var item in items)
                                {
                                    item.TypeID = null;
                                }
                                db.SaveChanges();
                            }
                            foreach (var item in temp)
                            {
                                db.CouponTypes.Attach(item);
                                db.Entry(item).State = System.Data.Entity.EntityState.Deleted;
                            }
                            db.SaveChanges();
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        {
                            foreach (Models.CouponType item in e.NewItems)
                            {
                                db.CouponTypes.Attach(item);
                                db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                            }
                            db.SaveChanges();
                        }
                        break;
                    default:
                        break;
                }
            }

        }

     
        public static void Init()
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                var setting = db.SystemSettings.ToList();
                var init = new List<SystemSetting>();
               
              
                if (init.Count > 0)
                {
                    db.SystemSettings.AddRange(init);
                    db.SaveChanges();
                }
            }
        }



        //第三方优惠分類
        private static ObservableCollection<CouponType> _couponType;

        public static ObservableCollection<CouponType> ThirdPartyTicketType
        {
            get
            {
                return _couponType;
            }
        }


        /// <summary>
        /// 更新后Setting清空内存
        /// </summary>
        /// <param name="t"></param>
        /// <param name="o"></param>
        private static void Update(Enums.SystemSettingType t, object o)
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                var setting = db.SystemSettings.FirstOrDefault(s => s.Key == t);
                if (setting != null)
                {
                    setting.Value = JsonConvert.SerializeObject(o);
                }
                else
                {
                    db.SystemSettings.Add(new SystemSetting
                    {
                        Key = t,
                        Value = JsonConvert.SerializeObject(o)
                    });
                }
                db.SaveChanges();
            }
        }

        /// <summary>
        /// 清空内存
        /// </summary>
        public static void Clean()
        {
            Load();
        }

    }
}