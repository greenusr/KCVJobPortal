using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobPortalKCV.Models.ViewModel
{
    public class JobDetailViewModel
    {
        public int job_id { get; set; }
        public string job_title { get; set; }
        public string job_description { get; set; }
        public string salary_range { get; set; }
        public DateTime? posted_date { get; set; }
        public DateTime? application_deadline { get; set; }
        public int? company_id { get; set; }

        public string employment_type { get; set; }
        public string location_name { get; set; }

        // Company
        public string company_name { get; set; }
        public string industry { get; set; }
        public string website { get; set; }
        public string contact_email { get; set; }
        public string logo_path { get; set; }
        public int star_count { get; set; }
        public bool is_starred_by_current_user { get; set; }
        public bool can_view_starred_by { get; set; }

        // Skills
        public List<string> skills { get; set; }

        // Categories
        public List<string> categories { get; set; }
    }
}
