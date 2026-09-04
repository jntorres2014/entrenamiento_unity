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
            // Bootstrap agrega sus listeners en Start. Esperamos para que nuestro
            // selector se ejecute después de ChooseRole(Host).
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

            var eyebrow = CreateText(_selectorRoot.transform, "Eyebrow", "ENTRENADOR  /  EJERCICIOS", 18f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(eyebrow.rectTransform, 0.055f, 0.93f, 0.78f, 0.97f);
            eyebrow.color = UiTheme.Accent;
            eyebrow.characterSpacing = 2f;

            var title = CreateText(_selectorRoot.transform, "Title", "Elegí tu entrenamiento", 43f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.055f, 0.855f, 0.94f, 0.925f);

            var subtitle = CreateText(_selectorRoot.transform, "Subtitle", "Cada preset configura automáticamente el comportamiento de los pods.", 19f, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(subtitle.rectTransform, 0.055f, 0.805f, 0.94f, 0.855f);
            subtitle.color = UiTheme.TextSecondary;

            CreateExerciseCard(ExerciseMode.Reaction, "01", "REACCIÓN", "Un pod verde al azar. Tocá y buscá el siguiente.", UiTheme.Positive, 0.055f, 0.585f, 0.485f, 0.775f);
            CreateExerciseCard(ExerciseMode.AllSame, "02", "TODOS IGUALES", "Todos azules al mismo tiempo. Apagalos lo más rápido posible.", UiTheme.Info, 0.515f, 0.585f, 0.945f, 0.775f);
            CreateExerciseCard(ExerciseMode.Colors, "03", "COLORES", "Rojo, verde, azul y amarillo. Tocá solamente el color indicado.", UiTheme.Accent, 0.055f, 0.365f, 0.485f, 0.555f);
            CreateExerciseCard(ExerciseMode.Decision, "04", "DECISIÓN", "Cada color representa una dirección de movimiento.", UiTheme.AccentLime, 0.515f, 0.365f, 0.945f, 0.555f);
            CreateExerciseCard(ExerciseMode.CognitiveFake, "05", "FINTA COGNITIVA", "El estímulo cambia mientras te acercás. Reaccioná al nuevo color.", new Color32(0xC0, 0x75, 0xFF, 0xFF), 0.055f, 0.145f, 0.485f, 0.335f);
            CreateExerciseCard(ExerciseMode.Football, "06", "FÚTBOL", "Verde derecho · azul izquierdo · rojo no tocar.", new Color32(0x4C, 0xC9, 0x9A, 0xFF), 0.515f, 0.145f, 0.945f, 0.335f);

            var back = CreateButton(_selectorRoot.transform, "ExerciseBackButton", "←  VOLVER", UiTheme.CardElevated);
            SetRect(back.GetComponent<RectTransform>(), 0.055f, 0.045f, 0.34f, 0.105f);
            back.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

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
            SetRect(line.rectTransform, 0.055f, 0.86f, 0.42f, 0.89f);

            var num = CreateText(button.transform, "Number", number, 15f, FontStyles.Bold, TextAlignmentOptions.TopRight);
            SetRect(num.rectTransform, 0.76f, 0.70f, 0.92f, 0.91f);
            num.color = UiTheme.TextMuted;

            var heading = CreateText(button.transform, "Heading", title, 25f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(heading.rectTransform, 0.055f, 0.48f, 0.90f, 0.73f);
            heading.color = UiTheme.TextPrimary;

            var detail = CreateText(button.transform, "Detail", description, 16.5f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetRect(detail.rectTransform, 0.055f, 0.10f, 0.91f, 0.48f);
            detail.color = UiTheme.TextSecondary;
            detail.enableWordWrapping = true;

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
