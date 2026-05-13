namespace JobPortalKCV.Models.ViewModel
{
    using System.Collections.Generic;

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalJobs { get; set; }
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int TotalStars { get; set; }
        public int VerifiedUsers { get; set; }
        public int PendingEmailUsers { get; set; }
        public int CandidateUsers { get; set; }
        public int EmployerUsers { get; set; }
        public List<AdminTableLinkViewModel> Tables { get; set; }
        public List<AdminRecentUserViewModel> RecentUsers { get; set; }
        public List<AdminRecentJobViewModel> RecentJobs { get; set; }
        public List<AdminRecentApplicationViewModel> RecentApplications { get; set; }
        public List<AdminRecentLogViewModel> RecentLogs { get; set; }
        public List<AdminChartItemViewModel> ApplicationsByStatus { get; set; }
        public List<AdminChartItemViewModel> JobsByCategory { get; set; }
        public List<AdminChartItemViewModel> UsersByRole { get; set; }
    }

    public class AdminRecentUserViewModel
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
    }

    public class AdminRecentJobViewModel
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; }
        public string CompanyName { get; set; }
        public string PostedDate { get; set; }
    }

    public class AdminRecentApplicationViewModel
    {
        public int ApplicationId { get; set; }
        public string JobTitle { get; set; }
        public string ApplicantName { get; set; }
        public string Status { get; set; }
        public string AppliedDate { get; set; }
    }

    public class AdminRecentLogViewModel
    {
        public string Action { get; set; }
        public string Description { get; set; }
        public string CreatedAt { get; set; }
    }

    public class AdminChartItemViewModel
    {
        public string Label { get; set; }
        public int Count { get; set; }
        public int Percent { get; set; }
    }
}
