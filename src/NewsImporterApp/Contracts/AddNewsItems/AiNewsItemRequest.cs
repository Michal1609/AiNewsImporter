using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsImporterApp.Contracts.AddNewsItems
{
    public class AiNewsItemRequest
    {
        /// <summary>
        /// Title in English
        /// </summary>
        [Required]
        [StringLength(200)]
        public string TitleEn { get; set; } = string.Empty;

        /// <summary>
        /// Title in Czech
        /// </summary>
        [Required]
        [StringLength(200)]
        public string TitleCz { get; set; } = string.Empty;

        /// <summary>
        /// Content in English
        /// </summary>
        public string? ContentEn { get; set; }

        /// <summary>
        /// Content in Czech
        /// </summary>
        public string? ContentCz { get; set; }

        /// <summary>
        /// Summary in English
        /// </summary>
        [StringLength(1000)]
        public string? SummaryEn { get; set; }

        /// <summary>
        /// Summary in Czech
        /// </summary>
        [StringLength(1000)]
        public string? SummaryCz { get; set; }

        /// <summary>
        /// URL of the original source
        /// </summary>
        [Required]
        [Url]
        [StringLength(500)]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Image URL
        /// </summary>
        [Url]
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Source name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string SourceName { get; set; } = string.Empty;

        /// <summary>
        /// Publication date
        /// </summary>
        public DateTime? PublishedDate { get; set; }
    }
}
