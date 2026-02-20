#if UNITY_EDITOR
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Tools;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RavenDevOps.Fishing.EditorTools
{
    public static class ContentValidatorRunner
    {
        [MenuItem("Raven/Validate Content Catalog")]
        public static void ValidateCatalogFromMenu()
        {
            RunValidation(throwOnErrors: false);
        }

        public static void ValidateCatalogBatchMode()
        {
            var success = RunValidation(throwOnErrors: false);
            if (!success)
            {
                throw new BuildFailedException("Content validator reported one or more errors.");
            }
        }

        private static bool RunValidation(bool throwOnErrors)
        {
            var guid = AssetDatabase.FindAssets("t:GameConfigSO");
            if (guid == null || guid.Length == 0)
            {
                const string message = "Content Validator: No GameConfigSO asset found.";
                if (throwOnErrors)
                {
                    throw new BuildFailedException(message);
                }

                Debug.LogError(message);
                return false;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid[0]);
            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(path);
            var messages = ContentValidator.Validate(config);

            var errorCount = ContentValidator.CountErrors(messages);
            var warningCount = 0;
            foreach (var message in messages)
            {
                if (message.StartsWith("ERROR"))
                {
                    Debug.LogError(message);
                }
                else if (message.StartsWith("WARN"))
                {
                    warningCount++;
                    Debug.LogWarning(message);
                }
                else
                {
                    Debug.Log(message);
                }
            }

            Debug.Log($"Content Validator: checked '{path}' with {errorCount} error(s), {warningCount} warning(s).");

            if (errorCount > 0)
            {
                const string errorMessage = "Content Validator: failed with validation errors.";
                if (throwOnErrors)
                {
                    throw new BuildFailedException(errorMessage);
                }

                return false;
            }

            return true;
        }
    }
}
#endif
