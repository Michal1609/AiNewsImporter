using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NewsImporterApp.Core;

namespace NewsImporterApp.Services
{
    public class PlaywrightService : IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private readonly IExceptionHandler? _exceptionHandler;

        /// <summary>
        /// Initializes a new instance of the Playwright service
        /// </summary>
        /// <param name="exceptionHandler">Optional exception handler for global tracking</param>
        public PlaywrightService(IExceptionHandler? exceptionHandler = null)
        {
            _exceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// Gets the content of a web page using Playwright
        /// </summary>
        /// <param name="url">URL address of the page</param>
        /// <param name="timeout">Maximum loading time in milliseconds</param>
        /// <returns>HTML content of the page</returns>
        public async Task<string> GetPageContentAsync(string url, int timeout)
        {
            try
            {
                // Initialize Playwright if it hasn't been initialized yet
                await InitializePlaywrightAsync();

                // Create a new page
                var page = await _context!.NewPageAsync();

                try
                {
                    // Set page properties for better display
                    await page.SetViewportSizeAsync(1920, 1080);

                    // Navigate to the page with timeout
                    // We only wait for basic DOMContentLoaded, which is fast
                    var gotoOptions = new PageGotoOptions
                    {
                        Timeout = timeout,
                        WaitUntil = WaitUntilState.DOMContentLoaded
                    };

                    // Navigate to the page
                    await page.GotoAsync(url, gotoOptions);

                    // Perform a quick scroll to load lazy-loaded content
                    await page.EvaluateAsync(@"() => {
                        window.scrollTo(0, document.body.scrollHeight / 2);
                    }");

                    // Wait 500ms for content to load after scrolling
                    await Task.Delay(500);

                    // Now try to wait for the complete page load (NetworkIdle)
                    // If the page loads faster, the method will complete immediately
                    // If not, it will wait until timeout
                    try
                    {
                        // Use CancellationToken with time limit
                        using var cts = new CancellationTokenSource();
                        cts.CancelAfter(timeout);

                        // Create a task for NetworkIdle that will be canceled after timeout
                        var networkIdleTask = page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                            new PageWaitForLoadStateOptions { Timeout = timeout });

                        // Wait for task completion or cancellation
                        try
                        {
                            await networkIdleTask;
                            Console.WriteLine("Page is completely loaded (NetworkIdle)");
                        }
                        catch (PlaywrightException ex) when (ex.Message.Contains("timeout") || ex.Message.Contains("cancelled"))
                        {
                            Console.WriteLine($"NetworkIdle timeout expired: {ex.Message}");
                            _exceptionHandler?.AddException(ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error waiting for NetworkIdle: {ex.Message}");
                        _exceptionHandler?.AddException(ex);
                    }

                    // Get HTML content of the page - regardless of whether it loaded completely
                    var content = await page.ContentAsync();

                    // If the content is too small, try to get more using JavaScript

                    try
                    {
                        // Get text using JavaScript (alternative approach)
                        var jsContent = await page.EvaluateAsync<string>("() => document.body.innerHTML");

                        // If the JavaScript version is better, use it
                        if (jsContent.Length > content.Length)
                        {
                            content = jsContent;
                            Console.WriteLine("Using JavaScript version of page content, which is longer");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting content using JavaScript: {ex.Message}");
                        _exceptionHandler?.AddException(ex);
                    }


                    return content;
                }
                finally
                {
                    // Close the page
                    await page.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting page content: {ex.Message}");
                _exceptionHandler?.AddException(ex);
                // Return empty string on error
                return string.Empty;
            }
        }

        /// <summary>
        /// Initializes Playwright and creates a browser
        /// </summary>
        private async Task InitializePlaywrightAsync()
        {
            try
            {
                if (_playwright == null)
                {
                    // Initialize Playwright
                    _playwright = await Playwright.CreateAsync();

                    // Set up browser with parameters to bypass headless mode detection
                    _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                    {
                        Headless = true,
                        Args = new[]
                        {
                            "--disable-blink-features=AutomationControlled",
                            "--disable-features=IsolateOrigins,site-per-process"
                        }
                    });

                    // Create context with settings to bypass headless mode detection
                    _context = await _browser.NewContextAsync(new BrowserNewContextOptions
                    {
                        ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
                        IgnoreHTTPSErrors = true
                    });

                    // Set JavaScript before each navigation to bypass headless detection
                    await _context.AddInitScriptAsync(@"
                        Object.defineProperty(navigator, 'webdriver', {
                            get: () => false,
                        });
                        
                        window.chrome = {
                            runtime: {},
                        };
                        
                        Object.defineProperty(navigator, 'plugins', {
                            get: () => [1, 2, 3, 4, 5],
                        });
                        
                        Object.defineProperty(navigator, 'languages', {
                            get: () => ['cs-CZ', 'cs', 'en-US', 'en'],
                        });
                    ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Playwright: {ex.Message}");
                _exceptionHandler?.AddException(ex);
                throw;
            }
        }

        /// <summary>
        /// Releases Playwright resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_context != null)
                {
                    await _context.DisposeAsync();
                    _context = null;
                }

                if (_browser != null)
                {
                    await _browser.CloseAsync();
                    await _browser.DisposeAsync();
                    _browser = null;
                }

                if (_playwright != null)
                {
                    _playwright.Dispose();
                    _playwright = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing Playwright resources: {ex.Message}");
                _exceptionHandler?.AddException(ex);
            }
        }
    }
}
