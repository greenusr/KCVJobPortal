using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JobPortalKCV.Helpers;
using JobPortalKCV.Models;
using JobPortalKCV.Models.ViewModel;
using JobPortalKCV.Services;

namespace JobPortalKCV.Controllers
{
    public class JobsController : Controller
    {
        JobPortalDataContext data = new JobPortalDataContext();

        // GET: Jobs
        public ActionResult Index(string keyword, int? locationId, int? categoryId, int? skillId, string sort = "newest", int page = 1)
        {
            SystemSettingsService.AutoCloseExpiredJobs(data);
            var currentUser = GetCurrentUser();
            var query = from j in data.Jobs
                        join c in data.Companies on j.company_id equals c.company_id
                        join l in data.Locations on j.location_id equals l.location_id
                        join t in data.EmploymentTypes on j.employment_type_id equals t.employment_type_id
                        select new
                        {
                            Job = j,
                            Company = c,
                            Location = l,
                            EmploymentType = t
                        };

            if (!AuthRoleHelper.CanManageJobs(User.Identity.Name))
                query = query.Where(x => x.Job.is_active);

            if (!String.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.Job.job_title.Contains(keyword) ||
                    x.Job.job_description.Contains(keyword) ||
                    x.Company.company_name.Contains(keyword) ||
                    x.Location.city.Contains(keyword) ||
                    data.JobCategoryMaps.Any(map => map.job_id == x.Job.job_id && map.JobCategory.category_name.Contains(keyword)) ||
                    data.JobSkills.Any(jobSkill => jobSkill.job_id == x.Job.job_id && jobSkill.Skill.skill_name.Contains(keyword)));
            }

            if (locationId.HasValue)
                query = query.Where(x => x.Job.location_id == locationId.Value);

            if (categoryId.HasValue)
            {
                query = query.Where(x => data.JobCategoryMaps.Any(map =>
                    map.job_id == x.Job.job_id &&
                    map.category_id == categoryId.Value));
            }

            if (skillId.HasValue)
            {
                query = query.Where(x => data.JobSkills.Any(jobSkill =>
                    jobSkill.job_id == x.Job.job_id &&
                    jobSkill.skill_id == skillId.Value));
            }

            var jobRows = query
                .ToList()
                .Where(x => CanViewCompanyJobs(x.Company))
                .ToList();

            var starredJobIds = currentUser == null
                ? new List<int>()
                : data.Stars
                    .Where(star => star.user_id == currentUser.user_id && star.target_type == "Job")
                    .Select(star => star.target_id)
                    .ToList();

            var jobs = jobRows
                .Select(x => new JobViewModel
                {
                    job_id = x.Job.job_id,
                    job_title = x.Job.job_title,
                    job_description = x.Job.job_description,
                    salary_range = x.Job.salary_range,
                    posted_date = x.Job.posted_date,

                    company_id = x.Job.company_id,
                    location_id = x.Job.location_id,

                    company_name = x.Company.company_name,
                    logo_path = SystemSettingsService.GetCompanyLogoOrDefault(data, x.Company.logo_path),
                    location_name = x.Location.city,
                    employment_type = x.EmploymentType.type_name,
                    star_count = data.Stars.Count(star => star.target_type == "Job" && star.target_id == x.Job.job_id),
                    is_starred_by_current_user = starredJobIds.Contains(x.Job.job_id)
                }).ToList();

            jobs = SortJobs(jobs, sort);
            jobs = PaginationService.Paginate(
                jobs,
                page,
                SystemSettingsService.GetPaginationSize(data),
                out PaginationViewModel pagination,
                "Index",
                "Jobs",
                new { keyword, locationId, categoryId, skillId, sort });

            ViewBag.Keyword = keyword;
            ViewBag.LocationId = locationId;
            ViewBag.CategoryId = categoryId;
            ViewBag.SkillId = skillId;
            ViewBag.Sort = sort;
            ViewBag.TotalRecords = pagination.TotalRecords;
            ViewBag.Pagination = pagination;
            LoadFilterSelectLists(locationId, categoryId, skillId);
            LogSearchIfNeeded(currentUser, keyword, locationId, categoryId, skillId, sort);

            return View(jobs);
        }

        public ActionResult Details(int id)
        {
            SystemSettingsService.AutoCloseExpiredJobs(data);
            var currentUser = GetCurrentUser();

            // 1. Lấy job + company + location + job type
            var job = (from j in data.Jobs
                       join c in data.Companies on j.company_id equals c.company_id
                       join l in data.Locations on j.location_id equals l.location_id
                       join t in data.EmploymentTypes on j.employment_type_id equals t.employment_type_id
                       where j.job_id == id
                       select new JobDetailViewModel
                       {
                           job_id = j.job_id,
                           job_title = j.job_title,
                           job_description = j.job_description,
                           salary_range = j.salary_range,
                           posted_date = j.posted_date,
                           application_deadline = j.application_deadline,
                           company_id = j.company_id,

                           employment_type = t.type_name,
                           location_name = l.city,

                           // Company info đúng theo DB
                           company_name = c.company_name,
                           industry = c.industry,
                           website = c.website,
                           contact_email = c.contact_email,
                           logo_path = c.logo_path
                       }).FirstOrDefault();

            if (job == null)
                return HttpNotFound();

            job.logo_path = SystemSettingsService.GetCompanyLogoOrDefault(data, job.logo_path);
            var entity = data.Jobs.FirstOrDefault(item => item.job_id == id);
            var companyEntity = entity == null || !entity.company_id.HasValue
                ? null
                : data.Companies.FirstOrDefault(item => item.company_id == entity.company_id.Value);

            if (companyEntity == null || !CanViewCompanyJobs(companyEntity))
                return HttpNotFound();

            if (!AuthRoleHelper.CanManageCompany(User.Identity.Name, job.company_id))
            {
                if (entity != null && !entity.is_active)
                    return HttpNotFound();
            }

            if (currentUser != null)
            {
                AccountLogService.LogActivity(data, currentUser.user_id, "ViewDetail", "Viewed job detail: " + job.job_title, Request, relatedId: job.job_id, relatedType: "Job");
                data.SubmitChanges();
            }

            job.star_count = data.Stars.Count(star => star.target_type == "Job" && star.target_id == job.job_id);
            job.is_starred_by_current_user = currentUser != null && data.Stars.Any(star => star.user_id == currentUser.user_id && star.target_type == "Job" && star.target_id == job.job_id);
            job.can_view_starred_by = AuthRoleHelper.CanManageCompany(User.Identity.Name, job.company_id);

            // 2. Lấy skills
            var skills = (from js in data.JobSkills
                          join s in data.Skills on js.skill_id equals s.skill_id
                          where js.job_id == id
                          select s.skill_name).ToList();

            job.skills = skills;

            // Categories
            job.categories = (from jc in data.JobCategoryMaps
                              join c in data.JobCategories on jc.category_id equals c.category_id
                              where jc.job_id == id
                              select c.category_name).ToList();

            if (AuthRoleHelper.IsCandidate(User.Identity.Name))
            {
                var user = data.Users.FirstOrDefault(u => u.username == User.Identity.Name);

                if (user != null)
                {
                    ViewBag.UserCVs = new SelectList(
                        data.UserCVs.Where(cv => cv.user_id == user.user_id).OrderByDescending(cv => cv.is_default).ThenByDescending(cv => cv.created_at),
                        "cv_id",
                        "file_name");
                }
            }

            return View(job);
        }

        public ActionResult Create()
        {
            if (!AuthRoleHelper.CanManageJobs(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var settings = SystemSettingsService.GetSettings(data);
            LoadJobSelectLists();

            return View(new Job
            {
                posted_date = DateTime.Today,
                application_deadline = DateTime.Today.AddDays(settings.default_job_expiration_days)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "job_title,company_id,job_description,salary_range,posted_date,location_id,employment_type_id,application_deadline")] Job job, int[] skillIds, string newSkills, string returnUrl = null)
        {
            if (!AuthRoleHelper.CanManageJobs(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            if (!AuthRoleHelper.CanManageCompany(User.Identity.Name, job.company_id))
                ModelState.AddModelError("company_id", "You can only post jobs for your company.");

            if (SystemSettingsService.RequiresCompanyLogoToPostJob(data) && !CompanyHasLogo(job.company_id))
                ModelState.AddModelError("company_id", "Company logo is required to post a job.");

            if (ModelState.IsValid)
            {
                job.is_active = true;
                job.application_deadline = (job.posted_date ?? DateTime.Today).AddDays(SystemSettingsService.GetSettings(data).default_job_expiration_days);

                data.Jobs.InsertOnSubmit(job);
                data.SubmitChanges();

                SyncJobSkills(job.job_id, ResolveSkillIds(skillIds, newSkills));
                data.SubmitChanges();

                return RedirectToJobsContext(returnUrl);
            }

            LoadJobSelectLists(job, skillIds);
            ViewBag.NewSkills = newSkills;
            return View(job);
        }

        public ActionResult Edit(int id)
        {
            if (!AuthRoleHelper.CanManageJobs(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var job = data.Jobs.FirstOrDefault(j => j.job_id == id);

            if (job == null)
                return HttpNotFound();

            if (!AuthRoleHelper.CanManageCompany(User.Identity.Name, job.company_id))
                return new HttpStatusCodeResult(403);

            LoadJobSelectLists(job);
            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, [Bind(Include = "job_title,company_id,job_description,salary_range,posted_date,location_id,employment_type_id,application_deadline")] Job formJob, int[] skillIds, string newSkills, string returnUrl = null)
        {
            if (!AuthRoleHelper.CanManageJobs(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var job = data.Jobs.FirstOrDefault(j => j.job_id == id);

            if (job == null)
                return HttpNotFound();

            if (!AuthRoleHelper.CanManageCompany(User.Identity.Name, job.company_id))
                return new HttpStatusCodeResult(403);

            if (!AuthRoleHelper.CanManageCompany(User.Identity.Name, formJob.company_id))
                ModelState.AddModelError("company_id", "You can only assign jobs to your company.");

            if (ModelState.IsValid)
            {
                job.job_title = formJob.job_title;
                job.company_id = formJob.company_id;
                job.job_description = formJob.job_description;
                job.salary_range = formJob.salary_range;
                job.posted_date = formJob.posted_date;
                job.location_id = formJob.location_id;
                job.employment_type_id = formJob.employment_type_id;
                job.application_deadline = formJob.application_deadline;

                SyncJobSkills(job.job_id, ResolveSkillIds(skillIds, newSkills));
                data.SubmitChanges();

                return RedirectToJobsContext(returnUrl);
            }

            LoadJobSelectLists(formJob, skillIds);
            ViewBag.NewSkills = newSkills;
            return View(formJob);
        }

        public ActionResult Delete(int id)
        {
            if (!AuthRoleHelper.CanManageJobs(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var job = GetJobDetail(id);

            if (job == null)
                return HttpNotFound();

            if (!AuthRoleHelper.CanManageCompany(User.Identity.Name, job.company_id))
                return new HttpStatusCodeResult(403);

            return View(job);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id, string returnUrl = null)
        {
            if (!AuthRoleHelper.CanManageJobs(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var job = data.Jobs.FirstOrDefault(j => j.job_id == id);

            if (job == null)
                return HttpNotFound();

            if (!AuthRoleHelper.CanManageCompany(User.Identity.Name, job.company_id))
                return new HttpStatusCodeResult(403);

            if (data.JobApplications.Any(a => a.job_id == id))
            {
                ModelState.AddModelError("", "This job already has applications and cannot be deleted.");
                return View(GetJobDetail(id));
            }

            var jobSkills = data.JobSkills.Where(js => js.job_id == id);
            var jobCategories = data.JobCategoryMaps.Where(jc => jc.job_id == id);

            data.JobSkills.DeleteAllOnSubmit(jobSkills);
            data.JobCategoryMaps.DeleteAllOnSubmit(jobCategories);
            data.Jobs.DeleteOnSubmit(job);
            data.SubmitChanges();

            return RedirectToJobsContext(returnUrl);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Apply(int id, int? cvId, string coverLetter)
        {
            if (!AuthRoleHelper.IsCandidate(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var user = data.Users.FirstOrDefault(u => u.username == User.Identity.Name);
            var job = data.Jobs.FirstOrDefault(j => j.job_id == id);

            if (user == null || job == null)
                return HttpNotFound();

            var userCvs = data.UserCVs.Where(cv => cv.user_id == user.user_id);

            if (!userCvs.Any())
            {
                TempData["JobError"] = "Please upload your CV before applying.";
                return RedirectToAction("Details", new { id = id });
            }

            UserCV selectedCv;

            if (cvId.HasValue)
            {
                selectedCv = userCvs.FirstOrDefault(cv => cv.cv_id == cvId.Value);

                if (selectedCv == null)
                {
                    TempData["JobError"] = "Invalid CV selection.";
                    return RedirectToAction("Details", new { id = id });
                }
            }
            else
            {
                selectedCv = userCvs.FirstOrDefault(cv => cv.is_default) ?? userCvs.OrderByDescending(cv => cv.created_at).First();
            }

            var hasActiveApplication = data.JobApplications.Any(a =>
                a.job_id == id &&
                a.user_id == user.user_id &&
                (a.status == "Pending" || a.status == "Interview"));

            if (hasActiveApplication)
            {
                TempData["JobError"] = "You already have an active application for this job.";
                return RedirectToAction("Details", new { id = id });
            }

            var application = new JobApplication
            {
                job_id = id,
                user_id = user.user_id,
                application_date = DateTime.Today,
                applied_date = DateTime.Now,
                status = "Pending",
                cover_letter = coverLetter,
                cv_id = selectedCv.cv_id
            };

            data.JobApplications.InsertOnSubmit(application);
            NotificationService.NotifyApplicationSubmitted(data, application);
            AccountLogService.LogActivity(data, user.user_id, "ApplyJob", "Applied for " + job.job_title + ".", Request, relatedId: application.application_id, relatedType: "Application");
            data.SubmitChanges();

            TempData["JobMessage"] = "Application submitted successfully.";
            return RedirectToAction("Details", new { id = id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private ActionResult RedirectToJobsContext(string returnUrl)
        {
            if (!String.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

        private JobDetailViewModel GetJobDetail(int id)
        {
            var job = (from j in data.Jobs
                       join c in data.Companies on j.company_id equals c.company_id
                       join l in data.Locations on j.location_id equals l.location_id
                       join t in data.EmploymentTypes on j.employment_type_id equals t.employment_type_id
                       where j.job_id == id
                       select new JobDetailViewModel
                       {
                           job_id = j.job_id,
                           job_title = j.job_title,
                           job_description = j.job_description,
                           salary_range = j.salary_range,
                           posted_date = j.posted_date,
                           application_deadline = j.application_deadline,
                           company_id = j.company_id,

                           employment_type = t.type_name,
                           location_name = l.city,

                           company_name = c.company_name,
                           industry = c.industry,
                           website = c.website,
                           contact_email = c.contact_email,
                           logo_path = SystemSettingsService.GetCompanyLogoOrDefault(data, c.logo_path)
                       }).FirstOrDefault();

            if (job == null)
                return null;

            job.star_count = data.Stars.Count(star => star.target_type == "Job" && star.target_id == job.job_id);
            job.is_starred_by_current_user = false;
            job.can_view_starred_by = AuthRoleHelper.CanManageCompany(User.Identity.Name, job.company_id);

            job.skills = (from js in data.JobSkills
                          join s in data.Skills on js.skill_id equals s.skill_id
                          where js.job_id == id
                          select s.skill_name).ToList();

            job.categories = (from jc in data.JobCategoryMaps
                              join c in data.JobCategories on jc.category_id equals c.category_id
                              where jc.job_id == id
                              select c.category_name).ToList();

            return job;
        }

        private void LoadJobSelectLists(Job job = null, IEnumerable<int> selectedSkillIds = null)
        {
            var companies = data.Companies.AsQueryable();

            if (!AuthRoleHelper.IsAdmin(User.Identity.Name))
            {
                companies = from c in data.Companies
                            join cu in data.CompanyUsers on c.company_id equals cu.company_id
                            where cu.User.username == User.Identity.Name
                            select c;
            }

            ViewBag.company_id = new SelectList(companies.OrderBy(c => c.company_name), "company_id", "company_name", job?.company_id);
            ViewBag.location_id = new SelectList(data.Locations.OrderBy(l => l.city), "location_id", "city", job?.location_id);
            ViewBag.employment_type_id = new SelectList(data.EmploymentTypes.OrderBy(t => t.type_name), "employment_type_id", "type_name", job?.employment_type_id);

            var selectedSet = selectedSkillIds != null
                ? new HashSet<int>(selectedSkillIds)
                : new HashSet<int>();

            if ((selectedSkillIds == null || !selectedSet.Any()) && job != null && job.job_id > 0)
            {
                selectedSet = new HashSet<int>(data.JobSkills
                    .Where(jobSkill => jobSkill.job_id == job.job_id && jobSkill.skill_id.HasValue)
                    .Select(jobSkill => jobSkill.skill_id.Value));
            }

            ViewBag.SkillOptions = data.Skills
                .OrderBy(skill => skill.skill_name)
                .ToList()
                .Select(skill => new SelectListItem
                {
                    Value = skill.skill_id.ToString(),
                    Text = skill.skill_name,
                    Selected = selectedSet.Contains(skill.skill_id)
                })
                .ToList();
        }

        private IEnumerable<int> ResolveSkillIds(IEnumerable<int> selectedSkillIds, string newSkills)
        {
            var skillIds = selectedSkillIds == null
                ? new HashSet<int>()
                : new HashSet<int>(selectedSkillIds);

            if (String.IsNullOrWhiteSpace(newSkills))
                return skillIds;

            var names = newSkills
                .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim())
                .Where(name => !String.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var name in names)
            {
                var skill = FindOrCreateSkill(name);
                skillIds.Add(skill.skill_id);
            }

            return skillIds;
        }

        private Skill FindOrCreateSkill(string skillName)
        {
            var normalizedName = skillName.Trim();
            var lowerName = normalizedName.ToLower();
            var skill = data.Skills.FirstOrDefault(item => item.skill_name != null && item.skill_name.ToLower() == lowerName);

            if (skill != null)
                return skill;

            skill = new Skill
            {
                skill_name = normalizedName
            };

            data.Skills.InsertOnSubmit(skill);
            data.SubmitChanges();

            return skill;
        }

        private void SyncJobSkills(int jobId, IEnumerable<int> skillIds)
        {
            var existing = data.JobSkills.Where(jobSkill => jobSkill.job_id == jobId).ToList();
            data.JobSkills.DeleteAllOnSubmit(existing);

            if (skillIds == null)
                return;

            var requestedSkillIds = skillIds.Distinct().ToList();

            var validSkillIds = data.Skills
                .Where(skill => requestedSkillIds.Contains(skill.skill_id))
                .Select(skill => skill.skill_id)
                .Distinct()
                .ToList();

            foreach (var skillId in validSkillIds)
            {
                data.JobSkills.InsertOnSubmit(new JobSkill
                {
                    job_id = jobId,
                    skill_id = skillId
                });
            }
        }

        private bool CompanyHasLogo(int? companyId)
        {
            if (!companyId.HasValue)
                return false;

            return data.Companies.Any(company =>
                company.company_id == companyId.Value &&
                company.logo_path != null &&
                company.logo_path != "");
        }

        private bool CanViewCompanyJobs(Company company)
        {
            if (company == null)
                return false;

            if (company.show_jobs_publicly)
                return true;

            if (AuthRoleHelper.IsAdmin(User.Identity.Name))
                return true;

            var currentUser = GetCurrentUser();

            return currentUser != null && data.CompanyUsers.Any(item =>
                item.company_id == company.company_id &&
                item.user_id == currentUser.user_id);
        }

        private void LoadFilterSelectLists(int? locationId, int? categoryId, int? skillId)
        {
            ViewBag.Locations = new SelectList(data.Locations.OrderBy(location => location.city), "location_id", "city", locationId);
            ViewBag.Categories = new SelectList(data.JobCategories.OrderBy(category => category.category_name), "category_id", "category_name", categoryId);
            ViewBag.Skills = new SelectList(data.Skills.OrderBy(skill => skill.skill_name), "skill_id", "skill_name", skillId);
        }

        private List<JobViewModel> SortJobs(List<JobViewModel> jobs, string sort)
        {
            switch ((sort ?? "newest").ToLower())
            {
                case "oldest":
                    return jobs.OrderBy(job => job.posted_date ?? DateTime.MinValue).ToList();
                case "star_desc":
                    return jobs.OrderByDescending(job => job.star_count).ThenByDescending(job => job.posted_date ?? DateTime.MinValue).ToList();
                case "star_asc":
                    return jobs.OrderBy(job => job.star_count).ThenByDescending(job => job.posted_date ?? DateTime.MinValue).ToList();
                default:
                    return jobs.OrderByDescending(job => job.posted_date ?? DateTime.MinValue).ToList();
            }
        }

        private User GetCurrentUser()
        {
            if (!Request.IsAuthenticated)
                return null;

            return data.Users.FirstOrDefault(user => user.username == User.Identity.Name);
        }

        private void LogSearchIfNeeded(User currentUser, string keyword, int? locationId, int? categoryId, int? skillId, string sort)
        {
            if (currentUser == null)
                return;

            if (String.IsNullOrWhiteSpace(keyword) && !locationId.HasValue && !categoryId.HasValue && !skillId.HasValue && String.IsNullOrWhiteSpace(sort))
                return;

            var filters = AccountLogService.BuildFilters(
                categoryId.HasValue ? "category=" + categoryId.Value : null,
                locationId.HasValue ? "location=" + locationId.Value : null,
                skillId.HasValue ? "skill=" + skillId.Value : null,
                !String.IsNullOrWhiteSpace(sort) ? "sort=" + sort : null);

            AccountLogService.LogActivity(data, currentUser.user_id, "Search", "Searched jobs.", Request, keyword, filters);
            data.SubmitChanges();
        }
    }
}
