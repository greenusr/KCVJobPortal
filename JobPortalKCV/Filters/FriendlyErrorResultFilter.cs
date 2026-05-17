using System.Web.Mvc;
using JobPortalKCV.Models.ViewModel;

namespace JobPortalKCV.Filters
{
    public class FriendlyErrorResultFilter : IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.IsChildAction)
                return;

            if (filterContext.RouteData.Values["controller"] != null &&
                filterContext.RouteData.Values["controller"].ToString() == "Error")
                return;

            var statusResult = filterContext.Result as HttpStatusCodeResult;

            if (statusResult == null || statusResult.StatusCode < 400)
                return;

            filterContext.HttpContext.Response.StatusCode = statusResult.StatusCode;
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;

            filterContext.Result = new ViewResult
            {
                ViewName = "Error",
                ViewData = new ViewDataDictionary<ErrorViewModel>(BuildModel(statusResult.StatusCode, statusResult.StatusDescription)),
                TempData = filterContext.Controller.TempData
            };
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }

        private static ErrorViewModel BuildModel(int statusCode, string statusDescription)
        {
            switch (statusCode)
            {
                case 400:
                    return new ErrorViewModel
                    {
                        StatusCode = 400,
                        Title = "Bad request",
                        Message = "The request could not be processed. Please check the information and try again."
                    };
                case 403:
                    return new ErrorViewModel
                    {
                        StatusCode = 403,
                        Title = "Access denied",
                        Message = "You do not have permission to view this page."
                    };
                case 404:
                    return new ErrorViewModel
                    {
                        StatusCode = 404,
                        Title = "Page not found",
                        Message = "The page you are looking for does not exist or has been moved."
                    };
                case 413:
                    return new ErrorViewModel
                    {
                        StatusCode = 413,
                        Title = "File is too large",
                        Message = "The uploaded file is larger than the server allows. Please choose a smaller file and try again."
                    };
                default:
                    return new ErrorViewModel
                    {
                        StatusCode = statusCode,
                        Title = "Something went wrong",
                        Message = string.IsNullOrWhiteSpace(statusDescription)
                            ? "An unexpected error occurred. Please try again later."
                            : statusDescription
                    };
            }
        }
    }
}
