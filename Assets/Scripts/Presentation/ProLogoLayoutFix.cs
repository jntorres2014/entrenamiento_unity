using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Mantiene el logo aproximadamente cuadrado en la referencia 1080x1920 sin
    /// permitir que AspectRatioFitter reescriba los anchors del layout runtime.
    /// </summary>
    public sealed class ProLogoLayoutFix : MonoBehaviour
    {
        private Canvas _canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<ProLogoLayoutFix>() == null)
                {
                    canvas.gameObject.AddComponent<ProLogoLayoutFix>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            Canvas.willRenderCanvases += Apply;
        }

        private void Apply()
        {
            Fix("ProBrandLogo", 0.055f, 0.865f, 0.225f, 0.960f);
            Fix("ProgressLogo", 0.80f, 0.890f, 0.94f, 0.970f);
        }

        private void Fix(string name, float xMin, float yMin, float xMax, float yMax)
        {
            var go = FindDeep(name);
            if (go == null) return;
            var fitter = go.GetComponent<AspectRatioFitter>();
            if (fitter != null) fitter.enabled = false;
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private GameObject FindDeep(string objectName)
        {
            if (_canvas == null) return null;
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true))
                if (t.name == objectName) return t.gameObject;
            return null;
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= Apply;
        }
    }
}
