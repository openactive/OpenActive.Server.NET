using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace IdentityServer
{
    public static class KeyGenerator
    {
        private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        private static string ToBase62String(byte[] toConvert)
        {
            const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            BigInteger dividend = new BigInteger(toConvert);
            var builder = new StringBuilder();
            while (dividend != 0)
            {
                dividend = BigInteger.DivRem(dividend, alphabet.Length, out BigInteger remainder);
                builder.Insert(0, alphabet[Math.Abs(((int)remainder))]);
            }
            return builder.ToString();
        }

        private static string GenerateCryptographicallySecureRandomString(int length)
        {
            byte[] buffer = new byte[length];
            rngCsp.GetBytes(buffer);
            string uniq = ToBase62String(buffer);
            return uniq;
        }

        public static string GenerateSecret()
        {
            return GenerateCryptographicallySecureRandomString(32);
        }

        public static string GenerateInitialAccessToken(string bookingPartnerName)
        {
            Regex rgx = new Regex("[^a-z0-9 ]");
            return rgx.Replace(bookingPartnerName.ToLowerInvariant(), "").Replace(' ', '_') + "_" + GenerateCryptographicallySecureRandomString(16);
        }
    }
}
