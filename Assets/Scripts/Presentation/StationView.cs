using System;
using System.Reflection;
using Entrenamiento.Core.Models;
using Entrenamiento.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Pantalla táctil del pod. Además del color, muestra la consigna específica
    /// del preset sincronizado por el host.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class StationView : MonoBehaviour, IPointerClickHandler
    {
        public event Action OnTapped;

        private Image _backgroundImage;
        private TMP_Text _promptLabel;
        private TMP_Text _actionLabel;

        private void Awake()
        {
            EnsureImage();
            EnsurePrompt();
        }

        private void EnsureImage()
        {
            if (_backgroundImage == null)
            {
                _backgroundImage = GetComponent<Image>();
            }
        }

        private void EnsurePrompt()
        {
            if (_promptLabel == null)
            {
                foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.name == "TapHint")
                    {
                        _promptLabel = text;
                        break;
                    }
                }
            }

            if (_promptLabel == null)
            {
                var promptGo = new GameObject("TapHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                promptGo.transform.SetParent(transform, false);
                _promptLabel = promptGo.GetComponent<TextMeshProUGUI>();
            }

            ConfigureText(_promptLabel, 120f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_promptLabel.rectTransform, 0.08f, 0.405f, 0.92f, 0.615f);

            if (_actionLabel == null)
            {
                foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.name == "ActionHint")
                    {
                        _actionLabel = text;
                        break;
                    }
                }
            }

            if (_actionLabel == null)
            {
                var actionGo = new GameObject("ActionHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                actionGo.transform.SetParent(transform, false);
                _actionLabel = actionGo.GetComponent<TextMeshProUGUI>();
            }

            ConfigureText(_actionLabel, 34f, FontStyles.Bold, TextAlignmentOptions.Center);
            _actionLabel.characterSpacing = 2f;
            _actionLabel.color = new Color(1f, 1f, 1f, 0.88f);
            SetRect(_actionLabel.rectTransform, 0.10f, 0.325f, 0.90f, 0.405f);
        }

        public void SetColor(StationColor color)
        {
            EnsureImage();
            _backgroundImage.color = StationColorPalette.ToUnityColor(color);

            bool isGo = true;
            TryReadCurrentGoFlag(out isGo);
            SetPromptForExercise(color, isGo);
        }

        public void SetPrompt(bool isGo)
        {
            SetPromptForExercise(StationColor.None, isGo);
        }

        private void SetPromptForExercise(StationColor color, bool isGo)
        {
            EnsurePrompt();

            string prompt;
            string action;
            bool pulse = isGo;

            switch (ExerciseSelection.Current)
            {
                case ExerciseMode.AllSame:
                    prompt = "¡TOCÁ!";
                    action = "APAGÁ ESTE POD";
                    pulse = true;
                    break;

                case ExerciseMode.Colors:
                    prompt = "COLOR";
                    action = "BUSCÁ EL COLOR INDICADO";
                    pulse = false;
                    break;

                case ExerciseMode.Decision:
                    prompt = DirectionFor(color);
                    action = "EJECUTÁ LA DIRECCIÓN Y TOCÁ";
                    pulse = true;
                    break;

                case ExerciseMode.CognitiveFake:
                    prompt = DirectionFor(color);
                    action = "ATENTO: LA CONSIGNA PUEDE CAMBIAR";
                    pulse = true;
                    break;

                case ExerciseMode.Football:
                    if (color == StationColor.Green)
                    {
                        prompt = "DERECHO";
                        action = "TOCÁ CON PIE DERECHO";
                        pulse = true;
                    }
                    else if (color == StationColor.Blue)
                    {
                        prompt = "IZQUIERDO";
                        action = "TOCÁ CON PIE IZQUIERDO";
                        pulse = true;
                    }
                    else
                    {
                        prompt = "QUIETO";
                        action = "ROJO = NO TOCAR";
                        pulse = false;
                    }
                    break;

                default:
                    prompt = isGo ? "¡TOCÁ!" : "QUIETO";
                    action = isGo ? "REACCIONÁ AHORA" : "NO TOQUES LA PANTALLA";
                    pulse = isGo;
                    break;
            }

            _promptLabel.text = prompt;
            _actionLabel.text = action;

            var pulseScale = _promptLabel.GetComponent<PulseScale>();
            if (pulseScale != null)
            {
                pulseScale.enabled = pulse;
                if (!pulse) _promptLabel.transform.localScale = Vector3.one;
            }
        }

        private static string DirectionFor(StationColor color)
        {
            switch (color)
            {
                case StationColor.Green: return "AVANZÁ";
                case StationColor.Red: return "RETROCEDÉ";
                case StationColor.Blue: return "IZQUIERDA";
                case StationColor.Yellow: return "DERECHA";
                default: return "¡MOVETE!";
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnTapped?.Invoke();
        }

        private static bool TryReadCurrentGoFlag(out bool isGo)
        {
            isGo = true;

            var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || behaviour.GetType().Name != "TrainingNearbyBootstrap") continue;

                var field = behaviour.GetType().GetField("_localAgent", BindingFlags.Instance | BindingFlags.NonPublic);
                object agent = field?.GetValue(behaviour);
                if (agent == null) continue;

                var property = agent.GetType().GetProperty("LastArmWasGo", BindingFlags.Instance | BindingFlags.Public);
                if (property == null || property.PropertyType != typeof(bool)) continue;

                object value = property.GetValue(agent);
                if (value is bool flag)
                {
                    isGo = flag;
                    return true;
                }
            }

            return false;
        }

        private static void ConfigureText(TMP_Text text, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(22f, size * 0.48f);
            text.fontSizeMax = size;
        }

        private static void SetRect(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
