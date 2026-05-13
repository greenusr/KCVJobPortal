using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobPortalKCV.Models.ViewModel
{
    public class CompanyListItemViewModel
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string Industry { get; set; }
        public string Website { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string Address { get; set; }
        public string LogoPath { get; set; }
        public string Description { get; set; }
        public bool PublicCompanyProfile { get; set; }
        public bool ShowJobsPublicly { get; set; }
        public int StarCount { get; set; }
        public bool IsStarredByCurrentUser { get; set; }
        public bool CanManage { get; set; }
        public bool CanOwn { get; set; }
        public string MembershipRole { get; set; }
        public int JobCount { get; set; }
        public int PendingRequestCount { get; set; }
    }

    public class CompanyDetailsViewModel : CompanyListItemViewModel
    {
        public List<CompanyMemberViewModel> Members { get; set; }
        public List<CompanyJoinRequestViewModel> PendingRequests { get; set; }
        public List<JobViewModel> Jobs { get; set; }
    }

    public class CompanyMemberViewModel
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public class CompanyJoinRequestViewModel
    {
        public int RequestId { get; set; }
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    public class TransferCompanyOwnerViewModel
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }

        [Required]
        [Display(Name = "New owner")]
        public int? NewOwnerUserId { get; set; }

        public List<CompanyMemberViewModel> Members { get; set; }
    }

    public class CompanySettingsViewModel
    {
        public int CompanyId { get; set; }

        [Required]
        public string CompanyName { get; set; }

        public string Industry { get; set; }
        public string Description { get; set; }
        public string Website { get; set; }

        [EmailAddress]
        public string ContactEmail { get; set; }

        public string ContactPhone { get; set; }
        public string Address { get; set; }
        public string LogoPath { get; set; }
        public string LogoAcceptTypes { get; set; }
        public string AllowedImageTypes { get; set; }
        public bool PublicCompanyProfile { get; set; }
        public bool ShowJobsPublicly { get; set; }
    }
}
