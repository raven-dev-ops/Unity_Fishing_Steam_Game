using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Audio;
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

        private int _index;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        private void Awake()
        {
            _audioManager ??= FindObjectOfType<AudioManager>();
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Stop();
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame)
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
            if (_root != null)
            {
                _root.SetActive(true);
            }

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
                _lineText.text = line.text;
            }

            if (line.voiceClip != null)
            {
                _audioManager?.PlayVoice(line.voiceClip);
            }
        }
    }
}
