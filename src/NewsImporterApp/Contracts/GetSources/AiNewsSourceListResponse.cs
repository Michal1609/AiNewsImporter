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
        /// Seznam zdrojů
        /// </summary>
        public List<AiNewsSourceResponse> Sources { get; set; } = new();

        /// <summary>
        /// Celkový počet zdrojů
        /// </summary>
        public int TotalCount { get; set; }
    }
}
