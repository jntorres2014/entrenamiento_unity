using System.Collections;
using System.Text.RegularExpressions;
using Entrenamiento.Core.Models;
using Entrenamiento.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Capa visual final del concepto Deportivo Pro para configuración, sesión
    /// en vivo, resultados y pantallas SOLO. No modifica reglas ni transporte.
    /// </summary>
    public sealed class DeportivoProSessionSkin : MonoBehaviour
    {
        private Canvas _canvas;
        private Sprite _roundedSprite;
        private bool _configBuilt;
        private bool _liveBuilt;
        private bool _summaryBuilt;
        private bool _soloSelectorSkinned;
        private bool _soloOptionsSkinned;

        private TMP_Text _liveRound;
        private TMP_Text _liveExercise;
        private TMP_Text _liveStats;
        private Image _liveGlow;

        private TMP_Text _resultAccuracy;
        private TMP_Text _resultHits;
        private TMP_Text _resultErrors;
        private TMP_Text _resultAverage;
        private TMP_Text _resultBest;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<DeportivoProSessionSkin>() == null)
                {
                    canvas.gameObject.AddComponent<DeportivoProSessionSkin>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            StartCoroutine(BuildWhenReady());
        }

        private IEnumerator BuildWhenReady()
        {
            yield return null;
            yield return null;
            yield return null;
            CaptureRoundedSprite();
            BuildConfig();
            BuildLive();
            BuildSummary();
        }

        private void Update()
        {
            SkinSoloSelectorIfNeeded();
            SkinSoloOptionsIfNeeded();
            UpdateLive();
            UpdateSummary();
            UpdateStationWait();
            UpdateCameraSurfaces();
        }

        // ------------------------------------------------------------------
        // Configuración
        // ------------------------------------------------------------------

        private void BuildConfig()
        {
            if (_configBuilt) return;
            var panel = FindDeep("HostConfigPanel");
            if (panel == null) return;
            _configBuilt = true;

            SetPanelBackground(panel);

            var eyebrow = FindText(panel, "ModernEyebrow");
            if (eyebrow != null)
            {
                eyebrow.text = "DEPORTIVO PRO  /  CON PODS";
                eyebrow.color = UiTheme.Accent;
                SetRect(eyebrow.rectTransform, 0.055f, 0.925f, 0.72f, 0.962f);
            }
            var title = FindText(panel, "ModernTitle");
            if (title != null)
            {
                title.text = "Configurar sesión";
                title.fontSizeMax = 37f;
                SetRect(title.rectTransform, 0.055f, 0.865f, 0.94f, 0.925f);
            }
            var subtitle = FindText(panel, "ModernSubtitle");
            if (subtitle != null)
            {
                subtitle.text = "Ajustá el desafío y comprobá las estaciones antes de empezar.";
                subtitle.fontSizeMax = 17f;
                SetRect(subtitle.rectTransform, 0.055f, 0.825f, 0.94f, 0.865f);
            }

            var mode = FindButton(panel, "ModeButton");
            if (mode != null)
            {
                SetRect(mode.GetComponent<RectTransform>(), 0.055f, 0.705f, 0.945f, 0.805f);
                StyleButton(mode, UiTheme.CardElevated, UiTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, 24f);
                var accent = CreateImage(mode.transform, "ProExerciseRail", UiTheme.Accent);
                SetRect(accent.rectTransform, 0.018f, 0.18f, 0.030f, 0.82f);
                var caption = CreateText(mode.transform, "ProExerciseCaption", "EJERCICIO", 12.5f, FontStyles.Bold, TextAlignmentOptions.Left);
                SetRect(caption.rectTransform, 0.055f, 0.66f, 0.40f, 0.90f);
                caption.color = UiTheme.TextMuted;
                caption.characterSpacing = 1.2f;
                var label = mode.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    SetRect(label.rectTransform, 0.055f, 0.10f, 0.94f, 0.65f);
                    label.fontSizeMax = 24f;
                    label.fontSizeMin = 17f;
                }
            }

            // La regla del preset ya se ve en el selector y el card de ejercicio.
            var colorMode = FindButton(panel, "ColorModeButton");
            if (colorMode != null) colorMode.gameObject.SetActive(false);

            var roundsCard = FindIn(panel, "RoundsCard");
            if (roundsCard != null)
            {
                StyleCard(roundsCard, 0.055f, 0.585f, 0.945f, 0.685f);
                var value = FindText(panel, "RoundsValue");
                if (value != null)
                {
                    SetRect(value.rectTransform, 0.085f, 0.598f, 0.48f, 0.660f);
                    value.fontSizeMax = 34f;
                    value.fontSizeMin = 21f;
                    value.alignment = TextAlignmentOptions.MidlineLeft;
                }
                LayoutSmallButton(FindButton(panel, "RoundsMinus"), 0.61f, 0.600f, 0.73f, 0.665f, UiTheme.Neutral);
                LayoutSmallButton(FindButton(panel, "RoundsPlus"), 0.77f, 0.600f, 0.89f, 0.665f, UiTheme.Accent);
            }

            var timeout = FindButton(panel, "TimeoutButton");
            if (timeout != null)
            {
                SetRect(timeout.GetComponent<RectTransform>(), 0.055f, 0.485f, 0.945f, 0.565f);
                StyleButton(timeout, UiTheme.CardElevated, UiTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, 20f);
            }

            var participate = FindButton(panel, "ParticipateButton");
            if (participate != null)
            {
                SetRect(participate.GetComponent<RectTransform>(), 0.055f, 0.385f, 0.945f, 0.465f);
                StyleButton(participate, new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.12f), UiTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, 19f);
            }

            var connected = FindIn(panel, "ConnectedCard");
            if (connected != null)
            {
                StyleCard(connected, 0.055f, 0.235f, 0.945f, 0.365f);
                var rail = FindIn(connected, "ModernAccentRail");
                if (rail != null)
                {
                    var railImage = rail.GetComponent<Image>();
                    if (railImage != null) railImage.color = UiTheme.Accent;
                }
                var label = FindText(panel, "ConnectedLabel");
                if (label != null)
                {
                    SetRect(label.rectTransform, 0.105f, 0.255f, 0.91f, 0.345f);
                    label.fontSizeMax = 22f;
                    label.fontSizeMin = 15f;
                }
            }

            var request = FindIn(panel, "RequestCard");
            if (request != null) SetRect(request.GetComponent<RectTransform>(), 0.055f, 0.145f, 0.945f, 0.225f);

            var start = FindButton(panel, "StartButton");
            if (start != null)
            {
                SetRect(start.GetComponent<RectTransform>(), 0.075f, 0.055f, 0.925f, 0.130f);
                StyleButton(start, UiTheme.Accent, UiTheme.Background, TextAlignmentOptions.Center, 21f);
                var label = start.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "INICIAR SESIÓN   →";
            }
        }

        // ------------------------------------------------------------------
        // Sesión en vivo
        // ------------------------------------------------------------------

        private void BuildLive()
        {
            if (_liveBuilt) return;
            var panel = FindDeep("HostProgressPanel");
            if (panel == null) return;
            _liveBuilt = true;
            SetPanelBackground(panel);

            HideIfFound(panel, "ProgressModernCard");
            HideIfFound(panel, "LiveTipCard");

            var eyebrow = FindText(panel, "ModernEyebrow");
            if (eyebrow != null)
            {
                eyebrow.text = "DEPORTIVO PRO";
                eyebrow.color = UiTheme.Accent;
            }
            var title = FindText(panel, "ModernTitle");
            if (title != null) title.text = "Sesión en vivo";
            var subtitle = FindText(panel, "ModernSubtitle");
            if (subtitle != null) subtitle.gameObject.SetActive(false);

            var liveBadge = FindText(panel, "LiveBadge");
            if (liveBadge != null)
            {
                liveBadge.text = "●  EN VIVO";
                liveBadge.color = UiTheme.Accent;
                SetRect(liveBadge.rectTransform, 0.72f, 0.895f, 0.93f, 0.945f);
            }

            var arena = CreateImage(panel.transform, "ProLiveArena", UiTheme.CardElevated);
            SetRect(arena.rectTransform, 0.055f, 0.285f, 0.945f, 0.815f);
            arena.transform.SetAsFirstSibling();

            var halo = CreateImage(arena.transform, "ProLiveHalo", new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.10f));
            SetRect(halo.rectTransform, 0.18f, 0.15f, 0.82f, 0.85f);
            _liveGlow = CreateImage(arena.transform, "ProLiveGlow", new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.25f));
            SetRect(_liveGlow.rectTransform, 0.29f, 0.27f, 0.71f, 0.73f);
            var core = CreateImage(arena.transform, "ProLiveCore", new Color(UiTheme.Background.r, UiTheme.Background.g, UiTheme.Background.b, 0.88f));
            SetRect(core.rectTransform, 0.355f, 0.34f, 0.645f, 0.66f);

            _liveRound = CreateText(arena.transform, "ProLiveRound", "RONDA 1 / 10", 16f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_liveRound.rectTransform, 0.20f, 0.82f, 0.80f, 0.92f);
            _liveRound.color = UiTheme.TextSecondary;
            _liveRound.characterSpacing = 1.2f;

            _liveExercise = CreateText(arena.transform, "ProLiveExercise", "REACCIÓN", 36f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_liveExercise.rectTransform, 0.16f, 0.40f, 0.84f, 0.60f);
            _liveExercise.color = UiTheme.Accent;

            var progressLabel = FindText(panel, "ProgressLabel");
            if (progressLabel != null)
            {
                SetRect(progressLabel.rectTransform, 0.085f, 0.205f, 0.915f, 0.275f);
                progressLabel.fontSizeMax = 18f;
                progressLabel.fontSizeMin = 13f;
                progressLabel.alignment = TextAlignmentOptions.Center;
                progressLabel.color = UiTheme.TextSecondary;
            }

            var cue = FindIn(panel, "ExerciseCueCard");
            if (cue != null)
            {
                SetRect(cue.GetComponent<RectTransform>(), 0.18f, 0.405f, 0.82f, 0.545f);
                var cueImage = cue.GetComponent<Image>();
                if (cueImage != null) cueImage.color = new Color(UiTheme.Surface.r, UiTheme.Surface.g, UiTheme.Surface.b, 0.96f);
            }

            var barBack = FindIn(panel, "RoundProgressBack");
            if (barBack != null) SetRect(barBack.GetComponent<RectTransform>(), 0.085f, 0.165f, 0.915f, 0.180f);

            var statsCard = CreateImage(panel.transform, "ProLiveStatsCard", UiTheme.Surface);
            SetRect(statsCard.rectTransform, 0.055f, 0.055f, 0.945f, 0.135f);
            _liveStats = CreateText(statsCard.transform, "ProLiveStats", "ACIERTOS  0     ERRORES  0", 17f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(_liveStats.rectTransform);
            _liveStats.color = UiTheme.TextSecondary;
        }

        private void UpdateLive()
        {
            var panel = FindDeep("HostProgressPanel");
            if (!_liveBuilt || panel == null || !panel.activeInHierarchy) return;

            var progress = FindText(panel, "ProgressLabel");
            string value = progress != null ? StripTags(progress.text) : string.Empty;

            Match round = Regex.Match(value, @"Ronda\s+(\d+)\s*/\s*(\d+)", RegexOptions.IgnoreCase);
            if (round.Success && _liveRound != null)
                _liveRound.text = "RONDA  " + round.Groups[1].Value + " / " + round.Groups[2].Value;

            Match stats = Regex.Match(value, @"Aciertos:\s*(\d+)\s+Errores:\s*(\d+)", RegexOptions.IgnoreCase);
            if (stats.Success && _liveStats != null)
                _liveStats.text = "ACIERTOS  " + stats.Groups[1].Value + "     ERRORES  " + stats.Groups[2].Value;

            if (_liveExercise != null)
                _liveExercise.text = ExerciseSelection.Name(ExerciseSelection.Current);

            var coordinator = ExerciseRuntimeRegistry.CurrentCoordinator;
            if (coordinator != null && _liveGlow != null)
                _liveGlow.color = WithAlpha(ColorForStation(coordinator.CurrentStimulusColor), 0.30f);
        }

        // ------------------------------------------------------------------
        // Resultados
        // ------------------------------------------------------------------

        private void BuildSummary()
        {
            if (_summaryBuilt) return;
            var panel = FindDeep("SummaryPanel");
            if (panel == null) return;
            _summaryBuilt = true;
            SetPanelBackground(panel);

            HideIfFound(panel, "SummaryEyebrow");
            HideIfFound(panel, "SummaryTrophy");
            HideIfFound(panel, "SummaryModernTitle");
            HideIfFound(panel, "SummaryResultCard");

            var raw = FindText(panel, "SummaryLabel");
            if (raw != null) raw.gameObject.SetActive(false);

            var kicker = CreateText(panel.transform, "ProResultKicker", "ENTRENAMIENTO COMPLETADO", 14.5f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(kicker.rectTransform, 0.055f, 0.925f, 0.75f, 0.962f);
            kicker.color = UiTheme.Accent;
            kicker.characterSpacing = 1.6f;

            var title = CreateText(panel.transform, "ProResultTitle", "Resultados", 38f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.055f, 0.855f, 0.94f, 0.920f);

            var banner = CreateImage(panel.transform, "ProResultBanner", new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.12f));
            SetRect(banner.rectTransform, 0.055f, 0.755f, 0.945f, 0.835f);
            var bannerText = CreateText(banner.transform, "Text", "¡GRAN TRABAJO!  ·  SEGUÍ MEJORANDO", 17f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(bannerText.rectTransform);
            bannerText.color = UiTheme.Accent;

            var precision = CreateImage(panel.transform, "ProPrecisionCard", UiTheme.CardElevated);
            SetRect(precision.rectTransform, 0.055f, 0.525f, 0.945f, 0.735f);
            var precisionCaption = CreateText(precision.transform, "Caption", "PRECISIÓN", 13f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(precisionCaption.rectTransform, 0.20f, 0.72f, 0.80f, 0.90f);
            precisionCaption.color = UiTheme.TextMuted;
            _resultAccuracy = CreateText(precision.transform, "Accuracy", "--%", 62f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_resultAccuracy.rectTransform, 0.15f, 0.17f, 0.85f, 0.73f);
            _resultAccuracy.color = UiTheme.Accent;

            _resultHits = CreateMetric(panel.transform, "Hits", "ACIERTOS", 0.055f, 0.365f, 0.485f, 0.505f, UiTheme.Accent);
            _resultErrors = CreateMetric(panel.transform, "Errors", "ERRORES", 0.515f, 0.365f, 0.945f, 0.505f, UiTheme.Danger);
            _resultAverage = CreateMetric(panel.transform, "Average", "TIEMPO PROM.", 0.055f, 0.205f, 0.485f, 0.345f, UiTheme.Info);
            _resultBest = CreateMetric(panel.transform, "Best", "MEJOR TIEMPO", 0.515f, 0.205f, 0.945f, 0.345f, UiTheme.AccentLime);

            var quote = CreateText(panel.transform, "ProResultQuote", "“La constancia siempre da resultados.”", 14.5f, FontStyles.Normal, TextAlignmentOptions.Center);
            SetRect(quote.rectTransform, 0.10f, 0.155f, 0.90f, 0.195f);
            quote.color = UiTheme.TextSecondary;

            var restart = FindButton(panel, "RestartButton");
            if (restart != null)
            {
                SetRect(restart.GetComponent<RectTransform>(), 0.51f, 0.055f, 0.945f, 0.135f);
                StyleButton(restart, UiTheme.Accent, UiTheme.Background, TextAlignmentOptions.Center, 18f);
                var label = restart.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "NUEVA SESIÓN  →";
            }

            var progressButton = CreateButton(panel.transform, "ProResultProgressButton", "VER PROGRESO", UiTheme.Surface, UiTheme.TextPrimary);
            SetRect(progressButton.GetComponent<RectTransform>(), 0.055f, 0.055f, 0.49f, 0.135f);
            progressButton.onClick.AddListener(TrainingProgressController.ShowProgress);
        }

        private TMP_Text CreateMetric(Transform parent, string name, string caption, float xMin, float yMin, float xMax, float yMax, Color accent)
        {
            var card = CreateImage(parent, "ProMetric" + name, UiTheme.CardElevated);
            SetRect(card.rectTransform, xMin, yMin, xMax, yMax);
            var rail = CreateImage(card.transform, "Accent", accent);
            SetRect(rail.rectTransform, 0.06f, 0.80f, 0.32f, 0.84f);
            var cap = CreateText(card.transform, "Caption", caption, 12.5f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(cap.rectTransform, 0.06f, 0.51f, 0.90f, 0.76f);
            cap.color = UiTheme.TextMuted;
            var value = CreateText(card.transform, "Value", "--", 29f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(value.rectTransform, 0.06f, 0.10f, 0.90f, 0.52f);
            value.color = UiTheme.TextPrimary;
            return value;
        }

        private void UpdateSummary()
        {
            var panel = FindDeep("SummaryPanel");
            if (!_summaryBuilt || panel == null || !panel.activeInHierarchy) return;

            var raw = FindText(panel, "SummaryLabel");
            if (raw == null) return;
            string text = StripTags(raw.text);

            int hits = ParseInt(text, @"Aciertos:\s*(\d+)");
            int errors = ParseInt(text, @"Errores:\s*(\d+)");
            float avg = ParseFloat(text, @"Promedio:\s*([0-9.,]+)s");
            float best = ParseFloat(text, @"Mejor:\s*([0-9.,]+)s");
            int total = hits + errors;
            float accuracy = total > 0 ? hits * 100f / total : 0f;

            if (_resultAccuracy != null) _resultAccuracy.text = total > 0 ? accuracy.ToString("F0") + "%" : "--%";
            if (_resultHits != null) _resultHits.text = hits.ToString();
            if (_resultErrors != null) _resultErrors.text = errors.ToString();
            if (_resultAverage != null) _resultAverage.text = avg > 0f ? avg.ToString("F2") + " s" : "--";
            if (_resultBest != null) _resultBest.text = best > 0f ? best.ToString("F2") + " s" : "--";
        }

        // ------------------------------------------------------------------
        // SOLO
        // ------------------------------------------------------------------

        private void SkinSoloSelectorIfNeeded()
        {
            if (_soloSelectorSkinned) return;
            var root = FindDeep("SoloExerciseSelection");
            if (root == null) return;
            _soloSelectorSkinned = true;

            var eyebrow = FindText(root, "Eyebrow");
            if (eyebrow != null)
            {
                eyebrow.text = "SOLO  /  1 TELÉFONO";
                eyebrow.color = UiTheme.Accent;
                SetRect(eyebrow.rectTransform, 0.195f, 0.915f, 0.82f, 0.95f);
            }
            var title = FindText(root, "Title");
            if (title != null)
            {
                title.text = "Elegí tu entrenamiento";
                title.fontSizeMax = 36f;
                SetRect(title.rectTransform, 0.055f, 0.835f, 0.94f, 0.895f);
            }
            var subtitle = FindText(root, "Subtitle");
            if (subtitle != null)
            {
                subtitle.text = "La cámara convierte tu espacio en zonas de reacción.";
                subtitle.fontSizeMax = 17f;
                SetRect(subtitle.rectTransform, 0.055f, 0.785f, 0.94f, 0.83f);
            }

            ExerciseMode[] modes =
            {
                ExerciseMode.Reaction, ExerciseMode.AllSame, ExerciseMode.Colors,
                ExerciseMode.Decision, ExerciseMode.CognitiveFake, ExerciseMode.Football
            };
            float[] bottoms = { 0.665f, 0.550f, 0.435f, 0.320f, 0.205f, 0.090f };
            Color[] accents =
            {
                UiTheme.Accent, UiTheme.Info, new Color32(0xFF,0x95,0x35,0xFF),
                new Color32(0xF3,0xD3,0x44,0xFF), new Color32(0xB9,0x67,0xFF,0xFF),
                new Color32(0x38,0xD6,0xA1,0xFF)
            };

            for (int i = 0; i < modes.Length; i++)
            {
                var button = FindIn(root, "SoloExercise_" + modes[i]);
                if (button == null) continue;
                SetRect(button.GetComponent<RectTransform>(), 0.055f, bottoms[i], 0.945f, bottoms[i] + 0.100f);
                var image = button.GetComponent<Image>();
                if (image != null) image.color = UiTheme.CardElevated;
                var accent = FindIn(button, "Accent");
                if (accent != null)
                {
                    var ai = accent.GetComponent<Image>();
                    if (ai != null) ai.color = accents[i];
                    SetRect(accent.GetComponent<RectTransform>(), 0.018f, 0.18f, 0.028f, 0.82f);
                }
                var number = FindText(button, "Number");
                if (number != null)
                {
                    SetRect(number.rectTransform, 0.055f, 0.25f, 0.14f, 0.75f);
                    number.color = accents[i];
                    number.alignment = TextAlignmentOptions.Center;
                }
                var heading = FindText(button, "Heading");
                if (heading != null)
                {
                    SetRect(heading.rectTransform, 0.18f, 0.48f, 0.78f, 0.80f);
                    heading.fontSizeMax = 20f;
                }
                var detail = FindText(button, "Detail");
                if (detail != null)
                {
                    SetRect(detail.rectTransform, 0.18f, 0.12f, 0.90f, 0.48f);
                    detail.fontSizeMax = 14f;
                    detail.fontSizeMin = 11f;
                }
            }
        }

        private void SkinSoloOptionsIfNeeded()
        {
            if (_soloOptionsSkinned) return;
            var root = FindDeep("SoloOptionsPanel");
            if (root == null) return;
            _soloOptionsSkinned = true;

            var eyebrow = FindText(root, "Eyebrow");
            if (eyebrow != null)
            {
                eyebrow.text = "DEPORTIVO PRO  /  SOLO";
                eyebrow.color = UiTheme.Accent;
            }
            var ruleCard = FindIn(root, "RuleCard");
            if (ruleCard != null)
            {
                var image = ruleCard.GetComponent<Image>();
                if (image != null) image.color = new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.10f);
            }
            var start = FindButton(root, "SoloStartCamera");
            if (start != null) StyleButton(start, UiTheme.Accent, UiTheme.Background, TextAlignmentOptions.Center, 20f);
        }

        private void UpdateCameraSurfaces()
        {
            var soloCamera = FindDeep("SoloCameraPanel");
            if (soloCamera != null && soloCamera.activeInHierarchy)
            {
                var top = FindIn(soloCamera, "TopCard");
                if (top != null)
                {
                    var image = top.GetComponent<Image>();
                    if (image != null) image.color = new Color(0.02f, 0.045f, 0.035f, 0.94f);
                }
                var primary = FindButton(soloCamera, "SoloPrimaryButton");
                if (primary != null) StyleButton(primary, UiTheme.Accent, UiTheme.Background, TextAlignmentOptions.Center, 20f);
            }
        }

        private void UpdateStationWait()
        {
            var panel = FindDeep("StationWaitPanel");
            if (panel == null || !panel.activeInHierarchy) return;
            var radar = FindText(panel, "StationRadar");
            if (radar != null) radar.color = UiTheme.Accent;
            var badge = FindText(panel, "StationBadge");
            if (badge != null && !badge.text.Contains("ATENCIÓN")) badge.color = UiTheme.Accent;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

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

        private void SetPanelBackground(GameObject panel)
        {
            var image = panel.GetComponent<Image>();
            if (image != null) image.color = UiTheme.Background;
        }

        private void StyleCard(GameObject card, float xMin, float yMin, float xMax, float yMax)
        {
            if (card == null) return;
            SetRect(card.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            var image = card.GetComponent<Image>();
            if (image != null)
            {
                image.color = UiTheme.CardElevated;
                ApplyRounded(image);
            }
        }

        private void LayoutSmallButton(Button button, float xMin, float yMin, float xMax, float yMax, Color color)
        {
            if (button == null) return;
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            StyleButton(button, color, color == UiTheme.Accent ? UiTheme.Background : UiTheme.TextPrimary, TextAlignmentOptions.Center, 26f);
        }

        private void StyleButton(Button button, Color background, Color textColor, TextAlignmentOptions alignment, float fontSize)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = background;
                ApplyRounded(image);
            }
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = textColor;
                label.fontStyle = FontStyles.Bold;
                label.alignment = alignment;
                label.enableAutoSizing = true;
                label.fontSizeMax = fontSize;
                label.fontSizeMin = Mathf.Max(11f, fontSize * 0.62f);
                label.rectTransform.offsetMin = new Vector2(18f, 5f);
                label.rectTransform.offsetMax = new Vector2(-18f, -5f);
            }
        }

        private Button CreateButton(Transform parent, string name, string labelText, Color color, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            ApplyRounded(image);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            go.AddComponent<ButtonPressScale>();
            var label = CreateText(go.transform, "Label", labelText, 17f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.color = textColor;
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
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = UiTheme.TextPrimary;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10f, size * 0.56f);
            text.fontSizeMax = size;
            return text;
        }

        private static string StripTags(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : Regex.Replace(value, "<.*?>", string.Empty);
        }

        private static int ParseInt(string source, string pattern)
        {
            Match m = Regex.Match(source ?? string.Empty, pattern, RegexOptions.IgnoreCase);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
        }

        private static float ParseFloat(string source, string pattern)
        {
            Match m = Regex.Match(source ?? string.Empty, pattern, RegexOptions.IgnoreCase);
            if (!m.Success) return 0f;
            string normalized = m.Groups[1].Value.Replace(',', '.');
            return float.TryParse(normalized, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 0f;
        }

        private static Color ColorForStation(StationColor color)
        {
            switch (color)
            {
                case StationColor.Green: return new Color32(0x76, 0xE8, 0x00, 0xFF);
                case StationColor.Red: return new Color32(0xEF, 0x53, 0x50, 0xFF);
                case StationColor.Blue: return new Color32(0x4C, 0x8D, 0xFF, 0xFF);
                case StationColor.Yellow: return new Color32(0xFF, 0xC8, 0x3D, 0xFF);
                default: return UiTheme.Accent;
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static void HideIfFound(GameObject root, string name)
        {
            var go = FindIn(root, name);
            if (go != null) go.SetActive(false);
        }

        private GameObject FindDeep(string objectName)
        {
            if (_canvas == null) return null;
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true))
                if (t.name == objectName) return t.gameObject;
            return null;
        }

        private static GameObject FindIn(GameObject root, string objectName)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == objectName) return t.gameObject;
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

        private Button FindButton(string objectName)
        {
            var go = FindDeep(objectName);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static void SetRect(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            if (rect == null) return;
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
