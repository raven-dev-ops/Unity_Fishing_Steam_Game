using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class WaveAnimator : MonoBehaviour
    {
        [SerializeField] private Transform _waveLayerA;
        [SerializeField] private Transform _waveLayerB;
        [SerializeField] private float _waveSpeedA = 0.3f;
        [SerializeField] private float _waveSpeedB = 0.6f;
        [SerializeField] private FishingSceneWeatherController _weatherController;
        [SerializeField] private float _surfaceWaveVerticalToWorldScale = 0.08f;
        [SerializeField] private float _surfaceWaveVerticalFrequency = 0.28f;
        [SerializeField] private float _secondaryVerticalFrequencyMultiplier = 1.17f;
        [SerializeField] private float _secondaryVerticalAmplitudeScale = 0.72f;
        [SerializeField] private float _secondaryVerticalPhaseOffset = 1.35f;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private float _reducedMotionSpeedScale = 0.4f;
        [SerializeField] private bool _lockDuplicateBackdropLayers = true;

        private bool _duplicateBackdropLayersLocked;
        private bool _hasBaseWaveAY;
        private bool _hasBaseWaveBY;
        private float _baseWaveAY;
        private float _baseWaveBY;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            ResolveWeatherController();
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public void SetWaveSpeeds(float waveSpeedA, float waveSpeedB)
        {
            _waveSpeedA = waveSpeedA;
            _waveSpeedB = waveSpeedB;
        }

        public void ConfigureLayers(Transform waveLayerA, Transform waveLayerB)
        {
            _waveLayerA = waveLayerA;
            _waveLayerB = waveLayerB;
            _duplicateBackdropLayersLocked = _lockDuplicateBackdropLayers
                && AreLayersUsingSameSprite(_waveLayerA, _waveLayerB);
            _hasBaseWaveAY = false;
            _hasBaseWaveBY = false;
            CaptureBaseY(_waveLayerA, ref _hasBaseWaveAY, ref _baseWaveAY);
            CaptureBaseY(_waveLayerB, ref _hasBaseWaveBY, ref _baseWaveBY);
        }

        private void Update()
        {
            var speedScale = _settingsService != null && _settingsService.ReducedMotion
                ? Mathf.Clamp(_reducedMotionSpeedScale, 0f, 1f)
                : 1f;

            AnimateLayerX(_waveLayerA, _waveSpeedA * speedScale);
            if (_duplicateBackdropLayersLocked && _waveLayerA != null && _waveLayerB != null)
            {
                var layerBPosition = _waveLayerB.localPosition;
                layerBPosition.x = _waveLayerA.localPosition.x;
                _waveLayerB.localPosition = layerBPosition;
            }
            else
            {
                AnimateLayerX(_waveLayerB, _waveSpeedB * speedScale);
            }

            var weatherWaveMeters = ResolveWeatherSurfaceWaveMeters();
            var baseAmplitude = Mathf.Max(0f, weatherWaveMeters) * Mathf.Max(0f, _surfaceWaveVerticalToWorldScale) * speedScale;
            ApplyVerticalWave(
                _waveLayerA,
                ref _hasBaseWaveAY,
                ref _baseWaveAY,
                baseAmplitude,
                Mathf.Max(0.01f, _surfaceWaveVerticalFrequency),
                phaseOffset: 0f);
            ApplyVerticalWave(
                _waveLayerB,
                ref _hasBaseWaveBY,
                ref _baseWaveBY,
                baseAmplitude * Mathf.Max(0f, _secondaryVerticalAmplitudeScale),
                Mathf.Max(0.01f, _surfaceWaveVerticalFrequency * Mathf.Max(0.1f, _secondaryVerticalFrequencyMultiplier)),
                phaseOffset: _secondaryVerticalPhaseOffset);
        }

        private static void AnimateLayerX(Transform layer, float speed)
        {
            if (layer == null)
            {
                return;
            }

            var p = layer.localPosition;
            p.x += speed * Time.deltaTime;
            if (p.x > 1000f)
            {
                p.x = 0f;
            }

            layer.localPosition = p;
        }

        private static void CaptureBaseY(Transform layer, ref bool hasBaseY, ref float baseY)
        {
            if (layer == null)
            {
                hasBaseY = false;
                baseY = 0f;
                return;
            }

            baseY = layer.localPosition.y;
            hasBaseY = true;
        }

        private static void ApplyVerticalWave(
            Transform layer,
            ref bool hasBaseY,
            ref float baseY,
            float amplitude,
            float frequency,
            float phaseOffset)
        {
            if (layer == null)
            {
                hasBaseY = false;
                baseY = 0f;
                return;
            }

            if (!hasBaseY)
            {
                baseY = layer.localPosition.y;
                hasBaseY = true;
            }

            var p = layer.localPosition;
            p.y = baseY + (Mathf.Sin((Time.unscaledTime * frequency) + phaseOffset) * amplitude);
            layer.localPosition = p;
        }

        private float ResolveWeatherSurfaceWaveMeters()
        {
            ResolveWeatherController();
            return _weatherController != null
                ? Mathf.Max(0f, _weatherController.CurrentSurfaceWaveVerticalMeters)
                : 0f;
        }

        private void ResolveWeatherController()
        {
            if (_weatherController != null)
            {
                return;
            }

            _weatherController = FindAnyObjectByType<FishingSceneWeatherController>(FindObjectsInactive.Exclude);
        }

        private static bool AreLayersUsingSameSprite(Transform layerA, Transform layerB)
        {
            if (layerA == null || layerB == null)
            {
                return false;
            }

            var rendererA = layerA.GetComponent<SpriteRenderer>();
            var rendererB = layerB.GetComponent<SpriteRenderer>();
            if (rendererA == null || rendererB == null || rendererA.sprite == null)
            {
                return false;
            }

            return rendererA.sprite == rendererB.sprite;
        }
    }
}
