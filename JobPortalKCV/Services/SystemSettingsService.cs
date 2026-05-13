using System;
using System.Linq;
using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class SystemSettingsService
    {
        public const int DefaultUploadSizeMb = 20;
        public const int DefaultOtpExpirationSeconds = 90;
        public const int DefaultPaginationSize = 10;
        public const int DefaultJobExpirationDays = 30;
        public const string DefaultAllowedImages = "jpg,jpeg,png";
        public const string DefaultAllowedCvs = "pdf,doc,docx";

        public static SystemSetting GetSettings(JobPortalDataContext data)
        {
            var settings = data.SystemSettings.OrderBy(item => item.setting_id).FirstOrDefault();

            if (settings != null)
                return settings;

            settings = new SystemSetting
            {
                site_name = "Job Portal",
                max_cv_upload_size_mb = DefaultUploadSizeMb,
                max_avatar_upload_size_mb = DefaultUploadSizeMb,
                max_logo_upload_size_mb = DefaultUploadSizeMb,
                allowed_image_types = DefaultAllowedImages,
                allowed_cv_types = DefaultAllowedCvs,
                otp_expiration_seconds = DefaultOtpExpirationSeconds,
                default_pagination_size = DefaultPaginationSize,
                maintenance_mode = false,
                require_company_logo_to_post_job = true,
                auto_close_expired_jobs = false,
                default_job_expiration_days = DefaultJobExpirationDays
            };

            data.SystemSettings.InsertOnSubmit(settings);
            data.SubmitChanges();
            return settings;
        }

        public static int GetPaginationSize(JobPortalDataContext data)
        {
            return Clamp(GetSettings(data).default_pagination_size, 5, 100, DefaultPaginationSize);
        }

        public static int GetOtpExpirationSeconds(JobPortalDataContext data)
        {
            return Clamp(GetSettings(data).otp_expiration_seconds, 30, 600, DefaultOtpExpirationSeconds);
        }

        public static string GetDefaultUserAvatarPath(JobPortalDataContext data)
        {
            var path = GetSettings(data).default_user_avatar_path;
            return String.IsNullOrWhiteSpace(path) ? "/img/blog/author.png" : path;
        }

        public static string GetCompanyLogoOrDefault(JobPortalDataContext data, string logoPath)
        {
            if (!String.IsNullOrWhiteSpace(logoPath))
                return logoPath;

            var defaultPath = GetSettings(data).default_company_logo_path;
            return String.IsNullOrWhiteSpace(defaultPath) ? "/img/icon/job-list1.png" : defaultPath;
        }

        public static bool RequiresCompanyLogoToPostJob(JobPortalDataContext data)
        {
            return GetSettings(data).require_company_logo_to_post_job;
        }

        public static void AutoCloseExpiredJobs(JobPortalDataContext data)
        {
            var settings = GetSettings(data);

            if (!settings.auto_close_expired_jobs)
                return;

            data.ExecuteCommand(@"
IF COL_LENGTH('dbo.Jobs', 'is_active') IS NOT NULL
BEGIN
    UPDATE dbo.Jobs
    SET is_active = 0
    WHERE is_active = 1 AND application_deadline < GETDATE();
END");
        }

        public static int Clamp(int value, int min, int max, int fallback)
        {
            if (value < min || value > max)
                return fallback;

            return value;
        }
    }
}
