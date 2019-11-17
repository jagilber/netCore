// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

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
        public Uri _uri;
        private readonly Dispatcher _dispatcher;

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
        public Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.WebViewControlNavigationCompletedEventArgs NavigationCompletedResponse { get; set; }
        public WebView WebViewInstance { get; set; } = new WebView();
        public Window Window { get; set; }

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
                    App.WriteOutput("navigationcompleted");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));

                    if (e.Uri.Query.Contains("error="))
                    {
                        App.WriteOutput("navigationcompleted:error");
                        tcs.SetException(new Exception(e.Uri.Query));
                        Window.DialogResult = false;
                    }
                    else if (e.Uri.Query.Contains("code="))
                    {
                        App.WriteOutput("navigationcompleted:code");
                        tcs.SetResult(e.Uri);
                        Window.DialogResult = true;
                    }
                };

                WebViewInstance.UnsupportedUriSchemeIdentified += (_, e) =>
                {
                    App.WriteOutput("unsupported");
                    if (e.Uri.Query.Contains("code="))
                    {
                        tcs.SetResult(e.Uri);
                        Window.DialogResult = true;
                    }
                    else
                    {
                        App.WriteOutput("unknown error");
                        tcs.SetException(new Exception($"Unknown error: {e.Uri}"));
                        Window.DialogResult = false;
                    }
                };

                // debug
                WebViewInstance.ContainsFullScreenElementChanged += (_, e) =>
                {
                    App.WriteOutput("ContainsFullScreenElementChanged");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.ContentLoading += (_, e) =>
                {
                    App.WriteOutput("ContentLoading");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.DOMContentLoaded += (_, e) =>
                {
                    App.WriteOutput("DOMContentLoaded");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.FrameContentLoading += (_, e) =>
                {
                    App.WriteOutput("FrameContentLoading");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.FrameDOMContentLoaded += (_, e) =>
                {
                    App.WriteOutput("FrameDOMContentLoaded");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.FrameNavigationCompleted += (_, e) =>
                {
                    App.WriteOutput("FrameNavigationCompleted");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.FrameNavigationStarting += (_, e) =>
                {
                    App.WriteOutput("FrameNavigationStarting");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.LongRunningScriptDetected += (_, e) =>
                {
                    App.WriteOutput("LongRunningScriptDetected");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.MoveFocusRequested += (_, e) =>
                {
                    App.WriteOutput("MoveFocusRequested");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.NavigationStarting += (_, e) =>
                {
                    App.WriteOutput("NavigationStarting");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.NewWindowRequested += (_, e) =>
                {
                    App.WriteOutput("NewWindowRequested");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.PermissionRequested += (_, e) =>
                {
                    App.WriteOutput("PermissionRequested");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.ScriptNotify += (_, e) =>
                {
                    App.WriteOutput("ScriptNotify");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.UnsafeContentWarningDisplaying += (_, e) =>
                {
                    App.WriteOutput("UnsafeContentWarningDisplaying");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };

                WebViewInstance.UnviewableContentIdentified += (_, e) =>
                {
                    App.WriteOutput("UnviewableContentIdentified");
                    App.WriteOutput(JsonSerializer.Serialize(e, new JsonSerializerOptions() { WriteIndented = true }));
                };
                // end debug

                if (Window.ShowDialog() != true && !tcs.Task.IsCompleted)
                {
                    App.WriteOutput("cancelled");
                    tcs.SetException(new Exception("canceled"));
                }
            });

            return tcs.Task;
        }
    }
}