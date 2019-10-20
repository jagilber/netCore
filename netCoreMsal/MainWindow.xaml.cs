using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace netCoreMsal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static IPublicClientApplication PublicClientApp;
        public MainWindow()
        {
            InitializeComponent();
            Authorize();
        }
        private async void Authorize()
        {
            //AuthenticationContext authContext = new AuthenticationContext($"https://login.microsoftonline.com/{App.TenantId}");
            //AuthenticationResult result = default;
            //Window window = new Window();
            //MainWindow mainWindow = (MainWindow)this.FindResource("MainWindow");
            //MainWindow mainWindow = new MainWindow();

            PublicClientApp = PublicClientApplicationBuilder.Create(App.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, App.TenantId)
                .WithRedirectUri(App.RedirectUri)
                .Build();

            CustomWebUi customWebUi = new CustomWebUi(Dispatcher);
            AuthenticationResult authenticationResult = default;

            try
            {
                App.Scopes.Add("kusto.read");
                //authenticationResult = await PublicClientApp.AcquireTokenInteractive(App.Scopes).ExecuteAsync(); // pops in open browser. gets config error  AADSTS65002: Consent between first party applications and resources must be configured via preauthorization.  Visit https://identitydocs.azurewebsites.net/static/aad/preauthorization.html for details
                //authenticationResult = await PublicClientApp.AcquireTokenInteractive(App.Scopes).WithUseEmbeddedWebView(true).ExecuteAsync(); //true not avail on core,  false pops in open browser. gets config error  AADSTS65002
                authenticationResult = await PublicClientApp.AcquireTokenInteractive(App.Scopes).WithCustomWebUi(customWebUi).ExecuteAsync(); // works through to phone auth but fails AADSTS65002
                //PublicClientApp.AcquireTokenSilent // not tried

                /*
                Console.WriteLine("first try");
                result = await authContext.AcquireTokenAsync(App.Resource,
                    App.ClientId,
                    new Uri(App.RedirectUri),
                    new PlatformParameters(App.Force ? PromptBehavior.Always : PromptBehavior.Auto, customWebUi));
                    */
            }
            catch (Exception e)
            {
                //Console.WriteLine(JsonSerializer.Serialize<Exception>(e, new JsonSerializerOptions() { WriteIndented = true, MaxDepth = 1 }));
                Console.WriteLine(e.ToString());
                //customWebUi.WebViewInstance.Navigate(customWebUi.NavigationCompletedResponse.Uri);
                //customWebUi.Window.ShowDialog();
                //Console.WriteLine("second try");
                //CustomWebUi customWebUi2 = new CustomWebUi(Dispatcher);
                //customWebUi2.WebViewInstance.Source = customWebUi.NavigationCompletedResponse.Uri;

                //result = await authContext.AcquireTokenAsync(App.Resource,
                //    App.ClientId,
                //    customWebUi.NavigationCompletedResponse.Uri,
                //    new PlatformParameters(PromptBehavior.Auto, customWebUi2));
                //Console.ReadLine();

                //result = null;
            }

            //if (customWebUi.NavigationCompletedResponse != null)
            //{
                //Console.WriteLine("checking response");
                //WebViewControlNavigationCompletedEventArgs response = customWebUi.NavigationCompletedResponse;
                //customWebUi.WebViewInstance.Source = response.Uri;
                //Console.WriteLine($"navigationcompleted:sts signin source updated {response}");
                //customWebUi.WebViewInstance.UpdateLayout();

                //result = authContext.AcquireTokenByAuthorizationCodeAsync(); //?? not tried
                //CustomWebUi customWebUi2 = new CustomWebUi(mainWindow.Dispatcher);
                //var stsResult = customWebUi2.AcquireAuthorizationCodeAsync(response.Uri, new Uri("https://localhost:44321/"));
            //}
            //if (result == null & !_force)
            //{
            //    result = await authContext.AcquireTokenAsync(_resource,
            //    _clientId,
            //    new Uri(_redirectUri),
            //    new PlatformParameters(PromptBehavior.Always, customWebUi));
            //}

            Console.WriteLine(authenticationResult.AccessToken);
            App.Current.Shutdown();
        }

    }
    
}
