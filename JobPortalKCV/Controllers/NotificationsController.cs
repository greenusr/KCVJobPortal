using System;
using System.Linq;
using System.Web.Mvc;
using JobPortalKCV.Helpers;
using JobPortalKCV.Models;
using JobPortalKCV.Models.ViewModel;
using JobPortalKCV.Services;

namespace JobPortalKCV.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        [ChildActionOnly]
        public ActionResult Dropdown()
        {
            var user = GetCurrentUser();

            if (user == null)
                return PartialView("_Dropdown", new NotificationDropdownViewModel());

            var notifications = data.Notifications
                .Where(notification => notification.user_id == user.user_id)
                .OrderByDescending(notification => notification.created_at)
                .Take(10)
                .ToList()
                .Select(ToListItem)
                .ToList();

            return PartialView("_Dropdown", new NotificationDropdownViewModel
            {
                UnreadCount = data.Notifications.Count(notification => notification.user_id == user.user_id && !notification.is_read),
                Notifications = notifications
            });
        }

        public ActionResult Index(string filter = "all", int page = 1)
        {
            var user = GetCurrentUser();

            if (user == null)
                return new HttpStatusCodeResult(403, "You are not allowed to view these notifications.");

            page = Math.Max(1, page);
            filter = NormalizeFilter(filter);
            var pageSize = SystemSettingsService.GetPaginationSize(data);
            var query = data.Notifications.Where(notification => notification.user_id == user.user_id);

            if (filter == "unread")
                query = query.Where(notification => !notification.is_read);
            else if (filter == "read")
                query = query.Where(notification => notification.is_read);

            var total = query.Count();
            var notifications = query
                .OrderByDescending(notification => notification.created_at)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(ToListItem)
                .ToList();

            return View(new NotificationIndexViewModel
            {
                Filter = filter,
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                Notifications = notifications
            });
        }

        public ActionResult Open(int id, string returnUrl = null)
        {
            var user = GetCurrentUser();
            var notification = user == null
                ? null
                : data.Notifications.FirstOrDefault(item => item.notification_id == id && item.user_id == user.user_id);

            if (notification == null)
                return new HttpStatusCodeResult(403, "You are not allowed to view these notifications.");

            if (!notification.is_read)
            {
                notification.is_read = true;
                data.SubmitChanges();
                TempData["NotificationMessage"] = "Notification marked as read.";
            }

            return Redirect(ResolveNotificationUrl(notification, returnUrl));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAllAsRead(string filter = "all", int page = 1)
        {
            var user = GetCurrentUser();

            if (user == null)
                return new HttpStatusCodeResult(403, "You are not allowed to view these notifications.");

            var unread = data.Notifications.Where(notification => notification.user_id == user.user_id && !notification.is_read).ToList();

            foreach (var notification in unread)
                notification.is_read = true;

            data.SubmitChanges();
            TempData["NotificationMessage"] = "All notifications marked as read.";
            return RedirectToAction("Index", new { filter = filter, page = page });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private string ResolveNotificationUrl(Notification notification, string returnUrl)
        {
            returnUrl = !String.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action("Index", "Notifications");

            var relatedType = !String.IsNullOrWhiteSpace(notification.related_type)
                ? notification.related_type
                : notification.type;

            if (!notification.related_id.HasValue || String.IsNullOrWhiteSpace(relatedType))
                return returnUrl;

            switch (relatedType.ToLowerInvariant())
            {
                case "job":
                    return Url.Action("Details", "Jobs", new { id = notification.related_id.Value, returnUrl = returnUrl });
                case "invitation":
                    return Url.Action("Index", "CandidateInvitations");
                case "application":
                    return ResolveApplicationUrl(notification.related_id.Value, returnUrl);
                case "interview":
                    return ResolveInterviewUrl(notification.related_id.Value, returnUrl);
                case "companyjoinrequest":
                    return ResolveCompanyJoinRequestUrl(notification.related_id.Value, returnUrl);
                default:
                    return returnUrl;
            }
        }

        private string ResolveApplicationUrl(int applicationId, string returnUrl)
        {
            var application = data.JobApplications.FirstOrDefault(item => item.application_id == applicationId);

            if (application == null)
                return returnUrl;

            var job = data.Jobs.FirstOrDefault(item => item.job_id == application.job_id);

            if (AuthRoleHelper.IsCandidate(User.Identity.Name) && IsCurrentUser(application.user_id))
                return Url.Action("Details", "Jobs", new { id = application.job_id, returnUrl = returnUrl });

            if (AuthRoleHelper.IsAdmin(User.Identity.Name) ||
                (job != null && AuthRoleHelper.CanManageCompany(User.Identity.Name, job.company_id)))
                return Url.Action("Index", "Applications", new { applicationId = application.application_id }) + "#application-" + application.application_id;

            return returnUrl;
        }

        private string ResolveInterviewUrl(int interviewId, string returnUrl)
        {
            var interview = data.Interviews.FirstOrDefault(item => item.interview_id == interviewId);

            if (interview == null)
                return returnUrl;

            return ResolveApplicationUrl(interview.application_id, returnUrl);
        }

        private string ResolveCompanyJoinRequestUrl(int requestId, string returnUrl)
        {
            var request = data.CompanyJoinRequests.FirstOrDefault(item => item.request_id == requestId);

            if (request == null)
                return returnUrl;

            if (IsCurrentUser(request.user_id))
                return Url.Action("Details", "Companies", new { id = request.company_id, returnUrl = returnUrl }) + "#join-request-status";

            return Url.Action("Details", "Companies", new { id = request.company_id, returnUrl = returnUrl }) + "#join-request-" + request.request_id;
        }

        private bool IsCurrentUser(int? userId)
        {
            var user = GetCurrentUser();
            return user != null && userId.HasValue && user.user_id == userId.Value;
        }

        private string NormalizeFilter(string filter)
        {
            filter = (filter ?? "all").ToLower();
            return filter == "read" || filter == "unread" ? filter : "all";
        }

        private NotificationListItemViewModel ToListItem(Notification notification)
        {
            return new NotificationListItemViewModel
            {
                NotificationId = notification.notification_id,
                Title = notification.title,
                Message = notification.message,
                Type = notification.type,
                RelatedId = notification.related_id,
                RelatedType = notification.related_type,
                IsRead = notification.is_read,
                CreatedAt = notification.created_at
            };
        }

        private User GetCurrentUser()
        {
            return data.Users.FirstOrDefault(user => user.username == User.Identity.Name);
        }
    }
}
