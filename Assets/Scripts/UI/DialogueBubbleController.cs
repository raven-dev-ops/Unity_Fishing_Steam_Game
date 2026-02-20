using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Audio;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.UI
{
    [Serializable]
    public sealed class DialogueLine
    {
        [TextArea(2, 6)] public string text;
        public AudioClip voiceClip;
    }

    public sealed class DialogueBubbleController : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _lineText;
        [SerializeField] private List<DialogueLine> _lines = new List<DialogueLine>();
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private UserSettingsService _settingsService;

        private int _index;
        private bool _isRunning;
        private InputAction _uiSubmitAction;
        private InputAction _uiCancelAction;
        private InputAction _harborInteractAction;
        private InputAction _harborPauseAction;

        public bool IsRunning => _isRunning;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_settingsService != null)
            {
                _settingsService.SettingsChanged += OnSettingsChanged;
            }
        }

        private void OnDisable()
        {
            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= OnSettingsChanged;
            }
        }

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            RefreshActionsIfNeeded();

            if (WasCancelPressedThisFrame())
            {
                Stop();
                return;
            }

            if (WasSubmitPressedThisFrame())
            {
                Advance();
            }
        }

        public void Play()
        {
            if (_lines.Count == 0)
            {
                return;
            }

            _index = 0;
            _isRunning = true;
            RefreshRootVisibility();
            ShowCurrentLine();
        }

        public void Stop()
        {
            _isRunning = false;
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void Advance()
        {
            if (!_isRunning)
            {
                return;
            }

            _index++;
            if (_index >= _lines.Count)
            {
                Stop();
                return;
            }

            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            var line = _lines[_index];
            if (_lineText != null)
            {
                _lineText.text = AreSubtitlesEnabled() ? line.text : string.Empty;
            }

            if (line.voiceClip != null)
            {
                _audioManager?.PlayVoice(line.voiceClip);
            }
        }

        private void OnSettingsChanged()
        {
            RefreshRootVisibility();
            if (_isRunning)
            {
                ShowCurrentLine();
            }
        }

        private void RefreshRootVisibility()
        {
            if (_root == null)
            {
                return;
            }

            _root.SetActive(_isRunning && AreSubtitlesEnabled());
        }

        private bool AreSubtitlesEnabled()
        {
            return _settingsService == null || _settingsService.SubtitlesEnabled;
        }

        private void RefreshActionsIfNeeded()
        {
            if (_inputMapController == null)
            {
                return;
            }

            _uiSubmitAction ??= _inputMapController.FindAction("UI/Submit");
            _uiCancelAction ??= _inputMapController.FindAction("UI/Cancel");
            _harborInteractAction ??= _inputMapController.FindAction("Harbor/Interact");
            _harborPauseAction ??= _inputMapController.FindAction("Harbor/Pause");
        }

        private bool WasSubmitPressedThisFrame()
        {
            return (_uiSubmitAction != null && _uiSubmitAction.WasPressedThisFrame())
                || (_harborInteractAction != null && _harborInteractAction.WasPressedThisFrame());
        }

        private bool WasCancelPressedThisFrame()
        {
            return (_uiCancelAction != null && _uiCancelAction.WasPressedThisFrame())
                || (_harborPauseAction != null && _harborPauseAction.WasPressedThisFrame());
        }
    }
}
