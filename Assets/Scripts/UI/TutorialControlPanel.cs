using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using TMPro;
using UnityEngine;

namespace RavenDevOps.Fishing.UI
{
    public sealed class TutorialControlPanel : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private TMP_Text _statusText;

        public void Configure(TMP_Text statusText)
        {
            _statusText = statusText;
            EnsureSaveManager();
            RefreshStatus();
        }

        private void Awake()
        {
            EnsureSaveManager();
            RefreshStatus();
        }

        private void OnEnable()
        {
            EnsureSaveManager();
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged += OnSaveDataChanged;
            }

            RefreshStatus();
        }

        private void OnDisable()
        {
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged -= OnSaveDataChanged;
            }
        }

        public void SkipTutorial()
        {
            EnsureSaveManager();
            _saveManager?.SetTutorialSeen(true);
            RefreshStatus();
        }

        public void ReplayTutorial()
        {
            EnsureSaveManager();
            _saveManager?.RequestIntroTutorialReplay();
            _orchestrator?.RequestOpenIntroReplayFromProfile();
            RefreshStatus();
        }

        public void SkipFishingTutorial()
        {
            EnsureSaveManager();
            _saveManager?.CompleteFishingLoopTutorial(skipped: true);
            RefreshStatus();
        }

        public void ReplayFishingTutorial()
        {
            EnsureSaveManager();
            _saveManager?.RequestFishingLoopTutorialReplay();
            _orchestrator?.RequestOpenFishingTutorialReplayFromProfile();

            RefreshStatus();
        }

        private void OnSaveDataChanged(SaveDataV1 data)
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            EnsureSaveManager();
            if (_statusText == null)
            {
                return;
            }

            var save = _saveManager != null ? _saveManager.Current : null;
            if (save == null)
            {
                _statusText.text = "Tutorial flags: unavailable";
                return;
            }

            save.tutorialFlags ??= new TutorialFlags();
            var flags = save.tutorialFlags;
            var intro = flags.tutorialSeen ? "Seen" : "Pending";
            var introReplay = flags.introTutorialReplayRequested ? "Yes" : "No";
            var fishing = flags.fishingLoopTutorialCompleted
                ? (flags.fishingLoopTutorialSkipped ? "Skipped" : "Completed")
                : "Pending";
            var replay = flags.fishingLoopTutorialReplayRequested ? "Yes" : "No";
            _statusText.text = $"Tutorial flags: Intro={intro} | IntroReplay={introReplay} | Fishing={fishing} | FishingReplay={replay}";
        }

        private void EnsureSaveManager()
        {
            if (_saveManager != null && _orchestrator != null)
            {
                return;
            }

            if (_saveManager == null)
            {
                RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
                _saveManager ??= FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            }

            if (_orchestrator == null)
            {
                RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
                _orchestrator ??= FindAnyObjectByType<GameFlowOrchestrator>(FindObjectsInactive.Include);
            }
        }
    }
}
