using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Core
{
    public sealed class SceneLoader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _fadeCanvas;
        [SerializeField] private float _fadeDurationSeconds = 0.45f;
        [SerializeField] private float _defaultTitleCardHoldSeconds = 1.1f;
        [SerializeField] private CanvasGroup _transitionTitleOverlay;
        [SerializeField] private Text _transitionTitleText;

        private bool _isTransitioning;
        private bool _nextTransitionStartsFromBlack;
        private float _nextTransitionFadeInSeconds = 2f;
        private bool _nextTransitionHasTitleCard;
        private string _nextTransitionTitleCardLabel = string.Empty;
        private float _nextTransitionTitleCardHoldSeconds = 1.1f;
        private bool _transitionTitleVisible;

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
            SyncTransitionTitleAlpha(clamped);
        }

        public void QueueTransitionFromBlack(float fadeInDurationSeconds)
        {
            _nextTransitionStartsFromBlack = true;
            _nextTransitionFadeInSeconds = Mathf.Max(0.05f, fadeInDurationSeconds);
        }

        public void QueueTransitionTitleCard(string titleLabel, float holdSeconds = -1f)
        {
            if (string.IsNullOrWhiteSpace(titleLabel))
            {
                return;
            }

            _nextTransitionHasTitleCard = true;
            _nextTransitionTitleCardLabel = titleLabel.Trim();
            var hold = holdSeconds >= 0f ? holdSeconds : _defaultTitleCardHoldSeconds;
            _nextTransitionTitleCardHoldSeconds = Mathf.Max(0.05f, hold);
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
            HideTransitionTitleImmediate();

            if (_nextTransitionStartsFromBlack)
            {
                SetOverlayAlphaImmediate(1f);
                onBeforeLoad?.Invoke();

                var queuedOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
                while (!queuedOperation.isDone)
                {
                    yield return null;
                }

                yield return HoldQueuedTitleCardIfNeeded();
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

            yield return HoldQueuedTitleCardIfNeeded();
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
                SyncTransitionTitleAlpha(_fadeCanvas.alpha);
                yield return null;
            }

            _fadeCanvas.alpha = to;
            SyncTransitionTitleAlpha(_fadeCanvas.alpha);
            _fadeCanvas.blocksRaycasts = !Mathf.Approximately(to, 0f);
            _fadeCanvas.interactable = !Mathf.Approximately(to, 0f);
        }

        private IEnumerator HoldQueuedTitleCardIfNeeded()
        {
            if (!_nextTransitionHasTitleCard || string.IsNullOrWhiteSpace(_nextTransitionTitleCardLabel))
            {
                yield break;
            }

            EnsureTransitionTitleOverlay();
            if (_transitionTitleOverlay == null || _transitionTitleText == null)
            {
                ClearQueuedTitleCard();
                yield break;
            }

            _transitionTitleText.text = _nextTransitionTitleCardLabel;
            _transitionTitleOverlay.gameObject.SetActive(true);
            _transitionTitleOverlay.transform.SetAsLastSibling();
            _transitionTitleOverlay.alpha = 1f;
            _transitionTitleOverlay.blocksRaycasts = false;
            _transitionTitleOverlay.interactable = false;
            _transitionTitleVisible = true;

            var holdSeconds = Mathf.Max(0.05f, _nextTransitionTitleCardHoldSeconds);
            ClearQueuedTitleCard();
            yield return new WaitForSecondsRealtime(holdSeconds);
        }

        private void EnsureTransitionTitleOverlay()
        {
            if (_fadeCanvas == null)
            {
                return;
            }

            if (_transitionTitleOverlay != null && _transitionTitleText != null)
            {
                return;
            }

            var overlayObject = _transitionTitleOverlay != null
                ? _transitionTitleOverlay.gameObject
                : null;
            if (overlayObject == null)
            {
                overlayObject = new GameObject("__GlobalFadeTitleOverlay");
                overlayObject.transform.SetParent(_fadeCanvas.transform, worldPositionStays: false);
                var overlayRect = overlayObject.AddComponent<RectTransform>();
                overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
                overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
                overlayRect.pivot = new Vector2(0.5f, 0.5f);
                overlayRect.anchoredPosition = Vector2.zero;
                overlayRect.sizeDelta = new Vector2(1280f, 220f);
                _transitionTitleOverlay = overlayObject.AddComponent<CanvasGroup>();
            }
            else if (overlayObject.transform.parent != _fadeCanvas.transform)
            {
                overlayObject.transform.SetParent(_fadeCanvas.transform, worldPositionStays: false);
            }

            if (_transitionTitleOverlay == null)
            {
                return;
            }

            if (_transitionTitleText == null)
            {
                var titleObject = new GameObject("Title");
                titleObject.transform.SetParent(_transitionTitleOverlay.transform, worldPositionStays: false);
                var titleRect = titleObject.AddComponent<RectTransform>();
                titleRect.anchorMin = Vector2.zero;
                titleRect.anchorMax = Vector2.one;
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;

                var titleText = titleObject.AddComponent<Text>();
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.resizeTextForBestFit = true;
                titleText.resizeTextMinSize = 26;
                titleText.resizeTextMaxSize = 62;
                titleText.fontStyle = FontStyle.Bold;
                titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                titleText.verticalOverflow = VerticalWrapMode.Truncate;
                titleText.color = new Color(0.94f, 0.98f, 1f, 1f);
                titleText.text = string.Empty;
                titleText.font = ResolveBuiltInUiFont();
                _transitionTitleText = titleText;
                titleObject.transform.SetAsLastSibling();
            }

            _transitionTitleOverlay.alpha = 0f;
            _transitionTitleOverlay.blocksRaycasts = false;
            _transitionTitleOverlay.interactable = false;
            _transitionTitleOverlay.gameObject.SetActive(false);
        }

        private void HideTransitionTitleImmediate()
        {
            _transitionTitleVisible = false;
            if (_transitionTitleText != null)
            {
                _transitionTitleText.text = string.Empty;
            }

            if (_transitionTitleOverlay == null)
            {
                return;
            }

            _transitionTitleOverlay.alpha = 0f;
            _transitionTitleOverlay.blocksRaycasts = false;
            _transitionTitleOverlay.interactable = false;
            _transitionTitleOverlay.gameObject.SetActive(false);
        }

        private void SyncTransitionTitleAlpha(float overlayAlpha)
        {
            if (!_transitionTitleVisible || _transitionTitleOverlay == null)
            {
                return;
            }

            var clampedAlpha = Mathf.Clamp01(overlayAlpha);
            _transitionTitleOverlay.alpha = clampedAlpha;
            if (Mathf.Approximately(clampedAlpha, 0f))
            {
                HideTransitionTitleImmediate();
            }
        }

        private void ClearQueuedTitleCard()
        {
            _nextTransitionHasTitleCard = false;
            _nextTransitionTitleCardLabel = string.Empty;
            _nextTransitionTitleCardHoldSeconds = _defaultTitleCardHoldSeconds;
        }

        private static Font ResolveBuiltInUiFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}

