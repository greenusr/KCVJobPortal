using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class NotificationSchemaService
    {
        public static void EnsureSchema()
        {
            using (var data = new JobPortalDataContext())
            {
                data.ExecuteCommand(@"
IF OBJECT_ID('dbo.Notifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications
    (
        notification_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        title NVARCHAR(150) NOT NULL,
        message NVARCHAR(MAX) NOT NULL,
        type NVARCHAR(50) NOT NULL,
        related_id INT NULL,
        related_type NVARCHAR(50) NULL,
        is_read BIT NOT NULL CONSTRAINT DF_Notifications_is_read DEFAULT(0),
        created_at DATETIME NOT NULL CONSTRAINT DF_Notifications_created_at DEFAULT(GETDATE()),
        CONSTRAINT FK_Notifications_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id)
    );
END

IF COL_LENGTH('dbo.Notifications', 'related_id') IS NULL
BEGIN
    ALTER TABLE dbo.Notifications ADD related_id INT NULL;
END

IF COL_LENGTH('dbo.Notifications', 'related_type') IS NULL
BEGIN
    ALTER TABLE dbo.Notifications ADD related_type NVARCHAR(50) NULL;
END

IF COL_LENGTH('dbo.Notifications', 'is_read') IS NULL
BEGIN
    ALTER TABLE dbo.Notifications ADD is_read BIT NOT NULL CONSTRAINT DF_Notifications_is_read DEFAULT(0);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_User_Read_Created' AND object_id = OBJECT_ID('dbo.Notifications'))
BEGIN
    CREATE INDEX IX_Notifications_User_Read_Created ON dbo.Notifications(user_id, is_read, created_at DESC);
END
");
            }
        }
    }
}
