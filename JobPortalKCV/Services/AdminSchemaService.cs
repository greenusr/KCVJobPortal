using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class AdminSchemaService
    {
        public static void EnsureSchema()
        {
            using (var data = new JobPortalDataContext())
            {
                data.ExecuteCommand(@"
IF COL_LENGTH('dbo.Users', 'is_active') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD is_active BIT NOT NULL CONSTRAINT DF_Users_is_active DEFAULT(1);
END
");
            }
        }
    }
}
