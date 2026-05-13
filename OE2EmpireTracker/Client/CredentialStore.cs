// <copyright file="CredentialStore.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using NLog;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Provides DPAPI-based encryption and SecureString handling for sensitive credentials.
    /// Uses <see cref="DataProtectionScope.CurrentUser"/> so only the current Windows user can decrypt.
    /// </summary>
    public static class CredentialStore
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Encrypts a plain-text string using DPAPI (CurrentUser scope) and returns a base64-encoded blob.
        /// </summary>
        /// <param name="plainText">The plain text to protect.</param>
        /// <returns>Base64-encoded DPAPI-protected blob.</returns>
        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] protectedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            Array.Clear(plainBytes, 0, plainBytes.Length);
            Log.Debug("Protected credential ({0} bytes → {1} bytes)", plainText.Length, protectedBytes.Length);
            return Convert.ToBase64String(protectedBytes);
        }

        /// <summary>
        /// Decrypts a DPAPI-protected base64 blob and returns the result as a <see cref="SecureString"/>.
        /// </summary>
        /// <param name="protectedBase64">Base64-encoded DPAPI blob produced by <see cref="Protect"/>.</param>
        /// <returns>A read-only <see cref="SecureString"/> containing the decrypted value.</returns>
        public static SecureString Unprotect(string protectedBase64)
        {
            if (string.IsNullOrEmpty(protectedBase64))
            {
                var empty = new SecureString();
                empty.MakeReadOnly();
                return empty;
            }

            byte[] protectedBytes = Convert.FromBase64String(protectedBase64);
            byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);

            var secure = new SecureString();
            try
            {
                string plainText = Encoding.UTF8.GetString(plainBytes);
                foreach (char c in plainText)
                {
                    secure.AppendChar(c);
                }

                secure.MakeReadOnly();
            }
            finally
            {
                Array.Clear(plainBytes, 0, plainBytes.Length);
            }

            Log.Debug("Unprotected credential to SecureString ({0} chars)", secure.Length);
            return secure;
        }

        /// <summary>
        /// Temporarily converts a <see cref="SecureString"/> to a plain string for use in HTTP headers.
        /// The caller should use the result immediately and discard it.
        /// </summary>
        /// <param name="value">The secure string to convert.</param>
        /// <returns>The plain-text representation, or <see cref="string.Empty"/> if null/empty.</returns>
        public static string SecureStringToString(SecureString value)
        {
            if (value == null || value.Length == 0)
            {
                return string.Empty;
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
        }
    }
}
