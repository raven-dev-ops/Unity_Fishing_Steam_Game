using UnityEngine;
using UnityEngine.SceneManagement;

namespace RavenDevOps.Fishing.Core
{
    public static class SceneContractReferenceResolver
    {
        public static GameObject Resolve(
            Scene scene,
            string contractTypeName,
            string contractFieldName,
            GameObject contractReference,
            string fallbackObjectName,
            bool required)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            if (contractReference != null)
            {
                if (contractReference.scene == scene)
                {
                    return contractReference;
                }

                Debug.LogWarning(
                    $"Scene contract reference '{contractTypeName}.{contractFieldName}' points to '{contractReference.name}' in scene '{contractReference.scene.name}', expected scene '{scene.name}'.");
            }

            if (!string.IsNullOrWhiteSpace(fallbackObjectName))
            {
                var fallback = FindSceneObject(scene, fallbackObjectName);
                if (fallback != null)
                {
                    Debug.LogWarning(
                        $"Scene contract reference '{contractTypeName}.{contractFieldName}' is missing in scene '{scene.name}'. Using fallback object '{fallbackObjectName}'.");
                    return fallback;
                }
            }

            if (required)
            {
                Debug.LogError(
                    $"Scene contract reference '{contractTypeName}.{contractFieldName}' is required in scene '{scene.name}', and fallback '{fallbackObjectName}' was not found.");
            }

            return null;
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                if (root.name == objectName)
                {
                    return root;
                }

                var child = FindChildRecursive(root.transform, objectName);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (child.name == targetName)
                {
                    return child;
                }

                var nested = FindChildRecursive(child, targetName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
