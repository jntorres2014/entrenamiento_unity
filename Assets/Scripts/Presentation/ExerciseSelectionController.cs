using System.Collections;
using Entrenamiento.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Selector visual de presets. Se abre al elegir Entrenador y deja la
    /// configuración existente como segunda etapa del flujo.
    /// </summary>
    public sealed class ExerciseSelectionController : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _selectorRoot;
        private GameObject _configPanel;
        private Button _hostButton;
        private Button _modeButton;
        private Button _colorButton;
        private Sprite _roundedSprite;
        private bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<ExerciseSelectionController>() == null)
                {
                    canvas.gameObject.AddComponent<ExerciseSelectionController>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            StartCoroutine(SetupWhenReady());
        }

        private IEnumerator SetupWhenReady()
        {
            yield return null;
            yield return null;

            _hostButton = FindButton("HostRoleButton");
            _configPanel = FindDeep("HostConfigPanel");
            _modeButton = FindButton("ModeButton");
            _colorButton = FindButton("ColorModeButton");
            CaptureRoundedSprite();

            if (_hostButton == null || _configPanel == null) yield break;

            BuildSelector();
            _hostButton.onClick.AddListener(OpenSelectorNextFrame);
            _hooked = true;
        }

        private void OpenSelectorNextFrame()
        {
            StartCoroutine(OpenAfterRoleChanges());
        }

        private IEnumerator OpenAfterRoleChanges()
        {
            yield return null;
            ShowSelector();
        }

        private void BuildSelector()
        {
            if (_selectorRoot != null) return;

            _selectorRoot = new GameObject("ExerciseSelectionPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _selectorRoot.transform.SetParent(_canvas.transform, false);
            Stretch(_selectorRoot.GetComponent<RectTransform>());
            _selectorRoot.GetComponent<Image>().color = UiTheme.Background;

            var back = CreateButton(_selectorRoot.transform, "ExerciseBackButton", "←  ATRÁS", UiTheme.CardElevated);
            SetRect(back.GetComponent<RectTransform>(), 0.055f, 0.900f, 0.225f, 0.962f);
            var backLabel = back.GetComponentInChildren<TMP_Text>(true);
            if (backLabel != null)
            {
                backLabel.fontSizeMax = 18f;
                backLabel.fontSizeMin = 14f;
            }
            back.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

            var eyebrow = CreateText(_selectorRoot.transform, "Eyebrow", "CON PODS  /  EJERCICIOS", 16.5f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(eyebrow.rectTransform, 0.255f, 0.905f, 0.83f, 0.948f);
            eyebrow.color = UiTheme.Accent;
            eyebrow.characterSpacing = 1.5f;

            var title = CreateText(_selectorRoot.transform, "Title", "Elegí tu entrenamiento", 38f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.055f, 0.815f, 0.94f, 0.875f);

            var subtitle = CreateText(_selectorRoot.transform, "Subtitle", "Elegí una dinámica. La app configura automáticamente las reglas de los pods.", 18f, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(subtitle.rectTransform, 0.055f, 0.745f, 0.94f, 0.805f);
            subtitle.color = UiTheme.TextSecondary;

            CreateExerciseCard(ExerciseMode.Reaction, "01", "REACCIÓN", "Un pod verde al azar. Tocá y buscá el siguiente.", UiTheme.Positive, 0.055f, 0.555f, 0.485f, 0.710f);
            CreateExerciseCard(ExerciseMode.AllSame, "02", "TODOS IGUALES", "Todos azules al mismo tiempo. Apagalos lo más rápido posible.", UiTheme.Info, 0.515f, 0.555f, 0.945f, 0.710f);
            CreateExerciseCard(ExerciseMode.Colors, "03", "COLORES", "Rojo, verde, azul y amarillo. Tocá solamente el color indicado.", new Color32(0xFF, 0x9D, 0x38, 0xFF), 0.055f, 0.355f, 0.485f, 0.510f);
            CreateExerciseCard(ExerciseMode.Decision, "04", "DECISIÓN", "Cada color representa una dirección de movimiento.", UiTheme.Accent, 0.515f, 0.355f, 0.945f, 0.510f);
            CreateExerciseCard(ExerciseMode.CognitiveFake, "05", "FINTA COGNITIVA", "El estímulo cambia mientras te acercás. Reaccioná al nuevo color.", new Color32(0xC0, 0x75, 0xFF, 0xFF), 0.055f, 0.155f, 0.485f, 0.310f);
            CreateExerciseCard(ExerciseMode.Football, "06", "FÚTBOL", "Verde derecho · azul izquierdo · rojo no tocar.", new Color32(0x4C, 0xC9, 0x9A, 0xFF), 0.515f, 0.155f, 0.945f, 0.310f);

            _selectorRoot.SetActive(false);
        }

        private void CreateExerciseCard(ExerciseMode mode, string number, string title, string description, Color accent,
            float xMin, float yMin, float xMax, float yMax)
        {
            var button = CreateButton(_selectorRoot.transform, "Exercise_" + mode, string.Empty, UiTheme.CardElevated);
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);

            var image = button.GetComponent<Image>();
            image.color = UiTheme.CardElevated;

            var line = CreateImage(button.transform, "Accent", accent);
            SetRect(line.rectTransform, 0.055f, 0.855f, 0.38f, 0.885f);

            var num = CreateText(button.transform, "Number", number, 14f, FontStyles.Bold, TextAlignmentOptions.TopRight);
            SetRect(num.rectTransform, 0.76f, 0.72f, 0.92f, 0.91f);
            num.color = UiTheme.TextMuted;

            var heading = CreateText(button.transform, "Heading", title, 23f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(heading.rectTransform, 0.055f, 0.50f, 0.92f, 0.72f);
            heading.color = UiTheme.TextPrimary;

            var detail = CreateText(button.transform, "Detail", description, 16.5f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetRect(detail.rectTransform, 0.055f, 0.12f, 0.92f, 0.47f);
            detail.color = UiTheme.TextSecondary;
            detail.enableWordWrapping = true;
            detail.fontSizeMin = 13f;
            detail.fontSizeMax = 17f;

            button.onClick.AddListener(() => SelectExercise(mode));
        }

        private void ShowSelector()
        {
            if (_selectorRoot == null) return;
            _selectorRoot.SetActive(true);
            _selectorRoot.transform.SetAsLastSibling();

            var modernBack = FindDeep("ModernBackButton");
            if (modernBack != null) modernBack.SetActive(false);
        }

        private void SelectExercise(ExerciseMode mode)
        {
            ExerciseSelection.Current = mode;
            _selectorRoot.SetActive(false);

            if (_configPanel != null)
            {
                _configPanel.SetActive(true);
            }

            var modernBack = FindDeep("ModernBackButton");
            if (modernBack != null)
            {
                modernBack.SetActive(true);
                modernBack.transform.SetAsLastSibling();
            }

            ApplyPresetLabels();
        }

        private void LateUpdate()
        {
            if (!_hooked || _selectorRoot == null || _selectorRoot.activeSelf) return;
            if (_configPanel == null || !_configPanel.activeInHierarchy) return;
            ApplyPresetLabels();
        }

        private void ApplyPresetLabels()
        {
            if (_modeButton != null)
            {
                var label = _modeButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "EJERCICIO  •  " + ExerciseSelection.Name(ExerciseSelection.Current);
                }
                MakeReadOnlyVisible(_modeButton);
            }

            if (_colorButton != null)
            {
                var label = _colorButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = ExerciseSelection.Rule(ExerciseSelection.Current);
                }
                MakeReadOnlyVisible(_colorButton);
            }
        }

        private static void MakeReadOnlyVisible(Button button)
        {
            button.interactable = false;
            var colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
        }

        private void CaptureRoundedSprite()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                var image = button.GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    _roundedSprite = image.sprite;
                    return;
                }
            }
        }

        private Button CreateButton(Transform parent, string name, string text, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            if (_roundedSprite != null)
            {
                image.sprite = _roundedSprite;
                image.type = Image.Type.Sliced;
            }

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = UiTheme.Disabled;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            go.AddComponent<ButtonPressScale>();

            if (!string.IsNullOrEmpty(text))
            {
                var label = CreateText(go.transform, "Label", text, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(12f, 5f);
                label.rectTransform.offsetMax = new Vector2(-12f, -5f);
            }
            return button;
        }

        private Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (_roundedSprite != null)
            {
                image.sprite = _roundedSprite;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = UiTheme.TextPrimary;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(11f, size * 0.58f);
            label.fontSizeMax = size;
            return label;
        }

        private GameObject FindDeep(string objectName)
        {
            if (_canvas == null) return null;
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }
            return null;
        }

        private Button FindButton(string objectName)
        {
            var go = FindDeep(objectName);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static void SetRect(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
