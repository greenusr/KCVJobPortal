using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace JobPortalKCV.Models.ViewModel
{
    public class CandidateInvitationInviteViewModel
    {
        public int CandidateId { get; set; }
        public string CandidateName { get; set; }

        [Required]
        [Display(Name = "Job")]
        public int JobId { get; set; }

        public string Message { get; set; }
        public List<SelectListItem> Jobs { get; set; }
    }

    public class CandidateInvitationListItemViewModel
    {
        public int InvitationId { get; set; }
        public int JobId { get; set; }
        public int CandidateId { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidateAvatarPath { get; set; }
        public string EmployerName { get; set; }
        public string EmployerEmail { get; set; }
        public string JobTitle { get; set; }
        public string CompanyName { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
