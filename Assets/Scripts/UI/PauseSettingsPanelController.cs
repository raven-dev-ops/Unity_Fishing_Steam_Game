using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.UI
{
    public sealed class PauseSettingsPanelController : MonoBehaviour
    {
        [SerializeField] private PauseMenuController _pauseMenuController;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private Button _reelInputButton;
        [SerializeField] private Button _reducedMotionButton;
        [SerializeField] private Button _highContrastButton;
        [SerializeField] private Button _uiScaleDownButton;
        [SerializeField] private Button _uiScaleUpButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _uiScaleLabel;
        [SerializeField] private float _uiScaleStep = 0.05f;

        private Text _reelInputLabel;
        private Text _reducedMotionLabel;
        private Text _highContrastLabel;
        private bool _buttonHandlersBound;

        public void Configure(
            PauseMenuController pauseMenuController,
            Button reelInputButton,
            Button reducedMotionButton,
            Button highContrastButton,
            Button uiScaleDownButton,
            Button uiScaleUpButton,
            Text uiScaleLabel,
            Button backButton)
        {
            _pauseMenuController = pauseMenuController;
            _reelInputButton = reelInputButton;
            _reducedMotionButton = reducedMotionButton;
            _highContrastButton = highContrastButton;
            _uiScaleDownButton = uiScaleDownButton;
            _uiScaleUpButton = uiScaleUpButton;
            _uiScaleLabel = uiScaleLabel;
            _backButton = backButton;

            CacheButtonLabels();
            BindButtonHandlers();
            RefreshLabels();
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            if (_pauseMenuController == null)
            {
                _pauseMenuController = GetComponent<PauseMenuController>();
            }

            CacheButtonLabels();
            BindButtonHandlers();
        }

        private void OnEnable()
        {
            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= HandleSettingsChanged;
                _settingsService.SettingsChanged += HandleSettingsChanged;
            }

            RefreshLabels();
        }

        private void OnDisable()
        {
            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= HandleSettingsChanged;
            }
        }

        private void BindButtonHandlers()
        {
            if (_buttonHandlersBound)
            {
                return;
            }

            _buttonHandlersBound = true;
            if (_reelInputButton != null)
            {
                _reelInputButton.onClick.AddListener(ToggleReelInputMode);
            }

            if (_reducedMotionButton != null)
            {
                _reducedMotionButton.onClick.AddListener(ToggleReducedMotion);
            }

            if (_highContrastButton != null)
            {
                _highContrastButton.onClick.AddListener(ToggleHighContrast);
            }

            if (_uiScaleDownButton != null)
            {
                _uiScaleDownButton.onClick.AddListener(DecreaseUiScale);
            }

            if (_uiScaleUpButton != null)
            {
                _uiScaleUpButton.onClick.AddListener(IncreaseUiScale);
            }

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackPressed);
            }
        }

        private void CacheButtonLabels()
        {
            if (_reelInputLabel == null && _reelInputButton != null)
            {
                _reelInputLabel = _reelInputButton.GetComponentInChildren<Text>(includeInactive: true);
            }

            if (_reducedMotionLabel == null && _reducedMotionButton != null)
            {
                _reducedMotionLabel = _reducedMotionButton.GetComponentInChildren<Text>(includeInactive: true);
            }

            if (_highContrastLabel == null && _highContrastButton != null)
            {
                _highContrastLabel = _highContrastButton.GetComponentInChildren<Text>(includeInactive: true);
            }
        }

        private void ToggleReelInputMode()
        {
            if (_settingsService == null)
            {
                return;
            }

            _settingsService.SetReelInputToggle(!_settingsService.ReelInputToggle);
            RefreshLabels();
        }

        private void ToggleReducedMotion()
        {
            if (_settingsService == null)
            {
                return;
            }

            _settingsService.SetReducedMotion(!_settingsService.ReducedMotion);
            RefreshLabels();
        }

        private void ToggleHighContrast()
        {
            if (_settingsService == null)
            {
                return;
            }

            _settingsService.SetHighContrastFishingCues(!_settingsService.HighContrastFishingCues);
            RefreshLabels();
        }

        private void DecreaseUiScale()
        {
            if (_settingsService == null)
            {
                return;
            }

            _settingsService.SetUiScale(_settingsService.UiScale - Mathf.Abs(_uiScaleStep));
            RefreshLabels();
        }

        private void IncreaseUiScale()
        {
            if (_settingsService == null)
            {
                return;
            }

            _settingsService.SetUiScale(_settingsService.UiScale + Mathf.Abs(_uiScaleStep));
            RefreshLabels();
        }

        private void OnBackPressed()
        {
            _pauseMenuController?.OnBackFromSettingsPressed();
        }

        private void HandleSettingsChanged()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (_settingsService == null)
            {
                SetLabel(_reelInputLabel, "Reel Input: Hold");
                SetLabel(_reducedMotionLabel, "Reduced Motion: Off");
                SetLabel(_highContrastLabel, "High Contrast Cues: Off");
                SetLabel(_uiScaleLabel, "UI Scale: 1.00x");
                return;
            }

            SetLabel(_reelInputLabel, _settingsService.ReelInputToggle ? "Reel Input: Toggle" : "Reel Input: Hold");
            SetLabel(_reducedMotionLabel, _settingsService.ReducedMotion ? "Reduced Motion: On" : "Reduced Motion: Off");
            SetLabel(_highContrastLabel, _settingsService.HighContrastFishingCues ? "High Contrast Cues: On" : "High Contrast Cues: Off");
            SetLabel(_uiScaleLabel, $"UI Scale: {_settingsService.UiScale:0.00}x");
        }

        private static void SetLabel(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }
    }
}
