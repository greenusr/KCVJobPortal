using System.Web.Mvc;
using JobPortalKCV.Models.ViewModel;

namespace JobPortalKCV.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        public ActionResult Index()
        {
            return Status(500);
        }

        public ActionResult BadRequest()
        {
            return Status(400);
        }

        public ActionResult Forbidden()
        {
            return Status(403);
        }

        public ActionResult NotFound()
        {
            return Status(404);
        }

        public ActionResult TooLarge()
        {
            return Status(413);
        }

        public ActionResult ServerError()
        {
            return Status(500);
        }

        public ActionResult Status(int statusCode = 500)
        {
            Response.StatusCode = statusCode;
            Response.TrySkipIisCustomErrors = true;

            return View("Error", BuildModel(statusCode));
        }

        private static ErrorViewModel BuildModel(int statusCode)
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
                        StatusCode = 500,
                        Title = "Something went wrong",
                        Message = "An unexpected error occurred. Please try again later."
                    };
            }
        }
    }
}
