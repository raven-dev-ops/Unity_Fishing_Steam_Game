using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RavenDevOps.Fishing.Core
{
    public sealed class SceneLoader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _fadeCanvas;
        [SerializeField] private float _fadeDurationSeconds = 0.45f;

        private bool _isTransitioning;
        private bool _nextTransitionStartsFromBlack;
        private float _nextTransitionFadeInSeconds = 2f;

        public bool IsTransitioning => _isTransitioning;

        public void SetFadeCanvas(CanvasGroup fadeCanvas)
        {
            _fadeCanvas = fadeCanvas;
        }

        public void SetOverlayAlphaImmediate(float alpha)
        {
            if (_fadeCanvas == null)
            {
                return;
            }

            var clamped = Mathf.Clamp01(alpha);
            _fadeCanvas.alpha = clamped;
            _fadeCanvas.blocksRaycasts = !Mathf.Approximately(clamped, 0f);
            _fadeCanvas.interactable = !Mathf.Approximately(clamped, 0f);
        }

        public void QueueTransitionFromBlack(float fadeInDurationSeconds)
        {
            _nextTransitionStartsFromBlack = true;
            _nextTransitionFadeInSeconds = Mathf.Max(0.05f, fadeInDurationSeconds);
        }

        public IEnumerator LoadSceneWithFade(string scenePath, Action onBeforeLoad = null)
        {
            if (_isTransitioning)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                yield break;
            }

            _isTransitioning = true;

            if (_nextTransitionStartsFromBlack)
            {
                SetOverlayAlphaImmediate(1f);
                onBeforeLoad?.Invoke();

                var queuedOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
                while (!queuedOperation.isDone)
                {
                    yield return null;
                }

                var queuedFadeInSeconds = Mathf.Max(0.05f, _nextTransitionFadeInSeconds);
                yield return Fade(1f, 0f, queuedFadeInSeconds);

                _nextTransitionStartsFromBlack = false;
                _nextTransitionFadeInSeconds = 2f;
                _isTransitioning = false;
                yield break;
            }

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
            yield return Fade(from, to, _fadeDurationSeconds);
        }

        private IEnumerator Fade(float from, float to, float durationSeconds)
        {
            if (_fadeCanvas == null)
            {
                yield break;
            }

            var elapsed = 0f;
            _fadeCanvas.blocksRaycasts = true;
            _fadeCanvas.interactable = true;

            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, durationSeconds));
                _fadeCanvas.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _fadeCanvas.alpha = to;
            _fadeCanvas.blocksRaycasts = !Mathf.Approximately(to, 0f);
            _fadeCanvas.interactable = !Mathf.Approximately(to, 0f);
        }
    }
}
