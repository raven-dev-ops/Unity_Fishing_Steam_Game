using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.UI
{
    public sealed class TutorialControlPanel : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;

        private void Awake()
        {
            _saveManager ??= FindObjectOfType<SaveManager>();
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
