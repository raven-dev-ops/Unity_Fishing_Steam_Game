using System.Collections.Generic;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class BootstrapAssetContractValidatorTests
    {
        [Test]
        public void ValidateRequiredAssets_MissingContractEntries_ReportsAllMissingKeys()
        {
            var report = BootstrapAssetContractValidator.ValidateRequiredAssets(
                new BootstrapAssetValidationDependencies
                {
                    LoadInputActionAsset = _ => null,
                    LoadTextAsset = _ => null,
                    LoadGameConfig = _ => null,
                    LoadTuningConfig = _ => null,
                    LoadTutorialSpriteLibrary = _ => null
                });

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues.Count, Is.EqualTo(4));

            var keys = new HashSet<string>();
            for (var i = 0; i < report.Issues.Count; i++)
            {
                keys.Add(report.Issues[i].AssetKey);
            }

            Assert.That(keys, Contains.Item(BootstrapAssetContractValidator.InputActionsAssetKey));
            Assert.That(keys, Contains.Item(BootstrapAssetContractValidator.GameConfigAssetKey));
            Assert.That(keys, Contains.Item(BootstrapAssetContractValidator.TuningConfigAssetKey));
            Assert.That(keys, Contains.Item(BootstrapAssetContractValidator.TutorialSpriteLibraryAssetKey));
            Assert.That(report.BuildFailureMessage(), Does.Contain("Bootstrap asset validation failed"));
        }

        [Test]
        public void ValidateRequiredAssets_InputJsonFallbackAndRequiredAssetsPresent_Passes()
        {
            var textAsset = new TextAsset("{\"name\":\"InputActions_Gameplay\",\"maps\":[]}");
            var config = ScriptableObject.CreateInstance<GameConfigSO>();
            var tuning = ScriptableObject.CreateInstance<TuningConfigSO>();
            var tutorialLibrary = ScriptableObject.CreateInstance<TutorialSpriteLibrary>();

            try
            {
                var report = BootstrapAssetContractValidator.ValidateRequiredAssets(
                    new BootstrapAssetValidationDependencies
                    {
                        LoadInputActionAsset = _ => null,
                        LoadTextAsset = _ => textAsset,
                        LoadGameConfig = _ => config,
                        LoadTuningConfig = _ => tuning,
                        LoadTutorialSpriteLibrary = _ => tutorialLibrary
                    });

                Assert.That(report.IsValid, Is.True);
                Assert.That(report.Issues, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(tuning);
                Object.DestroyImmediate(tutorialLibrary);
            }
        }

        [Test]
        public void ValidateRequiredAssets_InputActionAssetPresent_PassesWithoutJsonFallback()
        {
            var inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
            var config = ScriptableObject.CreateInstance<GameConfigSO>();
            var tuning = ScriptableObject.CreateInstance<TuningConfigSO>();
            var tutorialLibrary = ScriptableObject.CreateInstance<TutorialSpriteLibrary>();

            try
            {
                var report = BootstrapAssetContractValidator.ValidateRequiredAssets(
                    new BootstrapAssetValidationDependencies
                    {
                        LoadInputActionAsset = _ => inputActions,
                        LoadTextAsset = _ => null,
                        LoadGameConfig = _ => config,
                        LoadTuningConfig = _ => tuning,
                        LoadTutorialSpriteLibrary = _ => tutorialLibrary
                    });

                Assert.That(report.IsValid, Is.True);
                Assert.That(report.Issues, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(inputActions);
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(tuning);
                Object.DestroyImmediate(tutorialLibrary);
            }
        }
    }
}
