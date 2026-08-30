using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Paleta y constantes visuales de la app: estilo "deportivo oscuro".
    /// La usan tanto los scripts de runtime como los de Editor que generan
    /// escenas, para que toda la UI quede consistente.
    /// </summary>
    public static class UiTheme
    {
        // Fondos
        public static readonly Color Background = new Color32(0x12, 0x14, 0x18, 0xFF);
        public static readonly Color Card = new Color32(0x1E, 0x22, 0x2A, 0xFF);

        // Acentos (levemente desaturados para que no queden ásperos)
        public static readonly Color Accent = new Color32(0xF2, 0x7B, 0x3D, 0xFF);      // naranja
        public static readonly Color AccentLime = new Color32(0xA8, 0xD8, 0x4F, 0xFF);  // verde lima
        public static readonly Color Positive = new Color32(0x3E, 0x8E, 0x4E, 0xFF);    // verde
        public static readonly Color Info = new Color32(0x3D, 0x6B, 0xB0, 0xFF);        // azul
        public static readonly Color Neutral = new Color32(0x42, 0x48, 0x54, 0xFF);     // gris
        public static readonly Color Danger = new Color32(0xD4, 0x4A, 0x45, 0xFF);      // rojo

        // Texto
        public static readonly Color TextPrimary = Color.white;
        public static readonly Color TextSecondary = new Color(1f, 1f, 1f, 0.55f);
    }
}
