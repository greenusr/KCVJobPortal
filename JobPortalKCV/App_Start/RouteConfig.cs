using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace JobPortalKCV
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "AdminSystemSettings",
                url: "Admin/SystemSettings",
                defaults: new { controller = "AdminSystemSettings", action = "Index" }
            );

            routes.MapRoute(
                name: "CompanySettings",
                url: "Company/Settings/{id}",
                defaults: new { controller = "CompanySettings", action = "Settings", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
