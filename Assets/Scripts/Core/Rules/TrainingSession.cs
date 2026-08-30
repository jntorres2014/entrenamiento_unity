using System;
using System.Collections.Generic;
using System.Diagnostics;
using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.Rules
{
    /// <summary>
    /// Lógica de una sesión de entrenamiento: mantiene las estaciones registradas,
    /// controla cuál está activa en cada momento, y registra los eventos de reacción
    /// (aciertos, errores, tiempo de reacción).
    /// Clase de reglas de negocio pura: no depende de Unity ni de MonoBehaviour.
    /// Usa System.Diagnostics.Stopwatch para medir tiempo de forma independiente del motor.
    /// </summary>
    public class TrainingSession
    {
        private readonly Dictionary<string, Station> _stations = new Dictionary<string, Station>();
        private readonly List<ReactionEvent> _history = new List<ReactionEvent>();
        private readonly Stopwatch _reactionStopwatch = new Stopwatch();

        private string _activeStationId;

        public IReadOnlyList<ReactionEvent> History => _history;
        public int HitCount { get; private set; }
        public int MissCount { get; private set; }

        public event Action<ReactionEvent> OnReactionRegistered;

        public void RegisterStation(Station station)
        {
            if (station == null)
            {
                throw new ArgumentNullException(nameof(station));
            }

            _stations[station.Id] = station;
        }

        public Station GetStation(string stationId)
        {
            _stations.TryGetValue(stationId, out var station);
            return station;
        }

        /// <summary>
        /// Activa una estación (le indica que muestre un color) y arranca el
        /// cronómetro de reacción. Solo puede haber una estación activa a la vez
        /// en esta versión del MVP.
        /// </summary>
        public void ActivateStation(string stationId, StationColor color)
        {
            if (!_stations.TryGetValue(stationId, out var station))
            {
                throw new InvalidOperationException($"No existe una estación registrada con id '{stationId}'.");
            }

            station.Activate(color);
            _activeStationId = stationId;
            _reactionStopwatch.Restart();
        }

        /// <summary>
        /// Registra el toque reportado por una estación. Si coincide con la estación
        /// activa, se computa un acierto con el tiempo de reacción medido; si no
        /// coincide, se computa un error (comportamiento reservado para una tarea futura,
        /// hoy solo se deja resuelto el caso de acierto simple).
        /// </summary>
        public ReactionEvent RegisterTouch(string stationId)
        {
            if (!_stations.TryGetValue(stationId, out var station))
            {
                throw new InvalidOperationException($"No existe una estación registrada con id '{stationId}'.");
            }

            bool isCorrectStation = stationId == _activeStationId;
            ReactionEvent reactionEvent;

            if (isCorrectStation)
            {
                _reactionStopwatch.Stop();
                float reactionTime = (float)_reactionStopwatch.Elapsed.TotalSeconds;

                station.MarkTouched();
                reactionEvent = new ReactionEvent(stationId, ReactionResult.Hit, reactionTime);
                HitCount++;
                _activeStationId = null;
            }
            else
            {
                reactionEvent = new ReactionEvent(stationId, ReactionResult.Miss, 0f);
                MissCount++;
            }

            _history.Add(reactionEvent);
            OnReactionRegistered?.Invoke(reactionEvent);
            return reactionEvent;
        }

        /// <summary>
        /// Elige una estación al azar entre las registradas y la activa. Si hay
        /// más de una estación disponible, evita repetir la que estaba activa
        /// en la ronda anterior.
        /// </summary>
        public string ActivateRandomStation(StationColor color, Random random, bool avoidRepeatingLast = true)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (_stations.Count == 0)
            {
                throw new InvalidOperationException("No hay estaciones registradas para activar.");
            }

            var candidateIds = new List<string>(_stations.Keys);

            if (avoidRepeatingLast && candidateIds.Count > 1 && _activeStationId != null)
            {
                candidateIds.Remove(_activeStationId);
            }

            int index = random.Next(candidateIds.Count);
            string chosenId = candidateIds[index];

            ActivateStation(chosenId, color);
            return chosenId;
        }
    }
}
