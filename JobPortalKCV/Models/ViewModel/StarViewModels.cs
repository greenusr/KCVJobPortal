using System;
using System.Collections.Generic;

namespace JobPortalKCV.Models.ViewModel
{
    public class SavedItemsViewModel
    {
        public List<SavedUserViewModel> Users { get; set; }
        public List<SavedJobViewModel> Jobs { get; set; }
        public List<SavedCompanyViewModel> Companies { get; set; }
    }

    public class SavedUserViewModel
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public int StarCount { get; set; }
    }

    public class SavedJobViewModel
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; }
        public string CompanyName { get; set; }
        public int StarCount { get; set; }
    }

    public class SavedCompanyViewModel
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string Industry { get; set; }
        public int StarCount { get; set; }
    }

    public class StarredByViewModel
    {
        public int TargetId { get; set; }
        public string TargetType { get; set; }
        public string TargetName { get; set; }
        public List<StarUserViewModel> Users { get; set; }
    }

    public class StarUserViewModel
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
