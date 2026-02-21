using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RavenDevOps.Fishing.Steam
{
    [Serializable]
    public sealed class CloudSaveManifestData
    {
        public string savedAtUtc = string.Empty;
        public string contentSha256 = string.Empty;
        public string policy = "newest-wins";
    }

    public enum CloudIntegrityFailure
    {
        None = 0,
        ManifestMissing = 1,
        ManifestJsonInvalid = 2,
        ManifestHashMissing = 3,
        HashMismatch = 4
    }

    public static class SteamCloudIntegrity
    {
        public static string ComputeSha256(string text)
        {
            var raw = Encoding.UTF8.GetBytes(text ?? string.Empty);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(raw);
            var builder = new StringBuilder(hash.Length * 2);
            for (var i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }

        public static bool TryParseManifest(string json, out CloudSaveManifestData manifest)
        {
            manifest = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                manifest = JsonUtility.FromJson<CloudSaveManifestData>(json);
            }
            catch
            {
                manifest = null;
            }

            return manifest != null;
        }

        public static bool TryValidatePayload(string payloadJson, CloudSaveManifestData manifest, out CloudIntegrityFailure failure)
        {
            if (manifest == null)
            {
                failure = CloudIntegrityFailure.ManifestJsonInvalid;
                return false;
            }

            var expectedHash = (manifest.contentSha256 ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                failure = CloudIntegrityFailure.ManifestHashMissing;
                return false;
            }

            var actualHash = ComputeSha256(payloadJson);
            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                failure = CloudIntegrityFailure.HashMismatch;
                return false;
            }

            failure = CloudIntegrityFailure.None;
            return true;
        }
    }
}
