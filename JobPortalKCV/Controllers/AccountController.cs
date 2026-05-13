using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using JobPortalKCV.Models;
using JobPortalKCV.Models.ViewModel;
using JobPortalKCV.Services;

namespace JobPortalKCV.Controllers
{
    public class AccountController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var account = model.UserNameOrEmail.Trim();
            var user = data.Users.FirstOrDefault(u => u.email == account || u.username == account);

            if (user == null || !VerifyPassword(model.Password, user.password_hash))
            {
                AccountLogService.LogLogin(data, user == null ? (int?)null : user.user_id, false, "Invalid username/email or password.", Request);
                data.SubmitChanges();
                ModelState.AddModelError("", "Invalid username/email or password.");
                return View(model);
            }

            if (!user.is_active)
            {
                AccountLogService.LogLogin(data, user.user_id, false, "Your account has been disabled.", Request);
                data.SubmitChanges();
                ModelState.AddModelError("", "Your account has been disabled.");
                return View(model);
            }

            var verificationService = new UserVerificationService(data);

            if (!verificationService.IsUserVerified(user.user_id))
            {
                if (verificationService.CanResendOtp(user.user_id, UserVerificationService.RegisterType))
                {
                    var otp = CreateOtp();
                    verificationService.CreateOtp(user.user_id, UserVerificationService.RegisterType, otp);
                    SendEmail(user.email, "Verify your KCV Job Portal account", BuildOtpEmail(user, otp, "email verification"));
                    TempData["AuthMessage"] = "Your account is not verified. A new OTP has been sent to your email.";
                }
                else
                {
                    TempData["AuthMessage"] = "Your account is not verified. Please enter the OTP sent to your email.";
                }

                AccountLogService.LogLogin(data, user.user_id, false, "Account is not verified.", Request);
                data.SubmitChanges();
                return RedirectToAction("VerifyEmail", new { email = user.email });
            }

            FormsAuthentication.SetAuthCookie(user.username, model.RememberMe);
            AccountLogService.LogLogin(data, user.user_id, true, null, Request);
            AccountLogService.LogActivity(data, user.user_id, "Login", "User logged in.", Request);
            data.SubmitChanges();

            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Register()
        {
            return View();
        }

        public ActionResult RegisterCandidate()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterCandidate(RegisterCandidateViewModel model)
        {
            if (data.Users.Any(u => u.email == model.Email))
                ModelState.AddModelError("Email", "Email is already registered.");

            if (data.Users.Any(u => u.username == model.Username))
                ModelState.AddModelError("Username", "Username is already taken.");

            if (!ModelState.IsValid)
                return View(model);

            var user = CreateUser(model);

            data.Users.InsertOnSubmit(user);
            data.SubmitChanges();

            CreateProfileForUser(user, model.FirstName, model.LastName);
            AssignRole(user.user_id, "Candidate");

            SendVerificationEmail(user);

            return RedirectToAction("VerifyEmail", new { email = user.email });
        }

        public ActionResult RegisterEmployer()
        {
            LoadCompanySelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterEmployer(RegisterEmployerViewModel model)
        {
            if (model.UseExistingCompany && !model.ExistingCompanyId.HasValue)
                ModelState.AddModelError("ExistingCompanyId", "Please select a company.");

            if (!model.UseExistingCompany && String.IsNullOrWhiteSpace(model.CompanyName))
                ModelState.AddModelError("CompanyName", "Company name is required when creating a new company.");

            if (!model.UseExistingCompany && (model.CompanyLogo == null || model.CompanyLogo.ContentLength == 0))
                ModelState.AddModelError("CompanyLogo", "No file selected.");

            if (data.Users.Any(u => u.email == model.Email))
                ModelState.AddModelError("Email", "Email is already registered.");

            if (data.Users.Any(u => u.username == model.Username))
                ModelState.AddModelError("Username", "Username is already taken.");

            if (!ModelState.IsValid)
            {
                LoadCompanySelectList(model.ExistingCompanyId);
                return View(model);
            }

            if (!model.UseExistingCompany)
            {
                var logoResult = FileUploadService.SaveLogo(model.CompanyLogo, Server);

                if (!logoResult.Success)
                {
                    ModelState.AddModelError("CompanyLogo", logoResult.ErrorMessage);
                    LoadCompanySelectList(model.ExistingCompanyId);
                    return View(model);
                }

                model.SavedLogoPath = logoResult.FilePath;
            }

            var user = CreateUser(model);

            data.Users.InsertOnSubmit(user);
            data.SubmitChanges();

            CreateProfileForUser(user, model.FirstName, model.LastName);
            CreateCompanyForEmployer(user, model);
            SendVerificationEmail(user);

            if (model.UseExistingCompany)
                TempData["AuthMessage"] = "Registration successful. Please verify your email. Your company join request is pending approval.";

            return RedirectToAction("VerifyEmail", new { email = user.email });
        }

        public ActionResult VerifyEmail(string email)
        {
            SetOtpExpirationViewBag();
            return View(new VerifyEmailOtpViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyEmail(VerifyEmailOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetOtpExpirationViewBag();
                return View(model);
            }

            var user = data.Users.FirstOrDefault(u => u.email == model.Email);

            if (user == null)
            {
                SetOtpExpirationViewBag();
                ModelState.AddModelError("", "Invalid OTP code.");
                return View(model);
            }

            var result = new UserVerificationService(data).VerifyOtp(user.user_id, UserVerificationService.RegisterType, model.OtpCode);

            if (result == OtpValidationResult.Invalid)
            {
                SetOtpExpirationViewBag();
                ModelState.AddModelError("", "Invalid OTP");
                return View(model);
            }

            if (result == OtpValidationResult.Expired)
            {
                SetOtpExpirationViewBag();
                ModelState.AddModelError("", "OTP has expired. Please request a new one.");
                return View(model);
            }

            TempData["AuthMessage"] = "Your email has been verified. You can sign in now.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResendRegisterOtp(ResendOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("VerifyEmail", new { email = model.Email });

            var user = data.Users.FirstOrDefault(u => u.email == model.Email);

            if (user != null)
                ResendOtp(user, UserVerificationService.RegisterType, "Verify your KCV Job Portal account", "email verification");

            return RedirectToAction("VerifyEmail", new { email = model.Email });
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = data.Users.FirstOrDefault(u => u.email == model.Email);

            if (user != null)
            {
                var verificationService = new UserVerificationService(data);

                if (verificationService.CanResendOtp(user.user_id, UserVerificationService.ResetPasswordType))
                {
                    var otp = CreateOtp();
                    verificationService.CreateOtp(user.user_id, UserVerificationService.ResetPasswordType, otp);
                    SendEmail(user.email, "Reset your KCV Job Portal password", BuildOtpEmail(user, otp, "password reset"));
                }
            }

            TempData["AuthMessage"] = "If the email exists, a password reset OTP has been sent.";
            return RedirectToAction("ResetPassword", new { email = model.Email });
        }

        public ActionResult ResetPassword(string email)
        {
            SetOtpExpirationViewBag();
            return View(new ResetPasswordViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetOtpExpirationViewBag();
                return View(model);
            }

            var user = data.Users.FirstOrDefault(u => u.email == model.Email);

            if (user == null)
            {
                SetOtpExpirationViewBag();
                ModelState.AddModelError("", "Invalid OTP");
                return View(model);
            }

            var result = new UserVerificationService(data).VerifyOtp(user.user_id, UserVerificationService.ResetPasswordType, model.OtpCode);

            if (result == OtpValidationResult.Invalid)
            {
                SetOtpExpirationViewBag();
                ModelState.AddModelError("", "Invalid OTP");
                return View(model);
            }

            if (result == OtpValidationResult.Expired)
            {
                SetOtpExpirationViewBag();
                ModelState.AddModelError("", "OTP has expired. Please request a new one.");
                return View(model);
            }

            user.password_hash = HashPassword(model.Password);
            data.SubmitChanges();

            TempData["AuthMessage"] = "Your password has been reset. Please sign in.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResendResetPasswordOtp(ResendOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("ResetPassword", new { email = model.Email });

            var user = data.Users.FirstOrDefault(u => u.email == model.Email);

            if (user != null)
                ResendOtp(user, UserVerificationService.ResetPasswordType, "Reset your KCV Job Portal password", "password reset");

            return RedirectToAction("ResetPassword", new { email = model.Email });
        }

        [Authorize]
        public new ActionResult Profile()
        {
            return RedirectToAction("Me", "Profile");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadAvatar(HttpPostedFileBase avatar)
        {
            var user = data.Users.FirstOrDefault(u => u.username == User.Identity.Name);

            if (user == null)
                return HttpNotFound();

            var result = FileUploadService.SaveProfileAvatar(avatar, Server);

            if (!result.Success)
            {
                TempData["ProfileError"] = result.ErrorMessage;
                return RedirectToAction("Profile");
            }

            var profile = data.UserProfileRecords.FirstOrDefault(item => item.user_id == user.user_id);

            if (profile == null)
            {
                profile = new UserProfileRecord
                {
                    user_id = user.user_id,
                    full_name = user.username,
                    created_at = DateTime.Now
                };
                data.UserProfileRecords.InsertOnSubmit(profile);
            }

            profile.avatar_path = result.FilePath;
            profile.updated_at = DateTime.Now;
            data.SubmitChanges();

            TempData["ProfileMessage"] = "File uploaded successfully.";
            return RedirectToAction("Profile");
        }

        [Authorize]
        public ActionResult Logout()
        {
            var user = data.Users.FirstOrDefault(u => u.username == User.Identity.Name);

            if (user != null)
            {
                AccountLogService.LogActivity(data, user.user_id, "Logout", "User logged out.", Request);
                data.SubmitChanges();
            }

            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private User CreateUser(RegisterCandidateViewModel model)
        {
            return new User
            {
                username = model.Username.Trim(),
                email = model.Email.Trim(),
                password_hash = HashPassword(model.Password),
                is_active = true
            };
        }

        private void CreateProfileForUser(User user, string firstName, string lastName)
        {
            var fullName = ((firstName ?? "") + " " + (lastName ?? "")).Trim();

            data.UserProfileRecords.InsertOnSubmit(new UserProfileRecord
            {
                user_id = user.user_id,
                full_name = String.IsNullOrWhiteSpace(fullName) ? user.username : fullName,
                location_id = EnsureDefaultLocationId(),
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            });

            data.SubmitChanges();
        }

        private void SendVerificationEmail(User user)
        {
            var otp = CreateOtp();
            new UserVerificationService(data).CreateOtp(user.user_id, UserVerificationService.RegisterType, otp);

            SendEmail(user.email, "Verify your KCV Job Portal account", BuildOtpEmail(user, otp, "email verification"));

            TempData["AuthMessage"] = "Registration successful. Please check your email for the OTP code.";
        }

        private void ResendOtp(User user, string type, string subject, string purpose)
        {
            var verificationService = new UserVerificationService(data);

            if (!verificationService.CanResendOtp(user.user_id, type))
            {
                TempData["AuthMessage"] = "OTP has not expired yet. Please wait before requesting a new one.";
                return;
            }

            var otp = CreateOtp();
            verificationService.CreateOtp(user.user_id, type, otp);
            SendEmail(user.email, subject, BuildOtpEmail(user, otp, purpose));

            TempData["AuthMessage"] = "A new OTP has been sent to your email.";
        }

        private void CreateCompanyForEmployer(User user, RegisterEmployerViewModel model)
        {
            Company company;

            if (model.UseExistingCompany)
            {
                company = data.Companies.FirstOrDefault(c => c.company_id == model.ExistingCompanyId.Value);

                if (company == null)
                    throw new InvalidOperationException("Company not found.");
            }
            else
            {
                company = new Company
                {
                    company_name = model.CompanyName.Trim(),
                    industry = model.Industry,
                    website = model.Website,
                    description = model.Description,
                    contact_email = user.email,
                    public_company_profile = true,
                    show_jobs_publicly = true,
                    updated_at = DateTime.Now
                };

                company.logo_path = model.SavedLogoPath;

                data.Companies.InsertOnSubmit(company);
                data.SubmitChanges();
            }

            if (model.UseExistingCompany)
            {
                if (!data.CompanyJoinRequests.Any(request =>
                    request.user_id == user.user_id &&
                    request.company_id == company.company_id &&
                    request.status == "Pending") &&
                    !data.CompanyUsers.Any(cu => cu.user_id == user.user_id && cu.company_id == company.company_id))
                {
                    data.CompanyJoinRequests.InsertOnSubmit(new CompanyJoinRequest
                    {
                        user_id = user.user_id,
                        company_id = company.company_id,
                        status = "Pending"
                    });
                }
            }
            else if (!data.CompanyUsers.Any(cu => cu.user_id == user.user_id && cu.company_id == company.company_id))
            {
                data.CompanyUsers.InsertOnSubmit(new CompanyUser
                {
                    user_id = user.user_id,
                    company_id = company.company_id,
                    role = "Owner"
                });
            }

            AssignRole(user.user_id, "Employer");
            data.SubmitChanges();
        }

        private void AssignRole(int userId, string roleName)
        {
            var role = data.Roles.FirstOrDefault(r => r.role_name == roleName);

            if (role == null)
            {
                role = new Role
                {
                    role_name = roleName
                };

                data.Roles.InsertOnSubmit(role);
                data.SubmitChanges();
            }

            if (!data.UserRoles.Any(ur => ur.user_id == userId && ur.role_id == role.role_id))
            {
                data.UserRoles.InsertOnSubmit(new UserRole
                {
                    user_id = userId,
                    role_id = role.role_id
                });
            }
        }

        private void LoadCompanySelectList(int? selectedCompanyId = null)
        {
            ViewBag.ExistingCompanyId = new SelectList(data.Companies.OrderBy(c => c.company_name), "company_id", "company_name", selectedCompanyId);
        }

        private void SetOtpExpirationViewBag()
        {
            ViewBag.OtpExpirationSeconds = SystemSettingsService.GetOtpExpirationSeconds(data);
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

        private string CreateOtp()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var value = BitConverter.ToUInt32(bytes, 0) % 1000000;

                return value.ToString("D6");
            }
        }

        private string HashPassword(string password)
        {
            var salt = new byte[16];

            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                var hash = deriveBytes.GetBytes(32);
                return "PBKDF2$10000$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            if (String.IsNullOrWhiteSpace(storedHash))
                return false;

            var parts = storedHash.Split('$');

            if (parts.Length != 4 || parts[0] != "PBKDF2")
                return password == storedHash;

            var iterations = Int32.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);

            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                var actualHash = deriveBytes.GetBytes(expectedHash.Length);
                return SlowEquals(actualHash, expectedHash);
            }
        }

        private bool SlowEquals(byte[] a, byte[] b)
        {
            var diff = (uint)a.Length ^ (uint)b.Length;

            for (var i = 0; i < a.Length && i < b.Length; i++)
                diff |= (uint)(a[i] ^ b[i]);

            return diff == 0;
        }

        private void SendEmail(string to, string subject, string body)
        {
            using (var message = new MailMessage())
            {
                message.To.Add(to);
                message.Subject = subject;
                message.Body = body;
                message.SubjectEncoding = Encoding.UTF8;
                message.BodyEncoding = Encoding.UTF8;
                message.HeadersEncoding = Encoding.UTF8;
                message.From = new MailAddress(
                    ConfigurationManager.AppSettings["MailFrom"] ?? "noreply@jobportalkcv.local",
                    ConfigurationManager.AppSettings["MailFromDisplayName"] ?? "KCV Job Portal",
                    Encoding.UTF8);

                using (var client = new SmtpClient())
                    client.Send(message);
            }
        }

        private string BuildOtpEmail(User user, string otp, string purpose)
        {
            var displayName = GetDisplayName(user);
            var expirationSeconds = SystemSettingsService.GetOtpExpirationSeconds(data);
            var expirationMinutes = Math.Max(1, (int)Math.Ceiling(expirationSeconds / 60.0));

            var builder = new StringBuilder();
            builder.AppendLine("Hello " + displayName + ",");
            builder.AppendLine();
            builder.AppendLine("We received a request to complete " + purpose + " for your KCV Job Portal account.");
            builder.AppendLine("Please use the one-time password below to continue:");
            builder.AppendLine();
            builder.AppendLine("Verification code: " + otp);
            builder.AppendLine("This code will expire in approximately " + expirationMinutes + " minute(s).");
            builder.AppendLine();
            builder.AppendLine("For your security:");
            builder.AppendLine("- Do not share this code with anyone.");
            builder.AppendLine("- KCV Job Portal will never ask you to provide your password or OTP outside the website.");
            builder.AppendLine("- If you did not request this code, you can safely ignore this email.");
            builder.AppendLine();
            builder.AppendLine("After entering the code, you can continue using your account normally.");
            builder.AppendLine();
            builder.AppendLine("Best regards,");
            builder.AppendLine("KCV Job Portal Support Team");

            return builder.ToString();
        }

        private string GetDisplayName(User user)
        {
            var profileName = data.UserProfileRecords
                .Where(profile => profile.user_id == user.user_id)
                .Select(profile => profile.full_name)
                .FirstOrDefault();

            return String.IsNullOrWhiteSpace(profileName) ? user.username : profileName;
        }
    }
}
