using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using JobPortalKCV.Controllers;
using JobPortalKCV.Services;

namespace JobPortalKCV
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private const int MaximumFriendlyRequestBytes = 100 * 1024 * 1024;

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

        protected void Application_BeginRequest()
        {
            if (Request.ContentLength <= MaximumFriendlyRequestBytes)
                return;

            RenderFriendlyError(413);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            var httpException = exception as HttpException;
            var statusCode = httpException == null ? 500 : httpException.GetHttpCode();

            if (statusCode < 400)
                statusCode = 500;

            Server.ClearError();
            RenderFriendlyError(statusCode);
        }

        private void RenderFriendlyError(int statusCode)
        {
            Response.Clear();
            Response.StatusCode = statusCode;
            Response.TrySkipIisCustomErrors = true;

            var routeData = new RouteData();
            routeData.Values["controller"] = "Error";
            routeData.Values["action"] = "Status";
            routeData.Values["statusCode"] = statusCode;

            var requestContext = new RequestContext(new HttpContextWrapper(Context), routeData);
            IController controller = new ErrorController();
            controller.Execute(requestContext);
        }
    }
}
