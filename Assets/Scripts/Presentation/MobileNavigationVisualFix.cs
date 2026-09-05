using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Última capa visual de navegación: un único Atrás contextual, dentro del
    /// safe area y alineado con los headers Deportivo Pro.
    /// </summary>
    public sealed class MobileNavigationVisualFix : MonoBehaviour
    {
        private static readonly string[] ContextBackButtons =
        {
            "ExerciseBackButton",
            "SoloSelectorBack",
            "SoloOptionsBack",
            "SoloCameraBack",
            "CameraBackButton",
            "ARBackButton",
            "ProgressBackButton",
            "ModernBackButton"
        };

        private Canvas _canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<MobileNavigationVisualFix>() == null)
                {
                    canvas.gameObject.AddComponent<MobileNavigationVisualFix>();
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
            if (_canvas == null || Screen.width <= 0 || Screen.height <= 0) return;

            bool exerciseSelector = IsActive("ExerciseSelectionPanel");
            bool soloSelector = IsActive("SoloExerciseSelection");
            bool soloOptions = IsActive("SoloOptionsPanel");
            bool progress = IsActive("TrainingProgressPanel");

            var modernBack = FindDeep("ModernBackButton");
            if ((exerciseSelector || soloSelector || progress) && modernBack != null && modernBack.activeSelf)
                modernBack.SetActive(false);

            ApplyCompactSafeBackButtons();

            if (exerciseSelector)
                ReflowSelectorHeader("ExerciseSelectionPanel", "Eyebrow", "Title", "Subtitle");
            if (soloSelector)
                ReflowSelectorHeader("SoloExerciseSelection", "Eyebrow", "Title", "Subtitle");
            if (soloOptions)
            {
                var root = FindDeep("SoloOptionsPanel");
                var eyebrow = FindChildText(root, "Eyebrow");
                if (eyebrow != null) SetRect(eyebrow.rectTransform, 0.195f, 0.915f, 0.86f, 0.95f);
            }
        }

        private void ApplyCompactSafeBackButtons()
        {
            Rect safe = Screen.safeArea;
            float safeLeft = safe.xMin / Screen.width;
            float safeTop = safe.yMax / Screen.height;
            bool landscape = Screen.width > Screen.height;

            float width = landscape ? 0.14f : 0.18f;
            float height = landscape ? 0.09f : 0.056f;
            float xMin = Mathf.Clamp01(safeLeft + 0.025f);
            float xMax = Mathf.Clamp01(xMin + width);
            float yMax = Mathf.Clamp01(safeTop - 0.016f);
            float yMin = Mathf.Clamp01(yMax - height);

            foreach (string name in ContextBackButtons)
            {
                var go = FindDeep(name);
                if (go == null || !go.activeInHierarchy) continue;

                var rect = go.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(xMin, yMin);
                    rect.anchorMax = new Vector2(xMax, yMax);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.localScale = Vector3.one;
                }

                var label = go.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "←  ATRÁS";
                    label.fontSizeMin = 12.5f;
                    label.fontSizeMax = 16.5f;
                    label.alignment = TextAlignmentOptions.Center;
                }

                go.transform.SetAsLastSibling();
            }
        }

        private void ReflowSelectorHeader(string rootName, string eyebrowName, string titleName, string subtitleName)
        {
            var root = FindDeep(rootName);
            if (root == null) return;

            var eyebrow = FindChildText(root, eyebrowName);
            if (eyebrow != null) SetRect(eyebrow.rectTransform, 0.195f, 0.915f, 0.84f, 0.95f);

            var title = FindChildText(root, titleName);
            if (title != null) SetRect(title.rectTransform, 0.055f, 0.835f, 0.94f, 0.895f);

            var subtitle = FindChildText(root, subtitleName);
            if (subtitle != null) SetRect(subtitle.rectTransform, 0.055f, 0.785f, 0.94f, 0.83f);
        }

        private bool IsActive(string name)
        {
            var go = FindDeep(name);
            return go != null && go.activeInHierarchy;
        }

        private GameObject FindDeep(string objectName)
        {
            if (_canvas == null) return null;
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true))
                if (t.name == objectName) return t.gameObject;
            return null;
        }

        private static TMP_Text FindChildText(GameObject root, string objectName)
        {
            if (root == null) return null;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                if (text.name == objectName) return text;
            return null;
        }

        private static void SetRect(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= Apply;
        }
    }
}
