using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class ApplicationSchemaService
    {
        public static void EnsureSchema()
        {
            using (var data = new JobPortalDataContext())
            {
                data.ExecuteCommand(@"
IF COL_LENGTH('dbo.JobApplications', 'cv_id') IS NULL
BEGIN
    ALTER TABLE dbo.JobApplications ADD cv_id INT NULL;
END

IF COL_LENGTH('dbo.JobApplications', 'cover_letter') IS NULL
BEGIN
    ALTER TABLE dbo.JobApplications ADD cover_letter NVARCHAR(MAX) NULL;
END

IF COL_LENGTH('dbo.JobApplications', 'status') IS NULL
BEGIN
    ALTER TABLE dbo.JobApplications ADD status NVARCHAR(30) NULL;
END

IF COL_LENGTH('dbo.JobApplications', 'applied_date') IS NULL
BEGIN
    ALTER TABLE dbo.JobApplications ADD applied_date DATETIME NULL;
END

IF COL_LENGTH('dbo.JobApplications', 'completed_at') IS NULL
BEGIN
    ALTER TABLE dbo.JobApplications ADD completed_at DATETIME NULL;
END

IF COL_LENGTH('dbo.JobApplications', 'final_result') IS NULL
BEGIN
    ALTER TABLE dbo.JobApplications ADD final_result NVARCHAR(30) NULL;
END

IF OBJECT_ID('dbo.UserCVs', 'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_JobApplications_UserCVs')
BEGIN
    ALTER TABLE dbo.JobApplications
    ADD CONSTRAINT FK_JobApplications_UserCVs FOREIGN KEY(cv_id) REFERENCES dbo.UserCVs(cv_id);
END

IF OBJECT_ID('dbo.Interviews', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Interviews
    (
        interview_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        application_id INT NOT NULL,
        interview_date DATETIME NOT NULL,
        location NVARCHAR(255) NOT NULL,
        contact_name NVARCHAR(150) NOT NULL,
        contact_email NVARCHAR(150) NOT NULL,
        contact_phone NVARCHAR(30) NOT NULL,
        additional_info NVARCHAR(MAX) NULL,
        created_at DATETIME NOT NULL CONSTRAINT DF_Interviews_created_at DEFAULT(GETDATE()),
        CONSTRAINT FK_Interviews_JobApplications FOREIGN KEY(application_id) REFERENCES dbo.JobApplications(application_id)
    );
END

IF COL_LENGTH('dbo.Interviews', 'additional_info') IS NULL
BEGIN
    ALTER TABLE dbo.Interviews ADD additional_info NVARCHAR(MAX) NULL;
END

EXEC('
UPDATE application
SET [status] = COALESCE(status.status_name, ''Pending'')
FROM dbo.JobApplications application
LEFT JOIN dbo.ApplicationStatuses status ON application.status_id = status.status_id
WHERE application.[status] IS NULL;
');

EXEC('
UPDATE dbo.JobApplications
SET applied_date = CAST(application_date AS DATETIME)
WHERE applied_date IS NULL AND application_date IS NOT NULL;
');
");
            }
        }
    }
}
