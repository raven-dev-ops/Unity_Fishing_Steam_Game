#if UNITY_EDITOR
using RavenDevOps.Fishing.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RavenDevOps.Fishing.EditorTools
{
    public static class BootstrapAssetContractValidatorRunner
    {
        [MenuItem("Raven/Validate Bootstrap Asset Contract")]
        public static void ValidateBootstrapAssetsFromMenu()
        {
            if (!ValidateRequiredBootstrapAssets(out var failureMessage))
            {
                Debug.LogError(failureMessage);
            }
        }

        public static void ValidateBootstrapAssetsBatchMode()
        {
            if (!ValidateRequiredBootstrapAssets(out var failureMessage))
            {
                throw new BuildFailedException(failureMessage);
            }
        }

        public static bool ValidateRequiredBootstrapAssets(out string failureMessage)
        {
            var report = BootstrapAssetContractValidator.ValidateRequiredAssets();
            if (report.IsValid)
            {
                failureMessage = string.Empty;
                Debug.Log("BootstrapAssetContractValidatorRunner: required bootstrap asset contract passed.");
                return true;
            }

            BootstrapAssetContractValidator.LogValidationReport(report);
            failureMessage = report.BuildFailureMessage();
            return false;
        }
    }
}
#endif
