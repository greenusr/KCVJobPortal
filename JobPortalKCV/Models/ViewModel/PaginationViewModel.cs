namespace JobPortalKCV.Models.ViewModel
{
    public class PaginationViewModel
    {
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public object RouteValues { get; set; }
    }
}
