using System.ComponentModel.DataAnnotations;

namespace JobPortalKCV.Models.ViewModel
{
    public class SystemSettingsViewModel
    {
        public int SettingId { get; set; }

        [Required]
        public string SiteName { get; set; }

        public string SiteLogoPath { get; set; }
        public string DefaultUserAvatarPath { get; set; }
        public string DefaultCompanyLogoPath { get; set; }

        [Range(1, 100)]
        public int MaxCvUploadSizeMb { get; set; }

        [Range(1, 100)]
        public int MaxAvatarUploadSizeMb { get; set; }

        [Range(1, 100)]
        public int MaxLogoUploadSizeMb { get; set; }

        public string AllowedImageTypes { get; set; }
        public string AllowedCvTypes { get; set; }

        [Range(30, 600)]
        public int OtpExpirationSeconds { get; set; }

        [Range(5, 100)]
        public int DefaultPaginationSize { get; set; }

        public bool MaintenanceMode { get; set; }
        public bool RequireCompanyLogoToPostJob { get; set; }
        public bool AutoCloseExpiredJobs { get; set; }

        [Range(1, 365)]
        public int DefaultJobExpirationDays { get; set; }
    }
}
