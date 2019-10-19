// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Extensibility;
using Microsoft.Toolkit.Wpf.UI.Controls;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Text.Json;

namespace netCoreAzAdal
{
    public partial class App : Application
    {
        private string _clientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        private bool _force = false;
        private string _redirectUri = "urn:ietf:wg:oauth:2.0:oob";
        private string _resource = null;
        private string _tenantId = "common";

        private void App_Startup(object sender, StartupEventArgs e)
        {
            if(e.Args.Length < 2)
            {
                //Console.WriteLine("requires /resource argument. optional /redirectUri /clientId /tenantId");
                throw new ArgumentException("requires /resource argument. optional /redirectUri /clientId /tenantId");
            }

            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "/resource") { _resource = e.Args[i + 1]; }
                if (e.Args[i] == "/redirectUri") { _redirectUri = e.Args[i + 1]; }
                if (e.Args[i] == "/clientId") { _clientId = e.Args[i + 1]; }
                if (e.Args[i] == "/tenantId") { _tenantId = e.Args[i + 1]; }
                if (e.Args[i] == "/force") { _force = true; }
            }

            Authorize();
        }

        private async void Authorize()
        {
            AuthenticationContext authContext = new AuthenticationContext($"https://login.microsoftonline.com/{_tenantId}");
            AuthenticationResult result = default;
            Window window = new Window();
            CustomWebUi ui = new CustomWebUi(window.Dispatcher);

            try
            {
                result = await authContext.AcquireTokenAsync(_resource,
                    _clientId,
                    new Uri(_redirectUri),
                    new PlatformParameters(PromptBehavior.Always, ui));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result = null;
            }

            if (result == null | _force)
            {
                result = await authContext.AcquireTokenAsync(_resource,
                _clientId,
                new Uri(_redirectUri),
                new PlatformParameters(PromptBehavior.Always, ui));
            }

            Console.WriteLine(result.AccessToken);
            App.Current.Shutdown();
        }
    }

    public class CustomWebUi : ICustomWebUi
    {
        private readonly Dispatcher _dispatcher;

        public CustomWebUi(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public string Authorization { get; set; }

        public Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri)
        {
            TaskCompletionSource<Uri> tcs = new TaskCompletionSource<Uri>();
            _dispatcher.InvokeAsync(() =>
            {
                WebView webView = new WebView();
                Window w = new Window
                {
                    Title = "Authorization",
                    WindowStyle = WindowStyle.ToolWindow,
                    Content = webView,
                    Width = 500,
                    Height = 500,
                    ResizeMode = ResizeMode.CanResizeWithGrip
                };

                w.Loaded += (_, __) => webView.Navigate(authorizationUri);
                webView.NavigationCompleted += (_, e) =>
                {
                    System.Diagnostics.Debug.WriteLine(e.Uri);
                    if (e.Uri.Query.Contains("code="))
                    {
                        tcs.SetResult(e.Uri);
                        w.DialogResult = true;
                        w.Close();
                    }
                    if (e.Uri.Query.Contains("error="))
                    {
                        tcs.SetException(new Exception(e.Uri.Query));
                        w.DialogResult = false;
                        w.Close();
                    }
                };

                webView.UnsupportedUriSchemeIdentified += (_, e) =>
                {
                    if (e.Uri.Query.Contains("code="))
                    {
                        tcs.SetResult(e.Uri);
                        w.DialogResult = true;
                        w.Close();
                    }
                    else
                    {
                        tcs.SetException(new Exception($"Unknown error: {e.Uri}"));
                        w.DialogResult = false;
                        w.Close();
                    }
                };

                if (w.ShowDialog() != true && !tcs.Task.IsCompleted)
                {
                    tcs.SetException(new Exception("canceled"));
                }
            });

            return tcs.Task;
        }
    }
}