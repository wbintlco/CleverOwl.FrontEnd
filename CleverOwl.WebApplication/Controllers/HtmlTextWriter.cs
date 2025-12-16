using System.IO;

namespace CleverOwl.WebApplication.Controllers
{
    internal class HtmlTextWriter : System.Web.UI.HtmlTextWriter
    {
        public HtmlTextWriter(TextWriter writer) : base(writer)
        {
        }
    }
}