using System.Collections;
using Entrenamiento.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Selector Deportivo Pro de los seis presets. Reutiliza el flujo de host
    /// existente y solo cambia presentación / elección del ExerciseMode.
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

            var glow = CreateImage(_selectorRoot.transform, "SelectorGlow", new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.055f));
            SetRect(glow.rectTransform, 0.64f, 0.66f, 1.0f, 1.0f);

            var back = CreateButton(_selectorRoot.transform, "ExerciseBackButton", "←", UiTheme.Surface);
            SetRect(back.GetComponent<RectTransform>(), 0.055f, 0.905f, 0.165f, 0.962f);
            var backLabel = back.GetComponentInChildren<TMP_Text>(true);
            if (backLabel != null)
            {
                backLabel.fontSizeMax = 24f;
                backLabel.fontSizeMin = 18f;
            }
            back.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

            var eyebrow = CreateText(_selectorRoot.transform, "Eyebrow", "CON PODS  /  EJERCICIOS", 14.5f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(eyebrow.rectTransform, 0.195f, 0.915f, 0.80f, 0.95f);
            eyebrow.color = UiTheme.Accent;
            eyebrow.characterSpacing = 1.8f;

            var title = CreateText(_selectorRoot.transform, "Title", "Elegí tu entrenamiento", 36f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.055f, 0.835f, 0.94f, 0.895f);

            var subtitle = CreateText(_selectorRoot.transform, "Subtitle", "Desafiá tus reflejos, velocidad y toma de decisiones.", 17f, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(subtitle.rectTransform, 0.055f, 0.785f, 0.94f, 0.83f);
            subtitle.color = UiTheme.TextSecondary;

            CreateExerciseRow(ExerciseMode.Reaction, "01", "REACCIÓN", "Un pod verde al azar. Tocá y buscá el siguiente.", UiTheme.Accent, 0.665f, 0.765f);
            CreateExerciseRow(ExerciseMode.AllSame, "02", "TODOS IGUALES", "Todos azules a la vez. Apagalos lo más rápido posible.", UiTheme.Info, 0.550f, 0.650f);
            CreateExerciseRow(ExerciseMode.Colors, "03", "COLORES", "Identificá el color indicado y tocá solamente ese pod.", new Color32(0xFF, 0x95, 0x35, 0xFF), 0.435f, 0.535f);
            CreateExerciseRow(ExerciseMode.Decision, "04", "DECISIÓN", "Cada color representa una dirección de movimiento.", new Color32(0xF3, 0xD3, 0x44, 0xFF), 0.320f, 0.420f);
            CreateExerciseRow(ExerciseMode.CognitiveFake, "05", "FINTA COGNITIVA", "El estímulo cambia durante la aproximación. Corregí la decisión.", new Color32(0xB9, 0x67, 0xFF, 0xFF), 0.205f, 0.305f);
            CreateExerciseRow(ExerciseMode.Football, "06", "FÚTBOL", "Verde derecho · azul izquierdo · rojo no tocar.", new Color32(0x38, 0xD6, 0xA1, 0xFF), 0.090f, 0.190f);

            _selectorRoot.SetActive(false);
        }

        private void CreateExerciseRow(ExerciseMode mode, string number, string title, string description, Color accent, float yMin, float yMax)
        {
            var button = CreateButton(_selectorRoot.transform, "Exercise_" + mode, string.Empty, UiTheme.CardElevated);
            SetRect(button.GetComponent<RectTransform>(), 0.055f, yMin, 0.945f, yMax);

            var image = button.GetComponent<Image>();
            image.color = UiTheme.CardElevated;

            var rail = CreateImage(button.transform, "AccentRail", accent);
            SetRect(rail.rectTransform, 0.018f, 0.17f, 0.028f, 0.83f);

            var badge = CreateImage(button.transform, "NumberBadge", new Color(accent.r, accent.g, accent.b, 0.18f));
            SetRect(badge.rectTransform, 0.055f, 0.22f, 0.155f, 0.78f);

            var num = CreateText(badge.transform, "Number", number, 15f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(num.rectTransform);
            num.color = accent;

            var heading = CreateText(button.transform, "Heading", title, 21f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(heading.rectTransform, 0.19f, 0.48f, 0.76f, 0.80f);
            heading.color = UiTheme.TextPrimary;

            var detail = CreateText(button.transform, "Detail", description, 14.5f, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(detail.rectTransform, 0.19f, 0.14f, 0.85f, 0.49f);
            detail.color = UiTheme.TextSecondary;
            detail.enableWordWrapping = true;
            detail.fontSizeMin = 11.5f;
            detail.fontSizeMax = 14.5f;

            var arrow = CreateText(button.transform, "Arrow", "→", 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(arrow.rectTransform, 0.86f, 0.25f, 0.95f, 0.75f);
            arrow.color = accent;

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

            if (_configPanel != null) _configPanel.SetActive(true);

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
                if (label != null) label.text = ExerciseSelection.Name(ExerciseSelection.Current);
                MakeReadOnlyVisible(_modeButton);
            }

            if (_colorButton != null)
            {
                var label = _colorButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = ExerciseSelection.Rule(ExerciseSelection.Current);
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
            ApplyRounded(image);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.04f, 1.04f, 1.04f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = UiTheme.Disabled;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            go.AddComponent<ButtonPressScale>();

            if (!string.IsNullOrEmpty(text))
            {
                var label = CreateText(go.transform, "Label", text, 19f, FontStyles.Bold, TextAlignmentOptions.Center);
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(10f, 4f);
                label.rectTransform.offsetMax = new Vector2(-10f, -4f);
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
            ApplyRounded(image);
            return image;
        }

        private void ApplyRounded(Image image)
        {
            if (image == null || _roundedSprite == null) return;
            image.sprite = _roundedSprite;
            image.type = Image.Type.Sliced;
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
            label.fontSizeMin = Mathf.Max(10f, size * 0.58f);
            label.fontSizeMax = size;
            return label;
        }

        private GameObject FindDeep(string objectName)
        {
            if (_canvas == null) return null;
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true))
                if (t.name == objectName) return t.gameObject;
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
            rect.localScale = Vector3.one;
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
