using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.UI
{
    public sealed class TutorialControlPanel : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
        }

        public void SkipTutorial()
        {
            _saveManager?.SetTutorialSeen(true);
        }

        public void ReplayTutorial()
        {
            _saveManager?.SetTutorialSeen(false);
        }

        public void SkipFishingTutorial()
        {
            _saveManager?.CompleteFishingLoopTutorial(skipped: true);
        }

        public void ReplayFishingTutorial()
        {
            _saveManager?.RequestFishingLoopTutorialReplay();
        }
    }
}
