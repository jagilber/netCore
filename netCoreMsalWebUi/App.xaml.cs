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
        private AuthenticationResult authenticationResult = default;
        private string clientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        private string clientName = null;
        private string clientSecret = null;
        private IConfidentialClientApplication confidentialClientApp;
        private List<string> defaultScope = new List<string>() { ".default" };
        private static bool detail = false;
        private bool help = false;
        private IPublicClientApplication publicClientApp;
        private string redirectUri = null; //"http://localhost";
        private string resource = null;
        private List<string> scopes = new List<string>();
        private string tenantId = "common";


        public void MsalLoggerCallback(LogLevel level, string message, bool containsPII)
        {
            WriteOutput($"// {level} {message}");
        }
        private void Execute(object sender, StartupEventArgs e)
        {
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (!e.Args[i].StartsWith('/') & !e.Args[i].StartsWith('-')) { continue; }

                string arg = e.Args[i].ToLower().TrimStart(new char[] { '-', '/' });
                if (arg == "resource") { resource = e.Args[++i]; }
                if (arg == "redirecturi") { redirectUri = e.Args[++i]; }
                if (arg == "clientid") { clientId = e.Args[++i]; }
                if (arg == "clientname") { clientName = e.Args[++i]; }
                if (arg == "clientsecret") { clientSecret = e.Args[++i]; }
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
                Console.WriteLine("// optional arguments --resource --redirectUri --clientId --clientsecret --tenantId --scopes --detail");
                Console.WriteLine("// run from non administrator prompt!");
                ShowDetail();
                return;
            }

            if (detail) { ShowDetail(); }
            Authorize();
        }

        private async void Authorize()
        {
            CustomWebUi customWebUi = new CustomWebUi(Dispatcher);

            try
            {
                List<string> newScopes = new List<string>();

                foreach (string scope in scopes)
                {
                    newScopes.Add($"{resource}/{scope}");
                }

                scopes = newScopes;

                if (string.IsNullOrEmpty(clientSecret))
                {
                    // user creds 
                    publicClientApp = PublicClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                        .WithLogging(MsalLoggerCallback, LogLevel.Verbose, true, true)
                        .WithDefaultRedirectUri()
                        .Build();

                    TokenCacheHelper.EnableSerialization(publicClientApp.UserTokenCache);
                    authenticationResult = publicClientApp
                        .AcquireTokenSilent(defaultScope, publicClientApp.GetAccountsAsync().Result.FirstOrDefault())
                        .ExecuteAsync().Result;
                }
                else
                {
                    // client creds
                    if (string.IsNullOrEmpty(clientName))
                    {
                        clientName = clientId;
                    }

                    confidentialClientApp = ConfidentialClientApplicationBuilder
                        .CreateWithApplicationOptions(new ConfidentialClientApplicationOptions
                        {
                            ClientId = clientId,
                            RedirectUri = redirectUri,
                            ClientSecret = clientSecret,
                            TenantId = tenantId,
                            ClientName = clientName
                        })
                        .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                        .WithLogging(MsalLoggerCallback, LogLevel.Verbose, true, true)
                        .Build();

                    TokenCacheHelper.EnableSerialization(confidentialClientApp.UserTokenCache);
                    authenticationResult = await confidentialClientApp
                        .AcquireTokenForClient(scopes.Count > 0 ? scopes : defaultScope)
                        .ExecuteAsync();//.Result;
                }
            }
            catch (MsalUiRequiredException)
            {
                authenticationResult = await publicClientApp
                    .AcquireTokenInteractive(defaultScope)
                    .WithCustomWebUi(customWebUi)
                    .ExecuteAsync();//.Result;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e}");
            }

            if (scopes.Count > 0)
            {
                authenticationResult = publicClientApp
                    .AcquireTokenSilent(scopes, publicClientApp.GetAccountsAsync().Result.FirstOrDefault())
                    .ExecuteAsync().Result;
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
                    writer.WriteString("Environment", authenticationResult.Account?.Environment);

                    writer.WriteStartObject("HomeAccountId");
                    writer.WriteString("Identifier", authenticationResult.Account?.HomeAccountId.Identifier);
                    writer.WriteString("ObjectId", authenticationResult.Account?.HomeAccountId.ObjectId);
                    writer.WriteString("TenantId", authenticationResult.Account?.HomeAccountId.TenantId);
                    writer.WriteEndObject();

                    writer.WriteString("Username", authenticationResult.Account?.Username);
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
