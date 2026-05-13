using System.Collections.Generic;

namespace JobPortalKCV.Models.ViewModel
{
    public class HomeIndexViewModel
    {
        public int TotalJobs { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalCandidates { get; set; }
        public List<JobViewModel> RecentJobs { get; set; }
        public List<HomeCategoryViewModel> Categories { get; set; }
        public List<HomeLocationViewModel> Locations { get; set; }
    }

    public class HomeCategoryViewModel
    {
        public int category_id { get; set; }
        public string category_name { get; set; }
        public int job_count { get; set; }
        public string icon_class { get; set; }
    }

    public class HomeLocationViewModel
    {
        public int location_id { get; set; }
        public string location_name { get; set; }
    }
}
