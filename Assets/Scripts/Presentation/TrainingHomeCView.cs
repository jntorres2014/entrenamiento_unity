using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Home visual variante C: AR como experiencia principal y Entrenador/Estación
    /// como accesos secundarios. Reutiliza los botones funcionales existentes para
    /// no tocar la lógica de TrainingNearby ni del modo AR.
    /// </summary>
    public sealed class TrainingHomeCView : MonoBehaviour
    {
        private GameObject _rolePanel;
        private Button _hostButton;
        private Button _stationButton;
        private Button _arButton;
        private GameObject _visualRoot;
        private CanvasGroup _visualGroup;
        private Image _heroGlow;
        private Sprite _roundedSprite;
        private Vector3 _arBaseScale = Vector3.one;
        private float _pulseOffset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<TrainingHomeCView>() == null)
                {
                    canvas.gameObject.AddComponent<TrainingHomeCView>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _pulseOffset = Random.Range(0f, 3f);
            StartCoroutine(BuildWhenReady());
        }

        private IEnumerator BuildWhenReady()
        {
            // ARTrainingModeController crea su botón en Awake y TrainingModernUiController
            // termina el estilo general un frame después. Esperamos a ambos.
            for (int i = 0; i < 30; i++)
            {
                _rolePanel = FindDeep("RolePanel");
                _hostButton = FindButton("HostRoleButton");
                _stationButton = FindButton("StationRoleButton");
                _arButton = FindButton("ARTrainingButton");

                if (_rolePanel != null && _hostButton != null && _stationButton != null && _arButton != null)
                {
                    break;
                }

                yield return null;
            }

            if (_rolePanel == null || _hostButton == null || _stationButton == null || _arButton == null)
            {
                Debug.LogWarning("[Home C] No se encontraron todos los controles necesarios para construir la portada moderna.");
                yield break;
            }

            // Un frame extra deja que el estilizador general termine antes de aplicar la portada C.
            yield return null;

            CaptureRoundedSprite();
            HideLegacyRoleCopy();
            BuildVisualComposition();
            LayoutFunctionalButtons();
            StartCoroutine(AnimateIn());
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

        private void HideLegacyRoleCopy()
        {
            // Conservamos los labels de los tres botones funcionales y ocultamos únicamente
            // los textos estáticos de la portada vieja.
            foreach (var text in _rolePanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (IsChildOf(text.transform, _hostButton.transform) ||
                    IsChildOf(text.transform, _stationButton.transform) ||
                    IsChildOf(text.transform, _arButton.transform))
                {
                    continue;
                }

                text.gameObject.SetActive(false);
            }

            var panelImage = _rolePanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = UiTheme.Background;
            }
        }

        private static bool IsChildOf(Transform child, Transform possibleParent)
        {
            if (child == null || possibleParent == null) return false;
            return child == possibleParent || child.IsChildOf(possibleParent);
        }

        private void BuildVisualComposition()
        {
            _visualRoot = new GameObject("HomeCVisuals", typeof(RectTransform), typeof(CanvasGroup));
            _visualRoot.transform.SetParent(_rolePanel.transform, false);
            _visualRoot.transform.SetAsFirstSibling();

            var rootRect = _visualRoot.GetComponent<RectTransform>();
            Stretch(rootRect);

            _visualGroup = _visualRoot.GetComponent<CanvasGroup>();
            _visualGroup.alpha = 0f;
            _visualGroup.interactable = false;
            _visualGroup.blocksRaycasts = false;

            CreateHeader();
            CreateHeroCard();
            CreateSecondaryCardDecorations();
            CreateStatusCard();
        }

        private void CreateHeader()
        {
            var eyebrow = CreateText(_visualRoot.transform, "HomeEyebrow", "ENTRENAMIENTO INTELIGENTE", 19, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(eyebrow.rectTransform, 0.06f, 0.945f, 0.72f, 0.985f);
            eyebrow.color = UiTheme.TextSecondary;
            eyebrow.characterSpacing = 2f;

            var dot = CreateImage(_visualRoot.transform, "LiveDot", UiTheme.AccentLime);
            SetRect(dot.rectTransform, 0.905f, 0.955f, 0.93f, 0.98f);

            var ready = CreateText(_visualRoot.transform, "ReadyLabel", "LISTO", 16, FontStyles.Bold, TextAlignmentOptions.Right);
            SetRect(ready.rectTransform, 0.77f, 0.947f, 0.895f, 0.985f);
            ready.color = UiTheme.TextSecondary;
        }

        private void CreateHeroCard()
        {
            var hero = CreateImage(_visualRoot.transform, "ARHeroCard", UiTheme.CardElevated);
            SetRect(hero.rectTransform, 0.055f, 0.515f, 0.945f, 0.925f);

            // Halo suave detrás del símbolo AR. Es puramente visual y no intercepta toques.
            _heroGlow = CreateImage(hero.transform, "HeroGlow", new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.12f));
            SetRect(_heroGlow.rectTransform, 0.66f, 0.48f, 0.96f, 0.92f);

            var target = CreateText(hero.transform, "ARTarget", "◎", 66, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(target.rectTransform, 0.70f, 0.59f, 0.92f, 0.88f);
            target.color = UiTheme.Accent;

            var badge = CreateText(hero.transform, "HeroBadge", "NUEVA EXPERIENCIA", 17, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(badge.rectTransform, 0.055f, 0.82f, 0.62f, 0.91f);
            badge.color = UiTheme.Accent;
            badge.characterSpacing = 1.5f;

            var title = CreateText(hero.transform, "HeroTitle", "Convertí el espacio\nen tu cancha.", 42, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.055f, 0.49f, 0.78f, 0.82f);
            title.color = UiTheme.TextPrimary;
            title.enableWordWrapping = true;

            var subtitle = CreateText(hero.transform, "HeroSubtitle", "Colocá objetivos con la cámara y entrená reacción, velocidad y coordinación.", 21, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(subtitle.rectTransform, 0.055f, 0.30f, 0.91f, 0.49f);
            subtitle.color = UiTheme.TextSecondary;
            subtitle.enableWordWrapping = true;

            // Firma visual de las cuatro zonas AR.
            float startX = 0.055f;
            float gap = 0.018f;
            float width = 0.135f;
            Color[] colors =
            {
                new Color32(0x3D, 0x8B, 0xFF, 0xFF),
                new Color32(0x45, 0xD4, 0x75, 0xFF),
                new Color32(0xFF, 0xC8, 0x3D, 0xFF),
                new Color32(0xEF, 0x53, 0x50, 0xFF)
            };

            for (int i = 0; i < colors.Length; i++)
            {
                float x0 = startX + i * (width + gap);
                var bar = CreateImage(hero.transform, $"ZoneBar{i + 1}", colors[i]);
                SetRect(bar.rectTransform, x0, 0.055f, x0 + width, 0.075f);
            }
        }

        private void CreateSecondaryCardDecorations()
        {
            CreateSecondaryCardBackground("CoachCard", 0.055f, 0.285f, 0.485f, 0.475f, UiTheme.Accent);
            CreateSecondaryCardBackground("StationCard", 0.515f, 0.285f, 0.945f, 0.475f, UiTheme.Info);

            var coachMark = CreateText(_visualRoot.transform, "CoachMark", "01", 16, FontStyles.Bold, TextAlignmentOptions.Right);
            SetRect(coachMark.rectTransform, 0.37f, 0.425f, 0.455f, 0.46f);
            coachMark.color = UiTheme.TextMuted;

            var stationMark = CreateText(_visualRoot.transform, "StationMark", "02", 16, FontStyles.Bold, TextAlignmentOptions.Right);
            SetRect(stationMark.rectTransform, 0.83f, 0.425f, 0.915f, 0.46f);
            stationMark.color = UiTheme.TextMuted;
        }

        private void CreateSecondaryCardBackground(string name, float xMin, float yMin, float xMax, float yMax, Color accent)
        {
            var card = CreateImage(_visualRoot.transform, name, UiTheme.CardElevated);
            SetRect(card.rectTransform, xMin, yMin, xMax, yMax);

            var line = CreateImage(card.transform, "Accent", accent);
            SetRect(line.rectTransform, 0.06f, 0.89f, 0.40f, 0.925f);
        }

        private void CreateStatusCard()
        {
            var card = CreateImage(_visualRoot.transform, "ARStatusCard", UiTheme.Surface);
            SetRect(card.rectTransform, 0.055f, 0.075f, 0.945f, 0.245f);

            var indicator = CreateImage(card.transform, "StatusIndicator", UiTheme.AccentLime);
            SetRect(indicator.rectTransform, 0.045f, 0.33f, 0.07f, 0.68f);

            var title = CreateText(card.transform, "StatusTitle", "AR TRAINING LISTO", 21, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.10f, 0.50f, 0.78f, 0.79f);
            title.color = UiTheme.TextPrimary;

            var detail = CreateText(card.transform, "StatusDetail", "Cámara fija  •  4 zonas  •  10 objetivos", 18, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(detail.rectTransform, 0.10f, 0.22f, 0.88f, 0.50f);
            detail.color = UiTheme.TextSecondary;

            var arrow = CreateText(card.transform, "StatusArrow", "→", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(arrow.rectTransform, 0.86f, 0.28f, 0.95f, 0.72f);
            arrow.color = UiTheme.TextMuted;
        }

        private void LayoutFunctionalButtons()
        {
            // AR: CTA principal dentro del hero.
            SetButtonRect(_arButton, 0.10f, 0.535f, 0.90f, 0.615f);
            SetButtonVisual(_arButton, UiTheme.Accent, "INICIAR AR TRAINING   →", 24, TextAlignmentOptions.Center);
            _arButton.transform.SetAsLastSibling();
            _arBaseScale = _arButton.transform.localScale;

            // Entrenador y Estación: cards táctiles secundarias.
            SetButtonRect(_hostButton, 0.075f, 0.305f, 0.465f, 0.445f);
            SetButtonVisual(_hostButton, new Color(UiTheme.CardElevated.r, UiTheme.CardElevated.g, UiTheme.CardElevated.b, 0.02f),
                "ENTRENADOR\n<size=67%><color=#A8B2C1>Crear y dirigir una sesión</color></size>", 24, TextAlignmentOptions.BottomLeft);
            MakeButtonBackgroundTransparent(_hostButton);
            _hostButton.transform.SetAsLastSibling();

            SetButtonRect(_stationButton, 0.535f, 0.305f, 0.925f, 0.445f);
            SetButtonVisual(_stationButton, new Color(UiTheme.CardElevated.r, UiTheme.CardElevated.g, UiTheme.CardElevated.b, 0.02f),
                "ESTACIÓN\n<size=67%><color=#A8B2C1>Unirme al entrenamiento</color></size>", 24, TextAlignmentOptions.BottomLeft);
            MakeButtonBackgroundTransparent(_stationButton);
            _stationButton.transform.SetAsLastSibling();
        }

        private void MakeButtonBackgroundTransparent(Button button)
        {
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(1f, 1f, 1f, 0.001f);
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 0.90f);
            colors.selectedColor = Color.white;
            colors.disabledColor = UiTheme.Disabled;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private void SetButtonVisual(Button button, Color background, string labelText, float maxFont, TextAlignmentOptions alignment)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = background;
                if (_roundedSprite != null)
                {
                    image.sprite = _roundedSprite;
                    image.type = Image.Type.Sliced;
                }
            }

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = labelText;
                label.color = UiTheme.TextPrimary;
                label.fontStyle = FontStyles.Bold;
                label.alignment = alignment;
                label.enableAutoSizing = true;
                label.fontSizeMin = 14f;
                label.fontSizeMax = maxFont;

                var rect = label.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(18f, 12f);
                rect.offsetMax = new Vector2(-18f, -12f);
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = UiTheme.Disabled;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static void SetButtonRect(Button button, float xMin, float yMin, float xMax, float yMax)
        {
            if (button == null) return;
            var rect = button.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private IEnumerator AnimateIn()
        {
            if (_visualGroup == null || _visualRoot == null) yield break;

            var rect = _visualRoot.GetComponent<RectTransform>();
            Vector3 startScale = new Vector3(0.985f, 0.985f, 1f);
            rect.localScale = startScale;

            float duration = 0.32f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                _visualGroup.alpha = eased;
                rect.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, eased);
                yield return null;
            }

            _visualGroup.alpha = 1f;
            rect.localScale = Vector3.one;
        }

        private void Update()
        {
            if (_rolePanel == null || !_rolePanel.activeInHierarchy) return;

            // Pulso muy leve del CTA AR y de su halo. No afecta su área táctil.
            float wave = (Mathf.Sin(Time.unscaledTime * 2.15f + _pulseOffset) + 1f) * 0.5f;
            if (_arButton != null)
            {
                float scale = Mathf.Lerp(1f, 1.012f, wave);
                _arButton.transform.localScale = _arBaseScale * scale;
            }

            if (_heroGlow != null)
            {
                var c = _heroGlow.color;
                c.a = Mathf.Lerp(0.07f, 0.15f, wave);
                _heroGlow.color = c;
            }
        }

        private GameObject FindDeep(string objectName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
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

        private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(11f, fontSize * 0.58f);
            text.fontSizeMax = fontSize;
            return text;
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

        private void OnDisable()
        {
            if (_arButton != null)
            {
                _arButton.transform.localScale = _arBaseScale;
            }
        }
    }
}
