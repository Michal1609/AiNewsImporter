# AI News Importer

An automated tool for fetching, processing, and importing AI-related news from various sources. The application uses Playwright for web scraping, Gemini AI for content analysis and translation, and integrates with a central news management system.

## Purpose

AI News Importer serves as a data collection pipeline that:

1. Retrieves news sources from a configured API endpoint
2. Scrapes news content from these sources
3. Uses AI to analyze and extract relevant information
4. Translates content between English and Czech
5. Imports the processed news into a central news database

## Features

- Automated web scraping with Playwright
- Content analysis and summarization with Google's Gemini AI
- Translation capabilities (English to Czech)
- Robust error handling and reporting
- Configuration through both appsettings.json and User Secrets
- Integration with a central news management API
- Smart date-based filtering to avoid duplicate imports

## Requirements

- .NET 9.0 or higher
- Playwright browser dependencies
- Google Gemini API key
- Access to the GrznarAi API service

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/Michal1609/AiNewsImporter.git
cd AiNewsImporter
```

### 2. Install Dependencies

```bash
dotnet restore
```

### 3. Install Playwright Dependencies

```bash
cd src/NewsImporterApp
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

### 4. Configuration

The application requires several API keys and configuration settings that should be stored securely using .NET User Secrets.

#### Configure User Secrets

Initialize user secrets for the project:

```bash
cd src/NewsImporterApp
dotnet user-secrets init
```

Add required secrets:

```bash
dotnet user-secrets set "googleApiKey" "YOUR_GOOGLE_GEMINI_API_KEY"
dotnet user-secrets set "grznarAiBaseUrl" "https://your-api-base-url.com/"
dotnet user-secrets set "GrznarAiApiKey" "YOUR_GRZNAR_API_KEY"
```

#### Configuration File

The application also uses `appsettings.json` for non-sensitive settings:

```json
{
  "googleApiKey": "",
  "pageLoadTimeoutMs": 5000,
  "grznarAiBaseUrl": ""
}
```

Note: The actual API keys should be stored in User Secrets, not in the appsettings.json file. The file contains empty placeholders for security.

### 5. Running the Application

```bash
cd src/NewsImporterApp
dotnet run
```

## How It Works

### News Collection Process

1. **Source Retrieval**: The application first contacts the GrznarAi API to get a list of news sources to process. If API connection fails, it falls back to locally stored sources.

2. **Content Scraping**: For each source, the application:
   - Loads the webpage using Playwright
   - Cleans the HTML content
   - Converts it to Markdown format
   - Uses Gemini AI to identify news items

3. **Individual News Processing**: For each news item identified:
   - The original article is fetched
   - Content is cleaned and converted
   - Gemini AI extracts key information, summary, and creates translations
   - Date filtering ensures only recent news is collected

4. **Data Storage & Reporting**:
   - Processed news is stored locally in a JSON file
   - News and error reports are sent to the GrznarAi API
   - Detailed error logs are maintained

### Date-Based Filtering Logic

The application implements an intelligent date-based filtering mechanism to avoid importing duplicate or outdated news:

1. **Source Last Fetched Date**: When retrieving news sources from the API, each source contains a `LastFetched` timestamp indicating when news from this source was last processed.

2. **Date Window for Collection**: The application only processes news items that have been published after `LastFetched - 1 day`. This one-day overlap ensures that no news items are missed due to timezone differences or delayed publication updates.

3. **Two-Level Date Verification**:
   - First check: When extracting news items from the source page, items with dates older than the date window are skipped
   - Second check: When processing individual news items, the publication date from the article is verified again against the date window

4. **Date Resolution Logic**: In cases where multiple date sources exist (list date vs. article date), the application uses the earliest available date for comparison, ensuring maximum accuracy in filtering.

5. **Update Mechanism**: After successful processing, the `LastFetched` date for each source is updated in the local repository and will be used as the reference point in the next run.

This approach minimizes duplicate processing while ensuring comprehensive coverage of all relevant news sources.

### Architecture

The application follows a clean separation of concerns:

- **Core**: Main application orchestration and configuration models
- **Services**: Specialized services for API communication, news processing, etc.
- **Models**: Data models representing news items and sources
- **Utilities**: Helper functions for HTML processing, markdown conversion, etc.

## Error Handling

The application maintains detailed error logs and sends error reports to the configured API. Errors are also saved locally to a file for troubleshooting.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is proprietary and confidential. Unauthorized copying, transfer, or reproduction of the contents is strictly prohibited.
