//
// .netcore 3.0 commandline utility to authorize to AAD with msal. 
// returns AuthenticationResult json output
//

using Microsoft.Identity.Client;
using System.IO;
using System.Security.Cryptography;

namespace netCoreMsal
{
    public static class TokenCacheHelper
    {
        private static readonly string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin3";
        private static readonly object FileLock = new object();

        public static void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                args.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath)
                        ? ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath), null, DataProtectionScope.CurrentUser)
                        : null);
            }
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    File.WriteAllBytes(CacheFilePath,
                        ProtectedData.Protect(args.TokenCache.SerializeMsalV3(), null, DataProtectionScope.CurrentUser));
                }
            }
        }
    }
}