using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.Steam;
using RavenDevOps.Fishing.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    [Category("LaunchRegression")]
    public sealed class LaunchPathRegressionPlayModeTests
    {
        private readonly List<GameObject> _roots = new List<GameObject>();
        private readonly Dictionary<string, string> _saveBackups = new Dictionary<string, string>();
        private string _backupDirectory = string.Empty;
        private string _savePath = string.Empty;

        [SetUp]
        public void SetUp()
        {
            BackupAndClearSaveFiles();
            RuntimeServiceRegistry.Clear();
            CleanupSingletons();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _roots.Count; i++)
            {
                var root = _roots[i];
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            _roots.Clear();
            CleanupSingletons();
            RuntimeServiceRegistry.Clear();
            RestoreSaveFiles();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator BootToCinematicToMainMenu_AutoAdvance_TransitionsInOrder()
        {
            var manager = CreateComponent<GameFlowManager>("LaunchRegression_GameFlowManager");
            yield return null;

            var statusText = CreateUiText("LaunchRegression_BootStatus");
            var bootRoot = new GameObject("LaunchRegression_BootController");
            _roots.Add(bootRoot);
            bootRoot.SetActive(false);
            var bootController = bootRoot.AddComponent<BootSceneFlowController>();
            SetPrivateField(bootController, "_autoAdvanceSeconds", 0.01f);
            SetPrivateField(bootController, "_bootToIntroFadeInSeconds", 0.05f);
            bootController.Configure(statusText);

            var cinematicRoot = new GameObject("LaunchRegression_CinematicController");
            _roots.Add(cinematicRoot);
            cinematicRoot.SetActive(false);
            var cinematicController = cinematicRoot.AddComponent<CinematicSceneFlowController>();
            SetPrivateField(cinematicController, "_minimumIntroWatchSeconds", 0f);
            SetPrivateField(cinematicController, "_introAutoAdvanceSeconds", 0.05f);
            SetPrivateField(cinematicController, "_titleFadeToBlackSeconds", 0.05f);
            SetPrivateField(cinematicController, "_titleHoldSeconds", 0f);
            SetPrivateField(cinematicController, "_inputDebounceSeconds", 0f);
            SetPrivateField(cinematicController, "_batchModeAutoAdvanceSeconds", 0.05f);

            var stateTransitions = new List<GameFlowState>();
            manager.StateChanged += (_, next) =>
            {
                stateTransitions.Add(next);
                if (next == GameFlowState.Cinematic && cinematicRoot != null && !cinematicRoot.activeSelf)
                {
                    cinematicRoot.SetActive(true);
                }
            };

            bootRoot.SetActive(true);

            var timeoutAt = Time.realtimeSinceStartup + 3f;
            while (manager.CurrentState != GameFlowState.MainMenu && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.MainMenu), "Expected deterministic boot->cinematic->main-menu flow to complete.");
            Assert.That(
                stateTransitions,
                Is.EqualTo(new[] { GameFlowState.Cinematic, GameFlowState.MainMenu }),
                "Expected only Cinematic then MainMenu transitions during launch path.");
            Assert.That(statusText.text, Does.Contain("Loading cinematic"), "Expected boot status text to reflect the cinematic hand-off.");
        }

        [UnityTest]
        public IEnumerator IntroReplayReturnRoutes_OpenCorrectMainMenuPanels()
        {
            var manager = CreateComponent<GameFlowManager>("LaunchRegression_GameFlowManager_IntroReplay");
            var orchestratorRoot = new GameObject("LaunchRegression_GameFlowOrchestrator_IntroReplay");
            _roots.Add(orchestratorRoot);
            var orchestrator = orchestratorRoot.AddComponent<GameFlowOrchestrator>();
            orchestrator.enabled = false;
            orchestrator.Initialize(manager, null, null, null, null);

            var eventSystemRoot = new GameObject("LaunchRegression_EventSystem_IntroReplay");
            _roots.Add(eventSystemRoot);
            eventSystemRoot.AddComponent<EventSystem>();

            var mainMenuRoot = new GameObject("LaunchRegression_MainMenuController_IntroReplay");
            _roots.Add(mainMenuRoot);
            var mainMenuController = mainMenuRoot.AddComponent<MainMenuController>();

            var startButton = new GameObject("LaunchRegression_StartButton_IntroReplay");
            var profileButton = new GameObject("LaunchRegression_ProfileButton_IntroReplay");
            var settingsButton = new GameObject("LaunchRegression_SettingsButton_IntroReplay");
            var exitButton = new GameObject("LaunchRegression_ExitButton_IntroReplay");
            _roots.Add(startButton);
            _roots.Add(profileButton);
            _roots.Add(settingsButton);
            _roots.Add(exitButton);

            var profilePanel = new GameObject("LaunchRegression_ProfilePanel_IntroReplay");
            var settingsPanel = new GameObject("LaunchRegression_SettingsPanel_IntroReplay");
            var profileDefault = new GameObject("LaunchRegression_ProfileDefault_IntroReplay");
            var settingsDefault = new GameObject("LaunchRegression_SettingsDefault_IntroReplay");
            _roots.Add(profilePanel);
            _roots.Add(settingsPanel);
            _roots.Add(profileDefault);
            _roots.Add(settingsDefault);
            profileDefault.transform.SetParent(profilePanel.transform, worldPositionStays: false);
            settingsDefault.transform.SetParent(settingsPanel.transform, worldPositionStays: false);

            mainMenuController.Configure(
                startButton: startButton,
                profileButton: profileButton,
                settingsButton: settingsButton,
                exitButton: exitButton,
                profilePanel: profilePanel,
                settingsPanel: settingsPanel,
                profileDefaultSelection: profileDefault,
                settingsDefaultSelection: settingsDefault);

            yield return null;

            profilePanel.SetActive(false);
            settingsPanel.SetActive(false);

            orchestrator.RequestOpenIntroReplayFromProfile();
            orchestrator.RequestCompleteIntroFlow();
            InvokePrivateMethod(orchestrator, "TryOpenMainMenuProfilePanel");

            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.MainMenu), "Expected intro replay route to return to main menu.");
            Assert.That(profilePanel.activeSelf, Is.True, "Profile replay route must open Profile panel.");
            Assert.That(settingsPanel.activeSelf, Is.False, "Profile replay route must not open Settings panel.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(profileDefault), "Profile replay route should focus the profile default selection.");

            orchestrator.RequestOpenIntroReplayFromSettings();
            orchestrator.RequestCompleteIntroFlow();
            InvokePrivateMethod(orchestrator, "TryOpenMainMenuSettingsPanel");

            Assert.That(settingsPanel.activeSelf, Is.True, "Settings replay route must open Settings panel.");
            Assert.That(profilePanel.activeSelf, Is.False, "Settings replay route must not leave Profile panel open.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(settingsDefault), "Settings replay route should focus the settings default selection.");
        }

        [UnityTest]
        public IEnumerator MainMenuSelectionWiring_StartProfileSettingsAndExitBehaveDeterministically()
        {
            var manager = CreateComponent<GameFlowManager>("LaunchRegression_GameFlowManager_MainMenu");
            var orchestratorRoot = new GameObject("LaunchRegression_GameFlowOrchestrator_MainMenu");
            _roots.Add(orchestratorRoot);
            var orchestrator = orchestratorRoot.AddComponent<GameFlowOrchestrator>();
            orchestrator.enabled = false;
            orchestrator.Initialize(manager, null, null, null, null);

            var eventSystemRoot = new GameObject("LaunchRegression_EventSystem_MainMenu");
            _roots.Add(eventSystemRoot);
            eventSystemRoot.AddComponent<EventSystem>();

            var mainMenuRoot = new GameObject("LaunchRegression_MainMenuController_MainMenu");
            _roots.Add(mainMenuRoot);
            var mainMenuController = mainMenuRoot.AddComponent<MainMenuController>();
            SetPrivateField(mainMenuController, "_orchestrator", orchestrator);

            var startButton = new GameObject("LaunchRegression_StartButton_MainMenu");
            var profileButton = new GameObject("LaunchRegression_ProfileButton_MainMenu");
            var settingsButton = new GameObject("LaunchRegression_SettingsButton_MainMenu");
            var exitButton = new GameObject("LaunchRegression_ExitButton_MainMenu");
            _roots.Add(startButton);
            _roots.Add(profileButton);
            _roots.Add(settingsButton);
            _roots.Add(exitButton);

            var profilePanel = new GameObject("LaunchRegression_ProfilePanel_MainMenu");
            var settingsPanel = new GameObject("LaunchRegression_SettingsPanel_MainMenu");
            var exitPanel = new GameObject("LaunchRegression_ExitPanel_MainMenu");
            var exitConfirm = new GameObject("LaunchRegression_ExitConfirm_MainMenu");
            var exitCancel = new GameObject("LaunchRegression_ExitCancel_MainMenu");
            var profileDefault = new GameObject("LaunchRegression_ProfileDefault_MainMenu");
            var settingsDefault = new GameObject("LaunchRegression_SettingsDefault_MainMenu");
            _roots.Add(profilePanel);
            _roots.Add(settingsPanel);
            _roots.Add(exitPanel);
            _roots.Add(exitConfirm);
            _roots.Add(exitCancel);
            _roots.Add(profileDefault);
            _roots.Add(settingsDefault);
            profileDefault.transform.SetParent(profilePanel.transform, worldPositionStays: false);
            settingsDefault.transform.SetParent(settingsPanel.transform, worldPositionStays: false);
            exitConfirm.transform.SetParent(exitPanel.transform, worldPositionStays: false);
            exitCancel.transform.SetParent(exitPanel.transform, worldPositionStays: false);

            mainMenuController.Configure(
                startButton: startButton,
                profileButton: profileButton,
                settingsButton: settingsButton,
                exitButton: exitButton,
                profilePanel: profilePanel,
                settingsPanel: settingsPanel,
                exitPanel: exitPanel,
                exitConfirmButton: exitConfirm,
                exitCancelButton: exitCancel,
                profileDefaultSelection: profileDefault,
                settingsDefaultSelection: settingsDefault);

            yield return null;

            EventSystem.current.SetSelectedGameObject(startButton);
            mainMenuController.SubmitCurrentSelection();
            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Harbor), "Start selection should route to Harbor state via orchestrator.");

            EventSystem.current.SetSelectedGameObject(profileButton);
            mainMenuController.SubmitCurrentSelection();
            Assert.That(profilePanel.activeSelf, Is.True, "Profile selection should open Profile panel.");
            Assert.That(settingsPanel.activeSelf, Is.False, "Profile selection should hide Settings panel.");

            EventSystem.current.SetSelectedGameObject(settingsButton);
            mainMenuController.SubmitCurrentSelection();
            Assert.That(settingsPanel.activeSelf, Is.True, "Settings selection should open Settings panel.");
            Assert.That(profilePanel.activeSelf, Is.False, "Settings selection should hide Profile panel.");

            EventSystem.current.SetSelectedGameObject(exitButton);
            mainMenuController.SubmitCurrentSelection();
            Assert.That(exitPanel.activeSelf, Is.True, "Exit selection should open Exit confirmation panel.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(exitCancel), "Exit confirmation should default focus to cancel.");

            EventSystem.current.SetSelectedGameObject(exitCancel);
            mainMenuController.SubmitCurrentSelection();
            Assert.That(exitPanel.activeSelf, Is.False, "Exit cancel should close Exit confirmation panel.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(exitButton), "Exit cancel should restore selection to exit button.");
        }

        [UnityTest]
        public IEnumerator SaveStartup_CleanProfile_CreatesDefaultsAndPersists()
        {
            var saveManager = CreateComponent<SaveManager>("LaunchRegression_SaveManager_Clean");
            yield return null;

            Assert.That(File.Exists(saveManager.SaveFilePath), Is.True, "Clean profile startup should persist a new save file.");
            Assert.That(saveManager.Current.copecs, Is.EqualTo(0), "Clean profile should start with zero copecs.");
            Assert.That(saveManager.Current.ownedShips, Contains.Item("ship_lv1"), "Clean profile should seed starter ship ownership.");
            Assert.That(saveManager.Current.ownedHooks, Contains.Item("hook_lv1"), "Clean profile should seed starter hook ownership.");
            Assert.That(saveManager.Current.equippedShipId, Is.EqualTo("ship_lv1"), "Clean profile should equip starter ship.");
            Assert.That(saveManager.Current.equippedHookId, Is.EqualTo("hook_lv1"), "Clean profile should equip starter hook.");
            Assert.That(saveManager.ShouldRunIntroTutorial(), Is.True, "Clean profile should request intro tutorial.");
            Assert.That(saveManager.ShouldRunFishingLoopTutorial(), Is.True, "Clean profile should request fishing tutorial.");
            Assert.That(string.IsNullOrWhiteSpace(saveManager.Current.careerStartLocalDate), Is.False, "Clean profile should stamp career start date.");
            Assert.That(string.IsNullOrWhiteSpace(saveManager.Current.lastLoginLocalDate), Is.False, "Clean profile should stamp last login date.");
        }

        [UnityTest]
        public IEnumerator SaveStartup_ExistingProfile_LoadsPersistedValuesWithoutReset()
        {
            var saveManager = CreateComponent<SaveManager>("LaunchRegression_SaveManager_Seed");
            yield return null;

            saveManager.Current.copecs = 432;
            saveManager.Current.stats.totalFishCaught = 7;
            saveManager.Current.tutorialFlags.tutorialSeen = true;
            saveManager.Current.tutorialFlags.fishingLoopTutorialCompleted = true;
            saveManager.Current.tutorialFlags.fishingLoopTutorialReplayRequested = false;
            if (!saveManager.Current.ownedHooks.Contains("hook_lv2"))
            {
                saveManager.Current.ownedHooks.Add("hook_lv2");
            }

            saveManager.Current.equippedHookId = "hook_lv2";
            saveManager.Current.progression.totalXp = 245;
            saveManager.Save(forceImmediate: true);

            UnityEngine.Object.DestroyImmediate(saveManager.gameObject);
            yield return null;

            var reloaded = CreateComponent<SaveManager>("LaunchRegression_SaveManager_Reloaded");
            yield return null;

            Assert.That(reloaded.Current.copecs, Is.EqualTo(432), "Existing profile should preserve copecs.");
            Assert.That(reloaded.Current.stats.totalFishCaught, Is.EqualTo(7), "Existing profile should preserve catch stats.");
            Assert.That(reloaded.Current.ownedHooks, Contains.Item("hook_lv2"), "Existing profile should preserve purchased ownership.");
            Assert.That(reloaded.Current.equippedHookId, Is.EqualTo("hook_lv2"), "Existing profile should preserve equipped hook.");
            Assert.That(reloaded.Current.progression.totalXp, Is.EqualTo(245), "Existing profile should preserve progression XP.");
            Assert.That(reloaded.ShouldRunIntroTutorial(), Is.False, "Existing profile with intro tutorial seen should not replay intro by default.");
            Assert.That(reloaded.ShouldRunFishingLoopTutorial(), Is.False, "Existing profile with completed fishing tutorial should not replay by default.");
        }

        [UnityTest]
        public IEnumerator SteamDisabledStartupAndRuntime_StaysInFallbackWithoutCloudSideEffects()
        {
#if STEAMWORKS_NET
            Assert.Ignore("Non-Steam fallback assertions apply only when STEAMWORKS_NET is not defined.");
#else
            var saveManager = CreateComponent<SaveManager>("LaunchRegression_SaveManager_NonSteam");
            var flowManager = CreateComponent<GameFlowManager>("LaunchRegression_GameFlowManager_NonSteam");
            var bootstrap = CreateComponent<SteamBootstrap>("LaunchRegression_SteamBootstrap_NonSteam");
            var cloudSync = CreateComponent<SteamCloudSyncService>("LaunchRegression_SteamCloudSync_NonSteam");
            CreateComponent<SteamRichPresenceService>("LaunchRegression_SteamRichPresence_NonSteam");
            CreateComponent<SteamStatsService>("LaunchRegression_SteamStats_NonSteam");
            yield return null;

            var localSavePath = saveManager.SaveFilePath;
            var localDirectory = Path.GetDirectoryName(localSavePath);
            var conflictFileCountBefore = string.IsNullOrWhiteSpace(localDirectory) || !Directory.Exists(localDirectory)
                ? 0
                : Directory.GetFiles(localDirectory, "save_v1.json.conflict_*", SearchOption.TopDirectoryOnly).Length;

            flowManager.SetState(GameFlowState.Harbor);
            flowManager.SetState(GameFlowState.Fishing);
            saveManager.AddCopecs(25);
            saveManager.RecordCatch("fish_launch_regression_non_steam", 1, weightKg: 0.6f, valueCopecs: 5);
            saveManager.Save(forceImmediate: true);
            yield return null;

            var conflictFileCountAfter = string.IsNullOrWhiteSpace(localDirectory) || !Directory.Exists(localDirectory)
                ? 0
                : Directory.GetFiles(localDirectory, "save_v1.json.conflict_*", SearchOption.TopDirectoryOnly).Length;

            Assert.That(bootstrap, Is.Not.Null, "Expected Steam bootstrap component.");
            Assert.That(SteamBootstrap.IsSteamInitialized, Is.False, "Steam should remain disabled when STEAMWORKS_NET is unavailable.");
            Assert.That(SteamBootstrap.LastFallbackReason, Does.Contain("STEAMWORKS_NET"), "Fallback reason should explain non-Steam mode.");
            Assert.That(cloudSync.LastConflictDecision, Is.EqualTo(string.Empty), "Cloud sync should not emit startup conflict decisions in non-Steam mode.");
            Assert.That(saveManager.Current.copecs, Is.EqualTo(25), "Non-Steam mode should preserve local save writes.");
            Assert.That(saveManager.Current.stats.totalFishCaught, Is.EqualTo(1), "Non-Steam mode should preserve local catch progression.");
            Assert.That(conflictFileCountAfter, Is.EqualTo(conflictFileCountBefore), "Non-Steam runtime should not create Steam conflict backups.");
            Assert.That(File.Exists(localSavePath), Is.True, "Local save file should remain present in non-Steam mode.");
#endif
        }

        private T CreateComponent<T>(string rootName) where T : Component
        {
            var root = new GameObject(rootName);
            _roots.Add(root);
            return root.AddComponent<T>();
        }

        private TMP_Text CreateUiText(string objectName)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            _roots.Add(go);
            var text = go.GetComponent<TMP_Text>();
            text.text = string.Empty;
            return text;
        }

        private void BackupAndClearSaveFiles()
        {
            _saveBackups.Clear();
            _backupDirectory = Path.Combine(Application.temporaryCachePath, "launch_regression_save_backups", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_backupDirectory);

            _savePath = Path.Combine(Application.persistentDataPath, "save_v1.json");
            var trackedFiles = new[]
            {
                _savePath,
                _savePath + ".bak",
                _savePath + ".tmp"
            };

            for (var i = 0; i < trackedFiles.Length; i++)
            {
                var path = trackedFiles[i];
                if (!File.Exists(path))
                {
                    continue;
                }

                var backupPath = Path.Combine(_backupDirectory, Path.GetFileName(path));
                File.Copy(path, backupPath, overwrite: true);
                _saveBackups[path] = backupPath;
                File.Delete(path);
            }
        }

        private void RestoreSaveFiles()
        {
            var trackedFiles = new[]
            {
                _savePath,
                _savePath + ".bak",
                _savePath + ".tmp"
            };

            for (var i = 0; i < trackedFiles.Length; i++)
            {
                var path = trackedFiles[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            foreach (var pair in _saveBackups)
            {
                if (!File.Exists(pair.Value))
                {
                    continue;
                }

                var directory = Path.GetDirectoryName(pair.Key);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(pair.Value, pair.Key, overwrite: true);
            }

            if (Directory.Exists(_backupDirectory))
            {
                Directory.Delete(_backupDirectory, recursive: true);
            }

            _saveBackups.Clear();
            _backupDirectory = string.Empty;
            _savePath = string.Empty;
        }

        private static void CleanupSingletons()
        {
            if (GameFlowOrchestrator.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(GameFlowOrchestrator.Instance.gameObject);
            }

            if (GameFlowManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(GameFlowManager.Instance.gameObject);
            }

            if (SaveManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(SaveManager.Instance.gameObject);
            }

            if (UserSettingsService.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(UserSettingsService.Instance.gameObject);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null || string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Target and fieldName are required for reflection assignment.");
            }

            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            field.SetValue(target, value);
        }

        private static object InvokePrivateMethod(object target, string methodName)
        {
            if (target == null || string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Target and methodName are required for reflection invocation.");
            }

            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, methodName);
            }

            return method.Invoke(target, null);
        }
    }
}
