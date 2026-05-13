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
    public class AdminSystemSettingsController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsAdmin())
            {
                filterContext.Result = new HttpStatusCodeResult(403, "You are not allowed to access this page.");
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {
            return View(ToViewModel(SystemSettingsService.GetSettings(data)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(SystemSettingsViewModel model, HttpPostedFileBase siteLogo, HttpPostedFileBase defaultUserAvatar, HttpPostedFileBase defaultCompanyLogo)
        {
            var settings = SystemSettingsService.GetSettings(data);

            if (!IsValidConfiguration(model))
            {
                TempData["SystemSettingsError"] = "Invalid configuration.";
                return View(ToViewModel(settings, model));
            }

            var siteLogoResult = SaveOptionalSystemImage(siteLogo);
            var defaultUserAvatarResult = SaveOptionalSystemImage(defaultUserAvatar);
            var defaultCompanyLogoResult = SaveOptionalSystemImage(defaultCompanyLogo);

            if (!siteLogoResult.Success || !defaultUserAvatarResult.Success || !defaultCompanyLogoResult.Success)
            {
                TempData["SystemSettingsError"] = FirstError(siteLogoResult, defaultUserAvatarResult, defaultCompanyLogoResult);
                return View(ToViewModel(settings, model));
            }

            settings.site_name = model.SiteName.Trim();
            settings.max_cv_upload_size_mb = model.MaxCvUploadSizeMb;
            settings.max_avatar_upload_size_mb = model.MaxAvatarUploadSizeMb;
            settings.max_logo_upload_size_mb = model.MaxLogoUploadSizeMb;
            settings.allowed_image_types = NormalizeTypes(model.AllowedImageTypes, SystemSettingsService.DefaultAllowedImages);
            settings.allowed_cv_types = NormalizeTypes(model.AllowedCvTypes, SystemSettingsService.DefaultAllowedCvs);
            settings.otp_expiration_seconds = model.OtpExpirationSeconds;
            settings.default_pagination_size = model.DefaultPaginationSize;
            settings.maintenance_mode = model.MaintenanceMode;
            settings.require_company_logo_to_post_job = model.RequireCompanyLogoToPostJob;
            settings.auto_close_expired_jobs = model.AutoCloseExpiredJobs;
            settings.default_job_expiration_days = model.DefaultJobExpirationDays;
            settings.updated_at = DateTime.Now;

            if (!String.IsNullOrWhiteSpace(siteLogoResult.FilePath))
                settings.site_logo_path = siteLogoResult.FilePath;

            if (!String.IsNullOrWhiteSpace(defaultUserAvatarResult.FilePath))
                settings.default_user_avatar_path = defaultUserAvatarResult.FilePath;

            if (!String.IsNullOrWhiteSpace(defaultCompanyLogoResult.FilePath))
                settings.default_company_logo_path = defaultCompanyLogoResult.FilePath;

            data.SubmitChanges();
            TempData["SystemSettingsMessage"] = "System settings updated successfully.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private bool IsAdmin()
        {
            return data.UserRoles.Any(userRole =>
                userRole.User.username == User.Identity.Name &&
                userRole.Role.role_name == "Admin");
        }

        private bool IsValidConfiguration(SystemSettingsViewModel model)
        {
            return model != null &&
                !String.IsNullOrWhiteSpace(model.SiteName) &&
                InRange(model.MaxCvUploadSizeMb, 1, 100) &&
                InRange(model.MaxAvatarUploadSizeMb, 1, 100) &&
                InRange(model.MaxLogoUploadSizeMb, 1, 100) &&
                InRange(model.OtpExpirationSeconds, 30, 600) &&
                InRange(model.DefaultPaginationSize, 5, 100) &&
                InRange(model.DefaultJobExpirationDays, 1, 365);
        }

        private bool InRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        private FileUploadResult SaveOptionalSystemImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return new FileUploadResult { Success = true };

            return FileUploadService.SaveSystemImage(file, Server);
        }

        private string FirstError(params FileUploadResult[] results)
        {
            return results
                .Where(result => result != null && !result.Success)
                .Select(result => result.ErrorMessage)
                .FirstOrDefault() ?? "Something went wrong. Please try again.";
        }

        private string NormalizeTypes(string value, string fallback)
        {
            if (String.IsNullOrWhiteSpace(value))
                return fallback;

            return String.Join(",", value
                .Split(',')
                .Select(item => item.Trim().TrimStart('.').ToLowerInvariant())
                .Where(item => !String.IsNullOrWhiteSpace(item))
                .Distinct());
        }

        private SystemSettingsViewModel ToViewModel(SystemSetting settings, SystemSettingsViewModel posted = null)
        {
            if (posted != null)
            {
                posted.SettingId = settings.setting_id;
                posted.SiteLogoPath = settings.site_logo_path;
                posted.DefaultUserAvatarPath = settings.default_user_avatar_path;
                posted.DefaultCompanyLogoPath = settings.default_company_logo_path;
                return posted;
            }

            return new SystemSettingsViewModel
            {
                SettingId = settings.setting_id,
                SiteName = settings.site_name,
                SiteLogoPath = settings.site_logo_path,
                DefaultUserAvatarPath = settings.default_user_avatar_path,
                DefaultCompanyLogoPath = settings.default_company_logo_path,
                MaxCvUploadSizeMb = settings.max_cv_upload_size_mb,
                MaxAvatarUploadSizeMb = settings.max_avatar_upload_size_mb,
                MaxLogoUploadSizeMb = settings.max_logo_upload_size_mb,
                AllowedImageTypes = settings.allowed_image_types,
                AllowedCvTypes = settings.allowed_cv_types,
                OtpExpirationSeconds = settings.otp_expiration_seconds,
                DefaultPaginationSize = settings.default_pagination_size,
                MaintenanceMode = settings.maintenance_mode,
                RequireCompanyLogoToPostJob = settings.require_company_logo_to_post_job,
                AutoCloseExpiredJobs = settings.auto_close_expired_jobs,
                DefaultJobExpirationDays = settings.default_job_expiration_days
            };
        }
    }
}
