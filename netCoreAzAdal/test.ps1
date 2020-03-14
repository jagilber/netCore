$code1 = @"
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Extensibility;
using Microsoft.Toolkit.Wpf.UI.Controls;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;


    public class AuthView : Window
    {
        public AuthView()
        {
            //MessageBox.Show("here");
        }

        static AuthView()
        {
            //MessageBox.Show("here");
        }

        public async void Authenticate()
        {
            var authContext = new AuthenticationContext("https://login.microsoftonline.com/common");
            AuthenticationResult result = await authContext.AcquireTokenAsync("https://sflogs.kusto.windows.net",
                "1950a258-227b-4e31-a9cf-717495945fc2",
                new Uri("urn:ietf:wg:oauth:2.0:oob"),
                new PlatformParameters(PromptBehavior.Always, new CustomWebUi()));
            //MessageBox.Show(result.AccessToken);
        }

        public static System.Reflection.Assembly Test()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFrom(@"C:\Users\user\.nuget\packages\microsoft.toolkit.wpf.ui.controls.webview\6.0.0-preview9.1\lib\netcoreapp3.1\Microsoft.Toolkit.Wpf.UI.Controls.WebView.dll");
            return assembly;
        }

    }

    public class CustomWebUi : ICustomWebUi
    {
        private readonly Dispatcher _dispatcher;

        public CustomWebUi()
        {
            //MessageBox.Show("here");
        }
        public CustomWebUi(Dispatcher dispatcher)
        {
            //MessageBox.Show("here");
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri)
        {
            var tcs = new TaskCompletionSource<Uri>();
            //_dispatcher.InvokeAsync(() =>
            //{
                WebView webView = new WebView();
                Window w = new Window
                {
                    Title = "Auth",
                    WindowStyle = WindowStyle.ToolWindow,
                    Content = webView,
                    Width = 500,
                    Height = 500,
                    ResizeMode = ResizeMode.CanResizeWithGrip
                };
                w.Loaded += (_, __) => webView.Navigate(authorizationUri);
                webView.NavigationCompleted += (_, e) =>
                {
                    //System.Diagnostics.Debug.WriteLine(e.Uri);
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
            //});
            return tcs.Task;
        }
    }

"@
<#
add-type $code1 `
    -CompilerOptions @(
        "-reference:C:\Users\user\.nuget\packages\microsoft.toolkit.wpf.ui.controls.webview\6.0.0-preview9.1\lib\netcoreapp3.1\Microsoft.Toolkit.Wpf.UI.Controls.WebView.dll",
        #"C:\Users\user\.nuget\packages\system.runtime\4.0.20\ref\dotnet\System.Runtime.dll",
        #"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.dll",
        "-reference:C:\Users\user\.nuget\packages\microsoft.identitymodel.clients.activedirectory\5.2.2\lib\netstandard1.3\Microsoft.IdentityModel.Clients.ActiveDirectory.dll",
        "-reference:C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\System.Windows.dll",
        "-reference:C:\Users\user\.nuget\packages\windowsbase\4.6.1055\lib\WindowsBase.dll",
        "-reference:C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\mscorlib.dll",
        "-reference:C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\System.Threading.dll",
        "-reference:C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\System.Threading.Tasks.dll",
        "-reference:C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\3.1.0\ref\netcoreapp3.1\PresentationCore.dll",
        "-reference:C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\3.1.0\ref\netcoreapp3.1\PresentationFramework.dll"
        #"C:\Users\user\.nuget\packages\system.threading.tasks.extensions\4.6.0-preview.18571.3\lib\netstandard2.0\System.Threading.Tasks.Extensions.dll"
    )

[AuthView]::Test()
$global:authView = [AuthView]::new()
$global:authView
$global:authView.Authenticate().result

return
#>

# -CompilerOptions @('-nowarn:CS1701','-debug:full','-unsafe','-version') #`
add-type $code1 `
    -ReferencedAssemblies @("C:\Users\user\.nuget\packages\microsoft.toolkit.wpf.ui.controls.webview\6.0.0-preview9.1\lib\netcoreapp3.1\Microsoft.Toolkit.Wpf.UI.Controls.WebView.dll",
        #"C:\Users\user\.nuget\packages\system.runtime\4.0.20\ref\dotnet\System.Runtime.dll",
        #"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.dll",
        "C:\Users\user\.nuget\packages\microsoft.identitymodel.clients.activedirectory\5.2.2\lib\netstandard1.3\Microsoft.IdentityModel.Clients.ActiveDirectory.dll",
        "C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\System.Windows.dll",
        "C:\Users\user\.nuget\packages\windowsbase\4.6.1055\lib\WindowsBase.dll",
        "C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\mscorlib.dll",
        "C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\System.Threading.dll",
        "C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\System.Threading.Tasks.dll",
        "C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\3.1.0\ref\netcoreapp3.1\PresentationCore.dll",
        "C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\3.1.0\ref\netcoreapp3.1\PresentationFramework.dll"
        #"C:\Users\user\.nuget\packages\system.threading.tasks.extensions\4.6.0-preview.18571.3\lib\netstandard2.0\System.Threading.Tasks.Extensions.dll"
        ) 
        #>
#[AuthView]::Authenticate()
[AuthView]::Test()
$global:authView = [AuthView]::new()
$global:authView
$global:authView.Authenticate().result
#$ass = [Reflect]::Test()
#$ass


return

$code2 = @"
using Microsoft.Toolkit.Wpf.UI.Controls;
public class Ident
{
    public static bool Test()
    {
        System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFrom(@"C:\Users\user\.nuget\packages\microsoft.toolkit.wpf.ui.controls.webview\6.0.0-preview9.1\lib\netcoreapp3.1\Microsoft.Toolkit.Wpf.UI.Controls.WebView.dll");
        return true;
    }
}
"@

add-type $code2 -ReferencedAssemblies $ass
[Ident]::Test()


<#
        "-reference:C:\Users\user\.nuget\packages\microsoft.toolkit.wpf.ui.controls.webview\6.0.0-preview9.1\lib\netcoreapp3.1\Microsoft.Toolkit.Wpf.UI.Controls.WebView.dll",
        #"C:\Users\user\.nuget\packages\system.runtime\4.0.20\ref\dotnet\System.Runtime.dll",
        #"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.dll",
        "-reference:C:\Users\user\.nuget\packages\microsoft.identitymodel.clients.activedirectory\5.2.2\lib\netstandard1.3\Microsoft.IdentityModel.Clients.ActiveDirectory.dll",
        "-reference:C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\System.Windows.dll",
        "-reference:C:\Users\user\.nuget\packages\windowsbase\4.6.1055\lib\WindowsBase.dll",
        "-reference:C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\mscorlib.dll",
        "-reference:C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\System.Threading.dll",
        "-reference:C:\Users\user\.nuget\packages\microsoft.netcore.app\2.2.7\ref\netcoreapp2.2\System.Threading.Tasks.dll",
        "-reference:C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\3.1.0\ref\netcoreapp3.1\PresentationCore.dll",
        "-reference:C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\3.1.0\ref\netcoreapp3.1\PresentationFramework.dll"
        #"C:\Users\user\.nuget\packages\system.threading.tasks.extensions\4.6.0-preview.18571.3\lib\netstandard2.0\System.Threading.Tasks.Extensions.dll"

#>