using System;
using System.Collections.Generic;
using System.Linq;
using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class KeySequenceService
    {
        private static readonly Dictionary<string, string> Sequences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ApplicationStatuses.status_id", "Seq_ApplicationStatuses_status_id" },
            { "Companies.company_id", "Seq_Companies_company_id" },
            { "EmploymentTypes.employment_type_id", "Seq_EmploymentTypes_employment_type_id" },
            { "JobApplications.application_id", "Seq_JobApplications_application_id" },
            { "JobCategories.category_id", "Seq_JobCategories_category_id" },
            { "Jobs.job_id", "Seq_Jobs_job_id" },
            { "JobSkills.job_skill_id", "Seq_JobSkills_job_skill_id" },
            { "Locations.location_id", "Seq_Locations_location_id" },
            { "Roles.role_id", "Seq_Roles_role_id" },
            { "Skills.skill_id", "Seq_Skills_skill_id" },
            { "UserProfiles.profile_id", "Seq_UserProfiles_profile_id" },
            { "Users.user_id", "Seq_Users_user_id" },
            { "UserSkills.user_skill_id", "Seq_UserSkills_user_skill_id" }
        };

        public static void EnsureSchema()
        {
            using (var data = new JobPortalDataContext())
            {
                data.ExecuteCommand(@"
DECLARE @keys TABLE
(
    sequence_name SYSNAME NOT NULL,
    table_name SYSNAME NOT NULL,
    column_name SYSNAME NOT NULL
);

INSERT INTO @keys(sequence_name, table_name, column_name)
VALUES
('Seq_ApplicationStatuses_status_id', 'ApplicationStatuses', 'status_id'),
('Seq_Companies_company_id', 'Companies', 'company_id'),
('Seq_EmploymentTypes_employment_type_id', 'EmploymentTypes', 'employment_type_id'),
('Seq_JobApplications_application_id', 'JobApplications', 'application_id'),
('Seq_JobCategories_category_id', 'JobCategories', 'category_id'),
('Seq_Jobs_job_id', 'Jobs', 'job_id'),
('Seq_JobSkills_job_skill_id', 'JobSkills', 'job_skill_id'),
('Seq_Locations_location_id', 'Locations', 'location_id'),
('Seq_Roles_role_id', 'Roles', 'role_id'),
('Seq_Skills_skill_id', 'Skills', 'skill_id'),
('Seq_UserProfiles_profile_id', 'UserProfiles', 'profile_id'),
('Seq_Users_user_id', 'Users', 'user_id'),
('Seq_UserSkills_user_skill_id', 'UserSkills', 'user_skill_id');

DECLARE @sequenceName SYSNAME;
DECLARE @tableName SYSNAME;
DECLARE @columnName SYSNAME;
DECLARE @nextValue BIGINT;
DECLARE @currentValue BIGINT;
DECLARE @sql NVARCHAR(MAX);

DECLARE key_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT sequence_name, table_name, column_name
FROM @keys
WHERE OBJECT_ID('dbo.' + table_name, 'U') IS NOT NULL;

OPEN key_cursor;
FETCH NEXT FROM key_cursor INTO @sequenceName, @tableName, @columnName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'SELECT @nextOut = ISNULL(MAX(' + QUOTENAME(@columnName) + N'), 0) + 1 FROM dbo.' + QUOTENAME(@tableName);
    EXEC sp_executesql @sql, N'@nextOut BIGINT OUTPUT', @nextOut = @nextValue OUTPUT;

    IF OBJECT_ID('dbo.' + @sequenceName, 'SO') IS NULL
    BEGIN
        SET @sql = N'CREATE SEQUENCE dbo.' + QUOTENAME(@sequenceName) + N' AS INT START WITH ' + CAST(@nextValue AS NVARCHAR(30)) + N' INCREMENT BY 1';
        EXEC(@sql);
    END
    ELSE
    BEGIN
        SELECT @currentValue = CONVERT(BIGINT, current_value)
        FROM sys.sequences
        WHERE object_id = OBJECT_ID('dbo.' + @sequenceName, 'SO');

        IF @currentValue < @nextValue - 1
        BEGIN
            SET @sql = N'ALTER SEQUENCE dbo.' + QUOTENAME(@sequenceName) + N' RESTART WITH ' + CAST(@nextValue AS NVARCHAR(30));
            EXEC(@sql);
        END
    END

    FETCH NEXT FROM key_cursor INTO @sequenceName, @tableName, @columnName;
END

CLOSE key_cursor;
DEALLOCATE key_cursor;
");
            }
        }

        public static int NextInt(JobPortalDataContext data, string tableName, string columnName)
        {
            var sequenceName = GetSequenceName(tableName, columnName);

            if (String.IsNullOrWhiteSpace(sequenceName))
                throw new InvalidOperationException("No sequence is configured for " + tableName + "." + columnName);

            return data.ExecuteQuery<int>("SELECT NEXT VALUE FOR dbo." + Quote(sequenceName)).First();
        }

        public static string GetSequenceName(string tableName, string columnName)
        {
            string sequenceName;
            return Sequences.TryGetValue(tableName + "." + columnName, out sequenceName) ? sequenceName : null;
        }

        public static string Quote(string identifier)
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }
    }
}
