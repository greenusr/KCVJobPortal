using System;
using System.Collections.Generic;

namespace JobPortalKCV.Models.ViewModel
{
    public class NotificationDropdownViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationListItemViewModel> Notifications { get; set; }
    }

    public class NotificationIndexViewModel
    {
        public string Filter { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public List<NotificationListItemViewModel> Notifications { get; set; }
    }

    public class NotificationListItemViewModel
    {
        public int NotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public int? RelatedId { get; set; }
        public string RelatedType { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
