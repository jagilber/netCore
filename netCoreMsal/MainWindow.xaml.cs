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
            PublicClientApp = PublicClientApplicationBuilder.Create(App.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, App.TenantId)
                .WithRedirectUri(App.RedirectUri)
                .Build();

            CustomWebUi customWebUi = new CustomWebUi(Dispatcher);
            AuthenticationResult authenticationResult = default;

            try
            {
                Console.WriteLine($"scope: {App.Scope[0].ToString()}");
                Console.WriteLine($"resource: {App.Resource}");
                Console.WriteLine($"clientID: {App.ClientId}");
                Console.WriteLine($"RedirectUri: {App.RedirectUri}");
                Console.WriteLine($"tenantID: {App.TenantId}");

                authenticationResult = await PublicClientApp.AcquireTokenInteractive(App.Scope).WithCustomWebUi(customWebUi).ExecuteAsync(); // works through to phone auth but fails AADSTS65002
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine(authenticationResult.AccessToken);
            App.Current.Shutdown();
        }

    }
    
}
