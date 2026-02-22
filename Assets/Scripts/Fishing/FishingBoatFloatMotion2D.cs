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
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private float _reducedMotionScale = 0.45f;

        private float _baseY;
        private float _baseRotationZ;
        private bool _initialized;
        private float _phase;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            CaptureBaseline();
        }

        private void OnEnable()
        {
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
            var time = Time.unscaledTime + _phase;

            var position = transform.position;
            position.y = _baseY + (Mathf.Sin(time * Mathf.Max(0.01f, _verticalFrequency)) * _verticalAmplitude * scale);
            transform.position = position;

            var rotation = transform.rotation.eulerAngles;
            var normalizedBaseZ = NormalizeSignedAngle(_baseRotationZ);
            var offsetZ = Mathf.Sin(time * Mathf.Max(0.01f, _rotationFrequency)) * _rotationAmplitudeDegrees * scale;
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
    }
}
