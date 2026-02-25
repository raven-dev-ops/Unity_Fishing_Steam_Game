using System.Text.RegularExpressions;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class SceneContractReferenceResolverTests
    {
        [Test]
        public void Resolve_UsesFallbackAndLogsWarning_WhenContractReferenceMissing()
        {
            var scene = EnsureValidScene();
            const string fallbackObjectName = "__ContractFallbackObject";
            var fallback = new GameObject(fallbackObjectName);
            SceneManager.MoveGameObjectToScene(fallback, scene);

            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    new Regex("Scene contract reference 'FishingSceneContract.FishingShip'.*Using fallback object '__ContractFallbackObject'\\."));

                var resolved = SceneContractReferenceResolver.Resolve(
                    scene,
                    "FishingSceneContract",
                    "FishingShip",
                    contractReference: null,
                    fallbackObjectName: fallbackObjectName,
                    required: false);

                Assert.That(resolved, Is.EqualTo(fallback));
            }
            finally
            {
                Object.DestroyImmediate(fallback);
            }
        }

        [Test]
        public void Resolve_LogsErrorAndReturnsNull_WhenRequiredReferenceMissing()
        {
            var scene = EnsureValidScene();
            LogAssert.Expect(
                LogType.Error,
                new Regex("Scene contract reference 'FishingSceneContract.FishingHook' is required"));

            var resolved = SceneContractReferenceResolver.Resolve(
                scene,
                "FishingSceneContract",
                "FishingHook",
                contractReference: null,
                fallbackObjectName: "__MissingContractObject",
                required: true);

            Assert.That(resolved, Is.Null);
        }

        private static Scene EnsureValidScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                return scene;
            }

            return SceneManager.CreateScene("EditModeContractResolverScene");
        }
    }
}
