using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Aplica el estilo visual principal sin depender de que cada color quede
    /// serializado en la escena. Esto permite evolucionar la apariencia de la
    /// app sin tocar la lógica del entrenamiento ni recrear la escena.
    /// </summary>
    public static class TrainingUiStyler
    {
        public static void StyleScreen(GameObject panel, bool elevated = false)
        {
            if (panel == null) return;

            var background = panel.GetComponent<Image>();
            if (background != null)
            {
                background.color = elevated ? UiTheme.CardElevated : UiTheme.Background;
            }

            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null) continue;
                text.color = UiTheme.TextPrimary;
            }
        }

        public static void StyleCard(GameObject card)
        {
            if (card == null) return;

            var image = card.GetComponent<Image>();
            if (image != null)
            {
                image.color = UiTheme.CardElevated;
            }
        }

        public static void StylePrimary(Button button)
        {
            StyleButton(button, UiTheme.Accent, UiTheme.TextPrimary);
        }

        public static void StyleInfo(Button button)
        {
            StyleButton(button, UiTheme.Info, UiTheme.TextPrimary);
        }

        public static void StylePositive(Button button)
        {
            StyleButton(button, UiTheme.Positive, UiTheme.TextPrimary);
        }

        public static void StyleDanger(Button button)
        {
            StyleButton(button, UiTheme.Danger, UiTheme.TextPrimary);
        }

        public static void StyleSecondary(Button button)
        {
            StyleButton(button, UiTheme.Neutral, UiTheme.TextPrimary);
        }

        public static void StyleSecondaryText(TMP_Text text)
        {
            if (text != null)
            {
                text.color = UiTheme.TextSecondary;
            }
        }

        public static void StyleMutedText(TMP_Text text)
        {
            if (text != null)
            {
                text.color = UiTheme.TextMuted;
            }
        }

        private static void StyleButton(Button button, Color background, Color foreground)
        {
            if (button == null) return;

            var image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image != null)
            {
                image.color = background;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1.08f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = UiTheme.Disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = foreground;
                label.fontStyle |= FontStyles.Bold;
            }
        }
    }
}
