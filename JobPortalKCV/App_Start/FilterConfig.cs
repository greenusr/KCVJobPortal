using System.Web;
using System.Web.Mvc;
using JobPortalKCV.Filters;

namespace JobPortalKCV
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new FriendlyErrorResultFilter());
            filters.Add(new MaintenanceModeFilter());
        }
    }
}
