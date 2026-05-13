using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using JobPortalKCV.Models;
using JobPortalKCV.Models.ViewModel;
using JobPortalKCV.Services;

namespace JobPortalKCV.Controllers
{
    public class SearchController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        public ActionResult Results(string keyword, int? categoryId, int? locationId, int? skillId, string sort = "newest", string resultType = "all", int page = 1)
        {
            SystemSettingsService.AutoCloseExpiredJobs(data);
            var model = new SearchResultsViewModel
            {
                Keyword = keyword,
                CategoryId = categoryId,
                LocationId = locationId,
                SkillId = skillId,
                Sort = sort,
                ResultType = String.IsNullOrWhiteSpace(resultType) ? "all" : resultType,
                Categories = data.JobCategories.OrderBy(category => category.category_name).ToList(),
                Locations = data.Locations.OrderBy(location => location.city).ToList(),
                Skills = data.Skills.OrderBy(skill => skill.skill_name).ToList()
            };

            model.JobResults = ShouldInclude(model.ResultType, "jobs")
                ? SearchJobs(keyword, categoryId, locationId, skillId, sort)
                : new List<JobViewModel>();
            model.UserResults = ShouldInclude(model.ResultType, "users")
                ? SearchUsers(keyword, locationId, skillId, sort)
                : new List<UserSearchResultViewModel>();
            model.CompanyResults = ShouldInclude(model.ResultType, "companies")
                ? SearchCompanies(keyword, sort)
                : new List<CompanyListItemViewModel>();

            ApplyPagination(model, page);

            LogSearchIfNeeded(keyword, categoryId, locationId, skillId, sort, model.ResultType);

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private bool ShouldInclude(string resultType, string expected)
        {
            return String.IsNullOrWhiteSpace(resultType) ||
                resultType.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                resultType.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyPagination(SearchResultsViewModel model, int page)
        {
            var pageSize = SystemSettingsService.GetPaginationSize(data);
            var jobCount = model.JobResults == null ? 0 : model.JobResults.Count;
            var userCount = model.UserResults == null ? 0 : model.UserResults.Count;
            var companyCount = model.CompanyResults == null ? 0 : model.CompanyResults.Count;
            var totalRecords = Math.Max(jobCount, Math.Max(userCount, companyCount));

            pageSize = Math.Max(1, pageSize);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
            page = Math.Max(1, Math.Min(page, totalPages));

            if (model.JobResults != null)
                model.JobResults = model.JobResults.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            if (model.UserResults != null)
                model.UserResults = model.UserResults.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            if (model.CompanyResults != null)
                model.CompanyResults = model.CompanyResults.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Pagination = new PaginationViewModel
            {
                Page = page,
                TotalPages = totalPages,
                TotalRecords = jobCount + userCount + companyCount,
                ActionName = "Results",
                ControllerName = "Search",
                RouteValues = new
                {
                    keyword = model.Keyword,
                    categoryId = model.CategoryId,
                    locationId = model.LocationId,
                    skillId = model.SkillId,
                    sort = model.Sort,
                    resultType = model.ResultType
                }
            };
        }

        private List<JobViewModel> SearchJobs(string keyword, int? categoryId, int? locationId, int? skillId, string sort)
        {
            var query = from job in data.Jobs
                        join company in data.Companies on job.company_id equals company.company_id
                        join location in data.Locations on job.location_id equals location.location_id
                        join type in data.EmploymentTypes on job.employment_type_id equals type.employment_type_id
                        where job.is_active
                        select new { Job = job, Company = company, Location = location, EmploymentType = type };

            if (!String.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(item =>
                    item.Job.job_title.Contains(keyword) ||
                    item.Job.job_description.Contains(keyword) ||
                    item.Company.company_name.Contains(keyword) ||
                    item.Location.city.Contains(keyword) ||
                    data.JobCategoryMaps.Any(map => map.job_id == item.Job.job_id && map.JobCategory.category_name.Contains(keyword)) ||
                    data.JobSkills.Any(jobSkill => jobSkill.job_id == item.Job.job_id && jobSkill.Skill.skill_name.Contains(keyword)));
            }

            if (categoryId.HasValue)
                query = query.Where(item => data.JobCategoryMaps.Any(map => map.job_id == item.Job.job_id && map.category_id == categoryId.Value));

            if (locationId.HasValue)
                query = query.Where(item => item.Job.location_id == locationId.Value);

            if (skillId.HasValue)
                query = query.Where(item => data.JobSkills.Any(jobSkill => jobSkill.job_id == item.Job.job_id && jobSkill.skill_id == skillId.Value));

            var jobs = query
                .ToList()
                .Where(item => CanViewCompanyJobs(item.Company))
                .Select(item => new JobViewModel
            {
                job_id = item.Job.job_id,
                job_title = item.Job.job_title,
                job_description = item.Job.job_description,
                salary_range = item.Job.salary_range,
                posted_date = item.Job.posted_date,
                company_id = item.Company.company_id,
                location_id = item.Job.location_id,
                company_name = item.Company.company_name,
                logo_path = SystemSettingsService.GetCompanyLogoOrDefault(data, item.Company.logo_path),
                location_name = item.Location.city,
                employment_type = item.EmploymentType.type_name,
                star_count = data.Stars.Count(star => star.target_type == "Job" && star.target_id == item.Job.job_id)
            }).ToList();

            return SortJobs(jobs, sort);
        }

        private List<UserSearchResultViewModel> SearchUsers(string keyword, int? locationId, int? skillId, string sort)
        {
            var users = data.Users.ToList();
            var profiles = data.UserProfileRecords.ToList();

            if (!String.IsNullOrWhiteSpace(keyword))
            {
                users = users.Where(user =>
                {
                    var profile = profiles.FirstOrDefault(item => item.user_id == user.user_id);

                    return Contains(profile?.full_name, keyword) ||
                        Contains(user.username, keyword) ||
                        Contains(user.email, keyword) ||
                        Contains(profile?.address, keyword) ||
                        Contains(profile?.about_me, keyword) ||
                        data.UserSkills.Any(userSkill => userSkill.user_id == user.user_id && userSkill.Skill.skill_name.Contains(keyword));
                }).ToList();
            }

            if (locationId.HasValue)
                users = users.Where(user => profiles.Any(profile => profile.user_id == user.user_id && profile.location_id == locationId.Value)).ToList();

            if (skillId.HasValue)
                users = users.Where(user => data.UserSkills.Any(userSkill => userSkill.user_id == user.user_id && userSkill.skill_id == skillId.Value)).ToList();

            var results = users.Select(user =>
            {
                var profile = profiles.FirstOrDefault(item => item.user_id == user.user_id);

                return new UserSearchResultViewModel
                {
                    UserId = user.user_id,
                    DisplayName = FirstNotEmpty(profile?.full_name, user.username),
                    Email = user.email,
                    RoleName = data.UserRoles.Where(userRole => userRole.user_id == user.user_id).Select(userRole => userRole.Role.role_name).FirstOrDefault(),
                    LocationName = profile == null ? null : data.Locations.Where(location => location.location_id == profile.location_id).Select(location => location.city).FirstOrDefault(),
                    Address = profile?.address,
                    AboutMe = profile?.about_me,
                    StarCount = data.Stars.Count(star => star.target_type == "User" && star.target_id == user.user_id)
                };
            }).ToList();

            return SortUsers(results, sort);
        }

        private List<CompanyListItemViewModel> SearchCompanies(string keyword, string sort)
        {
            var companies = data.Companies
                .ToList()
                .Where(CanViewCompanyProfile);

            if (!String.IsNullOrWhiteSpace(keyword))
            {
                companies = companies.Where(company =>
                    Contains(company.company_name, keyword) ||
                    Contains(company.industry, keyword) ||
                    Contains(company.website, keyword) ||
                    Contains(company.contact_email, keyword) ||
                    Contains(company.description, keyword));
            }

            var results = companies.ToList().Select(company => new CompanyListItemViewModel
            {
                CompanyId = company.company_id,
                CompanyName = company.company_name,
                Industry = company.industry,
                Website = company.website,
                ContactEmail = company.contact_email,
                LogoPath = SystemSettingsService.GetCompanyLogoOrDefault(data, company.logo_path),
                Description = company.description,
                StarCount = data.Stars.Count(star => star.target_type == "Company" && star.target_id == company.company_id)
            }).ToList();

            return SortCompanies(results, sort);
        }

        private List<JobViewModel> SortJobs(List<JobViewModel> jobs, string sort)
        {
            switch ((sort ?? "newest").ToLower())
            {
                case "oldest":
                    return jobs.OrderBy(job => job.posted_date ?? DateTime.MinValue).ToList();
                case "star_desc":
                    return jobs.OrderByDescending(job => job.star_count).ToList();
                case "star_asc":
                    return jobs.OrderBy(job => job.star_count).ToList();
                default:
                    return jobs.OrderByDescending(job => job.posted_date ?? DateTime.MinValue).ToList();
            }
        }

        private List<UserSearchResultViewModel> SortUsers(List<UserSearchResultViewModel> users, string sort)
        {
            if ((sort ?? "").Equals("star_asc", StringComparison.OrdinalIgnoreCase))
                return users.OrderBy(user => user.StarCount).ToList();

            if ((sort ?? "").Equals("star_desc", StringComparison.OrdinalIgnoreCase))
                return users.OrderByDescending(user => user.StarCount).ToList();

            return users.OrderBy(user => user.DisplayName).ToList();
        }

        private List<CompanyListItemViewModel> SortCompanies(List<CompanyListItemViewModel> companies, string sort)
        {
            if ((sort ?? "").Equals("star_asc", StringComparison.OrdinalIgnoreCase))
                return companies.OrderBy(company => company.StarCount).ToList();

            if ((sort ?? "").Equals("star_desc", StringComparison.OrdinalIgnoreCase))
                return companies.OrderByDescending(company => company.StarCount).ToList();

            return companies.OrderBy(company => company.CompanyName).ToList();
        }

        private bool Contains(string value, string keyword)
        {
            return !String.IsNullOrWhiteSpace(value) &&
                value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string FirstNotEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !String.IsNullOrWhiteSpace(value));
        }

        private bool CanViewCompanyProfile(Company company)
        {
            if (company.public_company_profile)
                return true;

            if (IsAdmin())
                return true;

            var user = GetCurrentUser();

            return user != null && data.CompanyUsers.Any(item =>
                item.company_id == company.company_id &&
                item.user_id == user.user_id);
        }

        private bool CanViewCompanyJobs(Company company)
        {
            if (company.show_jobs_publicly)
                return true;

            if (IsAdmin())
                return true;

            var user = GetCurrentUser();

            return user != null && data.CompanyUsers.Any(item =>
                item.company_id == company.company_id &&
                item.user_id == user.user_id);
        }

        private bool IsAdmin()
        {
            return data.UserRoles.Any(userRole =>
                userRole.User.username == User.Identity.Name &&
                userRole.Role.role_name == "Admin");
        }

        private User GetCurrentUser()
        {
            if (!Request.IsAuthenticated)
                return null;

            return data.Users.FirstOrDefault(user => user.username == User.Identity.Name);
        }

        private void LogSearchIfNeeded(string keyword, int? categoryId, int? locationId, int? skillId, string sort, string resultType)
        {
            if (!Request.IsAuthenticated)
                return;

            if (String.IsNullOrWhiteSpace(keyword) && !categoryId.HasValue && !locationId.HasValue && !skillId.HasValue && String.IsNullOrWhiteSpace(sort) && String.IsNullOrWhiteSpace(resultType))
                return;

            var user = data.Users.FirstOrDefault(item => item.username == User.Identity.Name);

            if (user == null)
                return;

            var filters = AccountLogService.BuildFilters(
                categoryId.HasValue ? "category=" + categoryId.Value : null,
                locationId.HasValue ? "location=" + locationId.Value : null,
                skillId.HasValue ? "skill=" + skillId.Value : null,
                !String.IsNullOrWhiteSpace(sort) ? "sort=" + sort : null,
                !String.IsNullOrWhiteSpace(resultType) ? "resultType=" + resultType : null);

            AccountLogService.LogActivity(data, user.user_id, "Search", "Searched site.", Request, keyword, filters);
            data.SubmitChanges();
        }
    }
}
