using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingBoatFloatMotion2D : MonoBehaviour
    {
        [SerializeField] private float _verticalAmplitude = 0.14f;
        [SerializeField] private float _verticalFrequency = 0.85f;
        [SerializeField] private float _rotationAmplitudeDegrees = 2.4f;
        [SerializeField] private float _rotationFrequency = 0.65f;
        [SerializeField] private FishingSceneWeatherController _weatherController;
        [SerializeField] private float _surfaceWaveToVerticalAmplitude = 0.05f;
        [SerializeField] private float _fogWaveToVerticalAmplitude = 0.03f;
        [SerializeField] private float _surfaceWaveToRotationAmplitude = 0.42f;
        [SerializeField] private float _fogWaveToRotationAmplitude = 0.2f;
        [SerializeField] private float _maxVerticalAmplitude = 1.25f;
        [SerializeField] private float _maxRotationAmplitudeDegrees = 10f;
        [SerializeField] private float _weatherFrequencyBoost = 0.35f;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private float _reducedMotionScale = 0.45f;

        private float _baseY;
        private float _baseRotationZ;
        private bool _initialized;
        private float _phase;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            ResolveWeatherController();
            CaptureBaseline();
        }

        private void OnEnable()
        {
            ResolveWeatherController();
            CaptureBaseline();
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                CaptureBaseline();
            }

            var scale = _settingsService != null && _settingsService.ReducedMotion
                ? Mathf.Clamp(_reducedMotionScale, 0f, 1f)
                : 1f;
            var weatherMotionRatio = ResolveWeatherMotionRatio();
            var resolvedVerticalAmplitude = Mathf.Min(
                Mathf.Max(0f, _maxVerticalAmplitude),
                Mathf.Max(0f, _verticalAmplitude)
                    + ResolveWeatherVerticalAmplitude());
            var resolvedRotationAmplitude = Mathf.Min(
                Mathf.Max(0f, _maxRotationAmplitudeDegrees),
                Mathf.Max(0f, _rotationAmplitudeDegrees)
                    + ResolveWeatherRotationAmplitudeDegrees());
            var frequencyBoost = 1f + (weatherMotionRatio * Mathf.Max(0f, _weatherFrequencyBoost));
            var time = Time.unscaledTime + _phase;

            var position = transform.position;
            position.y = _baseY + (Mathf.Sin(time * Mathf.Max(0.01f, _verticalFrequency) * frequencyBoost) * resolvedVerticalAmplitude * scale);
            transform.position = position;

            var rotation = transform.rotation.eulerAngles;
            var normalizedBaseZ = NormalizeSignedAngle(_baseRotationZ);
            var offsetZ = Mathf.Sin(time * Mathf.Max(0.01f, _rotationFrequency) * frequencyBoost) * resolvedRotationAmplitude * scale;
            rotation.z = normalizedBaseZ + offsetZ;
            transform.rotation = Quaternion.Euler(rotation);
        }

        private void CaptureBaseline()
        {
            _baseY = transform.position.y;
            _baseRotationZ = NormalizeSignedAngle(transform.rotation.eulerAngles.z);
            _phase = (transform.position.x * 0.37f) + (transform.position.y * 0.11f);
            _initialized = true;
        }

        private static float NormalizeSignedAngle(float degrees)
        {
            var wrapped = Mathf.Repeat(degrees + 180f, 360f) - 180f;
            return wrapped;
        }

        private void ResolveWeatherController()
        {
            if (_weatherController != null)
            {
                return;
            }

            _weatherController = FindAnyObjectByType<FishingSceneWeatherController>(FindObjectsInactive.Exclude);
        }

        private float ResolveWeatherVerticalAmplitude()
        {
            ResolveWeatherController();
            if (_weatherController == null)
            {
                return 0f;
            }

            var surfaceWave = Mathf.Max(0f, _weatherController.CurrentSurfaceWaveVerticalMeters);
            var fogWave = Mathf.Max(0f, _weatherController.CurrentFogVerticalMeters);
            return (surfaceWave * Mathf.Max(0f, _surfaceWaveToVerticalAmplitude))
                + (fogWave * Mathf.Max(0f, _fogWaveToVerticalAmplitude));
        }

        private float ResolveWeatherRotationAmplitudeDegrees()
        {
            ResolveWeatherController();
            if (_weatherController == null)
            {
                return 0f;
            }

            var surfaceWave = Mathf.Max(0f, _weatherController.CurrentSurfaceWaveVerticalMeters);
            var fogWave = Mathf.Max(0f, _weatherController.CurrentFogVerticalMeters);
            return (surfaceWave * Mathf.Max(0f, _surfaceWaveToRotationAmplitude))
                + (fogWave * Mathf.Max(0f, _fogWaveToRotationAmplitude));
        }

        private float ResolveWeatherMotionRatio()
        {
            ResolveWeatherController();
            if (_weatherController == null)
            {
                return 0f;
            }

            var surfaceRatio = Mathf.Clamp01(_weatherController.CurrentSurfaceWaveVerticalMeters / 15f);
            var fogRatio = Mathf.Clamp01(_weatherController.CurrentFogVerticalMeters / 5f);
            return Mathf.Clamp01(Mathf.Max(surfaceRatio, fogRatio));
        }
    }
}
