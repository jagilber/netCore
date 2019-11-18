

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Identity.Client;

namespace netCoreMsalNoCache
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

            if (args.Length < 2)
            {
                ShowHelp();
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
    }
}