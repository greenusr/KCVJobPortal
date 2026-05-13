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
    public class CandidateInvitationsController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        public ActionResult Index(int page = 1)
        {
            if (!AuthRoleHelper.IsCandidate(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var candidate = GetCurrentUser();

            if (candidate == null)
                return HttpNotFound();

            var invitations = BuildInvitationsForCandidate(candidate.user_id);
            invitations = PaginationService.Paginate(
                invitations,
                page,
                SystemSettingsService.GetPaginationSize(data),
                out PaginationViewModel pagination,
                "Index",
                "CandidateInvitations");
            ViewBag.Pagination = pagination;

            return View(invitations);
        }

        public ActionResult Sent(string filter = "all", int page = 1)
        {
            if (!AuthRoleHelper.IsEmployer(User.Identity.Name) && !AuthRoleHelper.IsAdmin(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var employer = GetCurrentUser();

            if (employer == null)
                return HttpNotFound();

            var normalizedFilter = NormalizeSentFilter(filter);
            var invitations = BuildSentInvitations(employer.user_id, normalizedFilter);
            invitations = PaginationService.Paginate(
                invitations,
                page,
                SystemSettingsService.GetPaginationSize(data),
                out PaginationViewModel pagination,
                "Sent",
                "CandidateInvitations",
                new { filter = normalizedFilter });

            ViewBag.Filter = normalizedFilter;
            ViewBag.Pagination = pagination;
            return View(invitations);
        }

        public ActionResult Details(int id)
        {
            var invitation = GetInvitationForCurrentEmployer(id);

            if (invitation == null)
                return new HttpStatusCodeResult(403, "You are not allowed to view this invitation.");

            var viewer = GetCurrentUser();
            if (viewer != null)
            {
                AccountLogService.LogActivity(data, viewer.user_id, "ViewDetail", "Viewed invitation detail.", Request, relatedId: invitation.Invitation.invitation_id, relatedType: "Invitation");
                data.SubmitChanges();
            }

            return View(ToListItem(invitation));
        }

        public ActionResult Invite(int candidateId)
        {
            if (!AuthRoleHelper.IsEmployer(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var candidate = data.Users.FirstOrDefault(user => user.user_id == candidateId);

            if (candidate == null || !IsUserInRole(candidate.user_id, "Candidate"))
                return HttpNotFound();

            var jobs = GetInvitableJobs(null);

            if (!jobs.Any())
            {
                TempData["InvitationError"] = "You are not allowed to invite candidates to this job.";
                return RedirectToAction("View", "Profile", new { id = candidateId });
            }

            return View(BuildInviteModel(candidate, null, null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Invite(CandidateInvitationInviteViewModel model)
        {
            if (!AuthRoleHelper.IsEmployer(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var employer = GetCurrentUser();
            var candidate = data.Users.FirstOrDefault(user => user.user_id == model.CandidateId);
            var job = data.Jobs.FirstOrDefault(item => item.job_id == model.JobId);

            if (employer == null || candidate == null || job == null || !IsUserInRole(candidate.user_id, "Candidate"))
                return HttpNotFound();

            if (!CanInviteToJob(job))
            {
                TempData["InvitationError"] = "You are not allowed to invite candidates to this job.";
                return View(BuildInviteModel(candidate, model.JobId, model.Message));
            }

            if (data.CandidateInvitations.Any(invitation =>
                invitation.candidate_id == candidate.user_id &&
                invitation.job_id == job.job_id &&
                invitation.status == "Pending"))
            {
                TempData["InvitationError"] = "You have already invited this candidate to this job.";
                return View(BuildInviteModel(candidate, model.JobId, model.Message));
            }

            if (!ModelState.IsValid)
                return View(BuildInviteModel(candidate, model.JobId, model.Message));

            var candidateInvitation = new CandidateInvitation
            {
                employer_id = employer.user_id,
                candidate_id = candidate.user_id,
                job_id = job.job_id,
                message = model.Message,
                status = "Pending"
            };

            data.CandidateInvitations.InsertOnSubmit(candidateInvitation);
            data.SubmitChanges();
            NotificationService.NotifyCandidateInvited(data, candidateInvitation);
            data.SubmitChanges();
            TrySendInvitationEmail(candidate, employer, job, model.Message);

            TempData["InvitationMessage"] = "Invitation sent successfully.";
            return RedirectToAction("View", "Profile", new { id = candidate.user_id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Accept(int id, string returnUrl = null)
        {
            var invitation = GetPendingInvitationForCurrentCandidate(id);

            if (invitation == null)
                return new HttpStatusCodeResult(403);

            invitation.status = "Accepted";
            invitation.responded_at = DateTime.Now;
            AccountLogService.LogActivity(data, invitation.candidate_id, "AcceptInvitation", "Invitation accepted.", Request, relatedId: invitation.invitation_id, relatedType: "Invitation");
            data.SubmitChanges();

            TempData["JobMessage"] = "Invitation accepted.";
            return RedirectToAction("Details", "Jobs", new { id = invitation.job_id, returnUrl = SafeReturnUrl(returnUrl, Url.Action("Index")) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Decline(int id, string returnUrl = null)
        {
            var invitation = GetPendingInvitationForCurrentCandidate(id);

            if (invitation == null)
                return new HttpStatusCodeResult(403);

            invitation.status = "Declined";
            invitation.responded_at = DateTime.Now;
            AccountLogService.LogActivity(data, invitation.candidate_id, "DeclineInvitation", "Invitation declined.", Request, relatedId: invitation.invitation_id, relatedType: "Invitation");
            data.SubmitChanges();

            TempData["InvitationMessage"] = "Invitation declined.";
            return RedirectToInvitationList(returnUrl);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private CandidateInvitationInviteViewModel BuildInviteModel(User candidate, int? selectedJobId, string message)
        {
            var displayName = GetDisplayName(candidate);

            return new CandidateInvitationInviteViewModel
            {
                CandidateId = candidate.user_id,
                CandidateName = displayName,
                JobId = selectedJobId ?? 0,
                Message = message,
                Jobs = GetInvitableJobs(selectedJobId)
            };
        }

        private List<SelectListItem> GetInvitableJobs(int? selectedJobId)
        {
            var jobs = from job in data.Jobs
                       join company in data.Companies on job.company_id equals company.company_id
                       join companyUser in data.CompanyUsers on company.company_id equals companyUser.company_id
                       where companyUser.User.username == User.Identity.Name
                       orderby job.posted_date descending, job.job_title
                       select new
                       {
                           job.job_id,
                           job.job_title,
                           company.company_name
                       };

            return jobs
                .ToList()
                .Select(job => new SelectListItem
                {
                    Value = job.job_id.ToString(),
                    Text = job.job_title + " - " + job.company_name,
                    Selected = selectedJobId.HasValue && selectedJobId.Value == job.job_id
                })
                .ToList();
        }

        private bool CanInviteToJob(Job job)
        {
            return job != null &&
                AuthRoleHelper.IsEmployer(User.Identity.Name) &&
                AuthRoleHelper.CanManageCompany(User.Identity.Name, job.company_id);
        }

        private CandidateInvitation GetPendingInvitationForCurrentCandidate(int invitationId)
        {
            var candidate = GetCurrentUser();

            if (candidate == null || !AuthRoleHelper.IsCandidate(User.Identity.Name))
                return null;

            return data.CandidateInvitations.FirstOrDefault(invitation =>
                invitation.invitation_id == invitationId &&
                invitation.candidate_id == candidate.user_id &&
                invitation.status == "Pending");
        }

        private List<CandidateInvitationListItemViewModel> BuildInvitationsForCandidate(int candidateId)
        {
            return BuildInvitationQuery()
                .Where(item => item.Invitation.candidate_id == candidateId)
                .OrderByDescending(item => item.Invitation.created_at)
                .ToList()
                .Select(item => ToListItem(item))
                .ToList();
        }

        private List<CandidateInvitationListItemViewModel> BuildSentInvitations(int employerId, string filter)
        {
            var query = BuildInvitationQuery();

            if (!AuthRoleHelper.IsAdmin(User.Identity.Name))
                query = query.Where(item => item.Invitation.employer_id == employerId);

            if (filter != "all")
                query = query.Where(item => item.Invitation.status == CultureInfoInvariantTitle(filter));

            return query
                .OrderByDescending(item => item.Invitation.created_at)
                .ToList()
                .Select(item => ToListItem(item))
                .ToList();
        }

        private IQueryable<InvitationRow> BuildInvitationQuery()
        {
            return from invitation in data.CandidateInvitations
                   join job in data.Jobs on invitation.job_id equals job.job_id
                   join company in data.Companies on job.company_id equals company.company_id
                   join employer in data.Users on invitation.employer_id equals employer.user_id
                   join candidate in data.Users on invitation.candidate_id equals candidate.user_id
                   select new InvitationRow
                   {
                       Invitation = invitation,
                       Job = job,
                       Company = company,
                       Employer = employer,
                       Candidate = candidate
                   };
        }

        private CandidateInvitationListItemViewModel ToListItem(InvitationRow row)
        {
            return new CandidateInvitationListItemViewModel
            {
                InvitationId = row.Invitation.invitation_id,
                JobId = row.Job.job_id,
                CandidateId = row.Candidate.user_id,
                CandidateName = GetDisplayName(row.Candidate),
                CandidateEmail = row.Candidate.email,
                CandidateAvatarPath = GetAvatarPath(row.Candidate.user_id),
                EmployerName = GetDisplayName(row.Employer),
                EmployerEmail = row.Employer.email,
                JobTitle = row.Job.job_title,
                CompanyName = row.Company.company_name,
                Message = row.Invitation.message,
                Status = row.Invitation.status,
                CreatedAt = row.Invitation.created_at,
                RespondedAt = row.Invitation.responded_at
            };
        }

        private InvitationRow GetInvitationForCurrentEmployer(int invitationId)
        {
            var row = BuildInvitationQuery()
                .FirstOrDefault(item => item.Invitation.invitation_id == invitationId);

            if (row == null)
                return null;

            if (AuthRoleHelper.IsAdmin(User.Identity.Name))
                return row;

            var currentUser = GetCurrentUser();

            if (currentUser != null && AuthRoleHelper.IsEmployer(User.Identity.Name) && row.Invitation.employer_id == currentUser.user_id)
                return row;

            return null;
        }

        private string NormalizeSentFilter(string filter)
        {
            filter = (filter ?? "all").ToLower();
            return filter == "pending" || filter == "accepted" || filter == "declined" ? filter : "all";
        }

        private ActionResult RedirectToInvitationList(string returnUrl)
        {
            if (!String.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

        private string SafeReturnUrl(string returnUrl, string fallback)
        {
            return !String.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : fallback;
        }

        private string CultureInfoInvariantTitle(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return value;

            return value.Substring(0, 1).ToUpperInvariant() + value.Substring(1).ToLowerInvariant();
        }

        private bool IsUserInRole(int userId, string roleName)
        {
            return data.UserRoles.Any(userRole => userRole.user_id == userId && userRole.Role.role_name == roleName);
        }

        private User GetCurrentUser()
        {
            return data.Users.FirstOrDefault(user => user.username == User.Identity.Name);
        }

        private string GetDisplayName(User user)
        {
            var profileName = data.UserProfileRecords
                .Where(profile => profile.user_id == user.user_id)
                .Select(profile => profile.full_name)
                .FirstOrDefault();

            if (!String.IsNullOrWhiteSpace(profileName))
                return profileName;

            return user.username;
        }

        private string GetAvatarPath(int userId)
        {
            var avatarPath = data.UserProfileRecords
                .Where(profile => profile.user_id == userId)
                .Select(profile => profile.avatar_path)
                .FirstOrDefault();

            return String.IsNullOrWhiteSpace(avatarPath) ? SystemSettingsService.GetDefaultUserAvatarPath(data) : avatarPath;
        }

        private void TrySendInvitationEmail(User candidate, User employer, Job job, string message)
        {
            try
            {
                var body = "Hello " + GetDisplayName(candidate) + "," + Environment.NewLine + Environment.NewLine +
                           "An employer has reviewed your profile on KCV Job Portal and would like to invite you to consider a job opportunity." + Environment.NewLine + Environment.NewLine +
                           "Invitation details" + Environment.NewLine +
                           "------------------" + Environment.NewLine +
                           "Position: " + job.job_title + Environment.NewLine +
                           "Employer contact: " + GetDisplayName(employer) + Environment.NewLine;

                if (!String.IsNullOrWhiteSpace(message))
                    body += Environment.NewLine +
                            "Message from the employer" + Environment.NewLine +
                            "-------------------------" + Environment.NewLine +
                            message + Environment.NewLine;

                body += Environment.NewLine +
                        "What you can do next" + Environment.NewLine +
                        "--------------------" + Environment.NewLine +
                        "- Sign in to KCV Job Portal to review the invitation details." + Environment.NewLine +
                        "- Read the job information carefully before deciding whether to apply." + Environment.NewLine +
                        "- If the role fits your goals, submit your application with the most relevant CV." + Environment.NewLine +
                        "- If you are not interested, you may ignore or decline the invitation in your account." + Environment.NewLine + Environment.NewLine +
                        "This invitation does not require an immediate response, but responding early helps the employer plan their recruitment process." + Environment.NewLine + Environment.NewLine +
                        "Best regards," + Environment.NewLine +
                        "KCV Job Portal Support Team";

                EmailHelper.Send(candidate.email, "Job invitation: " + job.job_title, body);
            }
            catch
            {
                // The invitation is still saved so the candidate can see it even if SMTP is unavailable locally.
            }
        }

        private class InvitationRow
        {
            public CandidateInvitation Invitation { get; set; }
            public Job Job { get; set; }
            public Company Company { get; set; }
            public User Employer { get; set; }
            public User Candidate { get; set; }
        }
    }
}
