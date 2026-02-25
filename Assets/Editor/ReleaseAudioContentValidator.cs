#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RavenDevOps.Fishing.EditorTools
{
    public static class ReleaseAudioContentValidator
    {
        private const string ReleaseAudioAssetRoot = "Assets/Resources/Pilot/Audio";

        [MenuItem("Raven/Validate Release Audio Coverage")]
        public static void ValidateFromMenu()
        {
            if (!ValidateRequiredReleaseAudio(out var failureMessage))
            {
                Debug.LogError(failureMessage);
            }
        }

        public static void ValidateReleaseAudioBatchMode()
        {
            if (!ValidateRequiredReleaseAudio(out var failureMessage))
            {
                throw new BuildFailedException(failureMessage);
            }
        }

        public static bool ValidateRequiredReleaseAudio(out string failureMessage)
        {
            var missingKeys = new List<string>();
            var generatedFallbackKeys = new List<string>();

            for (var i = 0; i < PhaseTwoAudioContract.RequiredAudioKeys.Length; i++)
            {
                var key = PhaseTwoAudioContract.RequiredAudioKeys[i];
                var assetPath = $"{ReleaseAudioAssetRoot}/{key}.wav";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip == null)
                {
                    missingKeys.Add(key);
                    continue;
                }

                var importer = AssetImporter.GetAtPath(assetPath);
                if (importer == null)
                {
                    missingKeys.Add(key);
                    continue;
                }

                if (string.Equals(importer.userData?.Trim(), PhaseTwoAudioContract.GeneratedFallbackUserDataMarker, StringComparison.OrdinalIgnoreCase))
                {
                    generatedFallbackKeys.Add(key);
                }
            }

            if (missingKeys.Count == 0 && generatedFallbackKeys.Count == 0)
            {
                failureMessage = string.Empty;
                Debug.Log("ReleaseAudioContentValidator: required phase-two release audio coverage passed.");
                return true;
            }

            failureMessage =
                $"ReleaseAudioContentValidator: release audio validation failed. missing_keys=[{string.Join(", ", missingKeys)}], generated_fallback_keys=[{string.Join(", ", generatedFallbackKeys)}].";
            return false;
        }
    }
}
#endif
