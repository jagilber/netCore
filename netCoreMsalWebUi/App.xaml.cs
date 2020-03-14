// ------------------------------------------------------------
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// .netcore 3.1 commandline utility with customwebui to authorize to AAD with MSAL.
// returns AuthenticationResult json output
// ------------------------------------------------------------

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;


//https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-permissions-and-consent#requesting-individual-user-consent

namespace netCoreMsalWebUi
{
    public partial class App : Application
    {
        private string clientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        private static bool detail = false;
        private bool help = false;
        private IPublicClientApplication publicClientApp;
        private string redirectUri = null; //"http://localhost";
        private string resource = null;
        private List<string> scopes = new List<string>() { ".default" };
        private string tenantId = "common";

        private void App_Startup(object sender, StartupEventArgs e)
        {
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (!e.Args[i].StartsWith('/') & !e.Args[i].StartsWith('-')) { continue; }

                string arg = e.Args[i].ToLower().TrimStart(new char[] { '-', '/' });
                if (arg == "resource") { resource = e.Args[++i]; }
                if (arg == "redirecturi") { redirectUri = e.Args[++i]; }
                if (arg == "clientid") { clientId = e.Args[++i]; }
                if (arg == "tenantid") { tenantId = e.Args[++i]; }
                if (arg == "detail") { detail = true; }
                if (arg == "?") { help = true; }
                if (arg == "scopes")
                {
                    scopes.Clear();
                    scopes.AddRange(e.Args[++i].Split(','));
                }
            }

            if (help)
            {
                Console.WriteLine("// optional arguments --resource --redirectUri --clientId --tenantId --scope --detail");
                Console.WriteLine("// run from non administrator prompt!");
                ShowDetail();
                return;
            }

            if (detail) { ShowDetail(); }
            Authorize();
        }

        private async void Authorize()
        {
            AuthenticationResult authenticationResult = default;
            CustomWebUi customWebUi = new CustomWebUi(Dispatcher);

            if (string.IsNullOrEmpty(redirectUri))
            {
                publicClientApp = PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                    .WithDefaultRedirectUri()
                    .Build();
            }
            else
            {
                publicClientApp = PublicClientApplicationBuilder
                    .CreateWithApplicationOptions(new PublicClientApplicationOptions { ClientId = clientId, RedirectUri = redirectUri })
                    .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                    .Build();
            }

            try
            {
                TokenCacheHelper.EnableSerialization(publicClientApp.UserTokenCache);
                authenticationResult = await publicClientApp
                    .AcquireTokenSilent(scopes, publicClientApp.GetAccountsAsync().Result.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException me)
            {
                WriteOutput($"msal ui exception: {me.ToString()}");
                authenticationResult = await publicClientApp
                    .AcquireTokenInteractive(scopes)
                    .WithCustomWebUi(customWebUi)
                    .ExecuteAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"exception: {e.ToString()}");
            }

            FormatJsonOutput(authenticationResult);
            App.Current.Shutdown();
        }
        private void FormatJsonOutput(AuthenticationResult authenticationResult)
        {
            JsonWriterOptions options = new JsonWriterOptions { Indented = true };

            using (MemoryStream stream = new MemoryStream())
            {
                using (Utf8JsonWriter writer = new Utf8JsonWriter(stream, options))
                {
                    writer.WriteStartObject();
                    writer.WriteString("AccessToken", authenticationResult.AccessToken);

                    writer.WriteStartObject("Account");
                    writer.WriteString("Environment", authenticationResult.Account.Environment);

                    writer.WriteStartObject("HomeAccountId");
                    writer.WriteString("Identifier", authenticationResult.Account.HomeAccountId.Identifier);
                    writer.WriteString("ObjectId", authenticationResult.Account.HomeAccountId.ObjectId);
                    writer.WriteString("TenantId", authenticationResult.Account.HomeAccountId.TenantId);
                    writer.WriteEndObject();

                    writer.WriteString("Username", authenticationResult.Account.Username);
                    writer.WriteEndObject();

                    writer.WriteString("CorrelationId", authenticationResult.CorrelationId);
                    writer.WriteString("ExpiresOn", authenticationResult.ExpiresOn);
                    writer.WriteString("ExtendedExpiresOn", authenticationResult.ExtendedExpiresOn);
                    writer.WriteString("IdToken", authenticationResult.IdToken);
                    writer.WriteBoolean("IsExtendedLifeTimeToken", authenticationResult.IsExtendedLifeTimeToken);

                    writer.WriteStartArray("Scopes");
                    foreach (string scope in authenticationResult.Scopes)
                    {
                        writer.WriteStringValue(scope);
                    }
                    writer.WriteEndArray();

                    writer.WriteString("TenantId", authenticationResult.TenantId);
                    writer.WriteString("UniqueId", authenticationResult.UniqueId);
                    writer.WriteEndObject();
                }

                Console.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        private void ShowDetail()
        {
            Console.WriteLine("//");
            Console.WriteLine($"// scope: {string.Join(" ", scopes)}");
            Console.WriteLine($"// resource: {resource}");
            Console.WriteLine($"// clientID: {clientId}");
            Console.WriteLine($"// RedirectUri: {redirectUri}");
            Console.WriteLine($"// tenantID: {tenantId}");
            Console.WriteLine("//");
        }

        public static void WriteOutput(string output)
        {
            if (detail)
            {
                Console.WriteLine(output);
            }
        }
    }
}
