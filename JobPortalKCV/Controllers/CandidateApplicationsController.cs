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
    public class CandidateApplicationsController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        public ActionResult Index(string filter = "all", int page = 1)
        {
            if (!CanViewCandidateApplications())
                return new HttpStatusCodeResult(403, "You are not allowed to view this application.");

            var currentUser = GetCurrentUser();
            filter = NormalizeFilter(filter);

            var applications = BuildApplications(currentUser)
                .Where(item => filter == "all" || item.Status.Equals(filter, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.AppliedDate ?? DateTime.MinValue)
                .ToList();

            applications = PaginationService.Paginate(
                applications,
                page,
                SystemSettingsService.GetPaginationSize(data),
                out PaginationViewModel pagination,
                "Index",
                "CandidateApplications",
                new { filter });
            ViewBag.Pagination = pagination;

            return View(new CandidateApplicationsIndexViewModel
            {
                Filter = filter,
                Applications = applications
            });
        }

        public ActionResult InterviewDetails(int id)
        {
            if (!CanViewCandidateApplications())
                return new HttpStatusCodeResult(403, "You are not allowed to view this application.");

            var currentUser = GetCurrentUser();
            var application = BuildApplications(currentUser).FirstOrDefault(item => item.ApplicationId == id);

            if (application == null)
                return new HttpStatusCodeResult(403, "You are not allowed to view this application.");

            if (application.Interview == null)
            {
                TempData["CandidateApplicationError"] = "Something went wrong. Please try again.";
                return RedirectToAction("Index");
            }

            AccountLogService.LogActivity(data, currentUser.user_id, "ViewDetail", "Viewed interview detail.", Request, relatedId: application.Interview.InterviewId, relatedType: "Interview");
            data.SubmitChanges();

            return View(application.Interview);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private List<CandidateApplicationItemViewModel> BuildApplications(User currentUser)
        {
            var query = from application in data.JobApplications
                        join job in data.Jobs on application.job_id equals job.job_id
                        join company in data.Companies on job.company_id equals company.company_id
                        join cv in data.UserCVs on application.cv_id equals cv.cv_id into cvJoin
                        from cv in cvJoin.DefaultIfEmpty()
                        join legacyStatus in data.ApplicationStatuses on application.status_id equals legacyStatus.status_id into statusJoin
                        from legacyStatus in statusJoin.DefaultIfEmpty()
                        select new
                        {
                            Application = application,
                            Job = job,
                            Company = company,
                            CV = cv,
                            LegacyStatus = legacyStatus
                        };

            if (!AuthRoleHelper.IsAdmin(User.Identity.Name))
                query = query.Where(item => item.Application.user_id == currentUser.user_id);

            var rows = query.ToList();

            return rows.Select(row =>
            {
                var status = NormalizeStatus(row.Application.status, row.LegacyStatus == null ? null : row.LegacyStatus.status_name);
                var interview = data.Interviews
                    .Where(item => item.application_id == row.Application.application_id)
                    .OrderByDescending(item => item.interview_date)
                    .FirstOrDefault();

                return new CandidateApplicationItemViewModel
                {
                    ApplicationId = row.Application.application_id,
                    JobId = row.Job.job_id,
                    JobTitle = row.Job.job_title,
                    CompanyId = row.Company.company_id,
                    CompanyName = row.Company.company_name,
                    CompanyLogoPath = SystemSettingsService.GetCompanyLogoOrDefault(data, row.Company.logo_path),
                    CvFileName = row.CV == null ? null : row.CV.file_name,
                    CvFilePath = row.CV == null ? null : row.CV.file_path,
                    CoverLetter = row.Application.cover_letter,
                    AppliedDate = row.Application.applied_date ?? row.Application.application_date,
                    Status = status,
                    FinalResult = row.Application.final_result,
                    Interview = interview == null ? null : new CandidateApplicationInterviewViewModel
                    {
                        InterviewId = interview.interview_id,
                        ApplicationId = row.Application.application_id,
                        JobTitle = row.Job.job_title,
                        CompanyName = row.Company.company_name,
                        InterviewDate = interview.interview_date,
                        Location = interview.location,
                        ContactName = interview.contact_name,
                        ContactEmail = interview.contact_email,
                        ContactPhone = interview.contact_phone,
                        AdditionalInfo = interview.additional_info
                    }
                };
            }).ToList();
        }

        private bool CanViewCandidateApplications()
        {
            return AuthRoleHelper.IsCandidate(User.Identity.Name) || AuthRoleHelper.IsAdmin(User.Identity.Name);
        }

        private User GetCurrentUser()
        {
            return data.Users.FirstOrDefault(user => user.username == User.Identity.Name);
        }

        private string NormalizeFilter(string filter)
        {
            filter = (filter ?? "all").ToLower();
            return filter == "pending" || filter == "interview" || filter == "completed" ? filter : "all";
        }

        private string NormalizeStatus(string status, string legacyStatus)
        {
            var value = String.IsNullOrWhiteSpace(status) ? legacyStatus : status;

            if (String.IsNullOrWhiteSpace(value))
                return "Pending";

            if (value.Equals("Interview", StringComparison.OrdinalIgnoreCase))
                return "Interview";

            if (value.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                return "Completed";

            return "Pending";
        }
    }
}
