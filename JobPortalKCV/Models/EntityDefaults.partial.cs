using System;

namespace JobPortalKCV.Models
{
    public partial class CandidateInvitation
    {
        partial void OnCreated()
        {
            if (created_at == default(DateTime))
                created_at = DateTime.Now;
        }
    }

    public partial class CompanyJoinRequest
    {
        partial void OnCreated()
        {
            if (requested_at == default(DateTime))
                requested_at = DateTime.Now;
        }
    }

    public partial class Interview
    {
        partial void OnCreated()
        {
            if (created_at == default(DateTime))
                created_at = DateTime.Now;
        }
    }

    public partial class Notification
    {
        partial void OnCreated()
        {
            if (created_at == default(DateTime))
                created_at = DateTime.Now;
        }
    }

    public partial class Star
    {
        partial void OnCreated()
        {
            if (created_at == default(DateTime))
                created_at = DateTime.Now;
        }
    }

    public partial class SystemSetting
    {
        partial void OnCreated()
        {
            if (created_at == default(DateTime))
                created_at = DateTime.Now;
        }
    }

    public partial class UserActivityLog
    {
        partial void OnCreated()
        {
            if (created_at == default(DateTime))
                created_at = DateTime.Now;
        }
    }

    public partial class UserCV
    {
        partial void OnCreated()
        {
            if (created_at == default(DateTime))
                created_at = DateTime.Now;
        }
    }

    public partial class UserLoginLog
    {
        partial void OnCreated()
        {
            if (login_time == default(DateTime))
                login_time = DateTime.Now;
        }
    }

    public partial class UserSetting
    {
        partial void OnCreated()
        {
            if (created_at == default(DateTime))
                created_at = DateTime.Now;
        }
    }

    public partial class UserVerification
    {
        partial void OnCreated()
        {
            if (created_at == default(DateTime))
                created_at = DateTime.Now;
        }
    }
}
