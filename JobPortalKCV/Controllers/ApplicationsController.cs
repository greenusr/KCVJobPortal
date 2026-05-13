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
    public class ApplicationsController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        public ActionResult Index(int page = 1)
        {
            if (!AuthRoleHelper.CanManageJobs(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var query = from application in data.JobApplications
                        join job in data.Jobs on application.job_id equals job.job_id
                        join company in data.Companies on job.company_id equals company.company_id
                        join applicant in data.Users on application.user_id equals applicant.user_id
                        join cv in data.UserCVs on application.cv_id equals cv.cv_id into cvJoin
                        from cv in cvJoin.DefaultIfEmpty()
                        join status in data.ApplicationStatuses on application.status_id equals status.status_id into statusJoin
                        from status in statusJoin.DefaultIfEmpty()
                        select new
                        {
                            Application = application,
                            Job = job,
                            Company = company,
                            Applicant = applicant,
                            CV = cv,
                            Status = status
                        };

            if (!AuthRoleHelper.IsAdmin(User.Identity.Name))
            {
                query = from item in query
                        join companyUser in data.CompanyUsers on item.Company.company_id equals companyUser.company_id
                        where companyUser.User.username == User.Identity.Name
                        select item;
            }

            var applications = query
                .OrderByDescending(item => item.Application.applied_date ?? item.Application.application_date)
                .Select(item => new ApplicationViewModel
                {
                    application_id = item.Application.application_id,
                    job_title = item.Job.job_title,
                    company_name = item.Company.company_name,
                    applicant_name = GetDisplayName(item.Applicant),
                    applicant_email = item.Applicant.email,
                    cover_letter = item.Application.cover_letter,
                    application_date = item.Application.applied_date ?? item.Application.application_date,
                    cv_file_name = item.CV == null ? null : item.CV.file_name,
                    cv_file_path = item.CV == null ? null : item.CV.file_path,
                    status_name = item.Application.status ?? (item.Status == null ? null : item.Status.status_name),
                    final_result = item.Application.final_result,
                    can_invite_interview = item.Application.status == "Pending" || item.Application.status == null,
                    can_complete_interview = item.Application.status == "Interview"
                })
                .ToList();

            applications = PaginationService.Paginate(
                applications,
                page,
                SystemSettingsService.GetPaginationSize(data),
                out PaginationViewModel pagination,
                "Index",
                "Applications");
            ViewBag.Pagination = pagination;

            return View(applications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id, string returnUrl = null)
        {
            var application = GetApplicationForCurrentUser(id);

            if (application == null)
            {
                TempData["ApplicationError"] = "You are not allowed to access this application.";
                return RedirectToApplications(returnUrl);
            }

            if (!IsPending(application))
            {
                TempData["ApplicationError"] = "Something went wrong. Please try again.";
                return RedirectToApplications(returnUrl);
            }

            application.status = "Completed";
            application.completed_at = DateTime.Now;
            application.final_result = "Rejected";
            NotificationService.NotifyApplicationResult(data, application, "Rejected");
            LogCurrentUserActivity("RejectApplication", "Application rejected.", application.application_id, "Application");
            data.SubmitChanges();

            TempData["ApplicationMessage"] = "Application rejected successfully.";
            return RedirectToApplications(returnUrl);
        }

        public ActionResult Interview(int id)
        {
            var model = BuildInterviewModel(id);

            if (model == null)
                return new HttpStatusCodeResult(403, "You are not allowed to access this application.");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Interview(InterviewViewModel model)
        {
            var application = GetApplicationForCurrentUser(model.application_id);

            if (application == null)
            {
                TempData["ApplicationError"] = "You are not allowed to access this application.";
                return RedirectToApplications(Request.Form["returnUrl"]);
            }

            if (!IsPending(application))
            {
                TempData["ApplicationError"] = "Something went wrong. Please try again.";
                return RedirectToApplications(Request.Form["returnUrl"]);
            }

            if (!model.interview_date.HasValue || model.interview_date.Value <= DateTime.Now)
                ModelState.AddModelError("interview_date", "Invalid interview information.");

            if (!ModelState.IsValid)
            {
                var displayModel = BuildInterviewModel(model.application_id) ?? model;
                displayModel.interview_date = model.interview_date;
                displayModel.location = model.location;
                displayModel.contact_name = model.contact_name;
                displayModel.contact_email = model.contact_email;
                displayModel.contact_phone = model.contact_phone;
                displayModel.additional_info = model.additional_info;

                ModelState.AddModelError("", "Invalid interview information.");
                return View(displayModel);
            }

            var details = GetApplicationDetails(model.application_id);

            if (details == null)
            {
                TempData["ApplicationError"] = "You are not allowed to access this application.";
                return RedirectToApplications(Request.Form["returnUrl"]);
            }

            try
            {
                var interview = new Interview
                {
                    application_id = application.application_id,
                    interview_date = model.interview_date.Value,
                    location = model.location.Trim(),
                    contact_name = model.contact_name.Trim(),
                    contact_email = model.contact_email.Trim(),
                    contact_phone = model.contact_phone.Trim(),
                    additional_info = model.additional_info
                };

                data.Interviews.InsertOnSubmit(interview);

                application.status = "Interview";
                application.final_result = null;
                NotificationService.NotifyInterviewInvitation(data, application, interview);
                LogCurrentUserActivity("InviteInterview", "Interview invitation sent.", application.application_id, "Application");
                data.SubmitChanges();

                EmailHelper.Send(details.Applicant.email, "Interview invitation for " + details.Job.job_title, BuildInterviewEmail(details.Job.job_title, model));

                TempData["ApplicationMessage"] = "Interview invitation has been sent successfully.";
                return RedirectToApplications(Request.Form["returnUrl"]);
            }
            catch
            {
                TempData["ApplicationError"] = "Something went wrong. Please try again.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Complete(int id, string result, string returnUrl = null)
        {
            var application = GetApplicationForCurrentUser(id);

            if (application == null)
            {
                TempData["ApplicationError"] = "You are not allowed to access this application.";
                return RedirectToApplications(returnUrl);
            }

            if (result != "Accepted" && result != "Rejected")
            {
                TempData["ApplicationError"] = "Something went wrong. Please try again.";
                return RedirectToApplications(returnUrl);
            }

            if (application.status != "Interview")
            {
                TempData["ApplicationError"] = "Something went wrong. Please try again.";
                return RedirectToApplications(returnUrl);
            }

            var details = GetApplicationDetails(id);

            application.status = "Completed";
            application.completed_at = DateTime.Now;
            application.final_result = result;
            NotificationService.NotifyApplicationResult(data, application, result);
            LogCurrentUserActivity(result == "Accepted" ? "AcceptApplication" : "RejectApplication", "Application " + result.ToLowerInvariant() + ".", application.application_id, "Application");
            data.SubmitChanges();

            if (result == "Accepted")
            {
                var emailSent = SendApplicationAcceptedEmail(details);
                TempData["ApplicationMessage"] = emailSent
                    ? "Application accepted successfully. The candidate has been notified by email."
                    : "Application accepted successfully, but the email could not be sent.";
            }
            else
            {
                TempData["ApplicationMessage"] = "Application rejected successfully.";
            }

            return RedirectToApplications(returnUrl);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private JobApplication GetApplicationForCurrentUser(int id)
        {
            var application = data.JobApplications.FirstOrDefault(item => item.application_id == id);

            if (application == null)
                return null;

            if (AuthRoleHelper.IsAdmin(User.Identity.Name))
                return application;

            var job = data.Jobs.FirstOrDefault(item => item.job_id == application.job_id);

            if (job == null || !AuthRoleHelper.CanManageCompany(User.Identity.Name, job.company_id))
                return null;

            return application;
        }

        private InterviewViewModel BuildInterviewModel(int id)
        {
            var details = GetApplicationDetails(id);

            if (details == null)
                return null;

            if (!IsPending(details.Application))
                return null;

            var employer = data.Users.FirstOrDefault(user => user.username == User.Identity.Name);

            return new InterviewViewModel
            {
                application_id = details.Application.application_id,
                job_title = details.Job.job_title,
                applicant_name = GetDisplayName(details.Applicant),
                applicant_email = details.Applicant.email,
                interview_date = DateTime.Now.AddDays(1),
                location = details.Company.company_name,
                contact_name = employer == null ? "" : GetDisplayName(employer),
                contact_email = details.Company.contact_email,
                contact_phone = ""
            };
        }

        private ApplicationDetails GetApplicationDetails(int id)
        {
            var application = GetApplicationForCurrentUser(id);

            if (application == null)
                return null;

            var details = (from item in data.JobApplications
                           join job in data.Jobs on item.job_id equals job.job_id
                           join company in data.Companies on job.company_id equals company.company_id
                           join applicant in data.Users on item.user_id equals applicant.user_id
                           where item.application_id == id
                           select new ApplicationDetails
                           {
                               Application = item,
                               Job = job,
                               Company = company,
                               Applicant = applicant
                           }).FirstOrDefault();

            return details;
        }

        private string BuildInterviewEmail(string jobTitle, InterviewViewModel model)
        {
            var body = "Hello," + Environment.NewLine + Environment.NewLine +
                       "Thank you for your interest in this opportunity. The employer has reviewed your application and would like to invite you to an interview." + Environment.NewLine + Environment.NewLine +
                       "Interview details" + Environment.NewLine +
                       "-----------------" + Environment.NewLine +
                       "Position: " + jobTitle + Environment.NewLine +
                       "Date and time: " + model.interview_date.Value.ToString("dd/MM/yyyy HH:mm") + Environment.NewLine +
                       "Location / meeting method: " + model.location + Environment.NewLine +
                       "Contact person: " + model.contact_name + Environment.NewLine +
                       "Contact email: " + model.contact_email + Environment.NewLine +
                       "Contact phone: " + model.contact_phone + Environment.NewLine + Environment.NewLine +
                       "Recommended preparation" + Environment.NewLine +
                       "-----------------------" + Environment.NewLine +
                       "- Please confirm the interview information and prepare any documents requested by the employer." + Environment.NewLine +
                       "- If the interview is online, check your device, camera, microphone, and internet connection in advance." + Environment.NewLine +
                       "- If you need to reschedule, contact the employer as early as possible using the contact information above.";

            if (!String.IsNullOrWhiteSpace(model.additional_info))
                body += Environment.NewLine + Environment.NewLine +
                        "Additional information from the employer" + Environment.NewLine +
                        "----------------------------------------" + Environment.NewLine +
                        model.additional_info;

            body += Environment.NewLine + Environment.NewLine +
                    "We wish you the best in your interview." + Environment.NewLine + Environment.NewLine +
                    "Best regards," + Environment.NewLine +
                    "KCV Job Portal Support Team";

            return body;
        }

        private bool SendApplicationAcceptedEmail(ApplicationDetails details)
        {
            if (details == null || details.Applicant == null || String.IsNullOrWhiteSpace(details.Applicant.email))
                return false;

            try
            {
                EmailHelper.Send(
                    details.Applicant.email,
                    "Your application has been accepted - " + details.Job.job_title,
                    BuildApplicationAcceptedEmail(details));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string BuildApplicationAcceptedEmail(ApplicationDetails details)
        {
            var candidateName = GetDisplayName(details.Applicant);

            return "Hello " + candidateName + "," + Environment.NewLine + Environment.NewLine +
                   "Congratulations. Your application has been accepted by the employer." + Environment.NewLine + Environment.NewLine +
                   "Application summary" + Environment.NewLine +
                   "-------------------" + Environment.NewLine +
                   "Position: " + details.Job.job_title + Environment.NewLine +
                   "Company: " + details.Company.company_name + Environment.NewLine +
                   "Application status: Accepted" + Environment.NewLine + Environment.NewLine +
                   "What happens next" + Environment.NewLine +
                   "-----------------" + Environment.NewLine +
                   "The employer may contact you using the email address or phone number in your profile to discuss next steps. This may include an interview, additional document review, onboarding instructions, or a direct follow-up from the company." + Environment.NewLine + Environment.NewLine +
                   "Recommended actions" + Environment.NewLine +
                   "-------------------" + Environment.NewLine +
                   "- Keep your phone and email available for employer communication." + Environment.NewLine +
                   "- Review your CV and profile so the information remains accurate." + Environment.NewLine +
                   "- Prepare any certificates, portfolio links, or documents that may support your application." + Environment.NewLine + Environment.NewLine +
                   "Thank you for using KCV Job Portal. We hope this opportunity is a strong next step in your career." + Environment.NewLine + Environment.NewLine +
                   "Best regards," + Environment.NewLine +
                   "KCV Job Portal Support Team";
        }

        private string GetDisplayName(User user)
        {
            if (user == null)
                return "";

            var profileName = data.UserProfileRecords
                .Where(profile => profile.user_id == user.user_id)
                .Select(profile => profile.full_name)
                .FirstOrDefault();

            if (!String.IsNullOrWhiteSpace(profileName))
                return profileName;

            return user.username;
        }

        private bool IsPending(JobApplication application)
        {
            return application.status == "Pending" || String.IsNullOrWhiteSpace(application.status);
        }

        private ActionResult RedirectToApplications(string returnUrl)
        {
            if (!String.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

        private void LogCurrentUserActivity(string action, string description, int? relatedId, string relatedType)
        {
            var user = data.Users.FirstOrDefault(item => item.username == User.Identity.Name);

            if (user != null)
                AccountLogService.LogActivity(data, user.user_id, action, description, Request, relatedId: relatedId, relatedType: relatedType);
        }

        private class ApplicationDetails
        {
            public JobApplication Application { get; set; }
            public Job Job { get; set; }
            public Company Company { get; set; }
            public User Applicant { get; set; }
        }
    }
}
