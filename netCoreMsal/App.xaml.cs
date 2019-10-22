using Microsoft.Identity.Client.Extensibility;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Toolkit.Wpf.UI.Controls;
using Microsoft.Identity.Client;


//https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-permissions-and-consent#requesting-individual-user-consent

namespace netCoreMsal
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IPublicClientApplication PublicClientApp;
        public static string ClientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        public static bool Force = false;
        public static string RedirectUri = "http://localhost:44321";
        public static string Resource = null;
        public static string TenantId = "common";

        public static List<string> Scope { get; set; } = new List<string> { ".default" };//{"kusto.read"};

        private void App_Startup(object sender, StartupEventArgs e)
        {
            Console.WriteLine("app_startup. run from non administrator prompt");
            if (e.Args.Length < 2)
            {
                Console.WriteLine("requires /resource argument. optional /redirectUri /clientId /tenantId");
                throw new ArgumentException("requires /resource argument. optional /redirectUri /clientId /tenantId");
            }

            for (int i = 0; i != e.Args.Length; ++i)
            {
                string arg = e.Args[i].ToLower();
                if (arg.StartsWith('/')) { arg = '-' + arg.TrimStart('/'); }
                if (arg == "-?") { ShowHelp(); }
                if (arg == "-resource") { Resource = e.Args[i + 1]; }
                if (arg == "-redirecturi") { RedirectUri = e.Args[i + 1]; }
                if (arg == "-clientid") { ClientId = e.Args[i + 1]; }
                if (arg == "-tenantid") { TenantId = e.Args[i + 1]; }
                if (arg == "-scope") { Scope.Add(e.Args[i + 1]); }
                if (arg == "-force") { Force = true; }
            }

            Authorize();
        }

        private void ShowHelp()
        {
            Console.WriteLine("requires -resource argument. optional -redirectUri -clientId -tenantId -scope");
            App.Current.Shutdown();
        }


        private async void Authorize()
        {
            PublicClientApp = PublicClientApplicationBuilder.Create(App.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, App.TenantId)
                //.WithRedirectUri(App.RedirectUri)
                .WithDefaultRedirectUri()
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

                //authenticationResult = await PublicClientApp.AcquireTokenInteractive(App.Scope).WithCustomWebUi(customWebUi).ExecuteAsync(); // works through to phone auth but fails AADSTS65002
                authenticationResult = await PublicClientApp
                    .AcquireTokenInteractive(App.Scope)
                    .WithCustomWebUi(customWebUi)
                    .ExecuteAsync(); // 
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine(authenticationResult.AccessToken);
            App.Current.Shutdown();
        }
    }
    
    public class CustomWebUi : ICustomWebUi
    {
        private readonly Dispatcher _dispatcher;
        public Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.WebViewControlNavigationCompletedEventArgs NavigationCompletedResponse { get; set; }
        public Uri _uri;
        public Window Window { get; set; }
        public WebView WebViewInstance { get; set; } = new WebView();
        public CustomWebUi(Window window) : this(window.Dispatcher)
        {
            Window = window;
        }

        public CustomWebUi(Dispatcher dispatcher, Uri uri) : this(dispatcher)
        {
            _uri = uri;
        }

        public CustomWebUi(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public string Authorization { get; set; }

        public Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
        {
            TaskCompletionSource<Uri> tcs = new TaskCompletionSource<Uri>();
            _dispatcher.InvokeAsync(() =>
            {
                if (Window == null)
                {
                    Window = new Window();
                }

                Window.Title = "Authorization";
                Window.WindowStyle = WindowStyle.ToolWindow;
                Window.Content = WebViewInstance;
                Window.Width = 500;
                Window.Height = 500;
                Window.ResizeMode = ResizeMode.CanResizeWithGrip;
                Window.Loaded += (_, __) => WebViewInstance.Navigate(authorizationUri);

                WebViewInstance.NavigationCompleted += (_, e) =>
                {
                    Console.WriteLine("navigationcompleted");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                    Console.WriteLine(e.Uri);


                    if (e.Uri.Query.Contains("error="))
                    {
                        Console.WriteLine("navigationcompleted:error");
                        tcs.SetException(new Exception(e.Uri.Query));
                        Window.DialogResult = false;
                        Window.Close();
                    }
                    else if (e.Uri.Query.Contains("code="))
                    {
                        Console.WriteLine("navigationcompleted:code");
                        tcs.SetResult(e.Uri);
                        Window.DialogResult = true;
                        Window.Close();
                    }
                };

                WebViewInstance.UnsupportedUriSchemeIdentified += (_, e) =>
                {
                    Console.WriteLine("unsupported");
                    if (e.Uri.Query.Contains("code="))
                    {
                        tcs.SetResult(e.Uri);
                        Window.DialogResult = true;
                        Window.Close();
                    }
                    else
                    {
                        Console.WriteLine("unknown error");
                        tcs.SetException(new Exception($"Unknown error: {e.Uri}"));
                        Window.DialogResult = false;
                        Window.Close();
                    }
                };

                    // test
                    WebViewInstance.ContainsFullScreenElementChanged += (_, e) =>
                        {
                    Console.WriteLine("ContainsFullScreenElementChanged");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.ContentLoading += (_, e) =>
                {
                    Console.WriteLine("ContentLoading");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.DOMContentLoaded += (_, e) =>
                {
                    Console.WriteLine("DOMContentLoaded");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.FrameContentLoading += (_, e) =>
                {
                    Console.WriteLine("FrameContentLoading");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.FrameDOMContentLoaded += (_, e) =>
                {
                    Console.WriteLine("FrameDOMContentLoaded");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.FrameNavigationCompleted += (_, e) =>
                {
                    Console.WriteLine("FrameNavigationCompleted");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.FrameNavigationStarting += (_, e) =>
                {
                    Console.WriteLine("FrameNavigationStarting");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.LongRunningScriptDetected += (_, e) =>
                {
                    Console.WriteLine("LongRunningScriptDetected");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.MoveFocusRequested += (_, e) =>
                {
                    Console.WriteLine("MoveFocusRequested");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.NavigationStarting += (_, e) =>
                {
                    Console.WriteLine("NavigationStarting");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                    if (e.Uri.AbsoluteUri.Contains("stsredirect"))
                    {
                        Console.WriteLine("stsredirect");
                            //webView.Navigate(e.Uri);
                            //webView.Refresh();
                        }

                };

                WebViewInstance.NewWindowRequested += (_, e) =>
                {
                    Console.WriteLine("NewWindowRequested");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.PermissionRequested += (_, e) =>
                {
                    Console.WriteLine("PermissionRequested");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.ScriptNotify += (_, e) =>
                {
                    Console.WriteLine("ScriptNotify");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.UnsafeContentWarningDisplaying += (_, e) =>
                {
                    Console.WriteLine("UnsafeContentWarningDisplaying");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.UnviewableContentIdentified += (_, e) =>
                {
                    Console.WriteLine("UnviewableContentIdentified");
                    Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };


                    // end test

                    if (Window.ShowDialog() != true && !tcs.Task.IsCompleted)
                {
                    Console.WriteLine("cancelled");
                        //System.Diagnostics.Debug.WriteLine("cancelled");
                        tcs.SetException(new Exception("canceled"));
                }



            });

            return tcs.Task;
        }
    }
}
