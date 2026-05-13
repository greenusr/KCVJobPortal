using System;
using System.Web;
using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public static class AccountLogService
    {
        public static void LogLogin(JobPortalDataContext data, int? userId, bool isSuccess, string failureReason, HttpRequestBase request)
        {
            data.UserLoginLogs.InsertOnSubmit(new UserLoginLog
            {
                user_id = userId,
                is_success = isSuccess,
                failure_reason = Truncate(failureReason, 255),
                ip_address = Truncate(GetIpAddress(request), 100),
                user_agent = Truncate(request == null ? null : request.UserAgent, 500)
            });
        }

        public static void LogActivity(JobPortalDataContext data, int userId, string action, string description, HttpRequestBase request, string keyword = null, string filters = null, int? relatedId = null, string relatedType = null)
        {
            data.UserActivityLogs.InsertOnSubmit(new UserActivityLog
            {
                user_id = userId,
                action = Truncate(action, 80),
                description = description,
                keyword = Truncate(keyword, 255),
                filters = Truncate(filters, 500),
                related_id = relatedId,
                related_type = Truncate(relatedType, 50),
                ip_address = Truncate(GetIpAddress(request), 100),
                user_agent = Truncate(request == null ? null : request.UserAgent, 500)
            });
        }

        public static string BuildFilters(params string[] parts)
        {
            return String.Join(";", Array.FindAll(parts, part => !String.IsNullOrWhiteSpace(part)));
        }

        private static string GetIpAddress(HttpRequestBase request)
        {
            if (request == null)
                return null;

            var forwarded = request.Headers["X-Forwarded-For"];

            if (!String.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();

            return request.UserHostAddress;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (String.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength);
        }
    }
}
