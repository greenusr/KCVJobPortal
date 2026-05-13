using System;
using System.Linq;
using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public class UserVerificationService
    {
        public const string RegisterType = "Register";
        public const string ResetPasswordType = "ResetPassword";

        private readonly JobPortalDataContext data;

        public UserVerificationService(JobPortalDataContext data)
        {
            this.data = data;
            EnsureTableExists();
        }

        public UserVerification CreateOtp(int userId, string type, string otpCode)
        {
            var verification = new UserVerification
            {
                user_id = userId,
                otp_code = otpCode,
                type = type,
                is_verified = false,
                created_at = DateTime.Now,
                expired_at = DateTime.Now.AddSeconds(SystemSettingsService.GetOtpExpirationSeconds(data))
            };

            data.UserVerifications.InsertOnSubmit(verification);
            data.SubmitChanges();

            return verification;
        }

        public OtpValidationResult VerifyOtp(int userId, string type, string otpCode)
        {
            var latest = GetLatest(userId, type);

            if (latest == null)
                return OtpValidationResult.Invalid;

            if (latest.expired_at < DateTime.Now)
                return OtpValidationResult.Expired;

            if (latest.is_verified)
                return OtpValidationResult.Invalid;

            if (latest.otp_code != otpCode)
                return OtpValidationResult.Invalid;

            latest.is_verified = true;
            data.SubmitChanges();

            return OtpValidationResult.Valid;
        }

        public bool CanResendOtp(int userId, string type)
        {
            var latest = GetLatest(userId, type);

            return latest == null || latest.expired_at < DateTime.Now;
        }

        public bool IsUserVerified(int userId)
        {
            return data.UserVerifications.Any(v =>
                v.user_id == userId &&
                v.type == RegisterType &&
                v.is_verified);
        }

        private UserVerification GetLatest(int userId, string type)
        {
            return data.UserVerifications
                .Where(v => v.user_id == userId && v.type == type)
                .OrderByDescending(v => v.created_at)
                .ThenByDescending(v => v.verification_id)
                .FirstOrDefault();
        }

        private void EnsureTableExists()
        {
            data.ExecuteCommand(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserVerifications' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.UserVerifications
    (
        verification_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        otp_code NVARCHAR(6) NOT NULL,
        type NVARCHAR(50) NOT NULL,
        is_verified BIT NOT NULL CONSTRAINT DF_UserVerifications_is_verified DEFAULT(0),
        expired_at DATETIME NOT NULL,
        created_at DATETIME NOT NULL CONSTRAINT DF_UserVerifications_created_at DEFAULT(GETDATE()),
        CONSTRAINT FK_UserVerifications_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id)
    );
END");
        }
    }

    public enum OtpValidationResult
    {
        Valid,
        Invalid,
        Expired
    }
}
