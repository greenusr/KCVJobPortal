using System.Collections.Generic;

namespace JobPortalKCV.Models.ViewModel
{
    public class SearchResultsViewModel
    {
        public string Keyword { get; set; }
        public int? CategoryId { get; set; }
        public int? LocationId { get; set; }
        public int? SkillId { get; set; }
        public string Sort { get; set; }
        public string ResultType { get; set; }
        public List<JobViewModel> JobResults { get; set; }
        public List<UserSearchResultViewModel> UserResults { get; set; }
        public List<CompanyListItemViewModel> CompanyResults { get; set; }
        public List<JobCategory> Categories { get; set; }
        public List<Location> Locations { get; set; }
        public List<Skill> Skills { get; set; }
    }

    public class UserSearchResultViewModel
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public string LocationName { get; set; }
        public string Address { get; set; }
        public string AboutMe { get; set; }
        public int StarCount { get; set; }
        public bool IsStarredByCurrentUser { get; set; }
    }
}
