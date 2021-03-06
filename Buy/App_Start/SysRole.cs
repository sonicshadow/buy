﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Buy
{
    public static class SysRole
    {
        #region 系统权限

    
        public const string RoleManageRead = "RoleManageRead";
        public const string RoleManageCreate = "RoleManageCreate";
        public const string RoleManageEdit = "RoleManageEdit";
        public const string RoleManageDelete = "RoleManageDelete";

        public const string UserManageRead = "UserManageRead";
        public const string UserManageCreate = "UserManageCreate";
        public const string UserManageEdit = "UserManageEdit";
        public const string UserManageDelete = "UserManageDelete";
        public const string UserManageUpdate = "UserManageUpdate";
        public const string UserManageEnableTakeChildProxy = "UserManageEnableTakeChildProxy";

        public const string AdminManageRead = "AdminManageRead";
        public const string AdminManageCreate = "AdminManageCreate";
        public const string AdminManageEdit = "AdminManageEdit";
        public const string AdminManageDelete = "AdminManageDelete";

        public const string CouponTypeManageRead = "CouponTypeManageRead";
        public const string CouponTypeManageCreate = "CouponTypeManageCreate";
        public const string CouponTypeManageEdit = "CouponTypeManageEdit";
        public const string CouponTypeManageDelete = "CouponTypeManageDelete";

        public const string CouponManageRead = "CouponManageRead";
        public const string CouponManageCreate = "CouponTypeManageCreate";
        public const string CouponManageEdit = "CouponManageEdit";
        public const string CouponManageDelete = "CouponManageDelete";

        public const string RegistrationCodeManageRead = "RegistrationCodeManageRead";
        public const string RegistrationCodeManageCreate = "RegistrationCodeManageCreate";
        public const string RegistrationCodeManageEdit = "RegistrationCodeManageEdit";
        public const string RegistrationCodeManageDelete = "RegistrationCodeManageDelete";

        public const string ShopManageRead = "ShopManageRead";
        public const string ShopManageCreate = "ShopManageCreate";
        public const string ShopManageEdit = "ShopManageEdit";
        public const string ShopManageDelete = "ShopManageDelete";

        public const string LocalCouponManageRead = "LocalCouponManageRead";
        public const string LocalCouponManageCreate = "LocalCouponManageCreate";
        public const string LocalCouponManageEdit = "LocalCouponManageEdit";
        public const string LocalCouponManageDelete = "LocalCouponManageDelete";

        public const string BannerManageRead = "BannerManageRead";
        public const string BannerManageCreate = "BannerManageCreate";
        public const string BannerManageEdit = "BannerManageEdit";
        public const string BannerManageDelete = "BannerManageDelete";

        public const string ClassifyManageRead = "ClassifyManageRead";
        public const string ClassifyManageCreate = "ClassifyManageCreate";
        public const string ClassifyManageEdit = "ClassifyManageEdit";
        public const string ClassifyManageDelete = "ClassifyManageDelete";

        public const string CustomerServiceManageRead = "CustomerServiceManageRead";
        public const string CustomerServiceManageCreate = "CustomerServiceManageCreate";
        public const string CustomerServiceManageEdit = "CustomerServiceManageEdit";
        public const string CustomerServiceManageDelete = "CustomerServiceManageDelete";

        public const string UpdateLogManageRead = "UpdateLogManageRead";
        public const string UpdateLogManageCreate = "UpdateLogManageCreate";
        public const string UpdateLogManageEdit = "UpdateLogManageEdit";
        public const string UpdateLogManageDelete = "UpdateLogManageDelete";

        #endregion

        #region 用户权限
        /// <summary>
        /// 收二级代理
        /// </summary>
        public const string UserTakeChildProxy = "TakeChildProxy";
        #endregion

    }
}