using System;
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
    public class ProfileController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        [Authorize]
        public ActionResult Me()
        {
            var user = GetCurrentUser();

            if (user == null)
                return HttpNotFound();

            return RedirectToAction("View", new { id = user.user_id });
        }

        [AllowAnonymous]
        [ActionName("View")]
        public ActionResult ViewProfile(int id)
        {
            var user = data.Users.FirstOrDefault(item => item.user_id == id);

            if (user == null)
                return HttpNotFound();

            if (!CanViewProfile(user.user_id))
                return new HttpStatusCodeResult(403, "You are not allowed to access this page.");

            EnsureProfile(user);
            var currentUser = GetCurrentUser();
            if (currentUser != null)
            {
                AccountLogService.LogActivity(data, currentUser.user_id, "ViewDetail", "Viewed user profile: " + user.username, Request, relatedId: user.user_id, relatedType: "User");
                data.SubmitChanges();
            }

            return View(BuildDetailsModel(user));
        }

        [Authorize]
        public ActionResult Edit(int id)
        {
            var user = data.Users.FirstOrDefault(item => item.user_id == id);

            if (user == null)
                return HttpNotFound();

            if (!CanEditProfile(user.user_id))
            {
                TempData["ProfileError"] = "You are not allowed to edit this profile.";
                return RedirectToAction("View", new { id = id });
            }

            EnsureProfile(user);
            return View(BuildEditModel(user));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, ProfileEditViewModel model, HttpPostedFileBase avatar, string returnUrl = null)
        {
            var user = data.Users.FirstOrDefault(item => item.user_id == id);

            if (user == null)
                return HttpNotFound();

            if (!CanEditProfile(user.user_id))
            {
                TempData["ProfileError"] = "You are not allowed to edit this profile.";
                return RedirectToAction("View", new { id = id });
            }

            if (String.IsNullOrWhiteSpace(model.FullName))
                ModelState.AddModelError("FullName", "Invalid profile data.");

            if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date > DateTime.Today)
                ModelState.AddModelError("DateOfBirth", "Invalid profile data.");

            if (!String.IsNullOrWhiteSpace(model.Phone) && !Regex.IsMatch(model.Phone, @"^[0-9+\-\s().]{7,20}$"))
                ModelState.AddModelError("Phone", "Invalid profile data.");

            FileUploadResult avatarResult = null;

            if (avatar != null && avatar.ContentLength > 0)
            {
                avatarResult = FileUploadService.SaveProfileAvatar(avatar, Server);

                if (!avatarResult.Success)
                    ModelState.AddModelError("Avatar", avatarResult.ErrorMessage);
            }

            if (!ModelState.IsValid)
            {
                TempData["ProfileError"] = "Invalid profile data.";
                return View(BuildEditModel(user, model));
            }

            try
            {
                var profile = EnsureProfile(user);
                profile.full_name = model.FullName.Trim();
                profile.date_of_birth = model.DateOfBirth;
                profile.phone = model.Phone;
                profile.location_id = model.LocationId ?? EnsureDefaultLocationId();
                profile.address = model.Address;
                profile.about_me = model.AboutMe;
                profile.updated_at = DateTime.Now;

                if (avatarResult != null && avatarResult.Success)
                {
                    profile.avatar_path = avatarResult.FilePath;
                }

                data.SubmitChanges();
                TempData["ProfileMessage"] = "Profile updated successfully.";
                return RedirectToProfileContext(user.user_id, returnUrl);
            }
            catch
            {
                TempData["ProfileError"] = "Something went wrong. Please try again.";
                return View(BuildEditModel(user, model));
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddEducation(int userId, UserEducation education)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            education.user_id = userId;
            data.UserEducations.InsertOnSubmit(education);
            data.SubmitChanges();
            TempData["ProfileMessage"] = "Education added successfully.";
            return RedirectToAction("Edit", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateEducation(int userId, UserEducation education)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            var item = data.UserEducations.FirstOrDefault(e => e.education_id == education.education_id && e.user_id == userId);

            if (item != null)
            {
                item.school_name = education.school_name;
                item.degree = education.degree;
                item.field_of_study = education.field_of_study;
                item.start_date = education.start_date;
                item.end_date = education.end_date;
                item.description = education.description;
                data.SubmitChanges();
            }

            return RedirectToAction("Edit", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteEducation(int userId, int id)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            var item = data.UserEducations.FirstOrDefault(e => e.education_id == id && e.user_id == userId);

            if (item != null)
            {
                data.UserEducations.DeleteOnSubmit(item);
                data.SubmitChanges();
            }

            return RedirectToAction("Edit", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddExperience(int userId, UserExperience experience)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            experience.user_id = userId;
            data.UserExperiences.InsertOnSubmit(experience);
            data.SubmitChanges();
            TempData["ProfileMessage"] = "Experience added successfully.";
            return RedirectToAction("Edit", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateExperience(int userId, UserExperience experience)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            var item = data.UserExperiences.FirstOrDefault(e => e.experience_id == experience.experience_id && e.user_id == userId);

            if (item != null)
            {
                item.company_name = experience.company_name;
                item.position = experience.position;
                item.start_date = experience.start_date;
                item.end_date = experience.end_date;
                item.description = experience.description;
                data.SubmitChanges();
            }

            return RedirectToAction("Edit", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteExperience(int userId, int id)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            var item = data.UserExperiences.FirstOrDefault(e => e.experience_id == id && e.user_id == userId);

            if (item != null)
            {
                data.UserExperiences.DeleteOnSubmit(item);
                data.SubmitChanges();
            }

            return RedirectToAction("Edit", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddProject(int userId, UserProject project)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            project.user_id = userId;
            data.UserProjects.InsertOnSubmit(project);
            data.SubmitChanges();
            TempData["ProfileMessage"] = "Project added successfully.";
            return RedirectToAction("Edit", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProject(int userId, UserProject project)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            var item = data.UserProjects.FirstOrDefault(p => p.project_id == project.project_id && p.user_id == userId);

            if (item != null)
            {
                item.project_name = project.project_name;
                item.description = project.description;
                item.project_url = project.project_url;
                item.start_date = project.start_date;
                item.end_date = project.end_date;
                data.SubmitChanges();
            }

            return RedirectToAction("Edit", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProject(int userId, int id)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            var item = data.UserProjects.FirstOrDefault(p => p.project_id == id && p.user_id == userId);

            if (item != null)
            {
                data.UserProjects.DeleteOnSubmit(item);
                data.SubmitChanges();
            }

            return RedirectToAction("Edit", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddSkill(int userId, string skillName)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            if (!String.IsNullOrWhiteSpace(skillName))
            {
                var skill = FindOrCreateSkill(skillName);

                if (!data.UserSkills.Any(userSkill => userSkill.user_id == userId && userSkill.skill_id == skill.skill_id))
                {
                    data.UserSkills.InsertOnSubmit(new UserSkill
                    {
                        user_id = userId,
                        skill_id = skill.skill_id
                    });
                    data.SubmitChanges();
                }

                TempData["ProfileMessage"] = "Skill added successfully.";
            }

            return RedirectToAction("Edit", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateSkill(int userId, int id, string skillName)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            var item = data.UserSkills.FirstOrDefault(s => s.user_skill_id == id && s.user_id == userId);

            if (item != null && !String.IsNullOrWhiteSpace(skillName))
            {
                var skill = FindOrCreateSkill(skillName);
                item.skill_id = skill.skill_id;
                data.SubmitChanges();
            }

            return RedirectToAction("Edit", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteSkill(int userId, int id)
        {
            if (!CanEditProfile(userId))
                return NotAllowed(userId);

            var item = data.UserSkills.FirstOrDefault(s => s.user_skill_id == id && s.user_id == userId);

            if (item != null)
            {
                data.UserSkills.DeleteOnSubmit(item);
                data.SubmitChanges();
            }

            return RedirectToAction("Edit", new { id = userId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private ActionResult NotAllowed(int userId)
        {
            TempData["ProfileError"] = "You are not allowed to edit this profile.";
            return RedirectToAction("View", new { id = userId });
        }

        private ActionResult RedirectToProfileContext(int userId, string returnUrl)
        {
            if (!String.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("View", new { id = userId });
        }

        private bool CanEditProfile(int userId)
        {
            var currentUser = GetCurrentUser();
            return currentUser != null && (currentUser.user_id == userId || AuthRoleHelper.IsAdmin(User.Identity.Name));
        }

        private bool CanViewProfile(int userId)
        {
            if (CanEditProfile(userId))
                return true;

            var settings = data.UserSettings.FirstOrDefault(item => item.user_id == userId);
            return settings == null || settings.public_profile_enabled;
        }

        private User GetCurrentUser()
        {
            return data.Users.FirstOrDefault(user => user.username == User.Identity.Name);
        }

        private UserProfileRecord EnsureProfile(User user)
        {
            var profile = data.UserProfileRecords.FirstOrDefault(item => item.user_id == user.user_id);

            if (profile != null)
                return profile;

            profile = new UserProfileRecord
            {
                user_id = user.user_id,
                full_name = user.username,
                created_at = DateTime.Now,
                updated_at = DateTime.Now,
                location_id = EnsureDefaultLocationId()
            };

            data.UserProfileRecords.InsertOnSubmit(profile);
            data.SubmitChanges();

            return profile;
        }

        private int EnsureDefaultLocationId()
        {
            var existingLocationId = data.Locations
                .OrderBy(item => item.location_id)
                .Select(item => (int?)item.location_id)
                .FirstOrDefault();

            if (existingLocationId.HasValue)
                return existingLocationId.Value;

            var defaultLocation = new Location
            {
                country = "Unknown",
                city = "Unknown"
            };

            data.Locations.InsertOnSubmit(defaultLocation);
            data.SubmitChanges();

            return defaultLocation.location_id;
        }

        private ProfileDetailsViewModel BuildDetailsModel(User user)
        {
            var profile = data.UserProfileRecords.FirstOrDefault(item => item.user_id == user.user_id);

            return new ProfileDetailsViewModel
            {
                UserId = user.user_id,
                Username = user.username,
                Email = user.email,
                RoleName = GetRoleName(user.user_id),
                CanEdit = CanEditProfile(user.user_id),
                IsCandidate = IsUserInRole(user.user_id, "Candidate"),
                IsEmployer = IsUserInRole(user.user_id, "Employer"),
                StarCount = GetStarCount("User", user.user_id),
                IsStarredByCurrentUser = IsStarredByCurrentUser("User", user.user_id),
                CanStar = CanStarUser(user.user_id),
                CanViewStarredBy = CanEditProfile(user.user_id),
                CanInviteCandidate = CanInviteCandidate(user.user_id),
                AvatarPath = FirstNotEmpty(profile?.avatar_path, SystemSettingsService.GetDefaultUserAvatarPath(data)),
                FullName = profile?.full_name,
                DateOfBirth = profile?.date_of_birth,
                Phone = profile?.phone,
                LocationId = profile?.location_id,
                LocationName = GetLocationName(profile?.location_id),
                Address = profile?.address,
                AboutMe = profile?.about_me,
                Educations = data.UserEducations.Where(item => item.user_id == user.user_id).OrderByDescending(item => item.start_date).ToList(),
                Experiences = data.UserExperiences.Where(item => item.user_id == user.user_id).OrderByDescending(item => item.start_date).ToList(),
                Projects = data.UserProjects.Where(item => item.user_id == user.user_id).OrderByDescending(item => item.start_date).ToList(),
                Skills = GetProfileSkills(user.user_id),
                CVs = data.UserCVs.Where(item => item.user_id == user.user_id).OrderByDescending(item => item.is_default).ThenByDescending(item => item.created_at).ToList(),
                Companies = GetCompanies(user.user_id)
            };
        }

        private ProfileEditViewModel BuildEditModel(User user, ProfileEditViewModel postedModel = null)
        {
            var profile = data.UserProfileRecords.FirstOrDefault(item => item.user_id == user.user_id);

            return new ProfileEditViewModel
            {
                UserId = user.user_id,
                Username = user.username,
                Email = user.email,
                AvatarPath = FirstNotEmpty(profile?.avatar_path, SystemSettingsService.GetDefaultUserAvatarPath(data)),
                IsCandidate = IsUserInRole(user.user_id, "Candidate"),
                IsEmployer = IsUserInRole(user.user_id, "Employer"),
                FullName = postedModel?.FullName ?? profile?.full_name,
                DateOfBirth = postedModel?.DateOfBirth ?? profile?.date_of_birth,
                Phone = postedModel?.Phone ?? profile?.phone,
                LocationId = postedModel?.LocationId ?? profile?.location_id,
                Address = postedModel?.Address ?? profile?.address,
                AboutMe = postedModel?.AboutMe ?? profile?.about_me,
                Educations = data.UserEducations.Where(item => item.user_id == user.user_id).OrderByDescending(item => item.start_date).ToList(),
                Experiences = data.UserExperiences.Where(item => item.user_id == user.user_id).OrderByDescending(item => item.start_date).ToList(),
                Projects = data.UserProjects.Where(item => item.user_id == user.user_id).OrderByDescending(item => item.start_date).ToList(),
                Skills = GetProfileSkills(user.user_id),
                CVs = data.UserCVs.Where(item => item.user_id == user.user_id).OrderByDescending(item => item.is_default).ThenByDescending(item => item.created_at).ToList(),
                Companies = GetCompanies(user.user_id),
                Locations = data.Locations.OrderBy(location => location.city).ToList()
            };
        }

        private bool IsUserInRole(int userId, string roleName)
        {
            return data.UserRoles.Any(userRole => userRole.user_id == userId && userRole.Role.role_name == roleName);
        }

        private Skill FindOrCreateSkill(string skillName)
        {
            var normalizedName = skillName.Trim();
            var skill = data.Skills.FirstOrDefault(item => item.skill_name.ToLower() == normalizedName.ToLower());

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

        private System.Collections.Generic.List<ProfileSkillViewModel> GetProfileSkills(int userId)
        {
            return (from userSkill in data.UserSkills
                    join skill in data.Skills on userSkill.skill_id equals skill.skill_id
                    where userSkill.user_id == userId
                    orderby skill.skill_name
                    select new ProfileSkillViewModel
                    {
                        UserSkillId = userSkill.user_skill_id,
                        SkillName = skill.skill_name
                    }).ToList();
        }

        private string GetRoleName(int userId)
        {
            return data.UserRoles
                .Where(userRole => userRole.user_id == userId)
                .Select(userRole => userRole.Role.role_name)
                .FirstOrDefault();
        }

        private System.Collections.Generic.List<Company> GetCompanies(int userId)
        {
            return (from company in data.Companies
                    join companyUser in data.CompanyUsers on company.company_id equals companyUser.company_id
                    where companyUser.user_id == userId
                    orderby company.company_name
                    select company).ToList();
        }

        private string GetLocationName(int? locationId)
        {
            if (!locationId.HasValue)
                return null;

            return data.Locations
                .Where(location => location.location_id == locationId.Value)
                .Select(location => location.city)
                .FirstOrDefault();
        }

        private string FirstNotEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !String.IsNullOrWhiteSpace(value));
        }

        private int GetStarCount(string targetType, int targetId)
        {
            return data.Stars.Count(star => star.target_type == targetType && star.target_id == targetId);
        }

        private bool IsStarredByCurrentUser(string targetType, int targetId)
        {
            var currentUser = GetCurrentUser();

            return currentUser != null &&
                data.Stars.Any(star => star.user_id == currentUser.user_id && star.target_type == targetType && star.target_id == targetId);
        }

        private bool CanStarUser(int userId)
        {
            var currentUser = GetCurrentUser();
            return currentUser != null && currentUser.user_id != userId;
        }

        private bool CanInviteCandidate(int candidateId)
        {
            var currentUser = GetCurrentUser();

            return currentUser != null &&
                currentUser.user_id != candidateId &&
                AuthRoleHelper.IsEmployer(User.Identity.Name) &&
                IsUserInRole(candidateId, "Candidate");
        }
    }
}
