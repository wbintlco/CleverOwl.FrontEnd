using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RLI.WebApplication.Models
{
    public class PagesViewModel
    {
        public int PageKey { get; set; }
        public string Href { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string XmlString { get; set; }
        public string Preview { get; set; }
        public string Reportable { get; set; }
        public int LessonKey { get; set; }
    }

    public class AssetsViewModel
    {
        public int AssetsKey { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string Href { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string XmlString { get; set; }
        public string PageId { get; set; }
    }

    public class LeesonsViewModel
    {
        public int LessonKey { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string FilePath { get; set; }
    }

}