using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace JobPortalKCV.Models.ViewModel
{
    public class AccountSettingsViewModel
    {
        public string ActiveTab { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool IsCandidate { get; set; }
        public UserSettingsFormViewModel Settings { get; set; }
        public ChangePasswordViewModel Password { get; set; }
        public List<SelectListItem> CvOptions { get; set; }
        public List<LoginLogItemViewModel> LoginLogs { get; set; }
        public List<ActivityLogItemViewModel> ActivityLogs { get; set; }
        public string ActivityActionFilter { get; set; }
        public List<SelectListItem> ActivityActions { get; set; }
        public int LoginPage { get; set; }
        public int LoginTotalPages { get; set; }
        public int ActivityPage { get; set; }
        public int ActivityTotalPages { get; set; }
    }

    public class UserSettingsFormViewModel
    {
        public bool AppNotificationsEnabled { get; set; }
        public bool JobUpdatesEnabled { get; set; }
        public bool InterviewNotificationsEnabled { get; set; }
        public bool InvitationNotificationsEnabled { get; set; }
        public bool PublicProfileEnabled { get; set; }
        public bool ShowCvToEmployers { get; set; }
        public int? DefaultCvId { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }
    }

    public class LoginLogItemViewModel
    {
        public int LoginLogId { get; set; }
        public DateTime LoginTime { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public bool IsSuccess { get; set; }
        public string FailureReason { get; set; }
    }

    public class ActivityLogItemViewModel
    {
        public int ActivityLogId { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string Keyword { get; set; }
        public string Filters { get; set; }
        public int? RelatedId { get; set; }
        public string RelatedType { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
