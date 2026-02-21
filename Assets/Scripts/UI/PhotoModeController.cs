using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        private static MethodInfo _imageConversionEncodeToPngMethod;
        private static bool _imageConversionLookupCompleted;
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

            if (UnityEngine.Input.GetKeyDown(_togglePhotoModeKey))
            {
                SetPhotoMode(!_photoModeActive);
            }

            if (!_photoModeActive)
            {
                return;
            }

            HandleFreeCameraMovement();
            if (UnityEngine.Input.GetKeyDown(_captureScreenshotKey))
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

                if (_targetCamera == null)
                {
                    Debug.LogWarning("PhotoModeController: capture skipped because target camera is missing.");
                    return string.Empty;
                }

                var superSize = Mathf.Max(1, _screenshotSuperSize);
                var width = Mathf.Max(1, Screen.width * superSize);
                var height = Mathf.Max(1, Screen.height * superSize);

                var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                var previousRenderTexture = RenderTexture.active;
                var previousCameraTarget = _targetCamera.targetTexture;

                try
                {
                    _targetCamera.targetTexture = renderTexture;
                    _targetCamera.Render();
                    RenderTexture.active = renderTexture;

                    var screenshotTexture = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false);
                    screenshotTexture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    screenshotTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

                    if (!TryEncodePng(screenshotTexture, out var bytes))
                    {
                        Destroy(screenshotTexture);
                        Debug.LogError("PhotoModeController: PNG encode failed because ImageConversion module is unavailable.");
                        return string.Empty;
                    }

                    Destroy(screenshotTexture);

                    File.WriteAllBytes(path, bytes);
                }
                finally
                {
                    _targetCamera.targetTexture = previousCameraTarget;
                    RenderTexture.active = previousRenderTexture;
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

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

            var speed = _moveSpeed * (UnityEngine.Input.GetKey(KeyCode.LeftShift) ? _boostMultiplier : 1f);
            var move = Vector3.zero;

            if (UnityEngine.Input.GetKey(KeyCode.W))
            {
                move += _targetCamera.transform.forward;
            }

            if (UnityEngine.Input.GetKey(KeyCode.S))
            {
                move -= _targetCamera.transform.forward;
            }

            if (UnityEngine.Input.GetKey(KeyCode.D))
            {
                move += _targetCamera.transform.right;
            }

            if (UnityEngine.Input.GetKey(KeyCode.A))
            {
                move -= _targetCamera.transform.right;
            }

            if (UnityEngine.Input.GetKey(KeyCode.E))
            {
                move += _targetCamera.transform.up;
            }

            if (UnityEngine.Input.GetKey(KeyCode.Q))
            {
                move -= _targetCamera.transform.up;
            }

            _targetCamera.transform.position += move * (speed * Time.unscaledDeltaTime);
            if (UnityEngine.Input.GetMouseButton(1))
            {
                var mouseX = UnityEngine.Input.GetAxisRaw("Mouse X");
                var mouseY = UnityEngine.Input.GetAxisRaw("Mouse Y");
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
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

        private static bool TryEncodePng(Texture2D texture, out byte[] bytes)
        {
            bytes = null;
            if (texture == null)
            {
                return false;
            }

            if (!_imageConversionLookupCompleted)
            {
                _imageConversionLookupCompleted = true;
                var imageConversionType = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule", throwOnError: false);
                if (imageConversionType != null)
                {
                    _imageConversionEncodeToPngMethod = imageConversionType.GetMethod(
                        "EncodeToPNG",
                        BindingFlags.Public | BindingFlags.Static,
                        binder: null,
                        types: new[] { typeof(Texture2D) },
                        modifiers: null);
                }
            }

            if (_imageConversionEncodeToPngMethod == null)
            {
                return false;
            }

            try
            {
                bytes = _imageConversionEncodeToPngMethod.Invoke(null, new object[] { texture }) as byte[];
                return bytes != null && bytes.Length > 0;
            }
            catch
            {
                bytes = null;
                return false;
            }
        }
    }
}
