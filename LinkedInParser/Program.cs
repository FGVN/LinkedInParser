using Microsoft.Playwright;
using Newtonsoft.Json;

class Program
{
    static async Task Main(string[] args)
    {
        string cookieFilePath = "auth_cookies.json";
        string proxyServer = null;

        // Check if proxy server is provided as command-line argument
        if (args.Length > 0)
        {
            Console.WriteLine("Proxy is used");
            proxyServer = args[0];
        }

        // Authenticate user and save cookies if necessary
        await AuthenticateAndSaveCookiesIfNeeded(cookieFilePath, proxyServer);

        // Use saved cookies to navigate to LinkedIn feed page
        await NavigateToLinkedInFeed(cookieFilePath, proxyServer);

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static async Task AuthenticateAndSaveCookiesIfNeeded(string cookieFilePath, string proxyServer = null)
    {
        if (!File.Exists(cookieFilePath))
        {
            Console.WriteLine("Auth cookies not found. Launching browser for authentication...");

            var browserOptions = new BrowserTypeLaunchOptions
            {
                Headless = false,
                Timeout = 60000,
            };

            if (proxyServer != null)
            {
                browserOptions.Proxy = new Proxy
                {
                    Server = proxyServer,
                };
            }

            var browser = await Playwright.CreateAsync().ContinueWith(t => t.Result.Chromium.LaunchAsync(browserOptions)).Unwrap();

            var contextOptions = new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true,
            };

            var authContext = await browser.NewContextAsync(contextOptions);
            var authenticationPage = await authContext.NewPageAsync();
            try
            {
                await authenticationPage.GotoAsync("https://www.linkedin.com", new PageGotoOptions { Timeout = 60000 });
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
                return;
            }
            await authenticationPage.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 60000 });

            // Check if the user is already logged in by looking for elements specific to the page after successful login
            var profilePictureElement = await authenticationPage.QuerySelectorAsync("img.feed-identity-module__member-photo");
            if (profilePictureElement == null)
            {
                // User is not logged in, perform automatic login
                await AutomaticLogin(authenticationPage);
            }

            var authCookies = await authContext.CookiesAsync();
            File.WriteAllText(cookieFilePath, JsonConvert.SerializeObject(authCookies));

            await browser.CloseAsync();
        }
    }

    static async Task AutomaticLogin(IPage authenticationPage)
    {
        Console.WriteLine("Automatic login...");

        // Create a task completion source to signal when navigation occurs
        var navigationTaskCompletionSource = new TaskCompletionSource<bool>();

        authenticationPage.FrameNavigated += (sender, args) =>
        {
            if (args.Url.StartsWith("https://www.linkedin.com/feed/"))
            {
                Console.WriteLine("Logged in successfully.");
                navigationTaskCompletionSource.TrySetResult(true);
            }
        };

        await navigationTaskCompletionSource.Task;
    }


    static async Task NavigateToLinkedInFeed(string cookieFilePath, string proxyServer = null)
    {
        using var playwright = await Playwright.CreateAsync();
        var browserOptions = new BrowserTypeLaunchOptions 
        { 
            Headless = true,
            Timeout = 60000,
        };

        if (proxyServer != null)
        {
            browserOptions.Proxy = new Proxy
            {
                Server = proxyServer,
            };
        }

        var browser = await playwright.Chromium.LaunchAsync(browserOptions);

        var contextOptions = new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
        };

        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();

        var cookiesJson = File.ReadAllText(cookieFilePath);
        var cookies = JsonConvert.DeserializeObject<Cookie[]>(cookiesJson);

        foreach (var cookie in cookies)
        {
            await context.AddCookiesAsync(new[] { cookie });
        }

        await page.GotoAsync("https://www.linkedin.com/feed/", new PageGotoOptions { Timeout = 60000 });
        await page.WaitForLoadStateAsync();

        var profilePictureElement = await page.QuerySelectorAsync("img.feed-identity-module__member-photo");

        if (profilePictureElement != null)
        {
            var profilePictureUrl = await profilePictureElement.GetAttributeAsync("src");
            var httpClient = new System.Net.Http.HttpClient();
            var profilePictureBytes = await httpClient.GetByteArrayAsync(profilePictureUrl);

            var folderPath = "ProfilePictures";
            Directory.CreateDirectory(folderPath);
            var profilePictureFilePath = Path.Combine(folderPath, "profile_picture.jpg");
            File.WriteAllBytes(profilePictureFilePath, profilePictureBytes);
            Console.WriteLine($"Profile picture saved to: {profilePictureFilePath}");
        }
        else
        {
            Console.WriteLine("Profile picture not found on the page.");
        }

        await browser.CloseAsync();
    }
}

