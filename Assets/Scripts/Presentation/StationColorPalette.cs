using Entrenamiento.Core.Models;
using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Traduce los colores lógicos definidos en Core (StationColor) a colores
    /// concretos de Unity. Se mantiene separado de Core para que los modelos
    /// de datos no dependan de UnityEngine.
    /// </summary>
    public static class StationColorPalette
    {
        public static Color ToUnityColor(StationColor color)
        {
            // Tonos intensos pero no "puros": se ven fuerte a distancia sin
            // resultar tan ásperos como los colores RGB al 100%.
            switch (color)
            {
                case StationColor.Red:
                    return new Color32(0xE5, 0x39, 0x35, 0xFF);
                case StationColor.Green:
                    return new Color32(0x43, 0xA0, 0x47, 0xFF);
                case StationColor.Blue:
                    return new Color32(0x1E, 0x88, 0xE5, 0xFF);
                case StationColor.Yellow:
                    return new Color32(0xFD, 0xD8, 0x35, 0xFF);
                case StationColor.None:
                default:
                    return new Color32(0x10, 0x12, 0x16, 0xFF);
            }
        }
    }
}
