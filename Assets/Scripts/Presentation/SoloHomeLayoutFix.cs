using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Home fiel al prototipo Deportivo Pro: hero vertical deportivo,
    /// CTA SOLO dominante y accesos 2x2 en la mitad inferior.
    /// Reutiliza los botones reales para no tocar la lógica.
    /// </summary>
    public sealed class SoloHomeLayoutFix : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _rolePanel;
        private GameObject _visualRoot;
        private Button _solo;
        private Button _host;
        private Button _station;
        private Button _camera;
        private Button _ar;
        private Sprite _roundedSprite;
        private bool _ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<SoloHomeLayoutFix>() == null)
                {
                    canvas.gameObject.AddComponent<SoloHomeLayoutFix>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            StartCoroutine(BuildWhenReady());
            Canvas.willRenderCanvases += EnforceLayout;
        }

        private IEnumerator BuildWhenReady()
        {
            for (int i = 0; i < 180; i++)
            {
                _rolePanel = FindDeep("RolePanel");
                _solo = FindButton("SoloTrainingButton");
                _host = FindButton("HostRoleButton");
                _station = FindButton("StationRoleButton");
                _camera = FindButton("CameraTrainingButton");
                _ar = FindButton("ARTrainingButton");
                if (_rolePanel != null && _solo != null && _host != null && _station != null && _camera != null && _ar != null) break;
                yield return null;
            }

            if (_rolePanel == null || _solo == null) yield break;
            CaptureRoundedSprite();

            var oldVisuals = FindDeep("HomeCVisuals");
            if (oldVisuals != null) oldVisuals.SetActive(false);
            var oldStatus = FindDeep("ARStatusCard");
            if (oldStatus != null) oldStatus.SetActive(false);
            HideLegacyDirectText("Title");
            HideLegacyDirectText("Subtitle");

            BuildPrototypeVisuals();
            _ready = true;
            EnforceLayout();
        }

        private void BuildPrototypeVisuals()
        {
            if (_visualRoot != null) return;

            _visualRoot = new GameObject("DeportivoProHomeVisuals", typeof(RectTransform), typeof(CanvasGroup));
            _visualRoot.transform.SetParent(_rolePanel.transform, false);
            _visualRoot.transform.SetAsFirstSibling();
            Stretch(_visualRoot.GetComponent<RectTransform>());
            var group = _visualRoot.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            // Cabecera del prototipo.
            var logo = CreateRawImage(_visualRoot.transform, "ProBrandLogo", TransparentBrandLogo.Texture);
            SetRect(logo.rectTransform, 0.055f, 0.865f, 0.205f, 0.970f);

            var brand = CreateText(_visualRoot.transform, "ProBrandKicker", "ENTRENAMIENTO INTELIGENTE", 13.5f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(brand.rectTransform, 0.055f, 0.825f, 0.60f, 0.86f);
            brand.color = UiTheme.TextSecondary;
            brand.characterSpacing = 1.8f;

            // Arte del atleta, como en el prototipo: a la derecha y detrás del copy.
            var athleteGlow = CreateImage(_visualRoot.transform, "AthleteGlow", new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.07f));
            SetRect(athleteGlow.rectTransform, 0.47f, 0.515f, 1.02f, 0.965f);

            var athlete = CreateRawImage(_visualRoot.transform, "ProAthlete", ProVisualAssets.HeroAthlete);
            SetRect(athlete.rectTransform, 0.42f, 0.49f, 1.02f, 0.965f);
            athlete.color = Color.white;

            var title = CreateText(_visualRoot.transform, "ProHeroTitle", "ENTRENÁ\n<color=#76E800>SIN LÍMITES</color>", 46f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.055f, 0.635f, 0.66f, 0.825f);
            title.enableWordWrapping = true;

            var copy = CreateText(_visualRoot.transform, "ProHeroCopy", "REFLEJOS MÁS RÁPIDOS.\nDECISIONES MÁS INTELIGENTES.\nMEJORES RESULTADOS.", 16f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(copy.rectTransform, 0.055f, 0.525f, 0.62f, 0.635f);
            copy.color = UiTheme.TextSecondary;
            copy.lineSpacing = 5f;

            // Franja verde inferior del hero, presente en el prototipo.
            var fieldGlow = CreateImage(_visualRoot.transform, "FieldGlow", new Color(0.12f, 0.55f, 0.07f, 0.28f));
            SetRect(fieldGlow.rectTransform, 0.0f, 0.455f, 1f, 0.515f);
        }

        private void EnforceLayout()
        {
            if (!_ready || _rolePanel == null || !_rolePanel.activeInHierarchy) return;

            if (_visualRoot != null)
            {
                _visualRoot.SetActive(true);
                _visualRoot.transform.SetAsFirstSibling();
            }

            StylePrimary(_solo, 0.055f, 0.405f, 0.945f, 0.475f,
                "EMPEZAR SOLO   →", "1 TELÉFONO  ·  SIN ARCORE");

            StyleAccess(_host, 0.055f, 0.285f, 0.485f, 0.385f,
                "CON PODS", "Crear y dirigir una sesión", new Color32(0xFF, 0x93, 0x2F, 0xFF));
            StyleAccess(_station, 0.515f, 0.285f, 0.945f, 0.385f,
                "ESTACIÓN", "Usar este teléfono como pod", UiTheme.Info);
            StyleAccess(_camera, 0.055f, 0.165f, 0.485f, 0.265f,
                "CÁMARA", "Calibración libre", new Color32(0xB9, 0x67, 0xFF, 0xFF));
            StyleAccess(_ar, 0.515f, 0.165f, 0.945f, 0.265f,
                "AR TRAINING", "Equipos compatibles", new Color32(0xDF, 0xE8, 0xEC, 0xFF));

            var quote = EnsureQuote();
            quote.transform.SetAsLastSibling();

            _solo.transform.SetAsLastSibling();
            _host.transform.SetAsLastSibling();
            _station.transform.SetAsLastSibling();
            _camera.transform.SetAsLastSibling();
            _ar.transform.SetAsLastSibling();
        }

        private TMP_Text EnsureQuote()
        {
            var existing = FindDeep("ProQuote")?.GetComponent<TMP_Text>();
            if (existing != null)
            {
                SetRect(existing.rectTransform, 0.08f, 0.055f, 0.92f, 0.115f);
                return existing;
            }

            var quote = CreateText(_visualRoot.transform, "ProQuote", "“DISCIPLINA HOY, GRANDES RESULTADOS MAÑANA”", 12.5f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(quote.rectTransform, 0.08f, 0.055f, 0.92f, 0.115f);
            quote.color = UiTheme.TextMuted;
            quote.characterSpacing = 1.1f;
            return quote;
        }

        private void StylePrimary(Button button, float xMin, float yMin, float xMax, float yMax, string title, string detail)
        {
            if (button == null) return;
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            var image = button.GetComponent<Image>();
            if (image != null) { image.color = UiTheme.Accent; ApplyRounded(image); }
            SetButtonLabel(button,
                "<b>" + title + "</b>\n<size=58%><color=#1A3A08>" + detail + "</color></size>",
                UiTheme.Background, 21f, TextAlignmentOptions.MidlineLeft, 24f);
        }

        private void StyleAccess(Button button, float xMin, float yMin, float xMax, float yMax, string title, string detail, Color accent)
        {
            if (button == null) return;
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            var image = button.GetComponent<Image>();
            if (image != null) { image.color = UiTheme.CardElevated; ApplyRounded(image); }
            SetButtonLabel(button,
                "<color=#" + ColorUtility.ToHtmlStringRGB(accent) + ">●</color>  <b>" + title + "</b>\n<size=58%><color=#B7C5BF>" + detail + "</color></size>",
                UiTheme.TextPrimary, 18.5f, TextAlignmentOptions.MidlineLeft, 18f);
        }

        private void SetButtonLabel(Button button, string value, Color color, float maxSize, TextAlignmentOptions alignment, float leftPadding)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            label.text = value;
            label.color = color;
            label.fontStyle = FontStyles.Bold;
            label.alignment = alignment;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(11f, maxSize * 0.58f);
            label.fontSizeMax = maxSize;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(leftPadding, 7f);
            label.rectTransform.offsetMax = new Vector2(-14f, -7f);
        }

        private void HideLegacyDirectText(string name)
        {
            if (_rolePanel == null) return;
            foreach (Transform child in _rolePanel.transform)
            {
                if (child.name != name) continue;
                child.gameObject.SetActive(false);
            }
        }

        private void CaptureRoundedSprite()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                var image = button.GetComponent<Image>();
                if (image != null && image.sprite != null) { _roundedSprite = image.sprite; return; }
            }
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

        private static RawImage CreateRawImage(Transform parent, string name, Texture texture)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
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
            label.fontSizeMin = Mathf.Max(10f, size * 0.56f);
            label.fontSizeMax = size;
            return label;
        }

        private Button FindButton(string objectName)
        {
            var go = FindDeep(objectName);
            return go != null ? go.GetComponent<Button>() : null;
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

        private void OnDestroy() => Canvas.willRenderCanvases -= EnforceLayout;
    }
}
