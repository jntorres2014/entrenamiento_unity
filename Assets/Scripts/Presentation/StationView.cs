using System;
using System.Reflection;
using Entrenamiento.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Pantalla táctil de reacción. Muestra el color de la estación y un prompt
    /// coherente con GO / NO-GO, sin contener reglas de negocio.
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
            SetRect(_promptLabel.rectTransform, 0.10f, 0.405f, 0.90f, 0.615f);

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
            SetRect(_actionLabel.rectTransform, 0.12f, 0.335f, 0.88f, 0.405f);
        }

        public void SetColor(StationColor color)
        {
            EnsureImage();
            _backgroundImage.color = StationColorPalette.ToUnityColor(color);

            // TrainingNearbyBootstrap mantiene el dato real GO/NO-GO dentro del
            // StationAgent. Lo leemos solo para presentación, sin duplicar reglas.
            if (TryReadCurrentGoFlag(out bool isGo))
            {
                SetPrompt(isGo);
            }
            else
            {
                SetPrompt(true);
            }
        }

        public void SetPrompt(bool isGo)
        {
            EnsurePrompt();

            _promptLabel.text = isGo ? "¡TOCÁ!" : "QUIETO";
            _actionLabel.text = isGo ? "REACCIONÁ AHORA" : "NO TOQUES LA PANTALLA";

            var pulse = _promptLabel.GetComponent<PulseScale>();
            if (pulse != null)
            {
                pulse.enabled = isGo;
                if (!isGo) _promptLabel.transform.localScale = Vector3.one;
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
