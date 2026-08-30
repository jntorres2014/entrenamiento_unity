using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.Rules
{
    /// <summary>
    /// Configuración de una sesión de entrenamiento, definida por el host antes
    /// de arrancar. Clase de datos pura.
    /// </summary>
    public class SessionConfig
    {
        /// <summary>Cantidad total de rondas.</summary>
        public int TotalRounds = 10;

        /// <summary>
        /// Tiempo máximo por ronda en segundos; 0 = sin límite (la ronda espera
        /// el toque indefinidamente). En modo go/no-go debe ser mayor a 0 para
        /// poder resolver las rondas señuelo.
        /// </summary>
        public float TimeoutSeconds = 0f;

        /// <summary>
        /// Probabilidad (0..1) de que una ronda sea señuelo (no-go). 0 = modo
        /// clásico. En go/no-go los colores son fijos: verde = tocar,
        /// rojo = quieto.
        /// </summary>
        public float NoGoProbability = 0f;

        /// <summary>
        /// Si tiene valor, todas las estaciones usan este color; si es null,
        /// cada estación usa su color asignado (paleta variada).
        /// Se ignora en modo go/no-go.
        /// </summary>
        public StationColor? FixedColor = null;

        public bool IsGoNoGo => NoGoProbability > 0f;
    }
}
