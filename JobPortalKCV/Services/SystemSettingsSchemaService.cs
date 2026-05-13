using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class SystemSettingsSchemaService
    {
        public static void EnsureSchema()
        {
            using (var data = new JobPortalDataContext())
            {
                data.ExecuteCommand(@"
IF OBJECT_ID('dbo.SystemSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSettings
    (
        setting_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        site_name NVARCHAR(150) NOT NULL,
        site_logo_path NVARCHAR(255) NULL,
        default_user_avatar_path NVARCHAR(255) NULL,
        default_company_logo_path NVARCHAR(255) NULL,
        max_cv_upload_size_mb INT NOT NULL CONSTRAINT DF_SystemSettings_max_cv DEFAULT(20),
        max_avatar_upload_size_mb INT NOT NULL CONSTRAINT DF_SystemSettings_max_avatar DEFAULT(20),
        max_logo_upload_size_mb INT NOT NULL CONSTRAINT DF_SystemSettings_max_logo DEFAULT(20),
        allowed_image_types NVARCHAR(255) NOT NULL CONSTRAINT DF_SystemSettings_allowed_images DEFAULT('jpg,jpeg,png'),
        allowed_cv_types NVARCHAR(255) NOT NULL CONSTRAINT DF_SystemSettings_allowed_cv DEFAULT('pdf,doc,docx'),
        otp_expiration_seconds INT NOT NULL CONSTRAINT DF_SystemSettings_otp DEFAULT(90),
        default_pagination_size INT NOT NULL CONSTRAINT DF_SystemSettings_pagination DEFAULT(10),
        maintenance_mode BIT NOT NULL CONSTRAINT DF_SystemSettings_maintenance DEFAULT(0),
        require_company_logo_to_post_job BIT NOT NULL CONSTRAINT DF_SystemSettings_require_logo DEFAULT(1),
        auto_close_expired_jobs BIT NOT NULL CONSTRAINT DF_SystemSettings_auto_close DEFAULT(0),
        default_job_expiration_days INT NOT NULL CONSTRAINT DF_SystemSettings_job_expiration DEFAULT(30),
        created_at DATETIME NOT NULL CONSTRAINT DF_SystemSettings_created_at DEFAULT(GETDATE()),
        updated_at DATETIME NULL
    );
END

IF COL_LENGTH('dbo.SystemSettings', 'site_name') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD site_name NVARCHAR(150) NOT NULL CONSTRAINT DF_SystemSettings_site_name DEFAULT('Job Portal');
END
IF COL_LENGTH('dbo.SystemSettings', 'site_logo_path') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD site_logo_path NVARCHAR(255) NULL;
END
IF COL_LENGTH('dbo.SystemSettings', 'default_user_avatar_path') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD default_user_avatar_path NVARCHAR(255) NULL;
END
IF COL_LENGTH('dbo.SystemSettings', 'default_company_logo_path') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD default_company_logo_path NVARCHAR(255) NULL;
END
IF COL_LENGTH('dbo.SystemSettings', 'max_cv_upload_size_mb') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD max_cv_upload_size_mb INT NOT NULL CONSTRAINT DF_SystemSettings_max_cv DEFAULT(20);
END
IF COL_LENGTH('dbo.SystemSettings', 'max_avatar_upload_size_mb') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD max_avatar_upload_size_mb INT NOT NULL CONSTRAINT DF_SystemSettings_max_avatar DEFAULT(20);
END
IF COL_LENGTH('dbo.SystemSettings', 'max_logo_upload_size_mb') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD max_logo_upload_size_mb INT NOT NULL CONSTRAINT DF_SystemSettings_max_logo DEFAULT(20);
END
IF COL_LENGTH('dbo.SystemSettings', 'allowed_image_types') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD allowed_image_types NVARCHAR(255) NOT NULL CONSTRAINT DF_SystemSettings_allowed_images DEFAULT('jpg,jpeg,png');
END
IF COL_LENGTH('dbo.SystemSettings', 'allowed_cv_types') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD allowed_cv_types NVARCHAR(255) NOT NULL CONSTRAINT DF_SystemSettings_allowed_cv DEFAULT('pdf,doc,docx');
END
IF COL_LENGTH('dbo.SystemSettings', 'otp_expiration_seconds') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD otp_expiration_seconds INT NOT NULL CONSTRAINT DF_SystemSettings_otp DEFAULT(90);
END
IF COL_LENGTH('dbo.SystemSettings', 'default_pagination_size') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD default_pagination_size INT NOT NULL CONSTRAINT DF_SystemSettings_pagination DEFAULT(10);
END
IF COL_LENGTH('dbo.SystemSettings', 'maintenance_mode') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD maintenance_mode BIT NOT NULL CONSTRAINT DF_SystemSettings_maintenance DEFAULT(0);
END
IF COL_LENGTH('dbo.SystemSettings', 'require_company_logo_to_post_job') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD require_company_logo_to_post_job BIT NOT NULL CONSTRAINT DF_SystemSettings_require_logo DEFAULT(1);
END
IF COL_LENGTH('dbo.SystemSettings', 'auto_close_expired_jobs') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD auto_close_expired_jobs BIT NOT NULL CONSTRAINT DF_SystemSettings_auto_close DEFAULT(0);
END
IF COL_LENGTH('dbo.SystemSettings', 'default_job_expiration_days') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD default_job_expiration_days INT NOT NULL CONSTRAINT DF_SystemSettings_job_expiration DEFAULT(30);
END
IF COL_LENGTH('dbo.SystemSettings', 'created_at') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD created_at DATETIME NOT NULL CONSTRAINT DF_SystemSettings_created_at DEFAULT(GETDATE());
END
IF COL_LENGTH('dbo.SystemSettings', 'updated_at') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSettings ADD updated_at DATETIME NULL;
END

IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings)
BEGIN
    INSERT INTO dbo.SystemSettings
    (
        site_name,
        max_cv_upload_size_mb,
        max_avatar_upload_size_mb,
        max_logo_upload_size_mb,
        allowed_image_types,
        allowed_cv_types,
        otp_expiration_seconds,
        default_pagination_size,
        maintenance_mode,
        require_company_logo_to_post_job,
        auto_close_expired_jobs,
        default_job_expiration_days
    )
    VALUES
    (
        'Job Portal',
        20,
        20,
        20,
        'jpg,jpeg,png',
        'pdf,doc,docx',
        90,
        10,
        0,
        1,
        0,
        30
    );
END

IF COL_LENGTH('dbo.Jobs', 'is_active') IS NULL
BEGIN
    ALTER TABLE dbo.Jobs ADD is_active BIT NOT NULL CONSTRAINT DF_Jobs_is_active DEFAULT(1);
END
");
            }
        }
    }
}
