using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobPortalKCV.Models.ViewModel
{
    public class JobViewModel
    {
        public int job_id { get; set; }
        public string job_title { get; set; }
        public string job_description { get; set; }
        public string salary_range { get; set; }
        public DateTime? posted_date { get; set; }

        // ID (giữ lại)
        public int? company_id { get; set; }
        public int location_id { get; set; }

        // NAME (hiển thị)
        public string company_name { get; set; }
        public string logo_path { get; set; }
        public string location_name { get; set; }
        public string employment_type { get; set; }
        public int star_count { get; set; }
        public bool is_starred_by_current_user { get; set; }
    }
}
