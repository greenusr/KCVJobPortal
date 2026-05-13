using System;
using System.Linq;
using JobPortalKCV.Models;

namespace JobPortalKCV.Helpers
{
    public static class AuthRoleHelper
    {
        public static bool IsAdmin(string username)
        {
            return IsInRole(username, "Admin");
        }

        public static bool IsEmployer(string username)
        {
            return IsInRole(username, "Employer");
        }

        public static bool IsCandidate(string username)
        {
            return IsInRole(username, "Candidate");
        }

        public static bool CanManageJobs(string username)
        {
            return IsAdmin(username) || IsEmployer(username);
        }

        public static bool CanManageCompany(string username, int? companyId)
        {
            if (String.IsNullOrWhiteSpace(username))
                return false;

            if (IsAdmin(username))
                return true;

            if (!companyId.HasValue || !IsEmployer(username))
                return false;

            using (var data = new JobPortalDataContext())
            {
                return data.CompanyUsers.Any(companyUser =>
                    companyUser.company_id == companyId.Value &&
                    companyUser.User.username == username);
            }
        }

        private static bool IsInRole(string username, string roleName)
        {
            if (String.IsNullOrWhiteSpace(username))
                return false;

            using (var data = new JobPortalDataContext())
            {
                return data.UserRoles.Any(userRole =>
                    userRole.User.username == username &&
                    userRole.Role.role_name == roleName);
            }
        }
    }
}
