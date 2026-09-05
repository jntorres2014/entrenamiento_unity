using DateTimeOffset = System.DateTimeOffset;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    public sealed class TrainingProgressController : MonoBehaviour
    {
        private static TrainingProgressController _instance;
        private Canvas _canvas;
        private Sprite _roundedSprite;
        private Button _homeProgressButton;
        private GameObject _root;
        private GameObject _summaryView;
        private GameObject _sessionsView;
        private Button _summaryTab;
        private Button _sessionsTab;
        private TMP_Text _accuracyValue;
        private TMP_Text _sessionsValue;
        private TMP_Text _miniAccuracy;
        private TMP_Text _bestValue;
        private readonly List<Image> _bars = new List<Image>();
        private readonly List<TMP_Text> _recentRows = new List<TMP_Text>();
        private readonly List<TMP_Text> _sessionRows = new List<TMP_Text>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<TrainingProgressController>() == null)
                {
                    canvas.gameObject.AddComponent<TrainingProgressController>();
                    break;
                }
            }
        }

        public static void ShowProgress()
        {
            if (_instance != null) _instance.OpenProgress();
        }

        private void Awake()
        {
            _instance = this;
            _canvas = GetComponent<Canvas>();
            StartCoroutine(SetupWhenReady());
        }

        private IEnumerator SetupWhenReady()
        {
            for (int i = 0; i < 180; i++)
            {
                var role = FindDeep("RolePanel");
                if (role != null && FindDeep("DeportivoProHomeVisuals") != null)
                {
                    CaptureRoundedSprite();
                    AddHomeButton(role);
                    break;
                }
                yield return null;
            }
        }

        private void AddHomeButton(GameObject role)
        {
            if (_homeProgressButton != null || FindDeep("ProProgressButton") != null) return;
            _homeProgressButton = CreateButton(role.transform, "ProProgressButton", "PROGRESO", UiTheme.Surface, UiTheme.TextPrimary);
            SetRect(_homeProgressButton.GetComponent<RectTransform>(), 0.745f, 0.855f, 0.94f, 0.905f);
            var label = _homeProgressButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSizeMax = 14.5f;
                label.fontSizeMin = 11f;
                label.characterSpacing = 1f;
            }
            _homeProgressButton.onClick.AddListener(OpenProgress);
            _homeProgressButton.transform.SetAsLastSibling();
        }

        private void OpenProgress()
        {
            EnsureRoot();
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            ShowSummary();
        }

        private void EnsureRoot()
        {
            if (_root != null) return;
            CaptureRoundedSprite();

            _root = new GameObject("TrainingProgressPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _root.transform.SetParent(_canvas.transform, false);
            Stretch(_root.GetComponent<RectTransform>());
            var bg = _root.GetComponent<Image>();
            bg.color = UiTheme.Background;
            bg.raycastTarget = true;

            var glow = CreateImage(_root.transform, "ProgressGlow", new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.05f));
            SetRect(glow.rectTransform, 0.64f, 0.68f, 1f, 1f);

            var back = CreateButton(_root.transform, "ProgressBackButton", "←", UiTheme.Surface, UiTheme.TextPrimary);
            SetRect(back.GetComponent<RectTransform>(), 0.055f, 0.905f, 0.165f, 0.962f);
            back.onClick.AddListener(CloseProgress);

            var logo = CreateRawImage(_root.transform, "ProgressLogo", TransparentBrandLogo.Texture);
            SetRect(logo.rectTransform, 0.80f, 0.89f, 0.94f, 0.97f);

            var kicker = CreateText(_root.transform, "Kicker", "DEPORTIVO PRO", 13f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(kicker.rectTransform, 0.195f, 0.925f, 0.62f, 0.96f);
            kicker.color = UiTheme.Accent;
            kicker.characterSpacing = 1.8f;

            var title = CreateText(_root.transform, "Title", "Mi progreso", 38f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.055f, 0.835f, 0.76f, 0.90f);

            _summaryTab = CreateButton(_root.transform, "ProgressSummaryTab", "RESUMEN", UiTheme.Accent, UiTheme.Background);
            SetRect(_summaryTab.GetComponent<RectTransform>(), 0.055f, 0.775f, 0.35f, 0.825f);
            _summaryTab.onClick.AddListener(ShowSummary);

            _sessionsTab = CreateButton(_root.transform, "ProgressSessionsTab", "SESIONES", UiTheme.Surface, UiTheme.TextSecondary);
            SetRect(_sessionsTab.GetComponent<RectTransform>(), 0.37f, 0.775f, 0.665f, 0.825f);
            _sessionsTab.onClick.AddListener(ShowSessions);

            BuildSummaryView();
            BuildSessionsView();
            _root.SetActive(false);
        }

        private void BuildSummaryView()
        {
            _summaryView = CreateContainer(_root.transform, "ProgressSummaryView");
            SetRect(_summaryView.GetComponent<RectTransform>(), 0.055f, 0.055f, 0.945f, 0.755f);

            var accuracyCard = CreateImage(_summaryView.transform, "AccuracyCard", UiTheme.CardElevated);
            SetRect(accuracyCard.rectTransform, 0f, 0.66f, 1f, 0.98f);
            var cap = CreateText(accuracyCard.transform, "Caption", "PRECISIÓN PROMEDIO", 13f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(cap.rectTransform, 0.055f, 0.76f, 0.65f, 0.91f);
            cap.color = UiTheme.TextMuted;
            _accuracyValue = CreateText(accuracyCard.transform, "Value", "--%", 54f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(_accuracyValue.rectTransform, 0.055f, 0.28f, 0.48f, 0.75f);
            _accuracyValue.color = UiTheme.Accent;

            var barsRoot = CreateContainer(accuracyCard.transform, "Bars");
            SetRect(barsRoot.GetComponent<RectTransform>(), 0.53f, 0.20f, 0.94f, 0.78f);
            for (int i = 0; i < 7; i++)
            {
                float x0 = i / 7f + 0.02f;
                float x1 = (i + 1) / 7f - 0.02f;
                var back = CreateImage(barsRoot.transform, "BarBack" + i, UiTheme.Surface);
                SetRect(back.rectTransform, x0, 0f, x1, 1f);
                var fill = CreateImage(back.transform, "Fill", UiTheme.Accent);
                SetRect(fill.rectTransform, 0f, 0f, 1f, 0.15f);
                _bars.Add(fill);
            }

            _sessionsValue = CreateStatCard(_summaryView.transform, "Sessions", "SESIONES", 0f, 0.47f, 0.31f, 0.63f, UiTheme.Accent);
            _miniAccuracy = CreateStatCard(_summaryView.transform, "PrecisionMini", "PRECISIÓN", 0.345f, 0.47f, 0.655f, 0.63f, UiTheme.Positive);
            _bestValue = CreateStatCard(_summaryView.transform, "Best", "MEJOR TIEMPO", 0.69f, 0.47f, 1f, 0.63f, UiTheme.Info);

            var recentTitle = CreateText(_summaryView.transform, "RecentTitle", "ÚLTIMAS SESIONES", 15f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(recentTitle.rectTransform, 0f, 0.395f, 0.55f, 0.445f);
            recentTitle.color = UiTheme.TextSecondary;

            for (int i = 0; i < 3; i++)
            {
                var row = CreateImage(_summaryView.transform, "RecentRow" + i, UiTheme.CardElevated);
                float yMax = 0.37f - i * 0.12f;
                SetRect(row.rectTransform, 0f, yMax - 0.095f, 1f, yMax);
                var text = CreateText(row.transform, "Text", "", 14f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
                SetRect(text.rectTransform, 0.04f, 0.12f, 0.96f, 0.88f);
                _recentRows.Add(text);
            }
        }

        private TMP_Text CreateStatCard(Transform parent, string name, string caption, float xMin, float yMin, float xMax, float yMax, Color accent)
        {
            var card = CreateImage(parent, "Stat" + name, UiTheme.CardElevated);
            SetRect(card.rectTransform, xMin, yMin, xMax, yMax);
            var rail = CreateImage(card.transform, "Accent", accent);
            SetRect(rail.rectTransform, 0.07f, 0.78f, 0.36f, 0.82f);
            var cap = CreateText(card.transform, "Caption", caption, 10.5f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(cap.rectTransform, 0.07f, 0.50f, 0.94f, 0.74f);
            cap.color = UiTheme.TextMuted;
            var value = CreateText(card.transform, "Value", "--", 24f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(value.rectTransform, 0.07f, 0.10f, 0.94f, 0.52f);
            return value;
        }

        private void BuildSessionsView()
        {
            _sessionsView = CreateContainer(_root.transform, "ProgressSessionsView");
            SetRect(_sessionsView.GetComponent<RectTransform>(), 0.055f, 0.055f, 0.945f, 0.755f);
            var intro = CreateText(_sessionsView.transform, "Intro", "HISTORIAL RECIENTE", 15f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(intro.rectTransform, 0f, 0.93f, 0.65f, 1f);
            intro.color = UiTheme.TextSecondary;

            for (int i = 0; i < 6; i++)
            {
                var row = CreateImage(_sessionsView.transform, "SessionRow" + i, UiTheme.CardElevated);
                float yMax = 0.90f - i * 0.145f;
                SetRect(row.rectTransform, 0f, yMax - 0.115f, 1f, yMax);
                var text = CreateText(row.transform, "Text", "", 14f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
                SetRect(text.rectTransform, 0.045f, 0.10f, 0.955f, 0.90f);
                _sessionRows.Add(text);
            }
            var note = CreateText(_sessionsView.transform, "Note", "El historial se guarda en este teléfono.", 12.5f, FontStyles.Normal, TextAlignmentOptions.Center);
            SetRect(note.rectTransform, 0.10f, 0.005f, 0.90f, 0.06f);
            note.color = UiTheme.TextMuted;
        }

        private void Refresh()
        {
            var entries = TrainingHistoryStore.Load();
            float avgAccuracy = 0f;
            float best = float.MaxValue;
            foreach (var e in entries)
            {
                avgAccuracy += e.Accuracy;
                if (e.bestSeconds > 0f) best = Mathf.Min(best, e.bestSeconds);
            }
            if (entries.Count > 0) avgAccuracy /= entries.Count;

            string accuracyText = entries.Count > 0 ? avgAccuracy.ToString("F0") + "%" : "--%";
            if (_accuracyValue != null) _accuracyValue.text = accuracyText;
            if (_sessionsValue != null) _sessionsValue.text = entries.Count.ToString();
            if (_miniAccuracy != null) _miniAccuracy.text = entries.Count > 0 ? avgAccuracy.ToString("F0") + "%" : "--";
            if (_bestValue != null) _bestValue.text = best < float.MaxValue ? best.ToString("F2") + "s" : "--";

            for (int i = 0; i < _bars.Count; i++)
            {
                float accuracy = 0f;
                int index = entries.Count - 7 + i;
                if (index >= 0 && index < entries.Count) accuracy = entries[index].Accuracy / 100f;
                var fill = _bars[i].rectTransform;
                fill.anchorMax = new Vector2(1f, Mathf.Clamp(accuracy, 0.08f, 1f));
                _bars[i].color = accuracy >= 0.8f ? UiTheme.Accent : UiTheme.Info;
            }

            for (int i = 0; i < _recentRows.Count; i++)
                _recentRows[i].text = i < entries.Count ? FormatEntry(entries[i], true) : "<color=#7F8B9A>Sin sesión registrada</color>";
            for (int i = 0; i < _sessionRows.Count; i++)
                _sessionRows[i].text = i < entries.Count ? FormatEntry(entries[i], false) : "<color=#7F8B9A>—</color>";
        }

        private static string FormatEntry(TrainingHistoryEntry entry, bool compact)
        {
            string date = DateTimeOffset.FromUnixTimeSeconds(entry.unixSeconds).ToLocalTime().ToString("dd/MM  HH:mm");
            if (compact)
                return "<b>" + entry.exercise + "</b>   <color=#76E800>" + entry.Accuracy.ToString("F0") + "%</color>\n<size=82%><color=#B7C0CC>" + entry.source + "  ·  " + date + (entry.averageSeconds > 0f ? "  ·  " + entry.averageSeconds.ToString("F2") + "s" : "") + "</color></size>";
            return "<b>" + entry.exercise + "</b>   <color=#76E800>" + entry.Accuracy.ToString("F0") + "%</color>\n<size=80%><color=#B7C0CC>" + entry.source + "  ·  " + date + "  ·  " + entry.hits + " aciertos / " + entry.misses + " errores" + (entry.bestSeconds > 0f ? "  ·  mejor " + entry.bestSeconds.ToString("F2") + "s" : "") + "</color></size>";
        }

        private void ShowSummary()
        {
            if (_summaryView != null) _summaryView.SetActive(true);
            if (_sessionsView != null) _sessionsView.SetActive(false);
            SetTabStyle(_summaryTab, true);
            SetTabStyle(_sessionsTab, false);
        }

        private void ShowSessions()
        {
            if (_summaryView != null) _summaryView.SetActive(false);
            if (_sessionsView != null) _sessionsView.SetActive(true);
            SetTabStyle(_summaryTab, false);
            SetTabStyle(_sessionsTab, true);
        }

        private void SetTabStyle(Button button, bool selected)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null) image.color = selected ? UiTheme.Accent : UiTheme.Surface;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.color = selected ? UiTheme.Background : UiTheme.TextSecondary;
        }

        private void CloseProgress()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void CaptureRoundedSprite()
        {
            if (_roundedSprite != null) return;
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                var image = button.GetComponent<Image>();
                if (image != null && image.sprite != null) { _roundedSprite = image.sprite; return; }
            }
        }

        private Button CreateButton(Transform parent, string name, string value, Color bg, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = bg;
            ApplyRounded(image);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            go.AddComponent<ButtonPressScale>();
            image.color = bg;
            var label = CreateText(go.transform, "Label", value, 15f, FontStyles.Bold, TextAlignmentOptions.Center);
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

        private GameObject CreateContainer(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RawImage CreateRawImage(Transform parent, string name, Texture texture)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            var fitter = go.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = texture != null && texture.height > 0 ? texture.width / (float)texture.height : 1f;
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

        private GameObject FindDeep(string objectName)
        {
            if (_canvas == null) return null;
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true)) if (t.name == objectName) return t.gameObject;
            return null;
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

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}