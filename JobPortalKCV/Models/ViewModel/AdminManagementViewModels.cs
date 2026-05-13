using System.Collections.Generic;

namespace JobPortalKCV.Models.ViewModel
{
    public class AdminTableLinkViewModel
    {
        public string TableName { get; set; }
        public string DisplayName { get; set; }
        public int RecordCount { get; set; }
    }

    public class AdminTableListViewModel
    {
        public string TableName { get; set; }
        public string DisplayName { get; set; }
        public string Keyword { get; set; }
        public string Sort { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public string DeleteButtonText { get; set; }
        public List<string> Columns { get; set; }
        public List<Dictionary<string, string>> Rows { get; set; }
    }

    public class AdminRecordFormViewModel
    {
        public string TableName { get; set; }
        public string DisplayName { get; set; }
        public string Key { get; set; }
        public bool IsCreate { get; set; }
        public bool CanEdit { get; set; }
        public List<AdminFieldViewModel> Fields { get; set; }
    }

    public class AdminFieldViewModel
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
        public string DisplayValue { get; set; }
        public string DataType { get; set; }
        public bool IsEditable { get; set; }
        public bool IsRequired { get; set; }
        public List<AdminSelectOptionViewModel> Options { get; set; }
    }

    public class AdminSelectOptionViewModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
}
