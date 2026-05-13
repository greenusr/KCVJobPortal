using System.Configuration;

namespace JobPortalKCV.Models
{
    public partial class JobPortalDataContext
    {
        public JobPortalDataContext()
            : this(ConfigurationManager.ConnectionStrings["JobPortalDatabaseConnectionString"].ConnectionString)
        {
        }
    }
}
