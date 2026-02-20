using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Core.Logging
{
    public sealed class DevelopmentLogConsole : MonoBehaviour
    {
        [SerializeField] private bool _visible;
        [SerializeField] private float _windowWidth = 880f;
        [SerializeField] private float _windowHeight = 300f;
        [SerializeField] private int _maxVisibleEntries = 20;

        private Vector2 _scroll;

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.backquoteKey.wasPressedThisFrame)
            {
                _visible = !_visible;
            }
#else
            _visible = false;
#endif
        }

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_visible)
            {
                return;
            }

            var x = 16f;
            var y = Screen.height - _windowHeight - 16f;
            GUILayout.BeginArea(new Rect(x, y, _windowWidth, _windowHeight), "DEV Log Console", GUI.skin.window);
            GUILayout.Label($"Log file: {StructuredLogService.LogFilePath}");

            var snapshot = StructuredLogService.Instance != null
                ? StructuredLogService.Instance.GetRecentEntriesSnapshot()
                : new List<StructuredLogEntry>();

            _scroll = GUILayout.BeginScrollView(_scroll);
            var start = Mathf.Max(0, snapshot.Count - Mathf.Max(1, _maxVisibleEntries));
            for (var i = start; i < snapshot.Count; i++)
            {
                var entry = snapshot[i];
                GUILayout.Label($"[{entry.level}] [{entry.category}] {entry.message}");
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
#endif
        }
    }
}
