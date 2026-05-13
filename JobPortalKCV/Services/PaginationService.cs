using System;
using System.Collections.Generic;
using System.Linq;
using JobPortalKCV.Models.ViewModel;

namespace JobPortalKCV.Services
{
    public static class PaginationService
    {
        public static List<T> Paginate<T>(IEnumerable<T> items, int page, int pageSize, out PaginationViewModel pagination, string actionName, string controllerName, object routeValues = null)
        {
            var source = (items ?? Enumerable.Empty<T>()).ToList();
            pageSize = Math.Max(1, pageSize);
            var totalRecords = source.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
            page = Math.Max(1, Math.Min(page, totalPages));

            pagination = new PaginationViewModel
            {
                Page = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                ActionName = actionName,
                ControllerName = controllerName,
                RouteValues = routeValues
            };

            return source.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
    }
}
