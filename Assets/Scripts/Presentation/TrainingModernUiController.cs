using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Capa visual runtime para TrainingNearby.
    /// Pisa los estilos serializados de la escena, moderniza la presentación
    /// y agrega navegación Atrás sin modificar la lógica del entrenamiento.
    /// </summary>
    public sealed class TrainingModernUiController : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _rolePanel;
        private GameObject _hostConfigPanel;
        private GameObject _hostProgressPanel;
        private GameObject _stationWaitPanel;
        private GameObject _summaryPanel;
        private StationView _stationView;

        private Button _backButton;
        private TMP_Text _backLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby")
            {
                return;
            }

            Canvas canvas = null;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in canvases)
            {
                if (candidate != null && candidate.isRootCanvas)
                {
                    canvas = candidate;
                    break;
                }
            }

            if (canvas == null && canvases.Length > 0)
            {
                canvas = canvases[0];
            }

            if (canvas != null && canvas.GetComponent<TrainingModernUiController>() == null)
            {
                canvas.gameObject.AddComponent<TrainingModernUiController>();
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            CacheSceneObjects();
            CreateBackdrop();
            CreateBackButton();
            StartCoroutine(ApplyAfterBootstrapStarts());
        }

        private IEnumerator ApplyAfterBootstrapStarts()
        {
            // El Bootstrap escribe algunos textos y estados en Start().
            // Esperamos un frame y aplicamos el acabado visual después.
            yield return null;
            ApplyModernLook();
        }

        private void CacheSceneObjects()
        {
            _rolePanel = FindDeep("RolePanel");
            _hostConfigPanel = FindDeep("HostConfigPanel");
            _hostProgressPanel = FindDeep("HostProgressPanel");
            _stationWaitPanel = FindDeep("StationWaitPanel");
            _summaryPanel = FindDeep("SummaryPanel");
            _stationView = GetComponentInChildren<StationView>(true);
        }

        private GameObject FindDeep(string objectName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName)
                {
                    return t.gameObject;
                }
            }

            return null;
        }

        private void CreateBackdrop()
        {
            var go = new GameObject("ModernBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = UiTheme.Background;
            image.raycastTarget = false;

            // Línea superior de acento: da identidad sin recargar la pantalla.
            var accent = new GameObject("TopAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accent.transform.SetParent(go.transform, false);
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0f, 8f);
            var accentImage = accent.GetComponent<Image>();
            accentImage.color = UiTheme.Accent;
            accentImage.raycastTarget = false;
        }

        private void CreateBackButton()
        {
            var go = new GameObject("ModernBackButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            go.transform.SetAsLastSibling();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(170f, 64f);

            var image = go.GetComponent<Image>();
            image.color = UiTheme.CardElevated;

            // Reutiliza el sprite redondeado de los botones existentes si lo hay.
            foreach (var existing in GetComponentsInChildren<Button>(true))
            {
                var existingImage = existing.GetComponent<Image>();
                if (existingImage != null && existingImage.sprite != null)
                {
                    image.sprite = existingImage.sprite;
                    image.type = existingImage.type;
                    break;
                }
            }

            _backButton = go.GetComponent<Button>();
            _backButton.targetGraphic = image;
            _backButton.onClick.AddListener(GoBack);

            var colors = _backButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1.08f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = UiTheme.Disabled;
            colors.fadeDuration = 0.08f;
            _backButton.colors = colors;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);

            _backLabel = labelGo.GetComponent<TextMeshProUGUI>();
            _backLabel.text = "←  ATRÁS";
            _backLabel.color = UiTheme.TextPrimary;
            _backLabel.alignment = TextAlignmentOptions.Center;
            _backLabel.fontStyle = FontStyles.Bold;
            _backLabel.enableAutoSizing = true;
            _backLabel.fontSizeMin = 16f;
            _backLabel.fontSizeMax = 28f;
            _backLabel.raycastTarget = false;

            go.AddComponent<ButtonPressScale>();
            go.SetActive(false);
        }

        private void ApplyModernLook()
        {
            StylePanel(_rolePanel);
            StylePanel(_hostConfigPanel);
            StylePanel(_hostProgressPanel);
            StylePanel(_stationWaitPanel);
            StylePanel(_summaryPanel);

            PolishRoleScreen();
            PolishStaticTitles();
        }

        private static void StylePanel(GameObject panel)
        {
            if (panel == null) return;

            var rootImage = panel.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = UiTheme.Background;
            }

            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null) continue;
                text.color = UiTheme.TextPrimary;

                if (text.fontSize > 26f)
                {
                    text.fontStyle |= FontStyles.Bold;
                }
            }

            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;

                string n = button.name.ToLowerInvariant();
                if (n.Contains("start") || n.Contains("hostrole") || n.Contains("restart"))
                {
                    TrainingUiStyler.StylePrimary(button);
                }
                else if (n.Contains("stationrole"))
                {
                    TrainingUiStyler.StyleInfo(button);
                }
                else if (n.Contains("accept"))
                {
                    TrainingUiStyler.StylePositive(button);
                }
                else if (n.Contains("reject"))
                {
                    TrainingUiStyler.StyleDanger(button);
                }
                else
                {
                    TrainingUiStyler.StyleSecondary(button);
                }

                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.fontStyle |= FontStyles.Bold;
                }
            }

            // Las tarjetas informativas dejan de usar marrones/grises viejos.
            foreach (var image in panel.GetComponentsInChildren<Image>(true))
            {
                if (image == null || image.GetComponent<Button>() != null || image.gameObject == panel)
                {
                    continue;
                }

                string n = image.name.ToLowerInvariant();
                if (n.Contains("request") || n.Contains("card") || n.Contains("connected") || n.Contains("status"))
                {
                    image.color = UiTheme.CardElevated;
                }
            }
        }

        private void PolishRoleScreen()
        {
            if (_rolePanel == null) return;

            foreach (var button in _rolePanel.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;

                string n = button.name.ToLowerInvariant();
                if (n.Contains("host"))
                {
                    label.text = "ENTRENADOR\n<font-weight=400><size=72%>Crear y dirigir una sesión</size></font-weight>";
                }
                else if (n.Contains("station"))
                {
                    label.text = "ESTACIÓN\n<font-weight=400><size=72%>Unirme a un entrenamiento</size></font-weight>";
                }
            }
        }

        private void PolishStaticTitles()
        {
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null) continue;

                if (text.text.Trim().Equals("CONFIGURACIÓN", System.StringComparison.OrdinalIgnoreCase))
                {
                    text.text = "NUEVO ENTRENAMIENTO";
                    text.color = UiTheme.Accent;
                    text.fontStyle |= FontStyles.Bold;
                }
                else if (text.text.Trim().Equals("RESUMEN", System.StringComparison.OrdinalIgnoreCase))
                {
                    text.color = UiTheme.Accent;
                    text.fontStyle |= FontStyles.Bold;
                }
            }
        }

        private void LateUpdate()
        {
            UpdateBackButton();
            PolishDynamicLabels();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_backButton != null && _backButton.gameObject.activeSelf)
                {
                    GoBack();
                }
                else
                {
                    Application.Quit();
                }
            }
        }

        private void UpdateBackButton()
        {
            if (_backButton == null) return;

            bool onRoleScreen = _rolePanel != null && _rolePanel.activeInHierarchy;
            bool stationColorVisible = _stationView != null && _stationView.gameObject.activeInHierarchy;
            bool shouldShow = !onRoleScreen && !stationColorVisible;

            if (_backButton.gameObject.activeSelf != shouldShow)
            {
                _backButton.gameObject.SetActive(shouldShow);
                if (shouldShow)
                {
                    _backButton.transform.SetAsLastSibling();
                }
            }
        }

        private void PolishDynamicLabels()
        {
            if (_hostConfigPanel == null || !_hostConfigPanel.activeInHierarchy) return;

            foreach (var text in _hostConfigPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null || string.IsNullOrWhiteSpace(text.text)) continue;

                string value = text.text;

                if (value.StartsWith("Rondas: "))
                {
                    string number = value.Substring("Rondas: ".Length);
                    text.text = $"{number}  RONDAS";
                }
                else if (value.StartsWith("Modo: "))
                {
                    text.text = value.Substring("Modo: ".Length);
                }
                else if (value.StartsWith("Límite por ronda: "))
                {
                    text.text = "TIEMPO  •  " + value.Substring("Límite por ronda: ".Length);
                }
                else if (value.StartsWith("Colores: "))
                {
                    text.text = "COLORES  •  " + value.Substring("Colores: ".Length);
                }
                else if (value == "Participo como estación: SÍ")
                {
                    text.text = "●  ESTE TELÉFONO PARTICIPA";
                    text.color = UiTheme.Positive;
                }
                else if (value == "Participo como estación: NO")
                {
                    text.text = "○  ESTE TELÉFONO NO PARTICIPA";
                    text.color = UiTheme.TextSecondary;
                }
                else if (value == "Esperando estaciones...")
                {
                    text.text = "Buscando jugadores cercanos…";
                    text.color = UiTheme.TextSecondary;
                }
                else if (value.StartsWith("Estaciones conectadas: "))
                {
                    text.text = value.Replace("Estaciones conectadas:", "JUGADORES CONECTADOS  •");
                    text.color = UiTheme.Positive;
                }
            }
        }

        private void GoBack()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
