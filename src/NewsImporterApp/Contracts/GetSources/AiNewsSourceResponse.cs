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
        /// ID zdroje
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Název zdroje
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL zdroje
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Typ zdroje (RSS, Web, API, etc.)
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SourceType Type { get; set; }

        /// <summary>
        /// Zda je zdroj aktivní
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Datum posledního stažení
        /// </summary>
        public DateTime? LastFetched { get; set; }

        /// <summary>
        /// Dodatečné parametry pro stažení (JSON)
        /// </summary>
        public string? Parameters { get; set; }        
    }
}
