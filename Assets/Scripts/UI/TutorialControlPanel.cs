using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using TMPro;
using UnityEngine;

namespace RavenDevOps.Fishing.UI
{
    public sealed class TutorialControlPanel : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private TMP_Text _statusText;

        public void Configure(TMP_Text statusText)
        {
            _statusText = statusText;
            RefreshStatus();
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RefreshStatus();
        }

        private void OnEnable()
        {
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
            _saveManager?.SetTutorialSeen(true);
            RefreshStatus();
        }

        public void ReplayTutorial()
        {
            _saveManager?.SetTutorialSeen(false);
            RefreshStatus();
        }

        public void SkipFishingTutorial()
        {
            _saveManager?.CompleteFishingLoopTutorial(skipped: true);
            RefreshStatus();
        }

        public void ReplayFishingTutorial()
        {
            _saveManager?.RequestFishingLoopTutorialReplay();
            RefreshStatus();
        }

        private void OnSaveDataChanged(SaveDataV1 data)
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
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
            var fishing = flags.fishingLoopTutorialCompleted
                ? (flags.fishingLoopTutorialSkipped ? "Skipped" : "Completed")
                : "Pending";
            var replay = flags.fishingLoopTutorialReplayRequested ? "Yes" : "No";
            _statusText.text = $"Tutorial flags: Intro={intro} | Fishing={fishing} | ReplayRequested={replay}";
        }
    }
}
