#if UNITY_EDITOR
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Tools;
using UnityEditor;
using UnityEngine;

public static class ContentValidatorRunner
{
    [MenuItem("Raven/Validate Content Catalog")]
    public static void ValidateCatalog()
    {
        var guid = AssetDatabase.FindAssets("t:GameConfigSO");
        if (guid == null || guid.Length == 0)
        {
            Debug.LogError("Content Validator: No GameConfigSO asset found.");
            return;
        }

        var path = AssetDatabase.GUIDToAssetPath(guid[0]);
        var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(path);
        var messages = ContentValidator.Validate(config);

        Debug.Log($"Content Validator: Checked config at {path}");
        foreach (var message in messages)
        {
            if (message.StartsWith("ERROR"))
            {
                Debug.LogError(message);
            }
            else if (message.StartsWith("WARN"))
            {
                Debug.LogWarning(message);
            }
            else
            {
                Debug.Log(message);
            }
        }

        if (messages.Count == 0)
        {
            Debug.Log("Content Validator: No issues found.");
        }
    }
}
#endif
