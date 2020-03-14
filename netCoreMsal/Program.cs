// ------------------------------------------------------------
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// .netcore 3.1 commandline utility to authorize to AAD with MSAL.
// returns AuthenticationResult json output
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Security;
using Microsoft.Identity.Client;

namespace netCoreMsal
{
    internal class Program
    {
        private AuthenticationResult authenticationResult = default;
        private string clientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        private string clientName = null;
        private string clientSecret = null;
        private IConfidentialClientApplication confidentialClientApp;
        private List<string> defaultScope = new List<string>() { ".default" };
        private bool detail = false;
        private bool help = false;
        private IPublicClientApplication publicClientApp;
        private string redirectUri = null; //"http://localhost";
        private string resource = null;
        private List<string> scopes = new List<string>();
        private string tenantId = "common";

        public void MsalLoggerCallback(LogLevel level, string message, bool containsPII)
        {
            if (detail)
            {
                Console.WriteLine($"// {level} {message}");
            }
        }

        private static void Main(string[] args)
        {
            try
            {
                new Program().Execute(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void Authorize()
        {
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
                    authenticationResult = confidentialClientApp
                        .AcquireTokenForClient(scopes.Count > 0 ? scopes : defaultScope)
                        .ExecuteAsync().Result;
                }
            }
            catch (MsalUiRequiredException)
            {
                authenticationResult = publicClientApp
                    .AcquireTokenInteractive(defaultScope)
                    .ExecuteAsync().Result;
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

            //Console.WriteLine($"{JsonSerializer.Serialize(authenticationResult)}");
            FormatJsonOutput(authenticationResult);
        }

        private void Execute(string[] args)
        {
            for (int i = 0; i != args.Length; ++i)
            {
                if (!args[i].StartsWith('/') & !args[i].StartsWith('-')) { continue; }

                string arg = args[i].ToLower().TrimStart(new char[] { '-', '/' });
                if (arg == "resource") { resource = args[++i]; }
                if (arg == "redirecturi") { redirectUri = args[++i]; }
                if (arg == "clientid") { clientId = args[++i]; }
                if (arg == "clientname") { clientName = args[++i]; }
                if (arg == "clientsecret") { clientSecret = args[++i]; }
                if (arg == "tenantid") { tenantId = args[++i]; }
                if (arg == "detail") { detail = true; }
                if (arg == "?") { help = true; }
                if (arg == "scopes")
                {
                    scopes.Clear();
                    scopes.AddRange(args[++i].Split(','));
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
    }
}