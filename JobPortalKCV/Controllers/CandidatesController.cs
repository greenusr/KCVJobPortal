using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using JobPortalKCV.Helpers;
using JobPortalKCV.Models;
using JobPortalKCV.Models.ViewModel;
using JobPortalKCV.Services;

namespace JobPortalKCV.Controllers
{
    [Authorize]
    public class CandidatesController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        public ActionResult Index(string keyword, int? locationId, int? skillId, string sort = "name", int page = 1)
        {
            if (!AuthRoleHelper.IsEmployer(User.Identity.Name) && !AuthRoleHelper.IsAdmin(User.Identity.Name))
                return new HttpStatusCodeResult(403);

            var candidates = BuildCandidates(keyword, locationId, skillId, sort);
            candidates = PaginationService.Paginate(
                candidates,
                page,
                SystemSettingsService.GetPaginationSize(data),
                out PaginationViewModel pagination,
                "Index",
                "Candidates",
                new { keyword, locationId, skillId, sort });

            ViewBag.Keyword = keyword;
            ViewBag.LocationId = locationId;
            ViewBag.SkillId = skillId;
            ViewBag.Sort = sort;
            ViewBag.TotalRecords = pagination.TotalRecords;
            ViewBag.Locations = new SelectList(data.Locations.OrderBy(location => location.city), "location_id", "city", locationId);
            ViewBag.Skills = new SelectList(data.Skills.OrderBy(skill => skill.skill_name), "skill_id", "skill_name", skillId);
            ViewBag.Pagination = pagination;

            return View(candidates);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private List<UserSearchResultViewModel> BuildCandidates(string keyword, int? locationId, int? skillId, string sort)
        {
            var users = (from user in data.Users
                         join userRole in data.UserRoles on user.user_id equals userRole.user_id
                         join role in data.Roles on userRole.role_id equals role.role_id
                         where role.role_name == "Candidate"
                         select user).ToList();
            var profiles = data.UserProfileRecords.ToList();
            var currentUser = GetCurrentUser();

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
                    RoleName = "Candidate",
                    LocationName = profile == null ? null : data.Locations.Where(location => location.location_id == profile.location_id).Select(location => location.city).FirstOrDefault(),
                    Address = profile?.address,
                    AboutMe = profile?.about_me,
                    StarCount = data.Stars.Count(star => star.target_type == "User" && star.target_id == user.user_id),
                    IsStarredByCurrentUser = currentUser != null && data.Stars.Any(star => star.user_id == currentUser.user_id && star.target_type == "User" && star.target_id == user.user_id)
                };
            }).ToList();

            return SortCandidates(results, sort);
        }

        private List<UserSearchResultViewModel> SortCandidates(List<UserSearchResultViewModel> candidates, string sort)
        {
            switch ((sort ?? "name").ToLowerInvariant())
            {
                case "star_desc":
                    return candidates.OrderByDescending(candidate => candidate.StarCount).ThenBy(candidate => candidate.DisplayName).ToList();
                case "star_asc":
                    return candidates.OrderBy(candidate => candidate.StarCount).ThenBy(candidate => candidate.DisplayName).ToList();
                case "name_desc":
                    return candidates.OrderByDescending(candidate => candidate.DisplayName).ToList();
                default:
                    return candidates.OrderBy(candidate => candidate.DisplayName).ToList();
            }
        }

        private User GetCurrentUser()
        {
            return data.Users.FirstOrDefault(user => user.username == User.Identity.Name);
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
    }
}
