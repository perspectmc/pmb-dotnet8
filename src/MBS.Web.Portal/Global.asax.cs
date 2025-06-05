using System;
using System.Collections.Generic;
using System.Web.Caching;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using MBS.Web.Portal.Models;
using MBS.Web.Portal.Services;
using MBS.Common;
using System.Web;

namespace MBS.Web.Portal
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        //private static Cache myCache = null;
        //private static string myFeeFilePath = null;
        //private static string myICDFilePath = null;
        //private static string myRefDocFilePath = null;
        //private static string myExplainCodeFilePath = null;
        //private static string myRunScheduleFilePath = null;
        //private static string myWCBFeeFilePath = null;
        //private static string myCareCodeFilePath = null;

        protected void Application_Start()
        {
            ViewEngines.Engines.Clear(); 
            ViewEngines.Engines.Add(new RazorViewEngine());

            ModelMetadataProviders.Current = new CachedDataAnnotationsModelMetadataProvider();
            MvcHandler.DisableMvcResponseHeader = true;

            AreaRegistration.RegisterAllAreas();
            
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            #region Old Cache Setup Code

            //myCache = Context.Cache;

            //myFeeFilePath = Server.MapPath("App_Data/Fee.txt");

            //List<Fee> myFees = ItemLoader.LoadFee(myFeeFilePath);

            //myCache.Insert("Fees", myFees, new CacheDependency(myFeeFilePath),
            //				Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
            //				CacheItemPriority.Default,
            //				new CacheItemRemovedCallback(RefreshFees));

            //myICDFilePath = Server.MapPath("App_Data/ICD.txt");
            //List<ICD> myICDs = ItemLoader.LoadICD(myICDFilePath);
            //myCache.Insert("ICDs", myICDs, new CacheDependency(myICDFilePath),
            //						Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
            //						CacheItemPriority.Default,
            //						new CacheItemRemovedCallback(RefreshICDs));

            //myRefDocFilePath = Server.MapPath("App_Data/RefDoc.txt");
            //List<RefDoc> myRefDocs = ItemLoader.LoadRefDoc(myRefDocFilePath);
            //myCache.Insert("RefDocs", myRefDocs, new CacheDependency(myRefDocFilePath),
            //			   Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
            //			   CacheItemPriority.Default,
            //			   new CacheItemRemovedCallback(RefreshRefDocs));

            //myExplainCodeFilePath = Server.MapPath("App_Data/ExplainCode.txt");
            //List<ExplainCode> myExplainCodes = ItemLoader.LoadExplainCode(myExplainCodeFilePath);
            //myCache.Insert("ExplainCodes", myExplainCodes, new CacheDependency(myExplainCodeFilePath),
            //			   Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
            //			   CacheItemPriority.Default,
            //			   new CacheItemRemovedCallback(RefreshExplainCodes));

            //         myRunScheduleFilePath = Server.MapPath("App_Data/RunSchedule.txt");
            //         var myRunSchedules = ItemLoader.LoadRunSchedule(myRunScheduleFilePath);
            //         myCache.Insert("RunSchedules", myRunSchedules, new CacheDependency(myRunScheduleFilePath),
            //                        Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
            //                        CacheItemPriority.Default,
            //                        new CacheItemRemovedCallback(RefreshRunSchedules));

            //         myWCBFeeFilePath = Server.MapPath("App_Data/WCBFee.txt");
            //         List<Fee> myWCBFees = ItemLoader.LoadWCBFee(myWCBFeeFilePath);
            //         myCache.Insert("WCBFees", myWCBFees, new CacheDependency(myWCBFeeFilePath),
            //                         Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
            //                         CacheItemPriority.Default,
            //                         new CacheItemRemovedCallback(RefreshWCBFees));

            //         myCareCodeFilePath = Server.MapPath("App_Data/CareCode.txt");
            //         var myCareCodes = ItemLoader.LoadCareCodes(myCareCodeFilePath);
            //         myCache.Insert("CareCodes", myCareCodes, new CacheDependency(myCareCodeFilePath),
            //                        Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
            //                        CacheItemPriority.Default,
            //                        new CacheItemRemovedCallback(RefreshCareCodes));

            #endregion

            //JobScheduler.Start(ConfigHelper.GetTriggerExpression(), ConfigHelper.GetTriggerPendingClaimsExpression());
		}

        protected void Application_BeginRequest(Object source, EventArgs e)
        {
            //Response.AddHeader("Cache-Control", "private,max-age=0,no-cache,no-store,must-revalidate");
            //Response.AddHeader("Pragma", "no-cache");
            //Response.AddHeader("Expires", "Tue, 01 Jan 1970 00:00:00 GMT");

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.AppendCacheExtension("private, must-revalidate");
            Response.AppendHeader("Pragma", "no-cache");
            Response.AppendHeader("Expires", "0");

            if (!Context.Request.IsSecureConnection &&
                !Request.Url.Host.Contains("localhost"))
            {
                Response.Redirect(Request.Url.AbsoluteUri.Replace("http://", "https://"));
            }
        }

        protected void Application_Error()
        {
            var ex = Server.GetLastError();

            try
            {                                
                new MailSender().SendEmail(ConfigHelper.GetSupportEmail(), "Exception Error - " + DateTime.Now.ToString("yyyyMMddHHmmss"), GetExceptionMessage(ex));
            }
            catch (Exception)
            {                
            }
        }

        private string GetExceptionMessage(Exception ex)
        {
            var errorMessage = string.Empty;

            errorMessage += "Memory: " + string.Format("{0:N}MB", (GC.GetTotalMemory(false) / 1000000)) + "<br>";
            errorMessage += "Message: " + ex.Message + "<br>";
            errorMessage += "Source: " + ex.Source + "<br>";            
            errorMessage += "Stack Trace: " + ex.StackTrace + "<br>";

            if (ex.InnerException != null)
            {
                errorMessage += "<br><h3>Inner Exception</h3><br>";
                errorMessage += GetExceptionMessage(ex.InnerException);
            }

            return errorMessage;
        }

        #region Old Caching Code

        /*
		private static void RefreshFees(String key, Object item, CacheItemRemovedReason reason)
		{
			List<Fee> myFees = ItemLoader.LoadFee(myFeeFilePath);

			myCache.Insert("Fees", myFees, new CacheDependency(myFeeFilePath),
							Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
							CacheItemPriority.Default,
							new CacheItemRemovedCallback(RefreshFees));
		}

		private static void RefreshICDs(String key, Object item, CacheItemRemovedReason reason)
		{
			List<ICD> myICDs = ItemLoader.LoadICD(myICDFilePath);

			myCache.Insert("ICDs", myICDs, new CacheDependency(myICDFilePath),
							Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
							CacheItemPriority.Default,
							new CacheItemRemovedCallback(RefreshICDs));
		}

		private static void RefreshRefDocs(String key, Object item, CacheItemRemovedReason reason)
		{
			List<RefDoc> myRefDocs = ItemLoader.LoadRefDoc(myRefDocFilePath);

			myCache.Insert("RefDocs", myRefDocs, new CacheDependency(myRefDocFilePath),
							Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
							CacheItemPriority.Default,
							new CacheItemRemovedCallback(RefreshRefDocs));
		}

		private static void RefreshExplainCodes(String key, Object item, CacheItemRemovedReason reason)
		{
			List<ExplainCode> myExplainCodes = ItemLoader.LoadExplainCode(myExplainCodeFilePath);

			myCache.Insert("ExplainCodes", myExplainCodes, new CacheDependency(myExplainCodeFilePath),
							Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
							CacheItemPriority.Default,
							new CacheItemRemovedCallback(RefreshExplainCodes));
		}

        private static void RefreshRunSchedules(String key, Object item, CacheItemRemovedReason reason)
        {
            var myRunSchedules = ItemLoader.LoadRunSchedule(myRunScheduleFilePath);

            myCache.Insert("RunSchedules", myRunSchedules, new CacheDependency(myRunScheduleFilePath),
                           Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
                           CacheItemPriority.Default,
                           new CacheItemRemovedCallback(RefreshRunSchedules));
        }

        private static void RefreshWCBFees(String key, Object item, CacheItemRemovedReason reason)
        {
            List<Fee> myWCBFees = ItemLoader.LoadWCBFee(myWCBFeeFilePath);

            myCache.Insert("WCBFees", myWCBFees, new CacheDependency(myWCBFeeFilePath),
                            Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
                            CacheItemPriority.Default,
                            new CacheItemRemovedCallback(RefreshWCBFees));
        }

        private static void RefreshCareCodes(String key, Object item, CacheItemRemovedReason reason)
        {
            var myCareCodes = ItemLoader.LoadCareCodes(myCareCodeFilePath);
            myCache.Insert("CareCodes", myCareCodes, new CacheDependency(myCareCodeFilePath),
                           Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
                           CacheItemPriority.Default,
                           new CacheItemRemovedCallback(RefreshCareCodes));
        }

        */

        #endregion
    }
}