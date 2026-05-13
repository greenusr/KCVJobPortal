using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace JobPortalKCV.Models.ViewModel
{
    public class ProfileDetailsViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public bool CanEdit { get; set; }
        public bool IsCandidate { get; set; }
        public bool IsEmployer { get; set; }
        public int StarCount { get; set; }
        public bool IsStarredByCurrentUser { get; set; }
        public bool CanStar { get; set; }
        public bool CanViewStarredBy { get; set; }
        public bool CanInviteCandidate { get; set; }
        public string AvatarPath { get; set; }
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Phone { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        public string Address { get; set; }
        public string AboutMe { get; set; }
        public List<UserEducation> Educations { get; set; }
        public List<UserExperience> Experiences { get; set; }
        public List<UserProject> Projects { get; set; }
        public List<ProfileSkillViewModel> Skills { get; set; }
        public List<UserCV> CVs { get; set; }
        public List<Company> Companies { get; set; }
    }

    public class ProfileEditViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string AvatarPath { get; set; }
        public bool IsCandidate { get; set; }
        public bool IsEmployer { get; set; }

        [Required]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        [RegularExpression(@"^[0-9+\-\s().]{7,20}$", ErrorMessage = "Invalid profile data.")]
        public string Phone { get; set; }

        [Display(Name = "Location")]
        public int? LocationId { get; set; }

        public string Address { get; set; }

        [Display(Name = "About me")]
        public string AboutMe { get; set; }

        public HttpPostedFileBase Avatar { get; set; }
        public List<UserEducation> Educations { get; set; }
        public List<UserExperience> Experiences { get; set; }
        public List<UserProject> Projects { get; set; }
        public List<ProfileSkillViewModel> Skills { get; set; }
        public List<UserCV> CVs { get; set; }
        public List<Company> Companies { get; set; }
        public List<Location> Locations { get; set; }
    }

    public class ProfileSkillViewModel
    {
        public int UserSkillId { get; set; }
        public string SkillName { get; set; }
    }
}
