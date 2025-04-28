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
        /// Chybová zpráva
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Stack trace chyby
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// ID zdroje, u kterého chyba nastala
        /// </summary>
        public int? SourceId { get; set; }

        /// <summary>
        /// Detaily chyby (například další kontext)
        /// </summary>
        [StringLength(2000)]
        public string? Details { get; set; }

        /// <summary>
        /// Kategorie chyby (např. "HTTP", "Parser", "Database")
        /// </summary>
        [StringLength(50)]
        public string? Category { get; set; }
    }
}
