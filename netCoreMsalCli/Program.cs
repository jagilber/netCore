

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Identity.Client;

namespace netCoreMsalCli
{
    internal class Program
    {
        public static string ClientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        public static bool Force = false;
        public static IPublicClientApplication PublicClientApp;
        public static string RedirectUri = "http://localhost";
        public static string Resource = null;
        public static string TenantId = "common";

        public static List<string> Scope { get; set; } = new List<string>();

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

            if (args.Length < 2)
            {
                ShowHelp();
            }

            for (int i = 0; i != args.Length; ++i)
            {
                string arg = args[i].ToLower();
                if (arg.StartsWith('/')) { arg = '-' + arg.TrimStart('/'); }
                if (arg == "-?") { ShowHelp(); }
                if (arg == "-resource") { Resource = args[i + 1]; }
                if (arg == "-redirecturi") { RedirectUri = args[i + 1]; }
                if (arg == "-clientid") { ClientId = args[i + 1]; }
                if (arg == "-tenantid") { TenantId = args[i + 1]; }
                if (arg == "-scope") { Scope.Add(args[i + 1]); }
                if (arg == "-force") { Force = true; }
            }

            if (Scope.Count < 1)
            {
                Scope.Add(".default");
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

            Console.WriteLine(authenticationResult.AccessToken);
        }

        private void ShowHelp()
        {
            Console.WriteLine($"scope: {Scope[0].ToString()}");
            Console.WriteLine($"resource: {Resource}");
            Console.WriteLine($"clientID: {ClientId}");
            Console.WriteLine($"RedirectUri: {RedirectUri}");
            Console.WriteLine($"tenantID: {TenantId}");
            Console.WriteLine("");
            Console.WriteLine("requires -resource argument. optional -redirectUri -clientId -tenantId -scope");
            Console.WriteLine("run from non administrator prompt!");

        }
        static class TokenCacheHelper
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

            private static readonly object FileLock = new object();


            private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
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

            private static void AfterAccessNotification(TokenCacheNotificationArgs args)
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
}