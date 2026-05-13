using System.Collections.Generic;

namespace JobPortalKCV.Models.ViewModel
{
    public class DocsViewModel
    {
        public string Lang { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string SearchPlaceholder { get; set; }
        public string NoResultsText { get; set; }
        public string IntroText { get; set; }
        public string TocTitle { get; set; }
        public string LanguageLabel { get; set; }
        public string EnglishLabel { get; set; }
        public string VietnameseLabel { get; set; }
        public string ReadGuideText { get; set; }
        public string CategoryLabel { get; set; }
        public string Query { get; set; }
        public string Category { get; set; }
        public List<DocsSectionViewModel> AllSections { get; set; }
        public List<DocsSectionViewModel> Sections { get; set; }
    }

    public class DocsSectionViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public List<DocsItemViewModel> Items { get; set; }
    }

    public class DocsItemViewModel
    {
        public string Id { get; set; }
        public string SectionId { get; set; }
        public string SectionTitle { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Body { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public object RouteValues { get; set; }
        public string LinkText { get; set; }
        public List<DocsContentSectionViewModel> ContentSections { get; set; }
    }

    public class DocsContentSectionViewModel
    {
        public string Heading { get; set; }
        public List<string> Paragraphs { get; set; }
        public List<string> Steps { get; set; }
        public List<string> Bullets { get; set; }
        public string Note { get; set; }
        public string Warning { get; set; }
    }

    public class DocsTopicViewModel
    {
        public DocsViewModel Docs { get; set; }
        public DocsSectionViewModel Section { get; set; }
        public DocsItemViewModel Topic { get; set; }
        public string BackToDocsText { get; set; }
        public string OpenPageText { get; set; }
        public string PreviousText { get; set; }
        public string NextText { get; set; }
        public DocsItemViewModel PreviousTopic { get; set; }
        public DocsItemViewModel NextTopic { get; set; }
    }
}
