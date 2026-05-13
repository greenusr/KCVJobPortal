using System;
using System.Linq;
using System.Web.Mvc;
using JobPortalKCV.Models;
using JobPortalKCV.Services;

namespace JobPortalKCV.Filters
{
    public class MaintenanceModeFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (var data = new JobPortalDataContext())
            {
                var settings = SystemSettingsService.GetSettings(data);

                if (!settings.maintenance_mode || IsAllowedDuringMaintenance(filterContext, data))
                {
                    base.OnActionExecuting(filterContext);
                    return;
                }
            }

            filterContext.Result = new ContentResult
            {
                Content = "The system is currently under maintenance. Please try again later."
            };
            filterContext.HttpContext.Response.StatusCode = 503;
        }

        private bool IsAllowedDuringMaintenance(ActionExecutingContext filterContext, JobPortalDataContext data)
        {
            var controller = Convert.ToString(filterContext.RouteData.Values["controller"]);
            var action = Convert.ToString(filterContext.RouteData.Values["action"]);

            if (controller.Equals("Account", StringComparison.OrdinalIgnoreCase) &&
                (action.Equals("Login", StringComparison.OrdinalIgnoreCase) || action.Equals("Logout", StringComparison.OrdinalIgnoreCase)))
                return true;

            var username = filterContext.HttpContext.User == null ? null : filterContext.HttpContext.User.Identity.Name;

            return data.UserRoles.Any(userRole =>
                userRole.User.username == username &&
                userRole.Role.role_name == "Admin");
        }
    }
}
