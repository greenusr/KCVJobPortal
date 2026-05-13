using System;
using System.ComponentModel.DataAnnotations;

namespace JobPortalKCV.Models.ViewModel
{
    public class ApplicationViewModel
    {
        public int application_id { get; set; }
        public string job_title { get; set; }
        public string company_name { get; set; }
        public string applicant_name { get; set; }
        public string applicant_email { get; set; }
        public string cover_letter { get; set; }
        public DateTime? application_date { get; set; }
        public string cv_file_name { get; set; }
        public string cv_file_path { get; set; }
        public string status_name { get; set; }
        public string final_result { get; set; }
        public bool can_invite_interview { get; set; }
        public bool can_complete_interview { get; set; }
    }

    public class InterviewViewModel
    {
        public int application_id { get; set; }
        public string job_title { get; set; }
        public string applicant_name { get; set; }
        public string applicant_email { get; set; }

        [Required]
        [Display(Name = "Interview date")]
        public DateTime? interview_date { get; set; }

        [Required]
        public string location { get; set; }

        [Required]
        [Display(Name = "Contact name")]
        public string contact_name { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Contact email")]
        public string contact_email { get; set; }

        [Required]
        [RegularExpression(@"^[0-9+\-\s().]{7,30}$", ErrorMessage = "Invalid interview information.")]
        [Display(Name = "Contact phone")]
        public string contact_phone { get; set; }

        [Display(Name = "Additional info")]
        public string additional_info { get; set; }
    }
}
