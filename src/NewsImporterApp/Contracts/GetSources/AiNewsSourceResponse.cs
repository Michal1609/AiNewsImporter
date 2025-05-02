using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NewsImporterApp.Contracts.GetSources
{
    public class AiNewsSourceResponse
    {
        /// <summary>
        /// Source ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Source name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Source URL
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Source type (RSS, Web, API, etc.)
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SourceType Type { get; set; }

        /// <summary>
        /// Indicates if the source is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Date of last fetch
        /// </summary>
        public DateTime? LastFetched { get; set; }

        /// <summary>
        /// Additional parameters for fetching (JSON)
        /// </summary>
        public string? Parameters { get; set; }        
    }
}
