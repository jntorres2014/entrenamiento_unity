using Entrenamiento.Core.Models;
using Entrenamiento.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Ajustes de fidelidad visual respecto del mockup Deportivo Pro.
    /// Completa iconografía de configuración, objetivo circular de sesión y
    /// indicador circular de resultados sin alterar reglas de entrenamiento.
    /// </summary>
    public sealed class DeportivoProFidelityFix : MonoBehaviour
    {
        private Canvas _canvas;
        private bool _configReady;
        private bool _liveReady;
        private bool _resultReady;
        private RawImage _liveTarget;
        private TMP_Text _liveAction;
        private TMP_Text _liveColor;
        private RawImage _resultRing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<DeportivoProFidelityFix>() == null)
                {
                    canvas.gameObject.AddComponent<DeportivoProFidelityFix>();
                    break;
                }
            }
        }

        private void Awake() => _canvas = GetComponent<Canvas>();

        private void Update()
        {
            BuildConfigIfReady();
            BuildLiveIfReady();
            BuildResultsIfReady();
            UpdateLiveState();
        }

        private void BuildConfigIfReady()
        {
            if (_configReady) return;
            var panel = FindDeep("HostConfigPanel");
            var mode = FindDeep("ModeButton");
            if (panel == null || mode == null || FindDeep("ProExerciseCaption") == null) return;
            _configReady = true;

            var icon = CreateRawImage(mode.transform, "PrototypeExerciseIcon", TextureFor(ExerciseSelection.Current));
            SetRect(icon.rectTransform, 0.055f, 0.17f, 0.19f, 0.83f);

            var label = mode.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                SetRect(label.rectTransform, 0.235f, 0.12f, 0.93f, 0.64f);
                label.alignment = TextAlignmentOptions.MidlineLeft;
            }

            var caption = FindText(mode, "ProExerciseCaption");
            if (caption != null) SetRect(caption.rectTransform, 0.235f, 0.65f, 0.62f, 0.90f);
        }

        private void BuildLiveIfReady()
        {
            if (_liveReady) return;
            var panel = FindDeep("HostProgressPanel");
            var arena = FindDeep("ProLiveArena");
            if (panel == null || arena == null) return;
            _liveReady = true;

            var glow = FindDeep("ProLiveGlow");
            if (glow != null) glow.SetActive(false);
            var core = FindDeep("ProLiveCore");
            if (core != null) core.SetActive(false);
            var legacyExercise = FindDeep("ProLiveExercise");
            if (legacyExercise != null) legacyExercise.SetActive(false);

            _liveAction = CreateText(arena.transform, "PrototypeLiveAction", "TOCÁ YA", 26f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_liveAction.rectTransform, 0.20f, 0.79f, 0.80f, 0.91f);
            _liveAction.color = UiTheme.Accent;
            _liveAction.characterSpacing = 1.4f;

            _liveTarget = CreateRawImage(arena.transform, "PrototypeLiveTarget", ProVisualAssets.Reaction);
            SetRect(_liveTarget.rectTransform, 0.19f, 0.20f, 0.81f, 0.78f);

            _liveColor = CreateText(arena.transform, "PrototypeLiveColor", "VERDE", 38f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_liveColor.rectTransform, 0.20f, 0.06f, 0.80f, 0.20f);
            _liveColor.color = UiTheme.Accent;
        }

        private void UpdateLiveState()
        {
            if (!_liveReady || _liveTarget == null) return;
            var panel = FindDeep("HostProgressPanel");
            if (panel == null || !panel.activeInHierarchy) return;

            var coordinator = ExerciseRuntimeRegistry.CurrentCoordinator;
            StationColor color = coordinator != null ? coordinator.CurrentStimulusColor : StationColor.Green;
            Color ui = ColorFor(color);
            _liveColor.color = ui;
            _liveAction.color = ui;
            _liveColor.text = NameFor(color);

            string action = "TOCÁ YA";
            if (ExerciseSelection.Current == ExerciseMode.Football)
            {
                if (color == StationColor.Red) action = "QUIETO";
                else if (color == StationColor.Green) action = "PIE DERECHO";
                else if (color == StationColor.Blue) action = "PIE IZQUIERDO";
            }
            else if (ExerciseSelection.Current == ExerciseMode.Decision)
            {
                action = DirectionFor(color);
            }
            else if (ExerciseSelection.Current == ExerciseMode.CognitiveFake)
            {
                action = "REACCIONÁ AL CAMBIO";
            }
            _liveAction.text = action;
        }

        private void BuildResultsIfReady()
        {
            if (_resultReady) return;
            var panel = FindDeep("SummaryPanel");
            var accuracy = FindDeep("Accuracy");
            if (panel == null || accuracy == null || FindDeep("ProPrecisionCard") == null) return;
            _resultReady = true;

            var precisionCard = FindDeep("ProPrecisionCard");
            var caption = FindText(precisionCard, "Caption");
            if (caption != null) SetRect(caption.rectTransform, 0.55f, 0.66f, 0.91f, 0.86f);

            var value = accuracy.GetComponent<TMP_Text>();
            if (value != null)
            {
                SetRect(value.rectTransform, 0.49f, 0.20f, 0.94f, 0.70f);
                value.fontSizeMax = 48f;
                value.alignment = TextAlignmentOptions.Center;
            }

            _resultRing = CreateRawImage(precisionCard.transform, "PrototypePrecisionRing", ProVisualAssets.Reaction);
            SetRect(_resultRing.rectTransform, 0.055f, 0.12f, 0.47f, 0.88f);

            var center = CreateText(precisionCard.transform, "PrototypePrecisionCenter", "✓", 32f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(center.rectTransform, 0.17f, 0.37f, 0.355f, 0.63f);
            center.color = UiTheme.Accent;
        }

        private static Texture TextureFor(ExerciseMode mode)
        {
            switch (mode)
            {
                case ExerciseMode.AllSame: return ProVisualAssets.AllSame;
                case ExerciseMode.Colors: return ProVisualAssets.Colors;
                case ExerciseMode.Decision: return ProVisualAssets.Decision;
                case ExerciseMode.CognitiveFake: return ProVisualAssets.Finta;
                case ExerciseMode.Football: return ProVisualAssets.Football;
                default: return ProVisualAssets.Reaction;
            }
        }

        private static Color ColorFor(StationColor color)
        {
            switch (color)
            {
                case StationColor.Red: return new Color32(0xFF, 0x55, 0x55, 0xFF);
                case StationColor.Blue: return new Color32(0x4C, 0x8D, 0xFF, 0xFF);
                case StationColor.Yellow: return new Color32(0xFF, 0xC9, 0x3F, 0xFF);
                case StationColor.Green: return UiTheme.Accent;
                default: return UiTheme.Accent;
            }
        }

        private static string NameFor(StationColor color)
        {
            switch (color)
            {
                case StationColor.Red: return "ROJO";
                case StationColor.Blue: return "AZUL";
                case StationColor.Yellow: return "AMARILLO";
                case StationColor.Green: return "VERDE";
                default: return "LISTO";
            }
        }

        private static string DirectionFor(StationColor color)
        {
            switch (color)
            {
                case StationColor.Green: return "AVANZAR";
                case StationColor.Red: return "RETROCEDER";
                case StationColor.Blue: return "IZQUIERDA";
                case StationColor.Yellow: return "DERECHA";
                default: return "DECIDÍ";
            }
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
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = UiTheme.TextPrimary;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(11f, size * 0.58f);
            text.fontSizeMax = size;
            return text;
        }

        private GameObject FindDeep(string objectName)
        {
            if (_canvas == null) return null;
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true)) if (t.name == objectName) return t.gameObject;
            return null;
        }

        private static TMP_Text FindText(GameObject root, string objectName)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true)) if (t.name == objectName) return t;
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
    }
}
