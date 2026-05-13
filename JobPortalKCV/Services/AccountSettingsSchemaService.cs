using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class AccountSettingsSchemaService
    {
        public static void EnsureSchema()
        {
            using (var data = new JobPortalDataContext())
            {
                data.ExecuteCommand(@"
IF OBJECT_ID('dbo.UserSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSettings
    (
        setting_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        app_notifications_enabled BIT NOT NULL CONSTRAINT DF_UserSettings_app_notifications DEFAULT(1),
        job_updates_enabled BIT NOT NULL CONSTRAINT DF_UserSettings_job_updates DEFAULT(1),
        interview_notifications_enabled BIT NOT NULL CONSTRAINT DF_UserSettings_interview DEFAULT(1),
        invitation_notifications_enabled BIT NOT NULL CONSTRAINT DF_UserSettings_invitation DEFAULT(1),
        public_profile_enabled BIT NOT NULL CONSTRAINT DF_UserSettings_public_profile DEFAULT(1),
        show_cv_to_employers BIT NOT NULL CONSTRAINT DF_UserSettings_show_cv DEFAULT(0),
        default_cv_id INT NULL,
        created_at DATETIME NOT NULL CONSTRAINT DF_UserSettings_created_at DEFAULT(GETDATE()),
        updated_at DATETIME NULL,
        CONSTRAINT FK_UserSettings_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id)
    );
END

IF COL_LENGTH('dbo.UserSettings', 'app_notifications_enabled') IS NULL
BEGIN
    ALTER TABLE dbo.UserSettings ADD app_notifications_enabled BIT NOT NULL CONSTRAINT DF_UserSettings_app_notifications DEFAULT(1);
END

IF COL_LENGTH('dbo.UserSettings', 'job_updates_enabled') IS NULL
BEGIN
    ALTER TABLE dbo.UserSettings ADD job_updates_enabled BIT NOT NULL CONSTRAINT DF_UserSettings_job_updates DEFAULT(1);
END

IF COL_LENGTH('dbo.UserSettings', 'interview_notifications_enabled') IS NULL
BEGIN
    ALTER TABLE dbo.UserSettings ADD interview_notifications_enabled BIT NOT NULL CONSTRAINT DF_UserSettings_interview DEFAULT(1);
END

IF COL_LENGTH('dbo.UserSettings', 'invitation_notifications_enabled') IS NULL
BEGIN
    ALTER TABLE dbo.UserSettings ADD invitation_notifications_enabled BIT NOT NULL CONSTRAINT DF_UserSettings_invitation DEFAULT(1);
END

IF COL_LENGTH('dbo.UserSettings', 'public_profile_enabled') IS NULL
BEGIN
    ALTER TABLE dbo.UserSettings ADD public_profile_enabled BIT NOT NULL CONSTRAINT DF_UserSettings_public_profile DEFAULT(1);
END

IF COL_LENGTH('dbo.UserSettings', 'show_cv_to_employers') IS NULL
BEGIN
    ALTER TABLE dbo.UserSettings ADD show_cv_to_employers BIT NOT NULL CONSTRAINT DF_UserSettings_show_cv DEFAULT(0);
END

IF COL_LENGTH('dbo.UserSettings', 'default_cv_id') IS NULL
BEGIN
    ALTER TABLE dbo.UserSettings ADD default_cv_id INT NULL;
END

IF COL_LENGTH('dbo.UserSettings', 'created_at') IS NULL
BEGIN
    ALTER TABLE dbo.UserSettings ADD created_at DATETIME NOT NULL CONSTRAINT DF_UserSettings_created_at DEFAULT(GETDATE());
END

IF COL_LENGTH('dbo.UserSettings', 'updated_at') IS NULL
BEGIN
    ALTER TABLE dbo.UserSettings ADD updated_at DATETIME NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_UserSettings_user_id' AND object_id = OBJECT_ID('dbo.UserSettings'))
BEGIN
    CREATE UNIQUE INDEX UX_UserSettings_user_id ON dbo.UserSettings(user_id);
END

IF OBJECT_ID('dbo.UserCVs', 'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserSettings_UserCVs')
BEGIN
    ALTER TABLE dbo.UserSettings
    ADD CONSTRAINT FK_UserSettings_UserCVs FOREIGN KEY(default_cv_id) REFERENCES dbo.UserCVs(cv_id);
END

IF OBJECT_ID('dbo.UserLoginLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserLoginLogs
    (
        login_log_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NULL,
        login_time DATETIME NOT NULL CONSTRAINT DF_UserLoginLogs_login_time DEFAULT(GETDATE()),
        ip_address NVARCHAR(100) NULL,
        user_agent NVARCHAR(500) NULL,
        is_success BIT NOT NULL,
        failure_reason NVARCHAR(255) NULL,
        CONSTRAINT FK_UserLoginLogs_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id)
    );
END

IF COL_LENGTH('dbo.UserLoginLogs', 'user_id') IS NULL
BEGIN
    ALTER TABLE dbo.UserLoginLogs ADD user_id INT NULL;
END

IF COL_LENGTH('dbo.UserLoginLogs', 'login_time') IS NULL
BEGIN
    ALTER TABLE dbo.UserLoginLogs ADD login_time DATETIME NOT NULL CONSTRAINT DF_UserLoginLogs_login_time DEFAULT(GETDATE());
END

IF COL_LENGTH('dbo.UserLoginLogs', 'ip_address') IS NULL
BEGIN
    ALTER TABLE dbo.UserLoginLogs ADD ip_address NVARCHAR(100) NULL;
END

IF COL_LENGTH('dbo.UserLoginLogs', 'user_agent') IS NULL
BEGIN
    ALTER TABLE dbo.UserLoginLogs ADD user_agent NVARCHAR(500) NULL;
END

IF COL_LENGTH('dbo.UserLoginLogs', 'is_success') IS NULL
BEGIN
    ALTER TABLE dbo.UserLoginLogs ADD is_success BIT NOT NULL CONSTRAINT DF_UserLoginLogs_is_success DEFAULT(0);
END

IF COL_LENGTH('dbo.UserLoginLogs', 'failure_reason') IS NULL
BEGIN
    ALTER TABLE dbo.UserLoginLogs ADD failure_reason NVARCHAR(255) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserLoginLogs_user_time' AND object_id = OBJECT_ID('dbo.UserLoginLogs'))
BEGIN
    CREATE INDEX IX_UserLoginLogs_user_time ON dbo.UserLoginLogs(user_id, login_time DESC);
END

IF OBJECT_ID('dbo.UserActivityLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserActivityLogs
    (
        activity_log_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        action NVARCHAR(80) NOT NULL,
        description NVARCHAR(MAX) NULL,
        keyword NVARCHAR(255) NULL,
        filters NVARCHAR(500) NULL,
        related_id INT NULL,
        related_type NVARCHAR(50) NULL,
        ip_address NVARCHAR(100) NULL,
        user_agent NVARCHAR(500) NULL,
        created_at DATETIME NOT NULL CONSTRAINT DF_UserActivityLogs_created_at DEFAULT(GETDATE()),
        CONSTRAINT FK_UserActivityLogs_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id)
    );
END

IF COL_LENGTH('dbo.UserActivityLogs', 'user_id') IS NULL
BEGIN
    ALTER TABLE dbo.UserActivityLogs ADD user_id INT NOT NULL CONSTRAINT DF_UserActivityLogs_user_id DEFAULT(0);
END

IF COL_LENGTH('dbo.UserActivityLogs', 'action') IS NULL
BEGIN
    ALTER TABLE dbo.UserActivityLogs ADD action NVARCHAR(80) NOT NULL CONSTRAINT DF_UserActivityLogs_action DEFAULT('');
END

IF COL_LENGTH('dbo.UserActivityLogs', 'description') IS NULL
BEGIN
    ALTER TABLE dbo.UserActivityLogs ADD description NVARCHAR(MAX) NULL;
END

IF COL_LENGTH('dbo.UserActivityLogs', 'keyword') IS NULL
BEGIN
    ALTER TABLE dbo.UserActivityLogs ADD keyword NVARCHAR(255) NULL;
END

IF COL_LENGTH('dbo.UserActivityLogs', 'filters') IS NULL
BEGIN
    ALTER TABLE dbo.UserActivityLogs ADD filters NVARCHAR(500) NULL;
END

IF COL_LENGTH('dbo.UserActivityLogs', 'related_id') IS NULL
BEGIN
    ALTER TABLE dbo.UserActivityLogs ADD related_id INT NULL;
END

IF COL_LENGTH('dbo.UserActivityLogs', 'related_type') IS NULL
BEGIN
    ALTER TABLE dbo.UserActivityLogs ADD related_type NVARCHAR(50) NULL;
END

IF COL_LENGTH('dbo.UserActivityLogs', 'ip_address') IS NULL
BEGIN
    ALTER TABLE dbo.UserActivityLogs ADD ip_address NVARCHAR(100) NULL;
END

IF COL_LENGTH('dbo.UserActivityLogs', 'user_agent') IS NULL
BEGIN
    ALTER TABLE dbo.UserActivityLogs ADD user_agent NVARCHAR(500) NULL;
END

IF COL_LENGTH('dbo.UserActivityLogs', 'created_at') IS NULL
BEGIN
    ALTER TABLE dbo.UserActivityLogs ADD created_at DATETIME NOT NULL CONSTRAINT DF_UserActivityLogs_created_at DEFAULT(GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserActivityLogs_user_action_time' AND object_id = OBJECT_ID('dbo.UserActivityLogs'))
BEGIN
    CREATE INDEX IX_UserActivityLogs_user_action_time ON dbo.UserActivityLogs(user_id, action, created_at DESC);
END
");
            }
        }
    }
}
