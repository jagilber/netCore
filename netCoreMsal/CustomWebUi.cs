using Microsoft.Identity.Client.Extensibility;
using Microsoft.Toolkit.Wpf.UI.Controls;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace netCoreMsal
{
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