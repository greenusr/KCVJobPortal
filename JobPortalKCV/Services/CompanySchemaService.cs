using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class CompanySchemaService
    {
        public static void EnsureSchema()
        {
            using (var data = new JobPortalDataContext())
            {
                data.ExecuteCommand(@"
IF COL_LENGTH('dbo.Companies', 'description') IS NULL
BEGIN
    ALTER TABLE dbo.Companies ADD description NVARCHAR(MAX) NULL;
END

IF COL_LENGTH('dbo.Companies', 'contact_phone') IS NULL
BEGIN
    ALTER TABLE dbo.Companies ADD contact_phone NVARCHAR(30) NULL;
END

IF COL_LENGTH('dbo.Companies', 'address') IS NULL
BEGIN
    ALTER TABLE dbo.Companies ADD address NVARCHAR(255) NULL;
END

IF COL_LENGTH('dbo.Companies', 'logo_path') IS NULL
BEGIN
    ALTER TABLE dbo.Companies ADD logo_path NVARCHAR(255) NULL;
END

IF COL_LENGTH('dbo.Companies', 'public_company_profile') IS NULL
BEGIN
    ALTER TABLE dbo.Companies ADD public_company_profile BIT NOT NULL CONSTRAINT DF_Companies_public_profile DEFAULT(1);
END

IF COL_LENGTH('dbo.Companies', 'show_jobs_publicly') IS NULL
BEGIN
    ALTER TABLE dbo.Companies ADD show_jobs_publicly BIT NOT NULL CONSTRAINT DF_Companies_show_jobs DEFAULT(1);
END

IF COL_LENGTH('dbo.Companies', 'updated_at') IS NULL
BEGIN
    ALTER TABLE dbo.Companies ADD updated_at DATETIME NULL;
END

IF OBJECT_ID('dbo.CompanyJoinRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyJoinRequests
    (
        request_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        company_id INT NOT NULL,
        user_id INT NOT NULL,
        status NVARCHAR(30) NOT NULL CONSTRAINT DF_CompanyJoinRequests_status DEFAULT('Pending'),
        requested_at DATETIME NOT NULL CONSTRAINT DF_CompanyJoinRequests_requested_at DEFAULT(GETDATE()),
        responded_at DATETIME NULL,
        responded_by INT NULL,
        CONSTRAINT FK_CompanyJoinRequests_Companies FOREIGN KEY(company_id) REFERENCES dbo.Companies(company_id),
        CONSTRAINT FK_CompanyJoinRequests_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT FK_CompanyJoinRequests_RespondedBy FOREIGN KEY(responded_by) REFERENCES dbo.Users(user_id)
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CompanyJoinRequests_Pending' AND object_id = OBJECT_ID('dbo.CompanyJoinRequests'))
BEGIN
    CREATE UNIQUE INDEX UX_CompanyJoinRequests_Pending
    ON dbo.CompanyJoinRequests(company_id, user_id)
    WHERE status = 'Pending';
END
");
            }
        }
    }
}
