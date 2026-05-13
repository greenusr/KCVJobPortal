using System.ComponentModel.DataAnnotations;
using System.Web;

namespace JobPortalKCV.Models.ViewModel
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email or username")]
        public string UserNameOrEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class RegisterCandidateViewModel
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }
    }

    public class RegisterEmployerViewModel : RegisterCandidateViewModel
    {
        [Display(Name = "Use existing company")]
        public bool UseExistingCompany { get; set; }

        [Display(Name = "Existing company")]
        public int? ExistingCompanyId { get; set; }

        [Display(Name = "Company name")]
        public string CompanyName { get; set; }

        public string Industry { get; set; }
        public string Website { get; set; }
        public string Description { get; set; }

        [Display(Name = "Company logo")]
        public HttpPostedFileBase CompanyLogo { get; set; }

        public string SavedLogoPath { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class VerifyEmailOtpViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        [Display(Name = "OTP code")]
        public string OtpCode { get; set; }
    }

    public class ResendOtpViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        [Display(Name = "OTP code")]
        public string OtpCode { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        [Display(Name = "New password")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }
    }

    public class ProfileViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string AvatarPath { get; set; }
    }
}
