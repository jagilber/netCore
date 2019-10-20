using Microsoft.Identity.Client.Extensibility;
//using Microsoft.Toolkit.Wpf.UI.Controls;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.Web.UI;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using Microsoft.Toolkit.Wpf.UI.Controls;

namespace netCoreMsal
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string ClientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        public static bool Force = false;
        public static string RedirectUri = "http://localhost";//"urn:ietf:wg:oauth:2.0:oob";
        public static string Resource = null;
        public static string TenantId = "common";

        public static List<string> Scopes { get; set; } = new List<string>();

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
                string arg = e.Args[i].TrimStart('/').TrimStart('-').ToLower();
                if (arg == "resource") { Resource = e.Args[i + 1]; }
                if (arg == "redirectUri") { RedirectUri = e.Args[i + 1]; }
                if (arg == "clientId") { ClientId = e.Args[i + 1]; }
                if (arg == "tenantId") { TenantId = e.Args[i + 1]; }
                if (arg == "force") { Force = true; }
            }

            //Authorize();
        }
    }

    public class CustomWebUi : ICustomWebUi
    {
        private readonly Dispatcher _dispatcher;
        public Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.WebViewControlNavigationCompletedEventArgs NavigationCompletedResponse { get; set; }
        public Uri _uri;
        public Window Window { get; set; }
        public WebView WebViewInstance { get; set; } = new WebView();
        bool _redirected = false;
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
                    else if (e.Uri.Query.Contains("code="))// | e.Uri.Query.Contains("wa=wsignin1"))
                    {
                        Console.WriteLine("navigationcompleted:code");
                        tcs.SetResult(e.Uri);
                        Window.DialogResult = true;
                        Window.Close();
                    }
                    else if (e.Uri.Query.Contains("wa=wsignin1") & !_redirected)
                    {
                        Console.WriteLine("navigationcompleted:sts signin");
                        NavigationCompletedResponse = e;
                        //WebViewInstance.Source = e.Uri;
                        //Console.WriteLine("navigationcompleted:sts signin source updated");
                        //WebViewInstance.UpdateLayout();
                        if (_redirected)
                        {
                            tcs.SetResult(e.Uri);
                            Window.DialogResult = true;
                            Window.Close();
                        }

                        //WebViewInstance.Navigate(e.Uri); // not  working
                        //string stsContent = new WebClient().DownloadString(e.Uri);
                        //Console.WriteLine("sts content");
                        //Console.WriteLine(JsonSerializer.Serialize(stsContent, new JsonSerializerOptions() { WriteIndented = true }));
                        //WebViewInstance.NavigateToString(stsContent);
                        //Console.WriteLine("navigationcompleted:sts navigated");
                        //_window.UpdateLayout(); // not tried
                        //tcs.SetResult(e.Uri); // not working
                        //_window.DialogResult = true; // not working
                        //Console.WriteLine("navigationcompleted:sts updatedlayout");
                        //WebClient webClient = new WebClient();
                        //webClient.
                        _redirected = true;
                    }
                    //    WebView webView = new WebView();
                    //    Window w = new Window
                    //    {
                    //        Title = "Authorization sts",
                    //        WindowStyle = WindowStyle.ToolWindow,
                    //        Content = webView,
                    //        Width = 500,
                    //        Height = 500,
                    //        ResizeMode = ResizeMode.CanResizeWithGrip
                    //    };

                    //    w.Loaded += (_, __) => webView.Navigate(e.Uri);
                    //    w.Show();
                    //    //w.DialogResult = true;
                    //    //w.Close();

                    //}
                    //else if(e.Uri.AbsoluteUri.Contains("stsredirect"))
                    //{
                    //    Console.WriteLine("navigationcompleted:stsredirect");
                    //    NavigationCompletedResponse = e;
                    //    //AcquireAuthorizationCodeAsync(authorizationUri, e.Uri); // didnt work
                    //    //AcquireAuthorizationCodeAsync(e.Uri, new Uri("/adfs/ls/")); // 
                    //    //webView.Navigate(e.Uri);
                    //    tcs.SetResult(e.Uri);
                    //    //webView.Refresh();
                    //}

                };

                WebViewInstance.UnsupportedUriSchemeIdentified += (_, e) =>
                {
                    Console.WriteLine("unsupported");
                    //System.Diagnostics.Debug.WriteLine(e.Uri);
                    if (e.Uri.Query.Contains("code="))
                    {
                        tcs.SetResult(e.Uri);
                        Window.DialogResult = true;
                        Window.Close();
                    }
                    else
                    {
                        Console.WriteLine("unknown error");
                        //System.Diagnostics.Debug.WriteLine(e.Uri);
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
