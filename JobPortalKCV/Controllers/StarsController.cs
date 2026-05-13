using System;
using System.Linq;
using System.Web.Mvc;
using JobPortalKCV.Helpers;
using JobPortalKCV.Models;
using JobPortalKCV.Models.ViewModel;
using JobPortalKCV.Services;

namespace JobPortalKCV.Controllers
{
    public class StarsController : Controller
    {
        private const string UserTarget = "User";
        private const string JobTarget = "Job";
        private const string CompanyTarget = "Company";

        private readonly JobPortalDataContext data = new JobPortalDataContext();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Toggle(int targetId, string targetType, string returnUrl)
        {
            var isAjax = Request.IsAjaxRequest();

            if (!Request.IsAuthenticated)
            {
                TempData["StarError"] = "Please log in to use this feature.";
                if (isAjax)
                {
                    Response.StatusCode = 401;
                    return Json(new { success = false, message = "Please log in to use this feature.", loginUrl = Url.Action("Login", "Account", new { returnUrl }) });
                }

                return RedirectToLocal(returnUrl);
            }

            var currentUser = GetCurrentUser();

            if (currentUser == null)
            {
                TempData["StarError"] = "Please log in to use this feature.";
                if (isAjax)
                {
                    Response.StatusCode = 401;
                    return Json(new { success = false, message = "Please log in to use this feature.", loginUrl = Url.Action("Login", "Account", new { returnUrl }) });
                }

                return RedirectToLocal(returnUrl);
            }

            targetType = NormalizeTargetType(targetType);

            if (targetType == null || !TargetExists(targetId, targetType))
            {
                TempData["StarError"] = "Invalid target.";
                if (isAjax)
                {
                    Response.StatusCode = 400;
                    return Json(new { success = false, message = "Invalid target." });
                }

                return RedirectToLocal(returnUrl);
            }

            if (targetType == UserTarget && targetId == currentUser.user_id)
            {
                TempData["StarError"] = "You cannot star yourself.";
                if (isAjax)
                {
                    Response.StatusCode = 400;
                    return Json(new { success = false, message = "You cannot star yourself." });
                }

                return RedirectToLocal(returnUrl);
            }

            try
            {
                var existing = data.Stars.FirstOrDefault(star =>
                    star.user_id == currentUser.user_id &&
                    star.target_id == targetId &&
                    star.target_type == targetType);

                if (existing == null)
                {
                    data.Stars.InsertOnSubmit(new Star
                    {
                        user_id = currentUser.user_id,
                        target_id = targetId,
                        target_type = targetType
                    });

                    AccountLogService.LogActivity(data, currentUser.user_id, "Star", "Added to favorites.", Request, relatedId: targetId, relatedType: targetType);
                    TempData["StarMessage"] = "Added to favorites.";
                }
                else
                {
                    data.Stars.DeleteOnSubmit(existing);
                    AccountLogService.LogActivity(data, currentUser.user_id, "Unstar", "Removed from favorites.", Request, relatedId: targetId, relatedType: targetType);
                    TempData["StarMessage"] = "Removed from favorites.";
                }

                data.SubmitChanges();

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        isStarred = existing == null,
                        starCount = data.Stars.Count(star => star.target_id == targetId && star.target_type == targetType),
                        message = existing == null ? "Added to favorites." : "Removed from favorites."
                    });
                }
            }
            catch
            {
                TempData["StarError"] = "Something went wrong. Please try again.";
                if (isAjax)
                {
                    Response.StatusCode = 500;
                    return Json(new { success = false, message = "Something went wrong. Please try again." });
                }
            }

            return RedirectToLocal(returnUrl);
        }

        [Authorize]
        public ActionResult Saved(int page = 1)
        {
            var currentUser = GetCurrentUser();

            if (currentUser == null)
                return new HttpStatusCodeResult(403);

            var userTargetIds = data.Stars
                .Where(star => star.user_id == currentUser.user_id && star.target_type == UserTarget)
                .OrderByDescending(star => star.created_at)
                .Select(star => star.target_id)
                .ToList();

            var jobTargetIds = data.Stars
                .Where(star => star.user_id == currentUser.user_id && star.target_type == JobTarget)
                .OrderByDescending(star => star.created_at)
                .Select(star => star.target_id)
                .ToList();

            var companyTargetIds = data.Stars
                .Where(star => star.user_id == currentUser.user_id && star.target_type == CompanyTarget)
                .OrderByDescending(star => star.created_at)
                .Select(star => star.target_id)
                .ToList();

            var savedUsers = data.Users
                .Where(user => userTargetIds.Contains(user.user_id))
                .ToList()
                .OrderBy(user => userTargetIds.IndexOf(user.user_id))
                .Select(user => new SavedUserViewModel
                {
                    UserId = user.user_id,
                    DisplayName = GetUserDisplayName(user),
                    Email = user.email,
                    StarCount = data.Stars.Count(item => item.target_type == UserTarget && item.target_id == user.user_id)
                }).ToList();

            var savedJobs = (from job in data.Jobs
                             join company in data.Companies on job.company_id equals company.company_id
                             where jobTargetIds.Contains(job.job_id)
                             select new { Job = job, Company = company })
                .ToList()
                .OrderBy(item => jobTargetIds.IndexOf(item.Job.job_id))
                .Select(item => new SavedJobViewModel
                {
                    JobId = item.Job.job_id,
                    JobTitle = item.Job.job_title,
                    CompanyName = item.Company.company_name,
                    StarCount = data.Stars.Count(star => star.target_type == JobTarget && star.target_id == item.Job.job_id)
                }).ToList();

            var savedCompanies = data.Companies
                .Where(company => companyTargetIds.Contains(company.company_id))
                .ToList()
                .OrderBy(company => companyTargetIds.IndexOf(company.company_id))
                .Select(company => new SavedCompanyViewModel
                {
                    CompanyId = company.company_id,
                    CompanyName = company.company_name,
                    Industry = company.industry,
                    StarCount = data.Stars.Count(star => star.target_type == CompanyTarget && star.target_id == company.company_id)
                }).ToList();

            ApplySavedPagination(ref savedUsers, ref savedJobs, ref savedCompanies, page);

            return View(new SavedItemsViewModel
            {
                Users = savedUsers,
                Jobs = savedJobs,
                Companies = savedCompanies
            });
        }

        [Authorize]
        public ActionResult StarredBy(int targetId, string targetType, int page = 1)
        {
            targetType = NormalizeTargetType(targetType);

            if (targetType == null || !TargetExists(targetId, targetType))
                return new HttpStatusCodeResult(404);

            if (!CanViewStarredBy(targetId, targetType))
            {
                TempData["StarError"] = "You are not allowed to view this information.";
                return RedirectToAction("Saved");
            }

            var users = (from star in data.Stars
                         join user in data.Users on star.user_id equals user.user_id
                         where star.target_id == targetId && star.target_type == targetType
                         orderby star.created_at descending
                         select new StarUserViewModel
                         {
                             UserId = user.user_id,
                             DisplayName = GetUserDisplayName(user),
                             Email = user.email,
                             CreatedAt = star.created_at
                         }).ToList();

            users = PaginationService.Paginate(
                users,
                page,
                SystemSettingsService.GetPaginationSize(data),
                out PaginationViewModel pagination,
                "StarredBy",
                "Stars",
                new { targetId, targetType });
            ViewBag.Pagination = pagination;

            return View(new StarredByViewModel
            {
                TargetId = targetId,
                TargetType = targetType,
                TargetName = GetTargetName(targetId, targetType),
                Users = users
            });
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

        private string NormalizeTargetType(string targetType)
        {
            if (String.Equals(targetType, UserTarget, StringComparison.OrdinalIgnoreCase))
                return UserTarget;

            if (String.Equals(targetType, JobTarget, StringComparison.OrdinalIgnoreCase))
                return JobTarget;

            if (String.Equals(targetType, CompanyTarget, StringComparison.OrdinalIgnoreCase))
                return CompanyTarget;

            return null;
        }

        private bool TargetExists(int targetId, string targetType)
        {
            if (targetType == UserTarget)
                return data.Users.Any(user => user.user_id == targetId);

            if (targetType == JobTarget)
                return data.Jobs.Any(job => job.job_id == targetId);

            if (targetType == CompanyTarget)
                return data.Companies.Any(company => company.company_id == targetId);

            return false;
        }

        private void ApplySavedPagination(ref System.Collections.Generic.List<SavedUserViewModel> users, ref System.Collections.Generic.List<SavedJobViewModel> jobs, ref System.Collections.Generic.List<SavedCompanyViewModel> companies, int page)
        {
            var pageSize = SystemSettingsService.GetPaginationSize(data);
            var userCount = users.Count;
            var jobCount = jobs.Count;
            var companyCount = companies.Count;
            var totalRecords = Math.Max(userCount, Math.Max(jobCount, companyCount));

            pageSize = Math.Max(1, pageSize);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
            page = Math.Max(1, Math.Min(page, totalPages));

            users = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            jobs = jobs.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            companies = companies.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Pagination = new PaginationViewModel
            {
                Page = page,
                TotalPages = totalPages,
                TotalRecords = userCount + jobCount + companyCount,
                ActionName = "Saved",
                ControllerName = "Stars"
            };
        }

        private bool CanViewStarredBy(int targetId, string targetType)
        {
            if (AuthRoleHelper.IsAdmin(User.Identity.Name))
                return true;

            var currentUser = GetCurrentUser();

            if (currentUser == null)
                return false;

            if (targetType == UserTarget)
                return currentUser.user_id == targetId;

            if (targetType == JobTarget)
            {
                var companyId = data.Jobs
                    .Where(job => job.job_id == targetId)
                    .Select(job => job.company_id)
                    .FirstOrDefault();

                return AuthRoleHelper.CanManageCompany(User.Identity.Name, companyId);
            }

            if (targetType == CompanyTarget)
                return AuthRoleHelper.CanManageCompany(User.Identity.Name, targetId);

            return false;
        }

        private string GetTargetName(int targetId, string targetType)
        {
            if (targetType == UserTarget)
            {
                var user = data.Users.FirstOrDefault(item => item.user_id == targetId);
                return user == null ? "" : GetUserDisplayName(user);
            }

            if (targetType == JobTarget)
                return data.Jobs.Where(job => job.job_id == targetId).Select(job => job.job_title).FirstOrDefault();

            if (targetType == CompanyTarget)
                return data.Companies.Where(company => company.company_id == targetId).Select(company => company.company_name).FirstOrDefault();

            return "";
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (!String.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        private string FirstNotEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !String.IsNullOrWhiteSpace(value));
        }

        private string GetUserDisplayName(User user)
        {
            if (user == null)
                return "";

            var profileName = data.UserProfileRecords
                .Where(profile => profile.user_id == user.user_id)
                .Select(profile => profile.full_name)
                .FirstOrDefault();

            return FirstNotEmpty(profileName, user.username);
        }
    }
}
