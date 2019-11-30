// ------------------------------------------------------------
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace netCoreMsal
{
    public static class TokenCacheHelper
    {
        private static readonly string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin3";
        private static readonly object FileLock = new object();

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                WriteCache(args.TokenCache.SerializeMsalV3());
            }
        }

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            args.TokenCache.DeserializeMsalV3(ReadCache());
        }

        public static bool DeleteCache()
        {
            lock (FileLock)
            {
                if (File.Exists(CacheFilePath))
                {
                    File.Delete(CacheFilePath);
                    return true;
                }

                return false;
            }
        }

        public static void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        public static byte[] ReadCache()
        {
            lock (FileLock)
            {
                if (File.Exists(CacheFilePath))
                {
                    return ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath), null, DataProtectionScope.CurrentUser);
                }

                return null;
            }
        }

        public static string ReadCacheJson()
        {
            JsonDocument jsonDocument = JsonDocument.Parse(Encoding.UTF8.GetString(ReadCache()));
            string jsonString = JsonSerializer.Serialize(jsonDocument.RootElement, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(jsonString);
            return jsonString;
        }

        private static void WriteCache(byte[] tokenCacheBytes)
        {
            lock (FileLock)
            {
                File.WriteAllBytes(CacheFilePath, ProtectedData.Protect(tokenCacheBytes, null, DataProtectionScope.CurrentUser));
            }
        }
    }
}