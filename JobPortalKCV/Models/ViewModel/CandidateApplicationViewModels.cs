using System;
using System.Collections.Generic;

namespace JobPortalKCV.Models.ViewModel
{
    public class CandidateApplicationsIndexViewModel
    {
        public string Filter { get; set; }
        public List<CandidateApplicationItemViewModel> Applications { get; set; }
    }

    public class CandidateApplicationItemViewModel
    {
        public int ApplicationId { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyLogoPath { get; set; }
        public string CvFileName { get; set; }
        public string CvFilePath { get; set; }
        public string CoverLetter { get; set; }
        public DateTime? AppliedDate { get; set; }
        public string Status { get; set; }
        public string FinalResult { get; set; }
        public CandidateApplicationInterviewViewModel Interview { get; set; }
    }

    public class CandidateApplicationInterviewViewModel
    {
        public int InterviewId { get; set; }
        public int ApplicationId { get; set; }
        public string JobTitle { get; set; }
        public string CompanyName { get; set; }
        public DateTime InterviewDate { get; set; }
        public string Location { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string AdditionalInfo { get; set; }
    }
}
