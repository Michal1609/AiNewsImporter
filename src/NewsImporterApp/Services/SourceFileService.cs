using NewsImporterApp.Contracts.GetSources;
using NewsImporterApp.Core;

namespace NewsImporterApp.Services
{
    /// <summary>
    /// Service for managing sources.txt file
    /// </summary>
    public class SourceFileService
    {
        private readonly string _sourcesFilePath;
        private readonly IExceptionHandler? _exceptionHandler;
        
        /// <summary>
        /// Initializes a new instance of the SourceFileService
        /// </summary>
        /// <param name="exceptionHandler">Optional exception handler for global tracking</param>
        public SourceFileService(IExceptionHandler? exceptionHandler = null)
        {
            _sourcesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sources.txt");
            _exceptionHandler = exceptionHandler;
        }
        
        /// <summary>
        /// Gets the path to the sources file
        /// </summary>
        public string SourcesFilePath => _sourcesFilePath;
        
        /// <summary>
        /// Loads sources from sources.txt file
        /// </summary>
        /// <returns>Dictionary of sources (URL as key, tuple of SourceType and LastFetched date as value)</returns>
        public async Task<Dictionary<string, (SourceType Type, DateTime LastFetched)>> LoadSourcesAsync()
        {
            var sources = new Dictionary<string, (SourceType Type, DateTime LastFetched)>();
            
            try
            {
                if (!File.Exists(_sourcesFilePath))
                {
                    return sources;
                }
                
                var lines = await File.ReadAllLinesAsync(_sourcesFilePath);
                
                foreach (var line in lines)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                        
                        var parts = line.Split(';');
                        if (parts.Length != 3)
                        {
                            Console.WriteLine($"Invalid line format: {line}");
                            continue;
                        }
                        
                        var url = parts[0].Trim();
                        if (!Enum.TryParse<SourceType>(parts[1], out var type))
                        {
                            Console.WriteLine($"Invalid source type: {parts[1]}");
                            continue;
                        }
                        
                        if (!DateTime.TryParseExact(parts[2], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var lastFetched))
                        {
                            Console.WriteLine($"Invalid date format: {parts[2]}");
                            continue;
                        }
                        
                        sources[url] = (type, lastFetched);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing line: {line}. {ex.Message}");
                        _exceptionHandler?.AddException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sources file: {ex.Message}");
                _exceptionHandler?.AddException(ex);
            }
            
            return sources;
        }
        
        /// <summary>
        /// Saves sources to sources.txt file
        /// </summary>
        /// <param name="sources">Dictionary of sources (URL as key, tuple of SourceType and LastFetched date as value)</param>
        public async Task SaveSourcesAsync(Dictionary<string, (SourceType Type, DateTime LastFetched)> sources)
        {
            try
            {
                var lines = new List<string>();
                
                foreach (var source in sources)
                {
                    var line = $"{source.Key};{source.Value.Type};{source.Value.LastFetched:yyyyMMdd}";
                    lines.Add(line);
                }
                
                await File.WriteAllLinesAsync(_sourcesFilePath, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving sources file: {ex.Message}");
                _exceptionHandler?.AddException(ex);
            }
        }
        
        /// <summary>
        /// Updates sources with new data from API
        /// </summary>
        /// <param name="apiSources">List of sources from API</param>
        /// <returns>Updated dictionary of sources</returns>
        public async Task<Dictionary<string, (SourceType Type, DateTime LastFetched)>> UpdateSourcesFromApiAsync(
            List<AiNewsSourceResponse> apiSources)
        {
            try
            {
                // Load existing sources
                var existingSources = await LoadSourcesAsync();
                var today = DateTime.Now;
                
                // Update sources with new data from API
                foreach (var apiSource in apiSources)
                {
                    if (string.IsNullOrWhiteSpace(apiSource.Url))
                        continue;
                    
                    var url = apiSource.Url.Trim();
                    var lastFetched = apiSource.LastFetched ?? today;
                    
                    existingSources[url] = (apiSource.Type, lastFetched);
                }
                
                // Save updated sources
                await SaveSourcesAsync(existingSources);
                
                return existingSources;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating sources from API: {ex.Message}");
                _exceptionHandler?.AddException(ex);
                
                // Return empty dictionary in case of error
                return new Dictionary<string, (SourceType Type, DateTime LastFetched)>();
            }
        }
    }
} 