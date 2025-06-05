﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MBS.Web.Portal
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");            

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "ClaimsRTFReportDownload",
                url: "ClaimsReportRTFDownload/{start}/{end}/{type}",
                defaults: new { controller = "ClaimsReport", action = "Download" }
            );

            routes.MapRoute(
                name: "ClaimsPDFReportDownload",
                url: "ClaimsReportPDFDownload/{start}/{end}/{type}",
                defaults: new { controller = "ClaimsReport", action = "DownloadPDF" }
            );

        }
    }
}