using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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

            if (GetKeyDown(_togglePhotoModeKey))
            {
                SetPhotoMode(!_photoModeActive);
            }

            if (!_photoModeActive)
            {
                return;
            }

            HandleFreeCameraMovement();
            if (GetKeyDown(_captureScreenshotKey))
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

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var mouse = Mouse.current;
            var speed = _moveSpeed * (keyboard.leftShiftKey.isPressed ? _boostMultiplier : 1f);
            var move = Vector3.zero;

            if (keyboard.wKey.isPressed)
            {
                move += _targetCamera.transform.forward;
            }

            if (keyboard.sKey.isPressed)
            {
                move -= _targetCamera.transform.forward;
            }

            if (keyboard.dKey.isPressed)
            {
                move += _targetCamera.transform.right;
            }

            if (keyboard.aKey.isPressed)
            {
                move -= _targetCamera.transform.right;
            }

            if (keyboard.eKey.isPressed)
            {
                move += _targetCamera.transform.up;
            }

            if (keyboard.qKey.isPressed)
            {
                move -= _targetCamera.transform.up;
            }

            _targetCamera.transform.position += move * (speed * Time.unscaledDeltaTime);
            if (mouse != null && mouse.rightButton.isPressed)
            {
                var mouseDelta = mouse.delta.ReadValue();
                var mouseX = mouseDelta.x;
                var mouseY = mouseDelta.y;
                var yaw = mouseX * _lookSensitivity * Time.unscaledDeltaTime;
                var pitch = -mouseY * _lookSensitivity * Time.unscaledDeltaTime;
                _targetCamera.transform.Rotate(Vector3.up, yaw, Space.World);
                _targetCamera.transform.Rotate(Vector3.right, pitch, Space.Self);
            }
        }

        private static bool GetKeyDown(KeyCode keyCode)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return TryResolveKeyControl(keyboard, keyCode, out var keyControl) && keyControl.wasPressedThisFrame;
        }

        private static bool TryResolveKeyControl(Keyboard keyboard, KeyCode keyCode, out KeyControl keyControl)
        {
            keyControl = null;
            if (keyboard == null)
            {
                return false;
            }

            switch (keyCode)
            {
                case KeyCode.F9:
                    keyControl = keyboard.f9Key;
                    return true;
                case KeyCode.F12:
                    keyControl = keyboard.f12Key;
                    return true;
                default:
                    return false;
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
