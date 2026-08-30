using System;

namespace Entrenamiento.Core.Models
{
    /// <summary>
    /// Resultado de un intento del deportista sobre una estación activada.
    /// </summary>
    public enum ReactionResult
    {
        Hit,   // Tocó la estación correcta
        Miss   // Tocó una estación incorrecta (se agrega para futuro; no usado en esta tarea)
    }

    /// <summary>
    /// Registro inmutable de un evento de reacción: qué estación, qué resultado,
    /// y cuánto tiempo pasó desde que se activó hasta que se tocó.
    /// Clase de datos pura: no depende de Unity ni de MonoBehaviour.
    /// </summary>
    [Serializable]
    public class ReactionEvent
    {
        public string StationId { get; }
        public ReactionResult Result { get; }
        public float ReactionTimeSeconds { get; }

        public ReactionEvent(string stationId, ReactionResult result, float reactionTimeSeconds)
        {
            StationId = stationId;
            Result = result;
            ReactionTimeSeconds = reactionTimeSeconds;
        }

        public override string ToString()
        {
            return $"[{Result}] Estación={StationId} Tiempo={ReactionTimeSeconds:F3}s";
        }
    }
}
