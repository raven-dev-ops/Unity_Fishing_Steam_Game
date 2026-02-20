using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RavenDevOps.Fishing.Core
{
    public sealed class SceneLoader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _fadeCanvas;
        [SerializeField] private float _fadeDurationSeconds = 0.2f;

        private bool _isTransitioning;

        public bool IsTransitioning => _isTransitioning;

        public IEnumerator LoadSceneWithFade(string scenePath, Action onBeforeLoad = null)
        {
            if (_isTransitioning)
            {
                yield break;
            }

            _isTransitioning = true;
            yield return Fade(0f, 1f);
            onBeforeLoad?.Invoke();

            var op = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            while (!op.isDone)
            {
                yield return null;
            }

            yield return Fade(1f, 0f);
            _isTransitioning = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_fadeCanvas == null)
            {
                yield break;
            }

            var elapsed = 0f;
            _fadeCanvas.blocksRaycasts = true;
            _fadeCanvas.interactable = true;

            while (elapsed < _fadeDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, _fadeDurationSeconds));
                _fadeCanvas.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _fadeCanvas.alpha = to;
            _fadeCanvas.blocksRaycasts = !Mathf.Approximately(to, 0f);
            _fadeCanvas.interactable = !Mathf.Approximately(to, 0f);
        }
    }
}
