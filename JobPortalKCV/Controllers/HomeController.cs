using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JobPortalKCV.Models;
using JobPortalKCV.Models.ViewModel;
using JobPortalKCV.Services;

namespace JobPortalKCV.Controllers
{
    public class HomeController : Controller
    {
        JobPortalDataContext data = new JobPortalDataContext();

        public ActionResult Index()
        {
            SystemSettingsService.AutoCloseExpiredJobs(data);
            var currentUser = Request.IsAuthenticated
                ? data.Users.FirstOrDefault(user => user.username == User.Identity.Name)
                : null;
            var starredJobIds = currentUser == null
                ? new List<int>()
                : data.Stars
                    .Where(star => star.user_id == currentUser.user_id && star.target_type == "Job")
                    .Select(star => star.target_id)
                    .ToList();

            var recentJobs = (from j in data.Jobs
                              join c in data.Companies on j.company_id equals c.company_id
                              join l in data.Locations on j.location_id equals l.location_id
                              join t in data.EmploymentTypes on j.employment_type_id equals t.employment_type_id
                              where j.is_active && c.show_jobs_publicly
                              orderby j.posted_date descending
                              select new JobViewModel
                              {
                                  job_id = j.job_id,
                                  job_title = j.job_title,
                                  job_description = j.job_description,
                                  salary_range = j.salary_range,
                                  posted_date = j.posted_date,
                                  company_id = j.company_id,
                                  location_id = j.location_id,
                                  company_name = c.company_name,
                                  logo_path = SystemSettingsService.GetCompanyLogoOrDefault(data, c.logo_path),
                                  location_name = l.city,
                                  employment_type = t.type_name,
                                  star_count = data.Stars.Count(star => star.target_type == "Job" && star.target_id == j.job_id),
                                  is_starred_by_current_user = starredJobIds.Contains(j.job_id)
                              }).Take(4).ToList();

            var iconClasses = new[]
            {
                "flaticon-tour",
                "flaticon-cms",
                "flaticon-report",
                "flaticon-app",
                "flaticon-helmet",
                "flaticon-high-tech",
                "flaticon-real-estate",
                "flaticon-content"
            };

            var categories = data.JobCategories
                .OrderBy(c => c.category_name)
                .Take(8)
                .AsEnumerable()
                .Select((category, index) => new HomeCategoryViewModel
                {
                    category_id = category.category_id,
                    category_name = category.category_name,
                    job_count = data.JobCategoryMaps.Count(map => map.category_id == category.category_id),
                    icon_class = iconClasses[index % iconClasses.Length]
                }).ToList();

            var model = new HomeIndexViewModel
            {
                TotalJobs = data.Jobs.Count(),
                TotalCompanies = data.Companies.Count(),
                TotalCandidates = data.Users.Count(),
                RecentJobs = recentJobs,
                Categories = categories,
                Locations = data.Locations
                    .OrderBy(l => l.city)
                    .Select(l => new HomeLocationViewModel
                    {
                        location_id = l.location_id,
                        location_name = l.city
                    }).ToList()
            };

            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }
    }
}
