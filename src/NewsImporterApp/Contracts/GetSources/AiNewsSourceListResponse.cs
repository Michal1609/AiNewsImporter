using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsImporterApp.Contracts.GetSources
{
    public class AiNewsSourceListResponse
    {
        /// <summary>
        /// List of sources
        /// </summary>
        public List<AiNewsSourceResponse> Sources { get; set; } = new();

        /// <summary>
        /// Total count of sources
        /// </summary>
        public int TotalCount { get; set; }
    }
}
