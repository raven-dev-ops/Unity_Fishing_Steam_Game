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
            if (_saveManager == null)
            {
                return;
            }

            _saveManager.Current.tutorialFlags.tutorialSeen = true;
            _saveManager.Save();
        }

        public void ReplayTutorial()
        {
            if (_saveManager == null)
            {
                return;
            }

            _saveManager.Current.tutorialFlags.tutorialSeen = false;
            _saveManager.Save();
        }
    }
}
