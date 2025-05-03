using NewsImporterApp.Core;
using Microsoft.Playwright;

var driverPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".playwright", "node", "linux-arm64", "node");

if (!File.Exists(driverPath))
{
    Console.WriteLine("Node driver not found, installing Playwright...");
    int exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
    if (exitCode != 0)
    {
        Console.WriteLine($"Playwright install failed with code {exitCode}");
        return exitCode;
    }
}

// Vytvoření a spuštění aplikace
var app = new NewsImporterApplication();
return await app.RunAsync();
