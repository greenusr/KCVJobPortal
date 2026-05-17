using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using JobPortalKCV.Helpers;
using JobPortalKCV.Models;
using JobPortalKCV.Models.ViewModel;
using JobPortalKCV.Services;

namespace JobPortalKCV.Controllers
{
    [Authorize]
    public class AccountSettingsController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        public ActionResult Index(string tab = "account", int loginPage = 1, int activityPage = 1, string activityAction = "")
        {
            var user = GetCurrentUser();

            if (user == null)
                return new HttpStatusCodeResult(403, "You are not allowed to access this page.");

            var settings = EnsureSettings(user.user_id);

            return View(BuildModel(user, settings, tab, loginPage, activityPage, activityAction));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            var user = GetCurrentUser();

            if (user == null)
                return new HttpStatusCodeResult(403, "You are not allowed to access this page.");

            if (!ModelState.IsValid)
            {
                TempData["AccountSettingsError"] = "Something went wrong. Please try again.";
                return RedirectToAction("Index", new { tab = "security" });
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["AccountSettingsError"] = "Passwords do not match.";
                return RedirectToAction("Index", new { tab = "security" });
            }

            if (model.NewPassword == model.CurrentPassword)
            {
                TempData["AccountSettingsError"] = "New password must be different from current password.";
                return RedirectToAction("Index", new { tab = "security" });
            }

            var passwordResult = PasswordHashService.VerifyPassword(model.CurrentPassword, user.password_hash);

            if (!passwordResult.Success)
            {
                TempData["AccountSettingsError"] = "Current password is incorrect.";
                return RedirectToAction("Index", new { tab = "security" });
            }

            user.password_hash = PasswordHashService.HashPassword(model.NewPassword);
            AccountLogService.LogActivity(data, user.user_id, "ChangePassword", "Password changed successfully.", Request);
            data.SubmitChanges();

            TempData["AccountSettingsMessage"] = "Password changed successfully.";
            return RedirectToAction("Index", new { tab = "security" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateNotifications([Bind(Prefix = "Settings")] UserSettingsFormViewModel model)
        {
            var user = GetCurrentUser();

            if (user == null)
                return new HttpStatusCodeResult(403, "You are not allowed to access this page.");

            var settings = EnsureSettings(user.user_id);
            settings.app_notifications_enabled = model.AppNotificationsEnabled;
            settings.job_updates_enabled = model.JobUpdatesEnabled;
            settings.interview_notifications_enabled = model.InterviewNotificationsEnabled;
            settings.invitation_notifications_enabled = model.InvitationNotificationsEnabled;
            settings.updated_at = DateTime.Now;

            AccountLogService.LogActivity(data, user.user_id, "UpdateSettings", "Notification settings updated.", Request);
            data.SubmitChanges();

            TempData["AccountSettingsMessage"] = "Settings updated successfully.";
            return RedirectToAction("Index", new { tab = "notifications" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePrivacy([Bind(Prefix = "Settings")] UserSettingsFormViewModel model)
        {
            var user = GetCurrentUser();

            if (user == null)
                return new HttpStatusCodeResult(403, "You are not allowed to access this page.");

            var settings = EnsureSettings(user.user_id);
            settings.public_profile_enabled = model.PublicProfileEnabled;
            settings.updated_at = DateTime.Now;

            AccountLogService.LogActivity(data, user.user_id, "UpdateSettings", "Privacy settings updated.", Request);
            data.SubmitChanges();

            TempData["AccountSettingsMessage"] = "Settings updated successfully.";
            return RedirectToAction("Index", new { tab = "privacy" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCvSettings([Bind(Prefix = "Settings")] UserSettingsFormViewModel model)
        {
            var user = GetCurrentUser();

            if (user == null)
                return new HttpStatusCodeResult(403, "You are not allowed to access this page.");

            if (!AuthRoleHelper.IsCandidate(User.Identity.Name))
            {
                TempData["AccountSettingsError"] = "CV settings are only available for candidates.";
                return RedirectToAction("Index", new { tab = "privacy" });
            }

            if (model.DefaultCvId.HasValue && !data.UserCVs.Any(cv => cv.cv_id == model.DefaultCvId.Value && cv.user_id == user.user_id))
            {
                TempData["AccountSettingsError"] = "Invalid CV selection.";
                return RedirectToAction("Index", new { tab = "privacy" });
            }

            var settings = EnsureSettings(user.user_id);
            settings.show_cv_to_employers = model.ShowCvToEmployers;
            settings.default_cv_id = model.DefaultCvId;
            settings.updated_at = DateTime.Now;

            if (model.DefaultCvId.HasValue)
            {
                foreach (var cv in data.UserCVs.Where(cv => cv.user_id == user.user_id))
                    cv.is_default = cv.cv_id == model.DefaultCvId.Value;
            }

            AccountLogService.LogActivity(data, user.user_id, "UpdateSettings", "CV settings updated.", Request);
            data.SubmitChanges();

            TempData["AccountSettingsMessage"] = "CV settings updated successfully.";
            return RedirectToAction("Index", new { tab = "privacy" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteLoginLog(int id, int loginPage = 1)
        {
            var user = GetCurrentUser();

            if (user == null)
                return new HttpStatusCodeResult(403, "You are not allowed to access this page.");

            var log = data.UserLoginLogs.FirstOrDefault(item => item.login_log_id == id);

            if (log == null || log.user_id != user.user_id)
            {
                TempData["AccountSettingsError"] = "You are not allowed to delete this log.";
                return RedirectToAction("Index", new { tab = "login-logs", loginPage = loginPage });
            }

            data.UserLoginLogs.DeleteOnSubmit(log);
            AccountLogService.LogActivity(data, user.user_id, "DeleteLog", "Deleted a login log.", Request);
            data.SubmitChanges();

            TempData["AccountSettingsMessage"] = "Log deleted successfully.";
            return RedirectToAction("Index", new { tab = "login-logs", loginPage = loginPage });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAllLoginLogs(int loginPage = 1)
        {
            var user = GetCurrentUser();

            if (user == null)
                return new HttpStatusCodeResult(403, "You are not allowed to access this page.");

            var logs = data.UserLoginLogs.Where(item => item.user_id == user.user_id);
            data.UserLoginLogs.DeleteAllOnSubmit(logs);
            AccountLogService.LogActivity(data, user.user_id, "DeleteLog", "Deleted all login logs.", Request);
            data.SubmitChanges();

            TempData["AccountSettingsMessage"] = "All logs deleted successfully.";
            return RedirectToAction("Index", new { tab = "login-logs", loginPage = loginPage });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteActivityLog(int id, int activityPage = 1, string activityAction = "")
        {
            var user = GetCurrentUser();

            if (user == null)
                return new HttpStatusCodeResult(403, "You are not allowed to access this page.");

            var log = data.UserActivityLogs.FirstOrDefault(item => item.activity_log_id == id);

            if (log == null || log.user_id != user.user_id)
            {
                TempData["AccountSettingsError"] = "You are not allowed to delete this log.";
                return RedirectToAction("Index", new { tab = "activity-logs", activityPage = activityPage, activityAction = activityAction });
            }

            data.UserActivityLogs.DeleteOnSubmit(log);
            data.SubmitChanges();

            TempData["AccountSettingsMessage"] = "Log deleted successfully.";
            return RedirectToAction("Index", new { tab = "activity-logs", activityPage = activityPage, activityAction = activityAction });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAllActivityLogs(int activityPage = 1, string activityAction = "")
        {
            var user = GetCurrentUser();

            if (user == null)
                return new HttpStatusCodeResult(403, "You are not allowed to access this page.");

            var logs = data.UserActivityLogs.Where(item => item.user_id == user.user_id);
            data.UserActivityLogs.DeleteAllOnSubmit(logs);
            data.SubmitChanges();

            TempData["AccountSettingsMessage"] = "All logs deleted successfully.";
            return RedirectToAction("Index", new { tab = "activity-logs", activityPage = activityPage, activityAction = activityAction });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private AccountSettingsViewModel BuildModel(User user, UserSetting settings, string tab, int loginPage, int activityPage, string activityAction)
        {
            loginPage = Math.Max(1, loginPage);
            activityPage = Math.Max(1, activityPage);

            var loginQuery = data.UserLoginLogs
                .Where(log => log.user_id == user.user_id)
                .OrderByDescending(log => log.login_time);

            var activityQuery = data.UserActivityLogs
                .Where(log => log.user_id == user.user_id);

            if (!String.IsNullOrWhiteSpace(activityAction))
                activityQuery = activityQuery.Where(log => log.action == activityAction);

            var orderedActivityQuery = activityQuery.OrderByDescending(log => log.created_at);
            var pageSize = SystemSettingsService.GetPaginationSize(data);
            var loginCount = loginQuery.Count();
            var activityCount = orderedActivityQuery.Count();

            return new AccountSettingsViewModel
            {
                ActiveTab = NormalizeTab(tab),
                UserId = user.user_id,
                Username = user.username,
                Email = user.email,
                RoleName = GetRoleName(user.user_id),
                CreatedAt = GetCreatedAtIfExists(user.user_id),
                IsCandidate = AuthRoleHelper.IsCandidate(User.Identity.Name),
                Settings = new UserSettingsFormViewModel
                {
                    AppNotificationsEnabled = settings.app_notifications_enabled,
                    JobUpdatesEnabled = settings.job_updates_enabled,
                    InterviewNotificationsEnabled = settings.interview_notifications_enabled,
                    InvitationNotificationsEnabled = settings.invitation_notifications_enabled,
                    PublicProfileEnabled = settings.public_profile_enabled,
                    ShowCvToEmployers = settings.show_cv_to_employers,
                    DefaultCvId = settings.default_cv_id
                },
                Password = new ChangePasswordViewModel(),
                CvOptions = GetCvOptions(user.user_id, settings.default_cv_id),
                LoginPage = loginPage,
                LoginTotalPages = Math.Max(1, (int)Math.Ceiling(loginCount / (double)pageSize)),
                LoginLogs = loginQuery.Skip((loginPage - 1) * pageSize).Take(pageSize).ToList().Select(ToLoginLogItem).ToList(),
                ActivityPage = activityPage,
                ActivityTotalPages = Math.Max(1, (int)Math.Ceiling(activityCount / (double)pageSize)),
                ActivityActionFilter = activityAction,
                ActivityActions = GetActivityActions(activityAction),
                ActivityLogs = orderedActivityQuery.Skip((activityPage - 1) * pageSize).Take(pageSize).ToList().Select(ToActivityLogItem).ToList()
            };
        }

        private UserSetting EnsureSettings(int userId)
        {
            var settings = data.UserSettings.FirstOrDefault(item => item.user_id == userId);

            if (settings != null)
                return settings;

            settings = new UserSetting
            {
                user_id = userId,
                app_notifications_enabled = true,
                job_updates_enabled = true,
                interview_notifications_enabled = true,
                invitation_notifications_enabled = true,
                public_profile_enabled = true,
                show_cv_to_employers = false
            };

            data.UserSettings.InsertOnSubmit(settings);
            data.SubmitChanges();

            return settings;
        }

        private List<SelectListItem> GetCvOptions(int userId, int? selectedCvId)
        {
            return data.UserCVs
                .Where(cv => cv.user_id == userId)
                .OrderByDescending(cv => cv.is_default)
                .ThenByDescending(cv => cv.created_at)
                .ToList()
                .Select(cv => new SelectListItem
                {
                    Value = cv.cv_id.ToString(),
                    Text = cv.file_name,
                    Selected = selectedCvId.HasValue && cv.cv_id == selectedCvId.Value
                })
                .ToList();
        }

        private List<SelectListItem> GetActivityActions(string selectedAction)
        {
            var items = data.UserActivityLogs
                .Where(log => log.user_id == GetCurrentUser().user_id)
                .Select(log => log.action)
                .Distinct()
                .OrderBy(action => action)
                .ToList()
                .Select(action => new SelectListItem
                {
                    Text = action,
                    Value = action,
                    Selected = action == selectedAction
                })
                .ToList();

            items.Insert(0, new SelectListItem { Text = "All", Value = "", Selected = String.IsNullOrWhiteSpace(selectedAction) });
            return items;
        }

        private string GetRoleName(int userId)
        {
            return data.UserRoles
                .Where(userRole => userRole.user_id == userId)
                .Select(userRole => userRole.Role.role_name)
                .FirstOrDefault();
        }

        private DateTime? GetCreatedAtIfExists(int userId)
        {
            if (data.ExecuteQuery<int>("SELECT CASE WHEN COL_LENGTH('dbo.Users', 'created_at') IS NULL THEN 0 ELSE 1 END").First() == 0)
                return null;

            return data.ExecuteQuery<DateTime?>("SELECT created_at FROM dbo.Users WHERE user_id = {0}", userId).FirstOrDefault();
        }

        private LoginLogItemViewModel ToLoginLogItem(UserLoginLog log)
        {
            return new LoginLogItemViewModel
            {
                LoginLogId = log.login_log_id,
                LoginTime = log.login_time,
                IpAddress = log.ip_address,
                UserAgent = log.user_agent,
                IsSuccess = log.is_success,
                FailureReason = log.failure_reason
            };
        }

        private ActivityLogItemViewModel ToActivityLogItem(UserActivityLog log)
        {
            return new ActivityLogItemViewModel
            {
                ActivityLogId = log.activity_log_id,
                Action = log.action,
                Description = log.description,
                Keyword = log.keyword,
                Filters = log.filters,
                RelatedId = log.related_id,
                RelatedType = log.related_type,
                IpAddress = log.ip_address,
                UserAgent = log.user_agent,
                CreatedAt = log.created_at
            };
        }

        private string NormalizeTab(string tab)
        {
            tab = (tab ?? "account").ToLowerInvariant();

            if (tab == "security" || tab == "notifications" || tab == "privacy" || tab == "login-logs" || tab == "activity-logs")
                return tab;

            return "account";
        }

        private User GetCurrentUser()
        {
            return data.Users.FirstOrDefault(user => user.username == User.Identity.Name);
        }

    }
}
