using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class CandidateInvitationSchemaService
    {
        public static void EnsureSchema()
        {
            using (var data = new JobPortalDataContext())
            {
                data.ExecuteCommand(@"
IF OBJECT_ID('dbo.CandidateInvitations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CandidateInvitations
    (
        invitation_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        employer_id INT NOT NULL,
        candidate_id INT NOT NULL,
        job_id INT NOT NULL,
        message NVARCHAR(MAX) NULL,
        status NVARCHAR(30) NOT NULL CONSTRAINT DF_CandidateInvitations_status DEFAULT('Pending'),
        created_at DATETIME NOT NULL CONSTRAINT DF_CandidateInvitations_created_at DEFAULT(GETDATE()),
        responded_at DATETIME NULL,
        CONSTRAINT FK_CandidateInvitations_Employer FOREIGN KEY(employer_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT FK_CandidateInvitations_Candidate FOREIGN KEY(candidate_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT FK_CandidateInvitations_Jobs FOREIGN KEY(job_id) REFERENCES dbo.Jobs(job_id)
    );
END

IF COL_LENGTH('dbo.CandidateInvitations', 'message') IS NULL
BEGIN
    ALTER TABLE dbo.CandidateInvitations ADD message NVARCHAR(MAX) NULL;
END

IF COL_LENGTH('dbo.CandidateInvitations', 'responded_at') IS NULL
BEGIN
    ALTER TABLE dbo.CandidateInvitations ADD responded_at DATETIME NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CandidateInvitations_Pending' AND object_id = OBJECT_ID('dbo.CandidateInvitations'))
BEGIN
    CREATE UNIQUE INDEX UX_CandidateInvitations_Pending
    ON dbo.CandidateInvitations(candidate_id, job_id)
    WHERE status = 'Pending';
END
");
            }
        }
    }
}
