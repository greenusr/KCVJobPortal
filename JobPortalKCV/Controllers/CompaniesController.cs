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
    public class CompaniesController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        public ActionResult Index(string keyword, string industry, int? locationId, string location, string sort = "name", int page = 1)
        {
            var currentUser = GetCurrentUser();
            var companies = data.Companies
                .OrderBy(c => c.company_name)
                .ToList()
                .Where(CanViewCompanyProfile)
                .ToList();

            if (!String.IsNullOrWhiteSpace(keyword))
            {
                companies = companies.Where(company =>
                    Contains(company.company_name, keyword) ||
                    Contains(company.industry, keyword) ||
                    Contains(company.description, keyword) ||
                    Contains(company.website, keyword) ||
                    Contains(company.contact_email, keyword) ||
                    Contains(company.address, keyword)).ToList();
            }

            if (!String.IsNullOrWhiteSpace(industry))
                companies = companies.Where(company => String.Equals(company.industry, industry, StringComparison.OrdinalIgnoreCase)).ToList();

            if (locationId.HasValue)
            {
                var selectedLocation = data.Locations.FirstOrDefault(item => item.location_id == locationId.Value);

                if (selectedLocation != null)
                {
                    companies = companies.Where(company =>
                        Contains(company.address, selectedLocation.city) ||
                        Contains(company.address, selectedLocation.country)).ToList();
                }
            }
            else if (!String.IsNullOrWhiteSpace(location))
            {
                companies = companies.Where(company => Contains(company.address, location)).ToList();
            }

            var models = companies.Select(company => new CompanyListItemViewModel
            {
                CompanyId = company.company_id,
                CompanyName = company.company_name,
                Industry = company.industry,
                Website = company.website,
                ContactEmail = company.contact_email,
                ContactPhone = company.contact_phone,
                Address = company.address,
                LogoPath = SystemSettingsService.GetCompanyLogoOrDefault(data, company.logo_path),
                Description = company.description,
                PublicCompanyProfile = company.public_company_profile,
                ShowJobsPublicly = company.show_jobs_publicly,
                StarCount = data.Stars.Count(star => star.target_type == "Company" && star.target_id == company.company_id),
                IsStarredByCurrentUser = currentUser != null && data.Stars.Any(star => star.user_id == currentUser.user_id && star.target_type == "Company" && star.target_id == company.company_id),
                CanManage = AuthRoleHelper.CanManageCompany(User.Identity.Name, company.company_id),
                CanOwn = IsCompanyOwner(company.company_id)
            }).ToList();

            models = SortCompanies(models, sort);

            models = PaginationService.Paginate(
                models,
                page,
                SystemSettingsService.GetPaginationSize(data),
                out PaginationViewModel pagination,
                "Index",
                "Companies",
                new { keyword, industry, locationId, sort });

            ViewBag.Keyword = keyword;
            ViewBag.Industry = industry;
            ViewBag.LocationId = locationId;
            ViewBag.Location = location;
            ViewBag.Sort = sort;
            ViewBag.TotalRecords = pagination.TotalRecords;
            ViewBag.Industries = data.Companies
                .Select(company => company.industry)
                .ToList()
                .Where(value => !String.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();
            ViewBag.Locations = new SelectList(data.Locations.OrderBy(item => item.city), "location_id", "city", locationId);
            ViewBag.Pagination = pagination;

            return View(models);
        }

        public ActionResult Details(int id)
        {
            var company = data.Companies.FirstOrDefault(c => c.company_id == id);

            if (company == null)
                return HttpNotFound();

            if (!CanViewCompanyProfile(company))
                return HttpNotFound();

            var currentUser = GetCurrentUser();
            if (currentUser != null)
            {
                AccountLogService.LogActivity(data, currentUser.user_id, "ViewDetail", "Viewed company detail: " + company.company_name, Request, relatedId: company.company_id, relatedType: "Company");
                data.SubmitChanges();
            }

            return View(BuildDetailsModel(company));
        }

        public ActionResult Create()
        {
            if (!AuthRoleHelper.CanManageJobs(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            return View(new Company());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "company_name,industry,website,contact_email,description")] Company company, HttpPostedFileBase logo, string returnUrl = null)
        {
            if (!AuthRoleHelper.CanManageJobs(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var logoResult = FileUploadService.SaveLogo(logo, Server);

            if (!logoResult.Success)
                ModelState.AddModelError("", logoResult.ErrorMessage);

            if (ModelState.IsValid)
            {
                company.logo_path = logoResult.FilePath;
                company.public_company_profile = true;
                company.show_jobs_publicly = true;
                company.updated_at = DateTime.Now;

                data.Companies.InsertOnSubmit(company);
                data.SubmitChanges();

                var user = GetCurrentUser();

                if (user != null && !AuthRoleHelper.IsAdmin(User.Identity.Name))
                {
                    data.CompanyUsers.InsertOnSubmit(new CompanyUser
                    {
                        user_id = user.user_id,
                        company_id = company.company_id,
                        role = "Owner"
                    });
                    data.SubmitChanges();
                }

                return RedirectToCompaniesContext(returnUrl, company.company_id);
            }

            return View(company);
        }

        public ActionResult Edit(int id, string returnUrl = null)
        {
            return RedirectToAction("Settings", "CompanySettings", new { id = id, returnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, [Bind(Include = "company_name,industry,website,contact_email,description")] Company formCompany, HttpPostedFileBase logo, string returnUrl = null)
        {
            if (!AuthRoleHelper.CanManageCompany(User.Identity.Name, id))
                return new HttpStatusCodeResult(403);

            var company = data.Companies.FirstOrDefault(c => c.company_id == id);

            if (company == null)
                return HttpNotFound();

            if (ModelState.IsValid)
            {
                company.company_name = formCompany.company_name;
                company.industry = formCompany.industry;
                company.website = formCompany.website;
                company.contact_email = formCompany.contact_email;
                company.description = formCompany.description;

                var logoResult = FileUploadService.SaveCompanyLogo(logo, Server);

                if (logoResult.Success)
                {
                    company.logo_path = logoResult.FilePath;
                }
                else if (logo != null && logo.ContentLength > 0)
                {
                    ModelState.AddModelError("", logoResult.ErrorMessage);
                    return View(company);
                }

                data.SubmitChanges();
                return RedirectToCompaniesContext(returnUrl, company.company_id);
            }

            return View(company);
        }

        public ActionResult Delete(int id)
        {
            if (!AuthRoleHelper.CanManageCompany(User.Identity.Name, id))
                return new HttpStatusCodeResult(403);

            var company = data.Companies.FirstOrDefault(c => c.company_id == id);

            if (company == null)
                return HttpNotFound();

            return View(company);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id, string returnUrl = null)
        {
            if (!AuthRoleHelper.CanManageCompany(User.Identity.Name, id))
                return new HttpStatusCodeResult(403);

            var company = data.Companies.FirstOrDefault(c => c.company_id == id);

            if (company == null)
                return HttpNotFound();

            if (data.Jobs.Any(j => j.company_id == id))
            {
                ModelState.AddModelError("", "This company has jobs and cannot be deleted.");
                return View(company);
            }

            data.CompanyUsers.DeleteAllOnSubmit(data.CompanyUsers.Where(cu => cu.company_id == id));
            data.Companies.DeleteOnSubmit(company);
            data.SubmitChanges();

            return RedirectToCompaniesContext(returnUrl);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveRequest(int id)
        {
            var request = data.CompanyJoinRequests.FirstOrDefault(item => item.request_id == id);

            if (request == null)
                return HttpNotFound();

            if (!IsCompanyOwner(request.company_id))
            {
                TempData["CompanyError"] = "You are not allowed to view this information.";
                return RedirectToAction("Details", new { id = request.company_id });
            }

            var currentUser = GetCurrentUser();

            if (!data.CompanyUsers.Any(item => item.company_id == request.company_id && item.user_id == request.user_id))
            {
                data.CompanyUsers.InsertOnSubmit(new CompanyUser
                {
                    company_id = request.company_id,
                    user_id = request.user_id,
                    role = "Employer"
                });
            }

            request.status = "Accepted";
            request.responded_at = DateTime.Now;
            request.responded_by = currentUser?.user_id;
            data.SubmitChanges();

            TempData["CompanyMessage"] = "Company join request accepted successfully.";
            return RedirectToAction("Details", new { id = request.company_id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectRequest(int id)
        {
            var request = data.CompanyJoinRequests.FirstOrDefault(item => item.request_id == id);

            if (request == null)
                return HttpNotFound();

            if (!IsCompanyOwner(request.company_id))
            {
                TempData["CompanyError"] = "You are not allowed to view this information.";
                return RedirectToAction("Details", new { id = request.company_id });
            }

            var currentUser = GetCurrentUser();

            request.status = "Rejected";
            request.responded_at = DateTime.Now;
            request.responded_by = currentUser?.user_id;
            data.SubmitChanges();

            TempData["CompanyMessage"] = "Company join request rejected successfully.";
            return RedirectToAction("Details", new { id = request.company_id });
        }

        [Authorize]
        public ActionResult TransferOwner(int id)
        {
            var company = data.Companies.FirstOrDefault(item => item.company_id == id);

            if (company == null)
                return HttpNotFound();

            if (!IsCompanyOwner(id))
                return new HttpStatusCodeResult(403);

            return View(new TransferCompanyOwnerViewModel
            {
                CompanyId = company.company_id,
                CompanyName = company.company_name,
                Members = GetMembers(company.company_id)
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TransferOwner(TransferCompanyOwnerViewModel model)
        {
            var company = data.Companies.FirstOrDefault(item => item.company_id == model.CompanyId);

            if (company == null)
                return HttpNotFound();

            if (!IsCompanyOwner(company.company_id))
                return new HttpStatusCodeResult(403);

            if (!model.NewOwnerUserId.HasValue || !data.CompanyUsers.Any(item => item.company_id == company.company_id && item.user_id == model.NewOwnerUserId.Value))
                ModelState.AddModelError("NewOwnerUserId", "Invalid target.");

            if (!ModelState.IsValid)
            {
                model.CompanyName = company.company_name;
                model.Members = GetMembers(company.company_id);
                return View(model);
            }

            var currentUser = GetCurrentUser();
            var currentOwners = data.CompanyUsers.Where(item => item.company_id == company.company_id && item.role == "Owner").ToList();
            var newOwner = data.CompanyUsers.First(item => item.company_id == company.company_id && item.user_id == model.NewOwnerUserId.Value);

            foreach (var owner in currentOwners)
            {
                if (owner.user_id != newOwner.user_id)
                    owner.role = "Employer";
            }

            newOwner.role = "Owner";

            if (currentUser != null && currentUser.user_id != newOwner.user_id)
            {
                var currentMember = data.CompanyUsers.FirstOrDefault(item => item.company_id == company.company_id && item.user_id == currentUser.user_id);

                if (currentMember != null && currentMember.role == "Owner")
                    currentMember.role = "Employer";
            }

            data.SubmitChanges();

            TempData["CompanyMessage"] = "Company ownership transferred successfully.";
            return RedirectToAction("Details", new { id = company.company_id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private ActionResult RedirectToCompaniesContext(string returnUrl, int? companyId = null)
        {
            if (!String.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (companyId.HasValue)
                return RedirectToAction("Details", new { id = companyId.Value });

            return RedirectToAction("Index");
        }

        private CompanyDetailsViewModel BuildDetailsModel(Company company)
        {
            var currentUser = GetCurrentUser();

            return new CompanyDetailsViewModel
            {
                CompanyId = company.company_id,
                CompanyName = company.company_name,
                Industry = company.industry,
                Website = company.website,
                ContactEmail = company.contact_email,
                ContactPhone = company.contact_phone,
                Address = company.address,
                LogoPath = SystemSettingsService.GetCompanyLogoOrDefault(data, company.logo_path),
                Description = company.description,
                PublicCompanyProfile = company.public_company_profile,
                ShowJobsPublicly = company.show_jobs_publicly,
                StarCount = data.Stars.Count(star => star.target_type == "Company" && star.target_id == company.company_id),
                IsStarredByCurrentUser = currentUser != null && data.Stars.Any(star => star.user_id == currentUser.user_id && star.target_type == "Company" && star.target_id == company.company_id),
                CanManage = AuthRoleHelper.CanManageCompany(User.Identity.Name, company.company_id),
                CanOwn = IsCompanyOwner(company.company_id),
                Members = GetMembers(company.company_id),
                PendingRequests = GetPendingRequests(company.company_id),
                Jobs = GetCompanyJobs(company)
            };
        }

        private List<JobViewModel> GetCompanyJobs(Company company)
        {
            if (company == null || !CanViewCompanyJobs(company))
                return new List<JobViewModel>();

            var query = from job in data.Jobs
                        join location in data.Locations on job.location_id equals location.location_id
                        join type in data.EmploymentTypes on job.employment_type_id equals type.employment_type_id
                        where job.company_id == company.company_id
                        orderby job.posted_date descending
                        select new
                        {
                            Job = job,
                            Location = location,
                            EmploymentType = type
                        };

            if (!AuthRoleHelper.CanManageCompany(User.Identity.Name, company.company_id))
                query = query.Where(item => item.Job.is_active);

            return query.ToList().Select(item => new JobViewModel
            {
                job_id = item.Job.job_id,
                job_title = item.Job.job_title,
                job_description = item.Job.job_description,
                salary_range = item.Job.salary_range,
                posted_date = item.Job.posted_date,
                company_id = item.Job.company_id,
                location_id = item.Job.location_id,
                company_name = company.company_name,
                logo_path = SystemSettingsService.GetCompanyLogoOrDefault(data, company.logo_path),
                location_name = item.Location.city,
                employment_type = item.EmploymentType.type_name,
                star_count = data.Stars.Count(star => star.target_type == "Job" && star.target_id == item.Job.job_id)
            }).ToList();
        }

        private List<CompanyMemberViewModel> GetMembers(int companyId)
        {
            return (from companyUser in data.CompanyUsers
                    join user in data.Users on companyUser.user_id equals user.user_id
                    join profile in data.UserProfileRecords on user.user_id equals profile.user_id into profileJoin
                    from profile in profileJoin.DefaultIfEmpty()
                    where companyUser.company_id == companyId
                    orderby companyUser.role descending, profile.full_name, user.username
                    select new CompanyMemberViewModel
                    {
                        UserId = user.user_id,
                        DisplayName = String.IsNullOrEmpty(profile.full_name) ? user.username : profile.full_name,
                        Email = user.email,
                        Role = companyUser.role
                    }).ToList();
        }

        private List<CompanyJoinRequestViewModel> GetPendingRequests(int companyId)
        {
            if (!IsCompanyOwner(companyId))
                return new List<CompanyJoinRequestViewModel>();

            return (from request in data.CompanyJoinRequests
                    join user in data.Users on request.user_id equals user.user_id
                    join profile in data.UserProfileRecords on user.user_id equals profile.user_id into profileJoin
                    from profile in profileJoin.DefaultIfEmpty()
                    where request.company_id == companyId && request.status == "Pending"
                    orderby request.requested_at descending
                    select new CompanyJoinRequestViewModel
                    {
                        RequestId = request.request_id,
                        UserId = user.user_id,
                        DisplayName = String.IsNullOrEmpty(profile.full_name) ? user.username : profile.full_name,
                        Email = user.email,
                        RequestedAt = request.requested_at
                    }).ToList();
        }

        private bool IsCompanyOwner(int companyId)
        {
            if (AuthRoleHelper.IsAdmin(User.Identity.Name))
                return true;

            var currentUser = GetCurrentUser();

            return currentUser != null && data.CompanyUsers.Any(item =>
                item.company_id == companyId &&
                item.user_id == currentUser.user_id &&
                item.role == "Owner");
        }

        private bool CanViewCompanyProfile(Company company)
        {
            if (company.public_company_profile)
                return true;

            if (AuthRoleHelper.IsAdmin(User.Identity.Name))
                return true;

            var currentUser = GetCurrentUser();

            return currentUser != null && data.CompanyUsers.Any(item =>
                item.company_id == company.company_id &&
                item.user_id == currentUser.user_id);
        }

        private bool CanViewCompanyJobs(Company company)
        {
            if (company.show_jobs_publicly)
                return true;

            if (AuthRoleHelper.IsAdmin(User.Identity.Name))
                return true;

            var currentUser = GetCurrentUser();

            return currentUser != null && data.CompanyUsers.Any(item =>
                item.company_id == company.company_id &&
                item.user_id == currentUser.user_id);
        }

        private List<CompanyListItemViewModel> SortCompanies(List<CompanyListItemViewModel> companies, string sort)
        {
            switch ((sort ?? "name").ToLowerInvariant())
            {
                case "star_desc":
                    return companies.OrderByDescending(company => company.StarCount).ThenBy(company => company.CompanyName).ToList();
                case "star_asc":
                    return companies.OrderBy(company => company.StarCount).ThenBy(company => company.CompanyName).ToList();
                case "name_desc":
                    return companies.OrderByDescending(company => company.CompanyName).ToList();
                default:
                    return companies.OrderBy(company => company.CompanyName).ToList();
            }
        }

        private bool Contains(string value, string keyword)
        {
            return !String.IsNullOrWhiteSpace(value) &&
                value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private User GetCurrentUser()
        {
            if (!Request.IsAuthenticated)
                return null;

            return data.Users.FirstOrDefault(user => user.username == User.Identity.Name);
        }
    }
}
