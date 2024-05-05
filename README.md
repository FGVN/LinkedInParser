# LinkedInParser

LinkedInParser is a .NET Core application that authenticates users and retrieves LinkedIn profile pictures using Playwright.

## Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your_username/LinkedInParser.git
   ```

2. **Navigate to the project directory:**
   ```bash
   cd LinkedInParser
   ```
3. **Install playwright dependencies (download powershell if needed)**
    ```bash
    pwsh bin/Debug/net6.0/playwright.ps1 install
    ```
4. **Build the project:**
   ```bash
   dotnet build
   ```

5. **Run the application with optional proxy:**
   ```bash
   dotnet run -- "http://proxy_server_address:port"
   ```
   Replace `"http://proxy_server_address:port"` with your desired proxy server address and port.

## Usage

Once the application is running, it will authenticate the user (if necessary) and save cookies to a file named `auth_cookies.json`. These cookies will be used to navigate to the LinkedIn feed page and retrieve the profile picture.

After running the application, you can find the profile picture saved in the `ProfilePictures` folder within the project directory.

**Note:** Ensure that the required dependencies are installed, including Playwright and Newtonsoft.Json.

