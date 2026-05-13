using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class StarSchemaService
    {
        public static void EnsureSchema()
        {
            using (var data = new JobPortalDataContext())
            {
                data.ExecuteCommand(@"
IF OBJECT_ID('dbo.Stars', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Stars
    (
        star_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        target_id INT NOT NULL,
        target_type NVARCHAR(20) NOT NULL,
        created_at DATETIME NOT NULL CONSTRAINT DF_Stars_created_at DEFAULT(GETDATE()),
        CONSTRAINT FK_Stars_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id)
    );
END

IF COL_LENGTH('dbo.Stars', 'user_id') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Stars_Users')
BEGIN
    ALTER TABLE dbo.Stars
    ADD CONSTRAINT FK_Stars_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Stars_User_Target' AND object_id = OBJECT_ID('dbo.Stars'))
AND NOT EXISTS (
    SELECT user_id, target_id, target_type
    FROM dbo.Stars
    GROUP BY user_id, target_id, target_type
    HAVING COUNT(*) > 1
)
BEGIN
    CREATE UNIQUE INDEX UX_Stars_User_Target ON dbo.Stars(user_id, target_id, target_type);
END
");
            }
        }
    }
}
