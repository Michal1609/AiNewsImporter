using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NewsImporterApp.Models
{
    public class NewsContent
    {
        public string TitleCz { get; set; } = string.Empty;
        public string ContentCz { get; set; } = string.Empty;
        public string SummaryEn { get; set; } = string.Empty;
        public string SummaryCz { get; set; } = string.Empty;
        public DateTime? PublishDate { get; set; }
    }
}
