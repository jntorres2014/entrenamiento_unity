using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Paleta visual principal de la app.
    /// Mantiene una estética deportiva oscura, moderna y de alto contraste,
    /// pensada para uso rápido en celular durante entrenamientos.
    /// </summary>
    public static class UiTheme
    {
        // Fondos y superficies
        public static readonly Color Background = new Color32(0x0B, 0x0F, 0x14, 0xFF);
        public static readonly Color Surface = new Color32(0x12, 0x18, 0x20, 0xFF);
        public static readonly Color Card = new Color32(0x18, 0x20, 0x2B, 0xFF);
        public static readonly Color CardElevated = new Color32(0x20, 0x2A, 0x37, 0xFF);
        public static readonly Color Divider = new Color32(0x2D, 0x38, 0x46, 0xFF);

        // Acentos
        public static readonly Color Accent = new Color32(0xFF, 0x7A, 0x32, 0xFF);       // naranja deportivo
        public static readonly Color AccentSoft = new Color32(0xD9, 0x5F, 0x24, 0xFF);
        public static readonly Color AccentLime = new Color32(0xB8, 0xE8, 0x4B, 0xFF);
        public static readonly Color Positive = new Color32(0x43, 0xC9, 0x78, 0xFF);
        public static readonly Color Info = new Color32(0x4C, 0x8D, 0xFF, 0xFF);
        public static readonly Color Neutral = new Color32(0x36, 0x43, 0x52, 0xFF);
        public static readonly Color Danger = new Color32(0xEF, 0x53, 0x50, 0xFF);

        // Texto
        public static readonly Color TextPrimary = new Color32(0xF7, 0xF9, 0xFC, 0xFF);
        public static readonly Color TextSecondary = new Color32(0xA8, 0xB2, 0xC1, 0xFF);
        public static readonly Color TextMuted = new Color32(0x72, 0x7E, 0x8E, 0xFF);

        // Estados de interacción
        public static readonly Color ButtonPressed = new Color32(0xD8, 0xE0, 0xEA, 0xD9);
        public static readonly Color Disabled = new Color32(0x63, 0x6D, 0x79, 0x66);
    }
}
