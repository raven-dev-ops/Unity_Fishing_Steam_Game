using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    public sealed class GameFlowPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            CleanupSingletons();
            RuntimeServiceRegistry.Clear();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            CleanupSingletons();
            RuntimeServiceRegistry.Clear();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator GameFlowManager_PauseResume_RestoresPreviousPlayableState()
        {
            var root = new GameObject("GameFlowManagerTests");
            var manager = root.AddComponent<GameFlowManager>();

            yield return null;

            manager.SetState(GameFlowState.Harbor);
            manager.TogglePause();

            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Pause));
            Assert.That(manager.IsPaused, Is.True);

            manager.ResumeFromPause();

            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Harbor));
            Assert.That(manager.IsPaused, Is.False);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator GameFlowOrchestrator_IntroReplayFromSettings_QueuesSettingsPanelOnly()
        {
            var managerRoot = new GameObject("GameFlowManager_IntroSettings");
            var manager = managerRoot.AddComponent<GameFlowManager>();
            var orchestratorRoot = new GameObject("GameFlowOrchestrator_IntroSettings");
            var orchestrator = orchestratorRoot.AddComponent<GameFlowOrchestrator>();
            orchestrator.enabled = false;
            orchestrator.Initialize(manager, null, null, null, null);

            yield return null;

            orchestrator.RequestOpenIntroReplayFromSettings();
            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Cinematic));

            orchestrator.RequestCompleteIntroFlow();
            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.MainMenu));
            Assert.That(GetPrivateBool(orchestrator, "_openSettingsAfterMainMenuLoad"), Is.True);
            Assert.That(GetPrivateBool(orchestrator, "_openProfileAfterMainMenuLoad"), Is.False);

            Object.Destroy(orchestratorRoot);
            Object.Destroy(managerRoot);
        }

        [UnityTest]
        public IEnumerator GameFlowOrchestrator_IntroReplayFromProfile_QueuesMainMenuWithoutProfileFollowUp()
        {
            var managerRoot = new GameObject("GameFlowManager_IntroProfile");
            var manager = managerRoot.AddComponent<GameFlowManager>();
            var orchestratorRoot = new GameObject("GameFlowOrchestrator_IntroProfile");
            var orchestrator = orchestratorRoot.AddComponent<GameFlowOrchestrator>();
            orchestrator.enabled = false;
            orchestrator.Initialize(manager, null, null, null, null);

            yield return null;

            orchestrator.RequestOpenIntroReplayFromProfile();
            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Cinematic));

            orchestrator.RequestCompleteIntroFlow();
            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.MainMenu));
            Assert.That(GetPrivateBool(orchestrator, "_openSettingsAfterMainMenuLoad"), Is.False);
            Assert.That(GetPrivateBool(orchestrator, "_openProfileAfterMainMenuLoad"), Is.False);

            Object.Destroy(orchestratorRoot);
            Object.Destroy(managerRoot);
        }

        [UnityTest]
        public IEnumerator GameFlowOrchestrator_DefaultIntroExitRoute_ClearsMainMenuFollowUpFlags()
        {
            var managerRoot = new GameObject("GameFlowManager_IntroDefault");
            var manager = managerRoot.AddComponent<GameFlowManager>();
            var orchestratorRoot = new GameObject("GameFlowOrchestrator_IntroDefault");
            var orchestrator = orchestratorRoot.AddComponent<GameFlowOrchestrator>();
            orchestrator.enabled = false;
            orchestrator.Initialize(manager, null, null, null, null);

            yield return null;

            orchestrator.RequestOpenIntroReplayFromProfile();
            orchestrator.RequestCompleteIntroFlow();
            Assert.That(GetPrivateBool(orchestrator, "_openProfileAfterMainMenuLoad"), Is.False);
            Assert.That(GetPrivateBool(orchestrator, "_openSettingsAfterMainMenuLoad"), Is.False);

            orchestrator.RequestOpenCinematic();
            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Cinematic));
            orchestrator.RequestCompleteIntroFlow();

            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.MainMenu));
            Assert.That(GetPrivateBool(orchestrator, "_openSettingsAfterMainMenuLoad"), Is.False);
            Assert.That(GetPrivateBool(orchestrator, "_openProfileAfterMainMenuLoad"), Is.False);

            Object.Destroy(orchestratorRoot);
            Object.Destroy(managerRoot);
        }

        [UnityTest]
        public IEnumerator GameFlowOrchestrator_UsesTypedMainMenuNavigatorForSettingsFollowUpOnly()
        {
            var managerRoot = new GameObject("GameFlowManager_TypedNavigator");
            var manager = managerRoot.AddComponent<GameFlowManager>();
            var navigatorRoot = new GameObject("MainMenuNavigator_TypedNavigator");
            var navigator = navigatorRoot.AddComponent<TestMainMenuNavigator>();
            RuntimeServiceRegistry.RegisterInterface<IMainMenuNavigator>(navigator);

            var orchestratorRoot = new GameObject("GameFlowOrchestrator_TypedNavigator");
            var orchestrator = orchestratorRoot.AddComponent<GameFlowOrchestrator>();
            orchestrator.enabled = false;
            orchestrator.Initialize(manager, null, null, null, null, navigator);

            yield return null;

            orchestrator.RequestOpenIntroReplayFromSettings();
            orchestrator.RequestCompleteIntroFlow();
            InvokePrivateMethod(orchestrator, "TryOpenMainMenuSettingsPanel");

            orchestrator.RequestOpenIntroReplayFromProfile();
            orchestrator.RequestCompleteIntroFlow();
            InvokePrivateMethod(orchestrator, "TryOpenMainMenuProfilePanel");

            Assert.That(navigator.SettingsOpenCalls, Is.EqualTo(1), "Expected typed settings follow-up to invoke navigator once.");
            Assert.That(navigator.ProfileOpenCalls, Is.EqualTo(0), "Profile follow-up should not be queued in demo-first main menu.");

            RuntimeServiceRegistry.UnregisterInterface<IMainMenuNavigator>(navigator);
            Object.Destroy(navigatorRoot);
            Object.Destroy(orchestratorRoot);
            Object.Destroy(managerRoot);
        }

        private static bool GetPrivateBool(object target, string fieldName)
        {
            Assert.That(target, Is.Not.Null);
            Assert.That(string.IsNullOrWhiteSpace(fieldName), Is.False);

            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            return field != null && (bool)field.GetValue(target);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            Assert.That(target, Is.Not.Null);
            Assert.That(string.IsNullOrWhiteSpace(methodName), Is.False);

            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            method.Invoke(target, null);
        }

        private static void CleanupSingletons()
        {
            if (GameFlowOrchestrator.Instance != null)
            {
                Object.DestroyImmediate(GameFlowOrchestrator.Instance.gameObject);
            }

            if (GameFlowManager.Instance != null)
            {
                Object.DestroyImmediate(GameFlowManager.Instance.gameObject);
            }
        }

        private sealed class TestMainMenuNavigator : MonoBehaviour, IMainMenuNavigator
        {
            public int ProfileOpenCalls { get; private set; }
            public int SettingsOpenCalls { get; private set; }

            public bool TryOpenProfilePanel()
            {
                ProfileOpenCalls += 1;
                return true;
            }

            public bool TryOpenSettingsPanel()
            {
                SettingsOpenCalls += 1;
                return true;
            }
        }

        [UnityTest]
        public IEnumerator GameFlowManager_ReturnToHarborFromPause_TransitionsCorrectly()
        {
            var root = new GameObject("GameFlowPauseToHarborTests");
            var manager = root.AddComponent<GameFlowManager>();

            yield return null;

            manager.SetState(GameFlowState.Fishing);
            manager.TogglePause();

            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Pause));

            manager.ReturnToHarborFromFishingPause();

            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Harbor));
            Assert.That(manager.IsPaused, Is.False);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator MainMenuController_ProfileSubmit_OpensProfilePanel()
        {
            var eventSystemRoot = new GameObject("MainMenuControllerEventSystem");
            eventSystemRoot.AddComponent<EventSystem>();

            var root = new GameObject("MainMenuController_ProfileSubmit");
            var controller = root.AddComponent<MainMenuController>();

            var startButton = new GameObject("StartButton");
            var profileButton = new GameObject("ProfileButton");
            var settingsButton = new GameObject("SettingsButton");
            var exitButton = new GameObject("ExitButton");

            var profilePanel = new GameObject("ProfilePanel");
            var settingsPanel = new GameObject("SettingsPanel");
            var profileBackButton = new GameObject("ProfileBackButton");
            profileBackButton.transform.SetParent(profilePanel.transform, worldPositionStays: false);

            controller.Configure(
                startButton: startButton,
                profileButton: profileButton,
                settingsButton: settingsButton,
                exitButton: exitButton,
                profilePanel: profilePanel,
                settingsPanel: settingsPanel,
                profileDefaultSelection: profileBackButton,
                settingsDefaultSelection: settingsButton);

            yield return null;

            profilePanel.SetActive(false);
            settingsPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(profileButton);
            controller.SubmitCurrentSelection();

            Assert.That(profilePanel.activeSelf, Is.True, "Expected profile panel to open from profile submit.");
            Assert.That(settingsPanel.activeSelf, Is.False, "Expected other submenus to close when profile opens.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(profileBackButton));

            Object.Destroy(profileBackButton);
            Object.Destroy(profilePanel);
            Object.Destroy(settingsPanel);
            Object.Destroy(startButton);
            Object.Destroy(profileButton);
            Object.Destroy(settingsButton);
            Object.Destroy(exitButton);
            Object.Destroy(root);
            Object.Destroy(eventSystemRoot);
        }
    }
}
