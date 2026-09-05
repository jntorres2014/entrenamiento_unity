using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Design system Deportivo Pro: negro verdoso, superficies profundas y
    /// verde eléctrico como marca. Rojo/azul/amarillo quedan para estímulos.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Background = new Color32(0x07, 0x0D, 0x0B, 0xFF);
        public static readonly Color Surface = new Color32(0x0D, 0x17, 0x14, 0xFF);
        public static readonly Color Card = new Color32(0x11, 0x1C, 0x19, 0xFF);
        public static readonly Color CardElevated = new Color32(0x16, 0x24, 0x20, 0xFF);
        public static readonly Color Divider = new Color32(0x29, 0x38, 0x32, 0xFF);

        public static readonly Color Accent = new Color32(0x76, 0xE8, 0x00, 0xFF);
        public static readonly Color AccentSoft = new Color32(0x50, 0xB8, 0x00, 0xFF);
        public static readonly Color AccentLime = new Color32(0xA6, 0xF3, 0x45, 0xFF);
        public static readonly Color Positive = new Color32(0x43, 0xC9, 0x78, 0xFF);
        public static readonly Color Info = new Color32(0x4C, 0x8D, 0xFF, 0xFF);
        public static readonly Color Neutral = new Color32(0x31, 0x42, 0x3C, 0xFF);
        public static readonly Color Danger = new Color32(0xEF, 0x53, 0x50, 0xFF);

        public static readonly Color TextPrimary = new Color32(0xF5, 0xF8, 0xF6, 0xFF);
        public static readonly Color TextSecondary = new Color32(0xB7, 0xC5, 0xBF, 0xFF);
        public static readonly Color TextMuted = new Color32(0x78, 0x8B, 0x83, 0xFF);

        public static readonly Color ButtonPressed = new Color32(0xD8, 0xE0, 0xDA, 0xD9);
        public static readonly Color Disabled = new Color32(0x63, 0x70, 0x69, 0x66);
    }
}
