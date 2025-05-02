using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsImporterApp.Contracts.AddErrors
{
    public class AiNewsErrorRequest
    {
        /// <summary>
        /// Error message
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Stack trace of the error
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// ID of the source where the error occurred
        /// </summary>
        public int? SourceId { get; set; }

        /// <summary>
        /// Error details (for example additional context)
        /// </summary>
        [StringLength(2000)]
        public string? Details { get; set; }

        /// <summary>
        /// Error category (e.g. "HTTP", "Parser", "Database")
        /// </summary>
        [StringLength(50)]
        public string? Category { get; set; }
    }
}
