# Vibe Memory - AI News Importer

This document serves as a comprehensive reference guide for the AI News Importer project. It contains detailed information about the project structure, classes, workflows, and configuration that will be useful for future development and maintenance.

## Project Overview

AI News Importer is a tool that automatically collects, processes, and imports AI-related news from various sources. It scrapes web content, uses AI to analyze and translate it, and then imports the data into a central news management system (GrznarAi). The application is designed to run as a standalone service that reads news from configured sources, processes them, and sends the results to the API.

## Project Structure

```
NewsImporter/
├── src/
│   └── NewsImporterApp/
│       ├── Core/              # Core application logic and configuration
│       ├── Services/          # Service implementations
│       ├── Models/            # Data models
│       ├── Utilities/         # Helper utilities
│       ├── Contracts/         # API contract models
│       │   ├── AddErrors/     # Models for error reporting
│       │   ├── AddNewsItems/  # Models for news items submission
│       │   └── GetSources/    # Models for source retrieval
│       ├── Converters/        # Data conversion utilities
│       ├── Program.cs         # Application entry point
│       └── appsettings.json   # Configuration settings
└── README.md                  # Project documentation
```

## Key Classes and Descriptions

### Core

#### `IExceptionHandler.cs`
- Purpose: Interface for global exception handling
- Methods:
  - `AddException()`: Adds an exception to the global collection

#### `AppConfig.cs`
- Purpose: Holds configuration values for the application
- Properties:
  - `GoogleApiKey`: API key for Google Gemini AI
  - `PageLoadTimeoutMs`: Timeout setting for page loading
  - `GrznarAiApiKey`: API key for the GrznarAi service
  - `GrznarAiBaseUrl`: Base URL for the GrznarAi API

#### `NewsImporterApplication.cs`
- Purpose: Main application orchestration class
- Implements: `IExceptionHandler`
- Responsibilities:
  - Initialize services
  - Coordinate the news import process
  - Collect and handle exceptions
  - Handle error reporting and logging
- Key methods:
  - `AddException()`: Adds an exception to the internal collection
  - `RunAsync()`: Main execution method
  - `LoadNewsSourcesAsync()`: Loads news sources from API or fallback file

### Services

#### `ApiService.cs`
- Purpose: Handles all API communication with the GrznarAi backend
- Responsibilities:
  - Get news sources from the API
  - Send collected news to the API
  - Send error reports to the API
- Key methods:
  - `GetSourcesAsync()`: Retrieves news sources from the API
  - `SendNewsToWebAsync()`: Sends processed news to the API
  - `SendErrorsToWebAsync()`: Sends error reports to the API

#### `ConfigurationService.cs`
- Purpose: Manages application configuration loading and validation
- Responsibilities:
  - Load config from appsettings.json
  - Load user secrets
  - Validate configuration
- Key methods:
  - `LoadConfigAsync()`: Loads and validates the application configuration
  - `CreateJsonOptions()`: Creates JSON serialization options for consistent serialization

#### `GeminiAiService.cs`
- Purpose: Interacts with Google's Gemini AI for content analysis
- Responsibilities:
  - Extract news from markdown content
  - Generate content summaries and translations
- Key methods:
  - `SetApiKey()`: Sets the Google API key
  - `GetAllAiNewsFromMarkdownAsync()`: Extracts news from markdown
  - `GetContentOfNewFromMarkdownAsync()`: Processes individual news content

#### `NewsProcessor.cs`
- Purpose: Processes news sources and articles
- Responsibilities:
  - Orchestrate the news processing workflow
  - Filter news based on dates
  - Extract and clean content
- Key methods:
  - `ProcessNewsSourcesAsync()`: Processes all news sources
  - `ProcessNewsSourceAsync()`: Processes a single news source
  - `ProcessNewsItemAsync()`: Processes a single news item

#### `PlaywrightService.cs`
- Purpose: Handles web scraping operations using Playwright
- Responsibilities:
  - Navigate to web pages
  - Extract page content
  - Handle browser automation
- Key methods:
  - `GetPageContentAsync()`: Gets content from a web page
  - `InitializePlaywrightAsync()`: Initializes the Playwright browser
  - `DisposeAsync()`: Cleans up resources

#### `SourceFileService.cs`
- Purpose: Manages source data stored in local files
- Responsibilities:
  - Load sources from local storage
  - Update sources with data from API
  - Save sources to local storage
- Key methods:
  - `LoadSourcesAsync()`: Loads sources from the local file
  - `SaveSourcesAsync()`: Saves sources to the local file
  - `UpdateSourcesFromApiAsync()`: Updates sources with data from the API

### Models

#### `NewsItem.cs`
- Purpose: Represents a news article
- Properties:
  - `Id`: Unique identifier (GUID)
  - `Title`: Article title in English
  - `TitleCz`: Article title in Czech
  - `Url`: Article URL
  - `Date`: Publication date
  - `ImageUrl`: URL to the article image
  - `Text`: Article content in English
  - `ContentCz`: Czech translated content
  - `SummaryCz`: Czech summary
  - `SummaryEn`: English summary
  - `ImportedDate`: Date when the article was imported
  - `SourceName`: Source of the article

#### `NewsContent.cs`
- Purpose: Represents the processed content of a news article
- Properties:
  - `TitleCz`: Czech title
  - `ContentCz`: Czech content
  - `SummaryCz`: Czech summary
  - `SummaryEn`: English summary
  - `PublishDate`: Publication date

#### `NewsSourceItem.cs`
- Purpose: Represents a news source
- Properties:
  - `Url`: URL of the news source
  - `Type`: Type of the news source (Web, Facebook, Twitter)
  - `LastFetched`: Date when the source was last processed

### Contracts

#### `GetSources/`
- `AiNewsSourceListResponse.cs`: Container for a list of source responses
- `AiNewsSourceResponse.cs`: Individual source data from API
- `SourceType.cs`: Enum defining source types (Web, Facebook, Twitter)

#### `AddNewsItems/`
- `AiNewsItemRequest.cs`: Model for submitting a news item to the API

#### `AddErrors/`
- `AiNewsErrorRequest.cs`: Model for submitting error reports to the API

### Utilities

#### `HtmlCleaner.cs`
- Purpose: Cleans HTML content
- Key methods:
  - `CleanHtml()`: Removes unwanted HTML elements and normalizes content

#### `MarkdownConverter.cs`
- Purpose: Converts HTML to Markdown
- Key methods:
  - `ConvertToMarkdown()`: Converts HTML content to Markdown format

## Key Workflows

### Error Handling Flow

1. Exceptions are caught in individual service methods
2. Caught exceptions are added to the global collection via `IExceptionHandler.AddException()`
3. At the end of the process, all collected exceptions are:
   - Sent to the GrznarAi API via `ApiService.SendErrorsToWebAsync()`
   - Saved to a local file (exceptions.txt)

### Application Startup Flow

1. `Program.cs` creates and runs `NewsImporterApplication`
2. `NewsImporterApplication.RunAsync()` initializes the application:
   - Loads configuration via `ConfigurationService`
   - Creates service instances with the exception handler
   - Coordinates the news import process

### News Import Flow

1. **Source Retrieval**:
   - `ApiService.GetSourcesAsync()` fetches sources from the API
   - If API call fails, falls back to `SourceFileService.LoadSourcesAsync()`

2. **News Processing**:
   - For each source, `NewsProcessor.ProcessNewsSourceAsync()` is called
   - The processor loads the page content using `PlaywrightService`
   - HTML is cleaned and converted to Markdown
   - `GeminiAiService` identifies news items in the content

3. **Individual News Processing**:
   - For each news item, `NewsProcessor.ProcessNewsItemAsync()` is called
   - The original article is loaded and processed
   - Content is cleaned, converted, and analyzed by the AI
   - Date filtering is applied based on `LastFetched - 1 day`
   - Processed items are collected in a list

4. **Data Storage & Reporting**:
   - Processed news is saved locally to `result.json`
   - `ApiService.SendNewsToWebAsync()` sends news to the API
   - `ApiService.SendErrorsToWebAsync()` sends error reports to the API
   - Error logs are saved to `exceptions.txt`

### Date-Based Filtering

1. Each source has a `LastFetched` date from the API
2. News items are filtered if:
   - `item.Date < source.LastFetched.AddDays(-1)` OR
   - `content.PublishDate < source.LastFetched.AddDays(-1)` OR
   - Both date fields are null

## Configuration Management

### User Secrets

The application uses .NET User Secrets for sensitive configuration:

```bash
dotnet user-secrets set "googleApiKey" "YOUR_GOOGLE_GEMINI_API_KEY"
dotnet user-secrets set "grznarAiBaseUrl" "https://your-api-base-url.com/"
dotnet user-secrets set "GrznarAiApiKey" "YOUR_GRZNAR_API_KEY"
```

These values are loaded by `ConfigurationService.LoadConfigAsync()` using:
```csharp
var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile(configPath)
    .AddUserSecrets<AppConfig>();
```

### Configuration File

Non-sensitive settings are stored in `appsettings.json`:

```json
{
  "googleApiKey": "",
  "pageLoadTimeoutMs": 5000,
  "grznarAiBaseUrl": ""
}
```

The actual API keys should never be stored in this file for security reasons.

## Important Notes for Development

1. **Exception Handling**: All services now support global exception handling via `IExceptionHandler`. When adding new code with try-catch blocks, always add the caught exceptions to the global collection using `_exceptionHandler.AddException(ex)`.

2. When making code changes, always follow the existing architecture and separation of concerns.

3. After any significant changes to the codebase, update both `README.md` and `vibememory.md` to reflect these changes.

4. The `ApiService` is responsible for all API communication - do not create separate HTTP clients elsewhere.

5. When processing dates, be careful with the date filtering logic to avoid missing news items.

6. The application is designed to be resilient - it will try to continue processing sources even if some fail.

7. Follow these git commands to commit changes:
   ```bash
   git add .
   git commit -m "Descriptive message about changes"
   git push origin main
   ```

## Testing and Debugging

1. Check log output - the application prints detailed logs to the console.

2. Examine generated files:
   - `result.json`: Contains the processed news items
   - `exceptions.txt`: Contains error details if any occurred
   - `sources.txt`: Contains the source configuration information

3. If API connections fail, verify that User Secrets are correctly configured.

4. For Playwright issues, ensure browser dependencies are installed:
   ```bash
   playwright install
   ```

## Common Issues and Solutions

1. **API Connection Issues**: 
   - Check User Secrets and network connectivity
   - Verify the API keys are correctly configured
   - Check if the API endpoint is accessible

2. **Gemini AI Errors**: 
   - Verify the Google API key is valid and active
   - Check for rate limiting or quota issues
   - Ensure the prompt templates haven't changed

3. **Playwright Failures**: 
   - Ensure dependencies are installed (`playwright install`)
   - Check if website structure has changed requiring selector updates
   - Try different navigation strategies if timeouts occur

4. **No News Found**: 
   - Check if source URLs are still valid
   - Verify if the page structure has changed
   - Inspect the raw content to see if news is present but not detected

5. **Missing Translations**:
   - Check if the Gemini AI prompts are returning the expected format
   - Verify the JSON structure matches the expected model

## Technical Details
- Target Framework: .NET 9.0
- Key Dependencies:
  - Microsoft.Playwright: For web scraping and browser automation
  - System.Text.Json: For JSON serialization and deserialization
  - Microsoft.Extensions.Configuration: For configuration management
  - Microsoft.Extensions.Configuration.UserSecrets: For secure API keys storage
  - Microsoft.Extensions.Http: For HTTP client factory pattern implementation 