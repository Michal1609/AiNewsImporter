# NewsImporter Application

This application fetches and processes AI news from various sources.

## Configuration

Before running the application, configure the `appsettings.json` file with the following settings:

```json
{
    "googleApiKey": "your-google-api-key",
    "pageLoadTimeoutMs": 5000,
    "grznarAiApiKey": "your-grznar-ai-api-key",
    "grznarAiBaseUrl": "https://grznar.com"
}
```

## Running the Application

### Update Sources

To update the sources list from the GrznarAi API:

```bash
dotnet run update-sources
```

This will:
1. Fetch the latest sources list from the API endpoint `api/ainews/sources`
2. Update the `sources.txt` file with the format: `Url;Type;LastFetched`
3. Preserve existing sources and update their information if they exist in the API response

### Process News

To process news from the sources list:

```bash
dotnet run
```

This will:
1. Read sources from `sources.txt`
2. Visit each URL and extract news articles
3. Process the news content using Gemini AI
4. Save the results to `result.json`

## Sources File Format

The `sources.txt` file uses the following format:

```
Url;Type;LastFetched
```

Where:
- `Url`: The URL of the news source
- `Type`: The type of source (Web, Facebook, Twitter)
- `LastFetched`: The date when the source was last fetched (format: yyyyMMdd)

Example:
```
https://ai.googleblog.com;Web;20250415
``` 