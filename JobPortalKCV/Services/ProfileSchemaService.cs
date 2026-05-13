using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class ProfileSchemaService
    {
        public static void EnsureSchema()
        {
            using (var data = new JobPortalDataContext())
            {
                data.ExecuteCommand(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserProfiles' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.UserProfiles
    (
        profile_id INT NOT NULL PRIMARY KEY,
        user_id INT NULL,
        location_id INT NULL,
        full_name NVARCHAR(150) NULL,
        date_of_birth DATE NULL,
        phone NVARCHAR(30) NULL,
        address NVARCHAR(255) NULL,
        about_me NVARCHAR(MAX) NULL,
        avatar_path NVARCHAR(255) NULL,
        created_at DATETIME NULL CONSTRAINT DF_UserProfiles_created_at DEFAULT(GETDATE()),
        updated_at DATETIME NULL,
        CONSTRAINT FK_UserProfiles_Users_ProfileSystem FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id)
    );
END

IF COL_LENGTH('dbo.UserProfiles', 'full_name') IS NULL
BEGIN
    ALTER TABLE dbo.UserProfiles ADD full_name NVARCHAR(150) NULL;
END

IF COL_LENGTH('dbo.UserProfiles', 'date_of_birth') IS NULL
BEGIN
    ALTER TABLE dbo.UserProfiles ADD date_of_birth DATE NULL;
END

IF COL_LENGTH('dbo.UserProfiles', 'phone') IS NULL
BEGIN
    ALTER TABLE dbo.UserProfiles ADD phone NVARCHAR(30) NULL;
END

IF COL_LENGTH('dbo.UserProfiles', 'address') IS NULL
BEGIN
    ALTER TABLE dbo.UserProfiles ADD address NVARCHAR(255) NULL;
END

IF COL_LENGTH('dbo.UserProfiles', 'about_me') IS NULL
BEGIN
    ALTER TABLE dbo.UserProfiles ADD about_me NVARCHAR(MAX) NULL;
END

IF COL_LENGTH('dbo.UserProfiles', 'avatar_path') IS NULL
BEGIN
    ALTER TABLE dbo.UserProfiles ADD avatar_path NVARCHAR(255) NULL;
END

IF COL_LENGTH('dbo.UserProfiles', 'created_at') IS NULL
BEGIN
    ALTER TABLE dbo.UserProfiles ADD created_at DATETIME NULL CONSTRAINT DF_UserProfiles_created_at DEFAULT(GETDATE());
END

IF COL_LENGTH('dbo.UserProfiles', 'updated_at') IS NULL
BEGIN
    ALTER TABLE dbo.UserProfiles ADD updated_at DATETIME NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_UserProfiles_user_id' AND object_id = OBJECT_ID('dbo.UserProfiles'))
AND NOT EXISTS (
    SELECT user_id
    FROM dbo.UserProfiles
    WHERE user_id IS NOT NULL
    GROUP BY user_id
    HAVING COUNT(*) > 1
)
BEGIN
    CREATE UNIQUE INDEX UX_UserProfiles_user_id ON dbo.UserProfiles(user_id) WHERE user_id IS NOT NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserEducations' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.UserEducations
    (
        education_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        school_name NVARCHAR(150) NULL,
        degree NVARCHAR(150) NULL,
        field_of_study NVARCHAR(150) NULL,
        start_date DATE NULL,
        end_date DATE NULL,
        description NVARCHAR(MAX) NULL,
        CONSTRAINT FK_UserEducations_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id)
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserExperiences' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.UserExperiences
    (
        experience_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        company_name NVARCHAR(150) NULL,
        position NVARCHAR(150) NULL,
        start_date DATE NULL,
        end_date DATE NULL,
        description NVARCHAR(MAX) NULL,
        CONSTRAINT FK_UserExperiences_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id)
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserProjects' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.UserProjects
    (
        project_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        project_name NVARCHAR(150) NULL,
        description NVARCHAR(MAX) NULL,
        project_url NVARCHAR(255) NULL,
        start_date DATE NULL,
        end_date DATE NULL,
        CONSTRAINT FK_UserProjects_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id)
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserSkills' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.UserSkills
    (
        user_skill_id INT NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        skill_id INT NULL,
        CONSTRAINT FK_UserSkills_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT FK_UserSkills_Skills FOREIGN KEY(skill_id) REFERENCES dbo.Skills(skill_id)
    );
END");
            }
        }
    }
}
