using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using JobPortalKCV.Services;

namespace JobPortalKCV
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            SystemSettingsSchemaService.EnsureSchema();
            UploadSchemaService.EnsureSchema();
            ProfileSchemaService.EnsureSchema();
            ApplicationSchemaService.EnsureSchema();
            StarSchemaService.EnsureSchema();
            CompanySchemaService.EnsureSchema();
            CandidateInvitationSchemaService.EnsureSchema();
            NotificationSchemaService.EnsureSchema();
            AccountSettingsSchemaService.EnsureSchema();
            AdminSchemaService.EnsureSchema();
        }
    }
}
