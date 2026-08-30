using System;

namespace Entrenamiento.Core.Models
{
    /// <summary>Modo en que se jugó una sesión.</summary>
    public enum SessionMode
    {
        Classic,
        GoNoGo
    }

    /// <summary>
    /// Resumen de una sesión de entrenamiento ya terminada, para el historial.
    /// Clase de datos pura: no depende de Unity.
    /// </summary>
    public class SessionRecord
    {
        /// <summary>Momento (hora local) en que terminó la sesión.</summary>
        public DateTime EndedAt { get; }

        public SessionMode Mode { get; }
        public int Hits { get; }
        public int Misses { get; }

        /// <summary>Tiempo de reacción promedio en segundos; 0 si no hubo aciertos.</summary>
        public float AverageReactionSeconds { get; }

        public SessionRecord(DateTime endedAt, SessionMode mode, int hits, int misses,
            float averageReactionSeconds)
        {
            if (hits < 0) throw new ArgumentOutOfRangeException(nameof(hits));
            if (misses < 0) throw new ArgumentOutOfRangeException(nameof(misses));
            if (averageReactionSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(averageReactionSeconds));

            EndedAt = endedAt;
            Mode = mode;
            Hits = hits;
            Misses = misses;
            AverageReactionSeconds = averageReactionSeconds;
        }
    }
}
