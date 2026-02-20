using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RavenDevOps.Fishing.UI
{
    public sealed class PhotoModeController : MonoBehaviour
    {
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private KeyCode _togglePhotoModeKey = KeyCode.F9;
        [SerializeField] private KeyCode _captureScreenshotKey = KeyCode.F12;
        [SerializeField] private float _moveSpeed = 8f;
        [SerializeField] private float _boostMultiplier = 2.5f;
        [SerializeField] private float _lookSensitivity = 90f;
        [SerializeField] private int _screenshotSuperSize = 1;
        [SerializeField] private bool _hideHudInPhotoMode = true;
        [SerializeField] private bool _logCapturePaths = true;

        private readonly List<Canvas> _hiddenCanvases = new List<Canvas>();
        private bool _photoModeActive;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;

        public bool PhotoModeActive => _photoModeActive;
        public string ScreenshotDirectory => Path.Combine(Application.persistentDataPath, "Screenshots");

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = GetComponent<Camera>();
            }

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
                if (_targetCamera == null)
                {
                    return;
                }
            }

            if (Input.GetKeyDown(_togglePhotoModeKey))
            {
                SetPhotoMode(!_photoModeActive);
            }

            if (!_photoModeActive)
            {
                return;
            }

            HandleFreeCameraMovement();
            if (Input.GetKeyDown(_captureScreenshotKey))
            {
                CaptureScreenshot();
            }
        }

        public void SetPhotoMode(bool enabled)
        {
            if (_photoModeActive == enabled || _targetCamera == null)
            {
                return;
            }

            _photoModeActive = enabled;
            if (enabled)
            {
                _originalPosition = _targetCamera.transform.position;
                _originalRotation = _targetCamera.transform.rotation;
                if (_hideHudInPhotoMode)
                {
                    SetHudVisible(false);
                }

                if (_logCapturePaths)
                {
                    Debug.Log($"PhotoModeController: enabled. Capture key {_captureScreenshotKey}, output {ScreenshotDirectory}");
                }
            }
            else
            {
                _targetCamera.transform.SetPositionAndRotation(_originalPosition, _originalRotation);
                if (_hideHudInPhotoMode)
                {
                    SetHudVisible(true);
                }

                if (_logCapturePaths)
                {
                    Debug.Log("PhotoModeController: disabled.");
                }
            }
        }

        public string CaptureScreenshot()
        {
            try
            {
                Directory.CreateDirectory(ScreenshotDirectory);
                var filename = $"photo_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.png";
                var path = Path.Combine(ScreenshotDirectory, filename);
                ScreenCapture.CaptureScreenshot(path, Mathf.Max(1, _screenshotSuperSize));
                if (_logCapturePaths)
                {
                    Debug.Log($"PhotoModeController: screenshot captured -> {path}");
                }

                return path;
            }
            catch (Exception ex)
            {
                Debug.LogError($"PhotoModeController: failed to capture screenshot ({ex.Message}).");
                return string.Empty;
            }
        }

        private void HandleFreeCameraMovement()
        {
            if (_targetCamera == null)
            {
                return;
            }

            var speed = _moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? _boostMultiplier : 1f);
            var move = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
            {
                move += _targetCamera.transform.forward;
            }

            if (Input.GetKey(KeyCode.S))
            {
                move -= _targetCamera.transform.forward;
            }

            if (Input.GetKey(KeyCode.D))
            {
                move += _targetCamera.transform.right;
            }

            if (Input.GetKey(KeyCode.A))
            {
                move -= _targetCamera.transform.right;
            }

            if (Input.GetKey(KeyCode.E))
            {
                move += _targetCamera.transform.up;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                move -= _targetCamera.transform.up;
            }

            _targetCamera.transform.position += move * (speed * Time.unscaledDeltaTime);
            if (Input.GetMouseButton(1))
            {
                var mouseX = Input.GetAxisRaw("Mouse X");
                var mouseY = Input.GetAxisRaw("Mouse Y");
                var yaw = mouseX * _lookSensitivity * Time.unscaledDeltaTime;
                var pitch = -mouseY * _lookSensitivity * Time.unscaledDeltaTime;
                _targetCamera.transform.Rotate(Vector3.up, yaw, Space.World);
                _targetCamera.transform.Rotate(Vector3.right, pitch, Space.Self);
            }
        }

        private void SetHudVisible(bool visible)
        {
            if (visible)
            {
                for (var i = 0; i < _hiddenCanvases.Count; i++)
                {
                    var canvas = _hiddenCanvases[i];
                    if (canvas != null)
                    {
                        canvas.enabled = true;
                    }
                }

                _hiddenCanvases.Clear();
                return;
            }

            _hiddenCanvases.Clear();
            var canvases = FindObjectsOfType<Canvas>(true);
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (string.Equals(canvas.gameObject.name, "__GlobalFadeCanvas", StringComparison.Ordinal))
                {
                    continue;
                }

                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                canvas.enabled = false;
                _hiddenCanvases.Add(canvas);
            }
        }
    }
}
