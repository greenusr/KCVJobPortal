using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class UploadSchemaService
    {
        public static void EnsureSchema()
        {
            using (var data = new JobPortalDataContext())
            {
                data.ExecuteCommand(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserCVs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.UserCVs
    (
        cv_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        file_path NVARCHAR(255) NOT NULL,
        file_name NVARCHAR(255) NOT NULL,
        created_at DATETIME NOT NULL CONSTRAINT DF_UserCVs_created_at DEFAULT(GETDATE()),
        is_default BIT NOT NULL CONSTRAINT DF_UserCVs_is_default DEFAULT(0),
        CONSTRAINT FK_UserCVs_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id)
    );
END

IF COL_LENGTH('dbo.JobApplications', 'cv_id') IS NULL
BEGIN
    ALTER TABLE dbo.JobApplications ADD cv_id INT NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_JobApplications_UserCVs')
BEGIN
    ALTER TABLE dbo.JobApplications
    ADD CONSTRAINT FK_JobApplications_UserCVs FOREIGN KEY(cv_id) REFERENCES dbo.UserCVs(cv_id);
END

IF COL_LENGTH('dbo.Companies', 'logo_path') IS NULL
BEGIN
    ALTER TABLE dbo.Companies ADD logo_path NVARCHAR(255) NULL;
END");
            }
        }
    }
}
