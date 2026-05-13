using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JobPortalKCV.Models;
using JobPortalKCV.Models.ViewModel;
using JobPortalKCV.Services;

namespace JobPortalKCV.Controllers
{
    [Authorize]
    public class UserCVsController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        public ActionResult Index(int page = 1)
        {
            var user = GetCurrentUser();

            if (user == null)
                return HttpNotFound();

            var cvs = data.UserCVs
                .Where(cv => cv.user_id == user.user_id)
                .OrderByDescending(cv => cv.is_default)
                .ThenByDescending(cv => cv.created_at)
                .ToList();

            cvs = PaginationService.Paginate(
                cvs,
                page,
                SystemSettingsService.GetPaginationSize(data),
                out PaginationViewModel pagination,
                "Index",
                "UserCVs");
            ViewBag.Pagination = pagination;

            return View(cvs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(HttpPostedFileBase cvFile)
        {
            var user = GetCurrentUser();

            if (user == null)
                return HttpNotFound();

            var result = FileUploadService.SaveCv(cvFile, Server);

            if (!result.Success)
            {
                TempData["CVError"] = result.ErrorMessage;
                return RedirectToAction("Index");
            }

            var hasDefault = data.UserCVs.Any(cv => cv.user_id == user.user_id && cv.is_default);

            data.UserCVs.InsertOnSubmit(new UserCV
            {
                user_id = user.user_id,
                file_path = result.FilePath,
                file_name = result.OriginalFileName,
                created_at = DateTime.Now,
                is_default = !hasDefault
            });
            AccountLogService.LogActivity(data, user.user_id, "UploadCV", "Uploaded CV: " + result.OriginalFileName, Request);
            data.SubmitChanges();

            TempData["CVMessage"] = "CV uploaded successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetDefault(int id, int page = 1)
        {
            var user = GetCurrentUser();

            if (user == null)
                return HttpNotFound();

            var cv = data.UserCVs.FirstOrDefault(item => item.cv_id == id && item.user_id == user.user_id);

            if (cv == null)
            {
                TempData["CVError"] = "Invalid CV selection.";
                return RedirectToAction("Index", new { page = page });
            }

            foreach (var item in data.UserCVs.Where(item => item.user_id == user.user_id))
                item.is_default = item.cv_id == id;

            AccountLogService.LogActivity(data, user.user_id, "UpdateSettings", "Default CV updated.", Request, relatedId: id, relatedType: "CV");
            data.SubmitChanges();

            TempData["CVMessage"] = "File uploaded successfully.";
            return RedirectToAction("Index", new { page = page });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private User GetCurrentUser()
        {
            return data.Users.FirstOrDefault(user => user.username == User.Identity.Name);
        }
    }
}
