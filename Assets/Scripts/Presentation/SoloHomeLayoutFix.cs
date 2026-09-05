using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
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

            BuildProVisuals();
            _ready = true;
            EnforceLayout();
        }

        private void BuildProVisuals()
        {
            if (_visualRoot != null) return;

            _visualRoot = new GameObject("DeportivoProHomeVisuals", typeof(RectTransform), typeof(CanvasGroup));
            _visualRoot.transform.SetParent(_rolePanel.transform, false);
            _visualRoot.transform.SetAsFirstSibling();
            Stretch(_visualRoot.GetComponent<RectTransform>());
            var group = _visualRoot.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            var logo = CreateRawImage(_visualRoot.transform, "ProBrandLogo", TransparentBrandLogo.Texture);
            SetRect(logo.rectTransform, 0.055f, 0.855f, 0.225f, 0.975f);

            var kicker = CreateText(_visualRoot.transform, "ProBrandKicker", "ENTRENAMIENTO INTELIGENTE", 15.5f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(kicker.rectTransform, 0.245f, 0.920f, 0.78f, 0.955f);
            kicker.color = UiTheme.TextSecondary;
            kicker.characterSpacing = 2f;

            var status = CreateText(_visualRoot.transform, "ProReady", "●  LISTO", 14f, FontStyles.Bold, TextAlignmentOptions.Right);
            SetRect(status.rectTransform, 0.73f, 0.920f, 0.94f, 0.955f);
            status.color = UiTheme.Accent;

            var hero = CreateImage(_visualRoot.transform, "ProHero", UiTheme.CardElevated);
            SetRect(hero.rectTransform, 0.055f, 0.535f, 0.945f, 0.845f);
            var glow = CreateImage(hero.transform, "ProHeroGlow", new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.10f));
            SetRect(glow.rectTransform, 0.63f, 0.06f, 0.98f, 0.94f);
            var ring1 = CreateImage(hero.transform, "ProRing1", new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.16f));
            SetRect(ring1.rectTransform, 0.72f, 0.40f, 0.91f, 0.82f);
            var ring2 = CreateImage(hero.transform, "ProRing2", new Color(UiTheme.Background.r, UiTheme.Background.g, UiTheme.Background.b, 0.92f));
            SetRect(ring2.rectTransform, 0.755f, 0.47f, 0.875f, 0.75f);
            var ring3 = CreateImage(hero.transform, "ProRing3", UiTheme.Accent);
            SetRect(ring3.rectTransform, 0.792f, 0.545f, 0.838f, 0.665f);

            var heroTitle = CreateText(hero.transform, "ProHeroTitle", "ENTRENÁ\n<color=#76E800>SIN LÍMITES</color>", 45f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(heroTitle.rectTransform, 0.055f, 0.49f, 0.68f, 0.88f);
            heroTitle.enableWordWrapping = true;
            var heroCopy = CreateText(hero.transform, "ProHeroCopy", "Reflejos más rápidos. Decisiones más inteligentes. Mejores resultados.", 18f, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(heroCopy.rectTransform, 0.055f, 0.23f, 0.86f, 0.48f);
            heroCopy.color = UiTheme.TextSecondary;
            heroCopy.enableWordWrapping = true;
            var chips = CreateText(hero.transform, "ProHeroChips", "REACCIÓN  ·  VELOCIDAD  ·  DECISIÓN", 13.5f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(chips.rectTransform, 0.055f, 0.09f, 0.82f, 0.20f);
            chips.color = UiTheme.Accent;
            chips.characterSpacing = 1.2f;

            for (int i = 0; i < 3; i++)
            {
                var speed = CreateImage(hero.transform, "SpeedLine" + i, new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.70f - i * 0.16f));
                float y = 0.18f - i * 0.045f;
                SetRect(speed.rectTransform, 0.70f + i * 0.035f, y, 0.95f, y + 0.012f);
            }

            CreateAccessCard(_visualRoot.transform, "ProHostCard", 0.055f, 0.265f, 0.485f, 0.405f, UiTheme.Accent);
            CreateAccessCard(_visualRoot.transform, "ProStationCard", 0.515f, 0.265f, 0.945f, 0.405f, UiTheme.Info);
            var quote = CreateText(_visualRoot.transform, "ProQuote", "“DISCIPLINA HOY. GRANDES RESULTADOS MAÑANA.”", 13f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(quote.rectTransform, 0.08f, 0.035f, 0.92f, 0.085f);
            quote.color = UiTheme.TextMuted;
            quote.characterSpacing = 1.2f;
        }

        private void CreateAccessCard(Transform parent, string name, float xMin, float yMin, float xMax, float yMax, Color accent)
        {
            var card = CreateImage(parent, name, UiTheme.CardElevated);
            SetRect(card.rectTransform, xMin, yMin, xMax, yMax);
            var rail = CreateImage(card.transform, "Accent", accent);
            SetRect(rail.rectTransform, 0.06f, 0.86f, 0.34f, 0.89f);
        }

        private void EnforceLayout()
        {
            if (!_ready || _rolePanel == null || !_rolePanel.activeInHierarchy) return;
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(true);
                _visualRoot.transform.SetAsFirstSibling();
            }

            StylePrimary(_solo, 0.075f, 0.435f, 0.925f, 0.505f, "EMPEZAR SOLO   →", "1 TELÉFONO  ·  6 EJERCICIOS  ·  SIN ARCORE");
            StyleCardButton(_host, 0.075f, 0.282f, 0.465f, 0.385f, "CON PODS", "Crear y dirigir una sesión");
            StyleCardButton(_station, 0.535f, 0.282f, 0.925f, 0.385f, "ESTACIÓN", "Usar este teléfono como pod");
            StyleUtility(_camera, 0.075f, 0.145f, 0.465f, 0.225f, "CÁMARA", "Calibración libre");
            StyleUtility(_ar, 0.535f, 0.145f, 0.925f, 0.225f, "AR TRAINING", "Equipos compatibles");

            _solo.transform.SetAsLastSibling();
            _host.transform.SetAsLastSibling();
            _station.transform.SetAsLastSibling();
            _camera.transform.SetAsLastSibling();
            _ar.transform.SetAsLastSibling();
        }

        private void StylePrimary(Button button, float xMin, float yMin, float xMax, float yMax, string title, string detail)
        {
            if (button == null) return;
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            var image = button.GetComponent<Image>();
            if (image != null) { image.color = UiTheme.Accent; ApplyRounded(image); }
            SetButtonLabel(button, title + "\n<size=58%><color=#163000>" + detail + "</color></size>", new Color32(0x08, 0x16, 0x08, 0xFF), 22f, TextAlignmentOptions.MidlineLeft, 24f);
        }

        private void StyleCardButton(Button button, float xMin, float yMin, float xMax, float yMax, string title, string detail)
        {
            if (button == null) return;
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            var image = button.GetComponent<Image>();
            if (image != null) { image.color = new Color(1f, 1f, 1f, 0.001f); ApplyRounded(image); }
            SetButtonLabel(button, title + "\n<size=61%><color=#B7C0CC>" + detail + "</color></size>", UiTheme.TextPrimary, 21f, TextAlignmentOptions.BottomLeft, 8f);
        }

        private void StyleUtility(Button button, float xMin, float yMin, float xMax, float yMax, string title, string detail)
        {
            if (button == null) return;
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
            var image = button.GetComponent<Image>();
            if (image != null) { image.color = UiTheme.Surface; ApplyRounded(image); }
            SetButtonLabel(button, title + "\n<size=60%><color=#B7C0CC>" + detail + "</color></size>", UiTheme.TextPrimary, 16.5f, TextAlignmentOptions.MidlineLeft, 16f);
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

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= EnforceLayout;
        }
    }
}
