

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Identity.Client;

namespace netCoreMsalTokenCacheCli
{
    internal class Program
    {
        private string ClientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        private bool Detail = false;
        private bool Force = false;
        private bool Help = false;
        private IPublicClientApplication PublicClientApp;
        private string RedirectUri = "http://localhost";
        private string Resource = null;
        private string TenantId = "common";

        private List<string> Scope { get; set; } = new List<string>() { ".default" };

        private static void Main(string[] args)
        {
            try
            {
                new Program().App_Startup(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void App_Startup(string[] args)
        {
            for (int i = 0; i != args.Length; ++i)
            {
                string arg = '-' + args[i].ToLower().TrimStart(new char[] { '-', '/' });
                if (arg == "-resource") { Resource = args[++i]; }
                if (arg == "-redirecturi") { RedirectUri = args[++i]; }
                if (arg == "-clientid") { ClientId = args[++i]; }
                if (arg == "-tenantid") { TenantId = args[++i]; }
                if (arg == "-force") { Force = true; }
                if (arg == "-detail") { Detail = true; }
                if (arg == "-?") { Help = true; }
                if (arg == "-scope")
                {
                    Scope.Clear();
                    Scope.AddRange(args[++i].Split(','));
                }
            }

            if (Help)
            {
                Console.WriteLine("// optional arguments --resource --redirectUri --clientId --tenantId --scope --detail");
                Console.WriteLine("// run from non administrator prompt!");
                ShowDetail();
                return;
            }

            if (Detail)
            {
                ShowDetail();
            }

            Authorize();
        }

        private void Authorize()
        {
            AuthenticationResult authenticationResult = default;
            PublicClientApp = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, TenantId)
                .WithDefaultRedirectUri()
                .Build();
            TokenCacheHelper.EnableSerialization(PublicClientApp.UserTokenCache);

            try
            {
                authenticationResult = PublicClientApp
                    .AcquireTokenSilent(Scope, PublicClientApp.GetAccountsAsync().Result.FirstOrDefault())
                    .ExecuteAsync().Result;
            }
            catch (MsalUiRequiredException)
            {
                authenticationResult = PublicClientApp
                    .AcquireTokenInteractive(Scope)
                    .ExecuteAsync().Result;
            }

            FormatJsonOutput(authenticationResult);
        }

        private static void FormatJsonOutput(AuthenticationResult authenticationResult)
        {
            JsonWriterOptions options = new JsonWriterOptions{ Indented = true };

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

                string json = Encoding.UTF8.GetString(stream.ToArray());
                Console.WriteLine(json);
            }
        }

        private void ShowDetail()
        {
            Console.WriteLine("//");
            Console.WriteLine($"// scope: {string.Join(" ", Scope)}");
            Console.WriteLine($"// resource: {Resource}");
            Console.WriteLine($"// clientID: {ClientId}");
            Console.WriteLine($"// RedirectUri: {RedirectUri}");
            Console.WriteLine($"// tenantID: {TenantId}");
            Console.WriteLine("//");
        }

    }

    public static class TokenCacheHelper
    {
        public static void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        /// <summary>
        /// Path to the token cache
        /// </summary>
        public static readonly string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin3";

        public static readonly object FileLock = new object();


        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                args.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath)
                        ? ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath),
                                                  null,
                                                  DataProtectionScope.CurrentUser)
                        : null);
            }
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changesgs in the persistent store
                    File.WriteAllBytes(CacheFilePath,
                                        ProtectedData.Protect(args.TokenCache.SerializeMsalV3(),
                                                                null,
                                                                DataProtectionScope.CurrentUser)
                                        );
                }
            }
        }
    }

}