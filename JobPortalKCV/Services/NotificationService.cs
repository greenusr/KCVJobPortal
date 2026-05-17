using System;
using System.Linq;
using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class NotificationService
    {
        public static void Create(JobPortalDataContext data, int userId, string title, string message, string type, int? relatedId, string relatedType)
        {
            if (!CanCreateNotification(data, userId, type))
                return;

            data.Notifications.InsertOnSubmit(new Notification
            {
                user_id = userId,
                title = title,
                message = message,
                type = type,
                related_id = relatedId,
                related_type = relatedType,
                is_read = false
            });
        }

        private static bool CanCreateNotification(JobPortalDataContext data, int userId, string type)
        {
            var settings = data.UserSettings.FirstOrDefault(item => item.user_id == userId);

            if (settings == null)
                return true;

            if (!settings.app_notifications_enabled)
                return false;

            if (String.Equals(type, "Interview", StringComparison.OrdinalIgnoreCase))
                return settings.interview_notifications_enabled;

            if (String.Equals(type, "Invitation", StringComparison.OrdinalIgnoreCase))
                return settings.invitation_notifications_enabled;

            if (String.Equals(type, "Application", StringComparison.OrdinalIgnoreCase))
                return settings.job_updates_enabled;

            return true;
        }

        public static void NotifyApplicationSubmitted(JobPortalDataContext data, JobApplication application)
        {
            var job = data.Jobs.FirstOrDefault(item => item.job_id == application.job_id);
            var candidate = data.Users.FirstOrDefault(item => item.user_id == application.user_id);

            if (job == null || candidate == null)
                return;

            var name = GetUserDisplayName(data, candidate);
            var message = "User " + name + " has applied for " + job.job_title + ".";

            var employerIds = (from companyUser in data.CompanyUsers
                               join userRole in data.UserRoles on companyUser.user_id equals userRole.user_id
                               join role in data.Roles on userRole.role_id equals role.role_id
                               where companyUser.company_id == job.company_id && role.role_name == "Employer"
                               select companyUser.user_id)
                .Distinct()
                .ToList();

            foreach (var employerId in employerIds)
                Create(data, employerId, "New job application", message, "Application", application.application_id, "Application");
        }

        public static void NotifyInterviewInvitation(JobPortalDataContext data, JobApplication application, Interview interview)
        {
            var details = GetApplicationDetails(data, application);

            if (details == null)
                return;

            Create(
                data,
                details.Candidate.user_id,
                "Interview invitation",
                "You have been invited to an interview for " + details.Job.job_title + ".",
                "Interview",
                interview.interview_id,
                "Interview");
        }

        public static void NotifyApplicationResult(JobPortalDataContext data, JobApplication application, string result)
        {
            var details = GetApplicationDetails(data, application);

            if (details == null)
                return;

            var normalizedResult = String.Equals(result, "Accepted", StringComparison.OrdinalIgnoreCase)
                ? "accepted"
                : "rejected";

            Create(
                data,
                details.Candidate.user_id,
                "Application " + normalizedResult,
                "Your application for " + details.Job.job_title + " has been " + normalizedResult + ".",
                "Application",
                application.application_id,
                "Application");
        }

        public static void NotifyCandidateInvited(JobPortalDataContext data, CandidateInvitation invitation)
        {
            var job = data.Jobs.FirstOrDefault(item => item.job_id == invitation.job_id);

            if (job == null)
                return;

            Create(
                data,
                invitation.candidate_id,
                "Job invitation",
                "You have been invited to apply for " + job.job_title + ".",
                "Invitation",
                invitation.invitation_id,
                "Invitation");
        }

        public static void NotifyCompanyJoinRequested(JobPortalDataContext data, CompanyJoinRequest request)
        {
            var company = data.Companies.FirstOrDefault(item => item.company_id == request.company_id);
            var requester = data.Users.FirstOrDefault(item => item.user_id == request.user_id);

            if (company == null || requester == null)
                return;

            var requesterName = GetUserDisplayName(data, requester);
            var message = requesterName + " requested to join " + company.company_name + ".";
            var ownerIds = data.CompanyUsers
                .Where(item => item.company_id == company.company_id && item.role == "Owner")
                .Select(item => item.user_id)
                .Distinct()
                .ToList();

            foreach (var ownerId in ownerIds)
                Create(data, ownerId, "Company join request", message, "Company", request.request_id, "CompanyJoinRequest");
        }

        public static void NotifyCompanyJoinRequestPending(JobPortalDataContext data, CompanyJoinRequest request)
        {
            var company = data.Companies.FirstOrDefault(item => item.company_id == request.company_id);

            if (company == null)
                return;

            Create(
                data,
                request.user_id,
                "Company join request pending",
                "Your request to join " + company.company_name + " is pending owner approval.",
                "Company",
                request.request_id,
                "CompanyJoinRequest");
        }

        private static ApplicationDetails GetApplicationDetails(JobPortalDataContext data, JobApplication application)
        {
            return (from item in data.JobApplications
                    join job in data.Jobs on item.job_id equals job.job_id
                    join candidate in data.Users on item.user_id equals candidate.user_id
                    where item.application_id == application.application_id
                    select new ApplicationDetails
                    {
                        Job = job,
                        Candidate = candidate
                    }).FirstOrDefault();
        }

        private static string GetUserDisplayName(JobPortalDataContext data, User user)
        {
            var profileName = data.UserProfileRecords
                .Where(profile => profile.user_id == user.user_id)
                .Select(profile => profile.full_name)
                .FirstOrDefault();

            if (!String.IsNullOrWhiteSpace(profileName))
                return profileName;

            return user.username;
        }

        private class ApplicationDetails
        {
            public Job Job { get; set; }
            public User Candidate { get; set; }
        }
    }
}
