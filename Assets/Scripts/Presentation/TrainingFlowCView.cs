using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Extiende el lenguaje visual de la Home C al resto de TrainingNearby.
    /// Solo modifica presentación y layout de controles existentes: la lógica
    /// de sesión, Nearby y métricas sigue viviendo en TrainingNearbyBootstrap.
    /// </summary>
    public sealed class TrainingFlowCView : MonoBehaviour
    {
        private GameObject _configPanel;
        private GameObject _progressPanel;
        private GameObject _waitPanel;
        private GameObject _summaryPanel;
        private GameObject _colorView;
        private TMP_Text _overlayLabel;

        private Sprite _roundedSprite;
        private RectTransform _progressFill;
        private TMP_Text _liveBadge;
        private TMP_Text _stationBadge;
        private GameObject _overlayBackdrop;
        private Image _progressCard;
        private float _ambientOffset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<TrainingFlowCView>() == null)
                {
                    canvas.gameObject.AddComponent<TrainingFlowCView>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _ambientOffset = Random.Range(0f, 10f);
            StartCoroutine(BuildWhenReady());
        }

        private IEnumerator BuildWhenReady()
        {
            // Bootstrap y los controladores visuales inicializan sus referencias en Start/Awake.
            yield return null;
            yield return null;

            CacheObjects();
            CaptureRoundedSprite();

            StyleConfigScreen();
            StyleProgressScreen();
            StyleStationWaitScreen();
            StyleSummaryScreen();
            StyleColorScreen();
            StyleOverlay();
        }

        private void CacheObjects()
        {
            _configPanel = FindDeep("HostConfigPanel");
            _progressPanel = FindDeep("HostProgressPanel");
            _waitPanel = FindDeep("StationWaitPanel");
            _summaryPanel = FindDeep("SummaryPanel");
            _colorView = FindDeep("ColorView");

            var overlay = FindDeep("Overlay");
            _overlayLabel = overlay != null ? overlay.GetComponent<TMP_Text>() : null;
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

        // ------------------------------------------------------------------
        // Configuración de entrenador
        // ------------------------------------------------------------------

        private void StyleConfigScreen()
        {
            if (_configPanel == null) return;
            PreparePanel(_configPanel);
            HideLegacyTitle(_configPanel);

            CreateHeader(_configPanel.transform,
                "ENTRENADOR",
                "CONFIGURAR SESIÓN",
                "Ajustá el desafío y conectá las estaciones antes de empezar.",
                UiTheme.Accent);

            var roundsCard = FindIn(_configPanel, "RoundsCard");
            StyleExistingCard(roundsCard, 0.055f, 0.770f, 0.945f, 0.855f);
            AddCardCaption(roundsCard, "RONDAS", UiTheme.TextMuted);

            var roundsValue = FindText(_configPanel, "RoundsValue");
            if (roundsValue != null)
            {
                SetRect(roundsValue.rectTransform, 0.085f, 0.785f, 0.50f, 0.837f);
                SetupText(roundsValue, 36f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UiTheme.TextPrimary, 20f);
            }

            var minus = FindButton(_configPanel, "RoundsMinus");
            var plus = FindButton(_configPanel, "RoundsPlus");
            LayoutCompactButton(minus, 0.585f, 0.782f, 0.725f, 0.842f, UiTheme.Neutral, 34f);
            LayoutCompactButton(plus, 0.755f, 0.782f, 0.895f, 0.842f, UiTheme.Accent, 34f);

            LayoutOptionButton(FindButton(_configPanel, "ModeButton"), 0.055f, 0.680f, 0.945f, 0.750f, UiTheme.CardElevated);
            LayoutOptionButton(FindButton(_configPanel, "TimeoutButton"), 0.055f, 0.600f, 0.945f, 0.670f, UiTheme.CardElevated);
            LayoutOptionButton(FindButton(_configPanel, "ColorModeButton"), 0.055f, 0.520f, 0.945f, 0.590f, UiTheme.CardElevated);
            LayoutOptionButton(FindButton(_configPanel, "ParticipateButton"), 0.055f, 0.440f, 0.945f, 0.510f, new Color(UiTheme.Info.r, UiTheme.Info.g, UiTheme.Info.b, 0.38f));

            var connectedCard = FindIn(_configPanel, "ConnectedCard");
            StyleExistingCard(connectedCard, 0.055f, 0.305f, 0.945f, 0.425f);
            AddAccentRail(connectedCard, UiTheme.AccentLime);

            var connected = FindText(_configPanel, "ConnectedLabel");
            if (connected != null)
            {
                SetRect(connected.rectTransform, 0.105f, 0.325f, 0.905f, 0.405f);
                SetupText(connected, 29f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UiTheme.TextSecondary, 18f);
            }

            var request = FindIn(_configPanel, "RequestCard");
            if (request != null)
            {
                SetRect(request.GetComponent<RectTransform>(), 0.055f, 0.160f, 0.945f, 0.295f);
                var image = request.GetComponent<Image>();
                if (image != null)
                {
                    ApplyRounded(image);
                    image.color = UiTheme.CardElevated;
                }

                var requestLabel = FindText(request, "RequestLabel");
                if (requestLabel != null)
                {
                    SetRect(requestLabel.rectTransform, 0.055f, 0.56f, 0.945f, 0.92f);
                    SetupText(requestLabel, 28f, FontStyles.Bold, TextAlignmentOptions.Center, UiTheme.TextPrimary, 17f);
                }

                LayoutInnerButton(FindButton(request, "AcceptButton"), 0.055f, 0.08f, 0.48f, 0.49f, UiTheme.Positive);
                LayoutInnerButton(FindButton(request, "RejectButton"), 0.52f, 0.08f, 0.945f, 0.49f, UiTheme.Danger);
            }

            var start = FindButton(_configPanel, "StartButton");
            LayoutPrimaryButton(start, 0.085f, 0.050f, 0.915f, 0.140f, "INICIAR SESIÓN   →");
        }

        // ------------------------------------------------------------------
        // Sesión en vivo
        // ------------------------------------------------------------------

        private void StyleProgressScreen()
        {
            if (_progressPanel == null) return;
            PreparePanel(_progressPanel);
            HideLegacyTitle(_progressPanel);

            CreateHeader(_progressPanel.transform,
                "ENTRENADOR",
                "SESIÓN EN VIVO",
                "Seguimiento en tiempo real del ejercicio.",
                UiTheme.AccentLime);

            _liveBadge = CreateText(_progressPanel.transform, "LiveBadge", "●  LIVE", 19f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_liveBadge.rectTransform, 0.72f, 0.895f, 0.93f, 0.945f);
            _liveBadge.color = UiTheme.AccentLime;

            _progressCard = CreateImage(_progressPanel.transform, "ProgressModernCard", UiTheme.CardElevated);
            SetRect(_progressCard.rectTransform, 0.055f, 0.285f, 0.945f, 0.825f);
            _progressCard.transform.SetAsFirstSibling();

            var progressLabel = FindText(_progressPanel, "ProgressLabel");
            if (progressLabel != null)
            {
                SetRect(progressLabel.rectTransform, 0.095f, 0.365f, 0.905f, 0.775f);
                SetupText(progressLabel, 44f, FontStyles.Bold, TextAlignmentOptions.TopLeft, UiTheme.TextPrimary, 24f);
                progressLabel.lineSpacing = 8f;
            }

            var barBack = CreateImage(_progressPanel.transform, "RoundProgressBack", UiTheme.Divider);
            SetRect(barBack.rectTransform, 0.095f, 0.315f, 0.905f, 0.335f);

            var barFill = CreateImage(barBack.transform, "RoundProgressFill", UiTheme.AccentLime);
            SetRect(barFill.rectTransform, 0f, 0f, 0.05f, 1f);
            _progressFill = barFill.rectTransform;

            var footer = CreateImage(_progressPanel.transform, "LiveTipCard", UiTheme.Surface);
            SetRect(footer.rectTransform, 0.055f, 0.095f, 0.945f, 0.235f);
            var footerText = CreateText(footer.transform, "LiveTip", "⚡  Cada respuesta queda registrada automáticamente.\nMantené las estaciones visibles y listas para la próxima ronda.", 22f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            SetRect(footerText.rectTransform, 0.06f, 0.18f, 0.94f, 0.82f);
            footerText.color = UiTheme.TextSecondary;
        }

        // ------------------------------------------------------------------
        // Espera de estación
        // ------------------------------------------------------------------

        private void StyleStationWaitScreen()
        {
            if (_waitPanel == null) return;
            PreparePanel(_waitPanel);
            HideLegacyTitle(_waitPanel);

            CreateHeader(_waitPanel.transform,
                "ESTACIÓN",
                "LISTA PARA CONECTAR",
                "Este teléfono funcionará como punto de reacción.",
                UiTheme.Info);

            var radarCard = CreateImage(_waitPanel.transform, "StationRadarCard", UiTheme.CardElevated);
            SetRect(radarCard.rectTransform, 0.055f, 0.285f, 0.945f, 0.825f);
            radarCard.transform.SetAsFirstSibling();

            var radar = CreateText(_waitPanel.transform, "StationRadar", "◎", 128f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(radar.rectTransform, 0.25f, 0.575f, 0.75f, 0.77f);
            radar.color = UiTheme.Info;
            if (radar.GetComponent<PulseScale>() == null) radar.gameObject.AddComponent<PulseScale>();

            _stationBadge = CreateText(_waitPanel.transform, "StationBadge", "●  BUSCANDO", 19f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_stationBadge.rectTransform, 0.30f, 0.515f, 0.70f, 0.56f);
            _stationBadge.color = UiTheme.Info;

            var status = FindText(_waitPanel, "StationStatusLabel");
            if (status != null)
            {
                SetRect(status.rectTransform, 0.12f, 0.335f, 0.88f, 0.505f);
                SetupText(status, 34f, FontStyles.Bold, TextAlignmentOptions.Center, UiTheme.TextPrimary, 21f);
            }

            var tip = CreateImage(_waitPanel.transform, "StationTipCard", UiTheme.Surface);
            SetRect(tip.rectTransform, 0.055f, 0.095f, 0.945f, 0.235f);
            var tipText = CreateText(tip.transform, "StationTip", "Mantené esta pantalla abierta.\nCuando llegue tu turno, todo el teléfono se convertirá en el objetivo.", 22f, FontStyles.Normal, TextAlignmentOptions.Center);
            SetRect(tipText.rectTransform, 0.07f, 0.18f, 0.93f, 0.82f);
            tipText.color = UiTheme.TextSecondary;
        }

        // ------------------------------------------------------------------
        // Resumen
        // ------------------------------------------------------------------

        private void StyleSummaryScreen()
        {
            if (_summaryPanel == null) return;
            PreparePanel(_summaryPanel);
            HideLegacyTitle(_summaryPanel);

            var eyebrow = CreateText(_summaryPanel.transform, "SummaryEyebrow", "ENTRENAMIENTO COMPLETADO", 18f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(eyebrow.rectTransform, 0.12f, 0.900f, 0.88f, 0.945f);
            eyebrow.color = UiTheme.AccentLime;

            var trophy = CreateText(_summaryPanel.transform, "SummaryTrophy", "✓", 72f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(trophy.rectTransform, 0.35f, 0.790f, 0.65f, 0.895f);
            trophy.color = UiTheme.AccentLime;

            var title = CreateText(_summaryPanel.transform, "SummaryModernTitle", "RESULTADOS", 42f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, 0.10f, 0.735f, 0.90f, 0.805f);
            title.color = UiTheme.TextPrimary;

            var resultCard = CreateImage(_summaryPanel.transform, "SummaryResultCard", UiTheme.CardElevated);
            SetRect(resultCard.rectTransform, 0.055f, 0.245f, 0.945f, 0.710f);
            resultCard.transform.SetAsFirstSibling();

            var summary = FindText(_summaryPanel, "SummaryLabel");
            if (summary != null)
            {
                SetRect(summary.rectTransform, 0.10f, 0.285f, 0.90f, 0.675f);
                SetupText(summary, 35f, FontStyles.Normal, TextAlignmentOptions.TopLeft, UiTheme.TextPrimary, 20f);
                summary.lineSpacing = 5f;
            }

            var restart = FindButton(_summaryPanel, "RestartButton");
            LayoutPrimaryButton(restart, 0.085f, 0.085f, 0.915f, 0.185f, "NUEVA SESIÓN   →");
        }

        // ------------------------------------------------------------------
        // Pantalla de reacción y overlays
        // ------------------------------------------------------------------

        private void StyleColorScreen()
        {
            if (_colorView == null) return;

            var tapHint = FindText(_colorView, "TapHint");
            if (tapHint != null)
            {
                SetRect(tapHint.rectTransform, 0.10f, 0.405f, 0.90f, 0.615f);
                SetupText(tapHint, 118f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, 58f);
            }

            if (FindIn(_colorView, "ReactionTopLabel") == null)
            {
                var top = CreateText(_colorView.transform, "ReactionTopLabel", "REFLEX TRAINING", 22f, FontStyles.Bold, TextAlignmentOptions.Center);
                SetRect(top.rectTransform, 0.15f, 0.885f, 0.85f, 0.935f);
                top.color = new Color(1f, 1f, 1f, 0.82f);
                top.characterSpacing = 3f;
            }
        }

        private void StyleOverlay()
        {
            if (_overlayLabel == null) return;

            SetupText(_overlayLabel, 200f, FontStyles.Bold, TextAlignmentOptions.Center, UiTheme.AccentLime, 80f);

            var overlayGo = _overlayLabel.gameObject;
            var parent = overlayGo.transform.parent;
            if (parent == null || FindDeep("ModernOverlayBackdrop") != null) return;

            var backdrop = new GameObject("ModernOverlayBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdrop.transform.SetParent(parent, false);
            var rect = backdrop.GetComponent<RectTransform>();
            Stretch(rect);
            var image = backdrop.GetComponent<Image>();
            image.color = new Color(0.02f, 0.03f, 0.05f, 0.62f);
            image.raycastTarget = false;
            backdrop.transform.SetSiblingIndex(Mathf.Max(0, overlayGo.transform.GetSiblingIndex()));
            backdrop.SetActive(false);
            _overlayBackdrop = backdrop;
        }

        // ------------------------------------------------------------------
        // Actualización dinámica
        // ------------------------------------------------------------------

        private void Update()
        {
            UpdateLiveProgress();
            UpdateStationState();
            UpdateOverlayBackdrop();
            UpdateAmbientMotion();
        }

        private void UpdateLiveProgress()
        {
            if (_progressPanel == null || !_progressPanel.activeInHierarchy || _progressFill == null) return;

            var progress = FindText(_progressPanel, "ProgressLabel");
            if (progress == null || string.IsNullOrWhiteSpace(progress.text)) return;

            float ratio = ParseRoundRatio(progress.text);
            if (ratio > 0f)
            {
                var max = _progressFill.anchorMax;
                max.x = Mathf.Clamp01(ratio);
                _progressFill.anchorMax = max;
            }
        }

        private static float ParseRoundRatio(string value)
        {
            // Esperado: "Ronda 3/10" como primera línea.
            int marker = value.IndexOf("Ronda ", System.StringComparison.OrdinalIgnoreCase);
            if (marker < 0) return 0f;
            int slash = value.IndexOf('/', marker);
            if (slash < 0) return 0f;

            int numberStart = marker + 6;
            string currentText = value.Substring(numberStart, slash - numberStart).Trim();
            int end = slash + 1;
            while (end < value.Length && char.IsDigit(value[end])) end++;
            string totalText = value.Substring(slash + 1, end - slash - 1).Trim();

            if (!int.TryParse(currentText, out int current) || !int.TryParse(totalText, out int total) || total <= 0) return 0f;
            return current / (float)total;
        }

        private void UpdateStationState()
        {
            if (_waitPanel == null || !_waitPanel.activeInHierarchy || _stationBadge == null) return;

            var status = FindText(_waitPanel, "StationStatusLabel");
            if (status == null) return;

            string s = status.text.ToLowerInvariant();
            if (s.Contains("conectado") || s.Contains("sesión iniciada") || s.Contains("atento"))
            {
                _stationBadge.text = "●  CONECTADO";
                _stationBadge.color = UiTheme.AccentLime;
            }
            else if (s.Contains("perdió") || s.Contains("necesita un celular"))
            {
                _stationBadge.text = "●  ATENCIÓN";
                _stationBadge.color = UiTheme.Danger;
            }
            else
            {
                _stationBadge.text = "●  BUSCANDO";
                _stationBadge.color = UiTheme.Info;
            }
        }

        private void UpdateOverlayBackdrop()
        {
            if (_overlayBackdrop == null || _overlayLabel == null) return;
            bool shouldShow = _overlayLabel.gameObject.activeInHierarchy;
            if (_overlayBackdrop.activeSelf != shouldShow)
            {
                _overlayBackdrop.SetActive(shouldShow);
                if (shouldShow)
                {
                    _overlayBackdrop.transform.SetSiblingIndex(Mathf.Max(0, _overlayLabel.transform.GetSiblingIndex()));
                }
            }
        }

        private void UpdateAmbientMotion()
        {
            if (_progressCard == null || _progressPanel == null || !_progressPanel.activeInHierarchy) return;
            float wave = (Mathf.Sin(Time.unscaledTime * 1.7f + _ambientOffset) + 1f) * 0.5f;
            Color c = UiTheme.CardElevated;
            c.r = Mathf.Clamp01(c.r + 0.015f * wave);
            c.g = Mathf.Clamp01(c.g + 0.012f * wave);
            _progressCard.color = c;
        }

        // ------------------------------------------------------------------
        // Helpers visuales
        // ------------------------------------------------------------------

        private void PreparePanel(GameObject panel)
        {
            if (panel == null) return;
            var image = panel.GetComponent<Image>();
            if (image != null) image.color = UiTheme.Background;
        }

        private void HideLegacyTitle(GameObject panel)
        {
            var title = FindIn(panel, "Title");
            if (title != null) title.SetActive(false);
        }

        private void CreateHeader(Transform parent, string eyebrowText, string titleText, string subtitleText, Color accent)
        {
            var eyebrow = CreateText(parent, "ModernEyebrow", eyebrowText, 17f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(eyebrow.rectTransform, 0.055f, 0.927f, 0.70f, 0.965f);
            eyebrow.color = accent;
            eyebrow.characterSpacing = 2f;

            var title = CreateText(parent, "ModernTitle", titleText, 39f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.055f, 0.870f, 0.945f, 0.927f);
            title.color = UiTheme.TextPrimary;

            var subtitle = CreateText(parent, "ModernSubtitle", subtitleText, 19f, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(subtitle.rectTransform, 0.055f, 0.835f, 0.945f, 0.875f);
            subtitle.color = UiTheme.TextSecondary;
        }

        private void StyleExistingCard(GameObject card, float xMin, float yMin, float xMax, float yMax)
        {
            if (card == null) return;
            var rect = card.GetComponent<RectTransform>();
            if (rect != null) SetRect(rect, xMin, yMin, xMax, yMax);
            var image = card.GetComponent<Image>();
            if (image != null)
            {
                ApplyRounded(image);
                image.color = UiTheme.CardElevated;
                image.raycastTarget = false;
            }
        }

        private void AddCardCaption(GameObject card, string text, Color color)
        {
            if (card == null || FindIn(card, "ModernCaption") != null) return;
            var label = CreateText(card.transform, "ModernCaption", text, 14f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(label.rectTransform, 0.035f, 0.66f, 0.28f, 0.93f);
            label.color = color;
            label.characterSpacing = 1f;
        }

        private void AddAccentRail(GameObject card, Color color)
        {
            if (card == null || FindIn(card, "ModernAccentRail") != null) return;
            var rail = CreateImage(card.transform, "ModernAccentRail", color);
            SetRect(rail.rectTransform, 0.018f, 0.22f, 0.032f, 0.78f);
        }

        private void LayoutOptionButton(Button button, float xMin, float yMin, float xMax, float yMax, Color background)
        {
            if (button == null) return;
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            SetButtonColors(button, background);
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                SetupText(label, 28f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, UiTheme.TextPrimary, 18f);
                label.rectTransform.offsetMin = new Vector2(26f, 8f);
                label.rectTransform.offsetMax = new Vector2(-26f, -8f);
            }
        }

        private void LayoutCompactButton(Button button, float xMin, float yMin, float xMax, float yMax, Color background, float fontSize)
        {
            if (button == null) return;
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            SetButtonColors(button, background);
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) SetupText(label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, UiTheme.TextPrimary, 20f);
        }

        private void LayoutInnerButton(Button button, float xMin, float yMin, float xMax, float yMax, Color background)
        {
            if (button == null) return;
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            SetButtonColors(button, background);
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) SetupText(label, 22f, FontStyles.Bold, TextAlignmentOptions.Center, UiTheme.TextPrimary, 15f);
        }

        private void LayoutPrimaryButton(Button button, float xMin, float yMin, float xMax, float yMax, string labelText)
        {
            if (button == null) return;
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            SetButtonColors(button, UiTheme.Accent);
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = labelText;
                SetupText(label, 28f, FontStyles.Bold, TextAlignmentOptions.Center, UiTheme.TextPrimary, 18f);
            }
        }

        private void SetButtonColors(Button button, Color background)
        {
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                ApplyRounded(image);
                image.color = background;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = UiTheme.Disabled;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private void ApplyRounded(Image image)
        {
            if (image == null || _roundedSprite == null) return;
            image.sprite = _roundedSprite;
            image.type = Image.Type.Sliced;
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

        private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = UiTheme.TextPrimary;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(11f, fontSize * 0.58f);
            text.fontSizeMax = fontSize;
            text.enableWordWrapping = true;
            return text;
        }

        private static void SetupText(TMP_Text text, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color, float minSize)
        {
            if (text == null) return;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = fontSize;
            text.enableWordWrapping = true;
        }

        private GameObject FindDeep(string objectName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }
            return null;
        }

        private static GameObject FindIn(GameObject root, string objectName)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }
            return null;
        }

        private static TMP_Text FindText(GameObject root, string objectName)
        {
            var go = FindIn(root, objectName);
            return go != null ? go.GetComponent<TMP_Text>() : null;
        }

        private static Button FindButton(GameObject root, string objectName)
        {
            var go = FindIn(root, objectName);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static void SetRect(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            if (rect == null) return;
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
