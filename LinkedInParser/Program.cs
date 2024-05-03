using Microsoft.Playwright;
using Newtonsoft.Json;

class Program
{
    static async Task Main(string[] args)
    {
        string cookieFilePath = "auth_cookies.json";

        // Authenticate user and save cookies if necessary
        await AuthenticateAndSaveCookiesIfNeeded(cookieFilePath);

        // Use saved cookies to navigate to LinkedIn feed page
        await NavigateToLinkedInFeed(cookieFilePath);

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static async Task AuthenticateAndSaveCookiesIfNeeded(string cookieFilePath)
    {
        if (!File.Exists(cookieFilePath))
        {
            Console.WriteLine("Auth cookies not found. Launching browser for authentication...");

            var browserOptions = new BrowserTypeLaunchOptions
            {
                Headless = false
            };

            var browser = await Playwright.CreateAsync().ContinueWith(t => t.Result.Chromium.LaunchAsync(browserOptions)).Unwrap();
            var authContext = await browser.NewContextAsync();
            var authenticationPage = await authContext.NewPageAsync();

            await authenticationPage.GotoAsync("https://www.linkedin.com");
            await authenticationPage.WaitForLoadStateAsync();

            var loginButton = await authenticationPage.QuerySelectorAsync("a.nav__button-secondary");
            if (loginButton != null)
            {
                Console.WriteLine("Please log in to LinkedIn. Press any key to continue after logging in...");
                Console.ReadKey();
            }

            var authCookies = await authContext.CookiesAsync();
            File.WriteAllText(cookieFilePath, JsonConvert.SerializeObject(authCookies));

            await browser.CloseAsync();
        }
    }

    static async Task NavigateToLinkedInFeed(string cookieFilePath)
    {
        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var cookiesJson = File.ReadAllText(cookieFilePath);
        var cookies = JsonConvert.DeserializeObject<Cookie[]>(cookiesJson);

        foreach (var cookie in cookies)
        {
            await context.AddCookiesAsync(new[] { cookie });
        }

        await page.GotoAsync("https://www.linkedin.com/feed/");
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
