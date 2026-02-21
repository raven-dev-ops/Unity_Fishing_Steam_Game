using NUnit.Framework;
using RavenDevOps.Fishing.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class GlobalUiAccessibilityServiceTests
    {
        [Test]
        public void RegisterCanvas_AppliesScaleToOverlayCanvas()
        {
            var serviceGo = new GameObject("GlobalUiAccessibilityServiceTests");
            var service = serviceGo.AddComponent<GlobalUiAccessibilityService>();

            var canvasGo = new GameObject("CanvasOverlay");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.transform.localScale = Vector3.one * 2f;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 2f;

            service.RegisterCanvas(canvas);

            Assert.That(canvasGo.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(scaler.scaleFactor, Is.EqualTo(1f));

            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(serviceGo);
        }

        [Test]
        public void RegisterCanvas_IgnoresWorldSpaceCanvas()
        {
            var serviceGo = new GameObject("GlobalUiAccessibilityServiceTests");
            var service = serviceGo.AddComponent<GlobalUiAccessibilityService>();

            var canvasGo = new GameObject("CanvasWorld");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.transform.localScale = Vector3.one * 2f;

            service.RegisterCanvas(canvas);

            Assert.That(canvasGo.transform.localScale, Is.EqualTo(Vector3.one * 2f));

            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(serviceGo);
        }
    }
}
