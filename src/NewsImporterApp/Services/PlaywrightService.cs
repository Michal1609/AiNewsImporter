using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace NewsImporterApp.Services
{
    public class PlaywrightService : IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;

        /// <summary>
        /// Inicializuje novou instanci služby Playwright
        /// </summary>
        public PlaywrightService()
        {
        }

        /// <summary>
        /// Získá obsah webové stránky pomocí Playwright
        /// </summary>
        /// <param name="url">URL adresa stránky</param>
        /// <param name="timeout">Maximální doba načítání v milisekundách</param>
        /// <returns>HTML obsah stránky</returns>
        public async Task<string> GetPageContentAsync(string url, int timeout)
        {
            // Inicializace Playwright, pokud ještě nebyl inicializován
            await InitializePlaywrightAsync();

            // Vytvoření nové stránky
            var page = await _context!.NewPageAsync();

            try
            {
                // Nastavení vlastností stránky pro lepší zobrazení
                await page.SetViewportSizeAsync(1920, 1080);

                // Navigace na stránku s timeoutem
                // Čekáme pouze na základní načtení DOMContentLoaded, které je rychlé
                var gotoOptions = new PageGotoOptions
                {
                    Timeout = timeout,
                    WaitUntil = WaitUntilState.DOMContentLoaded
                };

                // Navigace na stránku
                await page.GotoAsync(url, gotoOptions);

                // Provedeme rychlý scroll pro načtení lazy-loaded obsahu
                await page.EvaluateAsync(@"() => {
                    window.scrollTo(0, document.body.scrollHeight / 2);
                }");

                // Počkáme 500ms, aby se načetl obsah po scrollování
                await Task.Delay(500);

                // Nyní zkusíme počkat na kompletní načtení stránky (NetworkIdle)
                // Pokud se stránka načte rychleji, metoda proběhne okamžitě
                // Pokud ne, čeká až do timoutu
                try
                {
                    // Použijeme CancellationToken s časovým limitem
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(timeout);

                    // Vytvoříme task pro NetworkIdle, který se zruší po vypršení timeoutu
                    var networkIdleTask = page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                        new PageWaitForLoadStateOptions { Timeout = timeout });

                    // Počkáme na dokončení úlohy nebo zrušení
                    try
                    {
                        await networkIdleTask;
                        Console.WriteLine("Stránka je kompletně načtena (NetworkIdle)");
                    }
                    catch (PlaywrightException ex) when (ex.Message.Contains("timeout") || ex.Message.Contains("cancelled"))
                    {
                        Console.WriteLine($"NetworkIdle timeout vypršel: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při čekání na NetworkIdle: {ex.Message}");
                }

                // Získání HTML obsahu stránky - bez ohledu na to, zda se kompletně načetla
                var content = await page.ContentAsync();

                // Pokud je obsah příliš malý, zkusíme získat více pomocí JavaScriptu

                try
                {
                    // Získáme text pomocí JavaScriptu (alternativní přístup)
                    var jsContent = await page.EvaluateAsync<string>("() => document.body.innerHTML");

                    // Pokud je JavaScript verze lepší, použijeme ji
                    if (jsContent.Length > content.Length)
                    {
                        content = jsContent;
                        Console.WriteLine("Použita JavaScript verze obsahu stránky, která je delší");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při získávání obsahu pomocí JavaScriptu: {ex.Message}");
                }


                return content;
            }
            finally
            {
                // Uzavření stránky
                await page.CloseAsync();
            }
        }

        /// <summary>
        /// Inicializuje Playwright a vytvoří prohlížeč
        /// </summary>
        private async Task InitializePlaywrightAsync()
        {
            if (_playwright == null)
            {
                // Inicializace Playwright
                _playwright = await Playwright.CreateAsync();

                // Nastavení prohlížeče s parametry pro obejití detekce headless režimu
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[]
                    {
                        "--disable-blink-features=AutomationControlled",
                        "--disable-features=IsolateOrigins,site-per-process"
                    }
                });

                // Vytvoření kontextu s nastavením pro obejití detekce headless režimu
                _context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
                    IgnoreHTTPSErrors = true
                });

                // Nastavení JavaScript před každou navigací pro obejití detekce headless
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

        /// <summary>
        /// Uvolní prostředky Playwright
        /// </summary>
        public async ValueTask DisposeAsync()
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
    }
}
