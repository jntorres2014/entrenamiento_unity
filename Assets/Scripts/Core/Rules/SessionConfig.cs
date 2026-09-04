using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.Rules
{
    /// <summary>
    /// Configuración de una sesión de entrenamiento, definida por el host antes
    /// de arrancar. Clase de datos pura.
    /// </summary>
    public class SessionConfig
    {
        /// <summary>Preset de ejercicio elegido antes de entrar a la configuración.</summary>
        public ExerciseMode Exercise = ExerciseSelection.Current;

        /// <summary>Cantidad total de rondas.</summary>
        public int TotalRounds = 10;

        /// <summary>
        /// Tiempo máximo por ronda en segundos; 0 = sin límite. Algunos presets
        /// (por ejemplo Fútbol con rojo=no tocar) fuerzan un mínimo internamente.
        /// </summary>
        public float TimeoutSeconds = 0f;

        /// <summary>
        /// Compatibilidad con el modo clásico anterior. Los presets nuevos pueden
        /// reemplazar esta regla con su propia lógica.
        /// </summary>
        public float NoGoProbability = 0f;

        /// <summary>
        /// Compatibilidad con configuración histórica de color fijo.
        /// </summary>
        public StationColor? FixedColor = null;

        /// <summary>Demora del cambio de estímulo en Finta Cognitiva.</summary>
        public float CognitiveChangeDelaySeconds = 0.65f;

        public bool IsGoNoGo => Exercise == ExerciseMode.Football || NoGoProbability > 0f;
    }
}
