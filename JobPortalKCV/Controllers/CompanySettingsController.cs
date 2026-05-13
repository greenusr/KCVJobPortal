using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using JobPortalKCV.Helpers;
using JobPortalKCV.Models;
using JobPortalKCV.Models.ViewModel;
using JobPortalKCV.Services;

namespace JobPortalKCV.Controllers
{
    [Authorize]
    public class CompanySettingsController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        public ActionResult Index(int page = 1)
        {
            if (!AuthRoleHelper.IsAdmin(User.Identity.Name) && !AuthRoleHelper.IsEmployer(User.Identity.Name))
                return new HttpStatusCodeResult(403, "You are not allowed to manage companies.");

            var companies = GetManageableCompanies();
            var models = companies
                .Select(item => new CompanyListItemViewModel
                {
                    CompanyId = item.Company.company_id,
                    CompanyName = item.Company.company_name,
                    Industry = item.Company.industry,
                    Website = item.Company.website,
                    ContactEmail = item.Company.contact_email,
                    ContactPhone = item.Company.contact_phone,
                    Address = item.Company.address,
                    LogoPath = SystemSettingsService.GetCompanyLogoOrDefault(data, item.Company.logo_path),
                    Description = item.Company.description,
                    PublicCompanyProfile = item.Company.public_company_profile,
                    ShowJobsPublicly = item.Company.show_jobs_publicly,
                    CanManage = AuthRoleHelper.CanManageCompany(User.Identity.Name, item.Company.company_id),
                    CanOwn = IsCompanyOwner(item.Company.company_id),
                    MembershipRole = item.Role,
                    JobCount = data.Jobs.Count(job => job.company_id == item.Company.company_id),
                    PendingRequestCount = data.CompanyJoinRequests.Count(request => request.company_id == item.Company.company_id && request.status == "Pending")
                })
                .OrderByDescending(item => item.CanOwn)
                .ThenBy(item => item.CompanyName)
                .ToList();

            models = PaginationService.Paginate(
                models,
                page,
                SystemSettingsService.GetPaginationSize(data),
                out PaginationViewModel pagination,
                "Index",
                "CompanySettings");

            ViewBag.Pagination = pagination;
            ViewBag.TotalRecords = pagination.TotalRecords;

            return View(models);
        }

        public ActionResult Settings(int? id, string returnUrl = null)
        {
            var company = ResolveCompany(id);

            if (company == null || !CanUpdateCompany(company.company_id))
                return new HttpStatusCodeResult(403, "You are not allowed to update this company.");

            ViewBag.ReturnUrl = returnUrl;
            return View(ToViewModel(company));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Settings(CompanySettingsViewModel model, HttpPostedFileBase logo, string returnUrl = null)
        {
            var company = data.Companies.FirstOrDefault(item => item.company_id == model.CompanyId);

            if (company == null || !CanUpdateCompany(company.company_id))
                return new HttpStatusCodeResult(403, "You are not allowed to update this company.");

            ValidateSettings(model, company, logo);

            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(ToViewModel(company, model));
            }

            var logoResult = SaveLogoIfProvided(logo);

            if (!logoResult.Success)
            {
                ModelState.AddModelError("LogoPath", logoResult.ErrorMessage);
                ViewBag.ReturnUrl = returnUrl;
                return View(ToViewModel(company, model));
            }

            company.company_name = model.CompanyName.Trim();
            company.industry = model.Industry;
            company.description = model.Description;
            company.website = model.Website;
            company.contact_email = model.ContactEmail;
            company.contact_phone = model.ContactPhone;
            company.address = model.Address;
            company.public_company_profile = model.PublicCompanyProfile;
            company.show_jobs_publicly = model.ShowJobsPublicly;
            company.updated_at = DateTime.Now;

            if (!String.IsNullOrWhiteSpace(logoResult.FilePath))
            {
                company.logo_path = logoResult.FilePath;
                TempData["CompanySettingsMessage"] = "Company logo updated successfully.";
            }
            else
            {
                TempData["CompanySettingsMessage"] = "Company settings updated successfully.";
            }

            data.SubmitChanges();
            return RedirectToAction("Settings", new { id = company.company_id, returnUrl = returnUrl });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private Company ResolveCompany(int? id)
        {
            if (id.HasValue)
                return data.Companies.FirstOrDefault(company => company.company_id == id.Value);

            var currentUser = GetCurrentUser();

            if (currentUser == null)
                return null;

            if (AuthRoleHelper.IsAdmin(User.Identity.Name))
                return data.Companies.OrderBy(company => company.company_name).FirstOrDefault();

            return (from company in data.Companies
                    join companyUser in data.CompanyUsers on company.company_id equals companyUser.company_id
                    where companyUser.user_id == currentUser.user_id && companyUser.role == "Owner"
                    orderby company.company_name
                    select company).FirstOrDefault();
        }

        private bool CanUpdateCompany(int companyId)
        {
            if (AuthRoleHelper.IsAdmin(User.Identity.Name))
                return true;

            return AuthRoleHelper.CanManageCompany(User.Identity.Name, companyId);
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

        private List<CompanyManagementRow> GetManageableCompanies()
        {
            if (AuthRoleHelper.IsAdmin(User.Identity.Name))
            {
                return data.Companies
                    .OrderBy(company => company.company_name)
                    .ToList()
                    .Select(company => new CompanyManagementRow
                    {
                        Company = company,
                        Role = "Admin"
                    })
                    .ToList();
            }

            var currentUser = GetCurrentUser();

            if (currentUser == null)
                return new List<CompanyManagementRow>();

            return (from companyUser in data.CompanyUsers
                    join company in data.Companies on companyUser.company_id equals company.company_id
                    where companyUser.user_id == currentUser.user_id
                    orderby company.company_name
                    select new CompanyManagementRow
                    {
                        Company = company,
                        Role = companyUser.role
                    }).ToList();
        }

        private void ValidateSettings(CompanySettingsViewModel model, Company company, HttpPostedFileBase logo)
        {
            if (String.IsNullOrWhiteSpace(model.CompanyName))
                ModelState.AddModelError("CompanyName", "Company name is required.");

            if (!String.IsNullOrWhiteSpace(model.ContactEmail) && !new EmailAddressAttribute().IsValid(model.ContactEmail))
                ModelState.AddModelError("ContactEmail", "Invalid email address.");

            if (!String.IsNullOrWhiteSpace(model.Website) && !Uri.IsWellFormedUriString(EnsureAbsoluteUrl(model.Website), UriKind.Absolute))
                ModelState.AddModelError("Website", "Invalid website URL.");

            if (!String.IsNullOrWhiteSpace(model.ContactPhone) && !Regex.IsMatch(model.ContactPhone, @"^[0-9+\-\s().]{7,20}$"))
                ModelState.AddModelError("ContactPhone", "Something went wrong. Please try again.");

            if (SystemSettingsService.RequiresCompanyLogoToPostJob(data) &&
                String.IsNullOrWhiteSpace(company.logo_path) &&
                (logo == null || logo.ContentLength == 0))
            {
                ModelState.AddModelError("LogoPath", "Company logo is required.");
            }
        }

        private FileUploadResult SaveLogoIfProvided(HttpPostedFileBase logo)
        {
            if (logo == null || logo.ContentLength == 0)
                return new FileUploadResult { Success = true };

            return FileUploadService.SaveCompanyLogo(logo, Server);
        }

        private string EnsureAbsoluteUrl(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                return url;

            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? url
                : "https://" + url;
        }

        private CompanySettingsViewModel ToViewModel(Company company, CompanySettingsViewModel posted = null)
        {
            var settings = SystemSettingsService.GetSettings(data);

            if (posted != null)
            {
                posted.LogoPath = SystemSettingsService.GetCompanyLogoOrDefault(data, company.logo_path);
                posted.AllowedImageTypes = settings.allowed_image_types;
                posted.LogoAcceptTypes = BuildAcceptTypes(settings.allowed_image_types);
                return posted;
            }

            return new CompanySettingsViewModel
            {
                CompanyId = company.company_id,
                CompanyName = company.company_name,
                Industry = company.industry,
                Description = company.description,
                Website = company.website,
                ContactEmail = company.contact_email,
                ContactPhone = company.contact_phone,
                Address = company.address,
                LogoPath = SystemSettingsService.GetCompanyLogoOrDefault(data, company.logo_path),
                AllowedImageTypes = settings.allowed_image_types,
                LogoAcceptTypes = BuildAcceptTypes(settings.allowed_image_types),
                PublicCompanyProfile = company.public_company_profile,
                ShowJobsPublicly = company.show_jobs_publicly
            };
        }

        private string BuildAcceptTypes(string allowedTypes)
        {
            var extensions = (allowedTypes ?? SystemSettingsService.DefaultAllowedImages)
                .Split(',')
                .Select(type => "." + type.Trim().TrimStart('.').ToLowerInvariant())
                .Where(type => type.Length > 1)
                .Distinct()
                .ToList();

            return extensions.Any() ? String.Join(",", extensions) : "image/*";
        }

        private User GetCurrentUser()
        {
            return data.Users.FirstOrDefault(user => user.username == User.Identity.Name);
        }

        private class CompanyManagementRow
        {
            public Company Company { get; set; }
            public string Role { get; set; }
        }
    }
}
