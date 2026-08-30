using System;
using System.Collections.Generic;
using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.Rules
{
    /// <summary>
    /// Lógica del HOST para una sesión distribuida por rondas: en cada ronda
    /// elige una estación al azar (evitando repetir la anterior), le manda ARM,
    /// espera el HIT (o el vencimiento del timeout, avisado por el bootstrap),
    /// y al completar todas las rondas emite el resumen.
    ///
    /// Modos (ver SessionConfig):
    ///  - Clásico: todas las rondas son "go" (hay que tocar). Colores por
    ///    estación o fijo.
    ///  - Go/No-Go: algunas rondas son señuelo. Colores fijos: verde = tocar,
    ///    rojo = quieto. Tocar el rojo es error; dejar pasar el verde (timeout)
    ///    es error; quedarse quieto en el rojo es acierto.
    ///
    /// El host puede participar como estación (id LocalStationId). Esta clase
    /// no sabe de red ni de Unity ni de relojes: emite mensajes por eventos y
    /// el bootstrap maneja transporte y timers.
    /// </summary>
    public class SessionCoordinator
    {
        /// <summary>Id reservado para "el host participando como estación".</summary>
        public const string LocalStationId = "local";

        private static readonly StationColor[] Palette =
        {
            StationColor.Red, StationColor.Green, StationColor.Blue, StationColor.Yellow
        };

        /// <summary>Mandar este payload a esta estación puntual (ARM/OFF).</summary>
        public event Action<string, string> OnSendToStation;

        /// <summary>Mandar este payload a todas las estaciones (START/END).</summary>
        public event Action<string> OnBroadcast;

        /// <summary>Arrancó una ronda: (ronda 1-based, stationId, color, esGo).</summary>
        public event Action<int, string, StationColor, bool> OnRoundStarted;

        /// <summary>Se resolvió una ronda: (evento, ronda). Miss = error.</summary>
        public event Action<ReactionEvent, int> OnRoundCompleted;

        /// <summary>Se completaron todas las rondas.</summary>
        public event Action OnSessionFinished;

        private readonly List<string> _stationIds;
        private readonly Dictionary<string, StationColor> _stationColors = new Dictionary<string, StationColor>();
        private readonly List<ReactionEvent> _results = new List<ReactionEvent>();
        private readonly Random _rng;

        public SessionConfig Config { get; }
        public int TotalRounds => Config.TotalRounds;
        public int CurrentRound { get; private set; }
        public string ArmedStationId { get; private set; }
        public bool CurrentRoundIsGo { get; private set; }
        public bool IsRunning { get; private set; }
        public IReadOnlyList<ReactionEvent> Results => _results;

        public int HitCount { get; private set; }
        public int MissCount { get; private set; }

        public SessionCoordinator(IEnumerable<string> stationIds, SessionConfig config, Random rng)
        {
            _stationIds = new List<string>(stationIds ?? throw new ArgumentNullException(nameof(stationIds)));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));

            if (_stationIds.Count == 0)
            {
                throw new ArgumentException("Se necesita al menos una estación.", nameof(stationIds));
            }

            if (Config.TotalRounds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(config), "TotalRounds debe ser mayor a 0.");
            }

            if (Config.IsGoNoGo && Config.TimeoutSeconds <= 0f)
            {
                throw new ArgumentException("El modo go/no-go necesita timeout mayor a 0.", nameof(config));
            }

            for (int i = 0; i < _stationIds.Count; i++)
            {
                _stationColors[_stationIds[i]] = Palette[i % Palette.Length];
            }
        }

        // ------------------------------------------------------------------
        // Arranque
        // ------------------------------------------------------------------

        /// <summary>Anuncia + arranca de inmediato (sin cuenta regresiva).</summary>
        public void Start()
        {
            AnnounceStart();
            BeginRounds();
        }

        /// <summary>
        /// Fase 1: anuncia la sesión (broadcast START) sin armar la primera ronda.
        /// Permite que el bootstrap muestre una cuenta regresiva antes de
        /// llamar a BeginRounds(). Las estaciones muestran su propia cuenta al
        /// recibir START, así quedan aproximadamente sincronizadas.
        /// </summary>
        public void AnnounceStart()
        {
            if (IsRunning)
            {
                return;
            }

            IsRunning = true;
            CurrentRound = 0;
            HitCount = 0;
            MissCount = 0;
            _results.Clear();
            OnBroadcast?.Invoke(TrainingProtocol.FormatStart(TotalRounds));
        }

        /// <summary>Fase 2: arma la primera ronda.</summary>
        public void BeginRounds()
        {
            if (!IsRunning || CurrentRound > 0)
            {
                return;
            }

            NextRound();
        }

        // ------------------------------------------------------------------
        // Resolución de rondas
        // ------------------------------------------------------------------

        /// <summary>
        /// Procesa un payload que llegó de una estación (o del agente local).
        /// Solo acepta HIT de la estación armada y de la ronda vigente.
        /// </summary>
        public void HandleStationPayload(string stationId, string payload)
        {
            if (!IsRunning ||
                !TrainingProtocol.TryParse(payload, out string type, out string[] args) ||
                type != TrainingProtocol.TypeHit ||
                args.Length < 2 ||
                !TrainingProtocol.TryParseInt(args[0], out int round) ||
                !TrainingProtocol.TryParseInt(args[1], out int elapsedMs))
            {
                return;
            }

            if (stationId != ArmedStationId || round != CurrentRound)
            {
                return;
            }

            // Go: tocar es acierto (con tiempo). No-go: tocar el señuelo es error.
            var result = CurrentRoundIsGo ? ReactionResult.Hit : ReactionResult.Miss;
            ResolveRound(new ReactionEvent(stationId, result, elapsedMs / 1000f));
        }

        /// <summary>
        /// El bootstrap avisa que venció el tiempo de la ronda vigente.
        /// Go: no tocó a tiempo = error. No-go: se quedó quieto = acierto.
        /// Manda OFF a la estación armada para que se apague.
        /// </summary>
        public void HandleRoundTimeout()
        {
            if (!IsRunning || ArmedStationId == null)
            {
                return;
            }

            OnSendToStation?.Invoke(ArmedStationId, TrainingProtocol.FormatOff(CurrentRound));

            var result = CurrentRoundIsGo ? ReactionResult.Miss : ReactionResult.Hit;
            ResolveRound(new ReactionEvent(ArmedStationId, result, 0f));
        }

        private void ResolveRound(ReactionEvent reactionEvent)
        {
            ArmedStationId = null;
            _results.Add(reactionEvent);

            if (reactionEvent.Result == ReactionResult.Hit)
            {
                HitCount++;
            }
            else
            {
                MissCount++;
            }

            OnRoundCompleted?.Invoke(reactionEvent, CurrentRound);

            if (CurrentRound >= TotalRounds)
            {
                Finish();
            }
            else
            {
                NextRound();
            }
        }

        // ------------------------------------------------------------------
        // Estadísticas (solo aciertos con tiempo real, > 0)
        // ------------------------------------------------------------------

        public float AverageSeconds()
        {
            float sum = 0f;
            int count = 0;

            foreach (var r in _results)
            {
                if (r.Result == ReactionResult.Hit && r.ReactionTimeSeconds > 0f)
                {
                    sum += r.ReactionTimeSeconds;
                    count++;
                }
            }

            return count == 0 ? 0f : sum / count;
        }

        public float BestSeconds()
        {
            float best = float.MaxValue;

            foreach (var r in _results)
            {
                if (r.Result == ReactionResult.Hit && r.ReactionTimeSeconds > 0f &&
                    r.ReactionTimeSeconds < best)
                {
                    best = r.ReactionTimeSeconds;
                }
            }

            return best == float.MaxValue ? 0f : best;
        }

        public StationColor GetStationColor(string stationId) =>
            _stationColors.TryGetValue(stationId, out var c) ? c : StationColor.None;

        // ------------------------------------------------------------------
        // Internas
        // ------------------------------------------------------------------

        private void NextRound()
        {
            CurrentRound++;

            var candidates = new List<string>(_stationIds);
            if (candidates.Count > 1 && _results.Count > 0)
            {
                candidates.Remove(_results[_results.Count - 1].StationId);
            }

            ArmedStationId = candidates[_rng.Next(candidates.Count)];
            CurrentRoundIsGo = !Config.IsGoNoGo || _rng.NextDouble() >= Config.NoGoProbability;

            StationColor color;
            if (Config.IsGoNoGo)
            {
                // Regla fija y fácil de explicar: verde = tocar, rojo = quieto.
                color = CurrentRoundIsGo ? StationColor.Green : StationColor.Red;
            }
            else
            {
                color = Config.FixedColor ?? GetStationColor(ArmedStationId);
            }

            OnSendToStation?.Invoke(ArmedStationId,
                TrainingProtocol.FormatArm(CurrentRound, color, CurrentRoundIsGo));
            OnRoundStarted?.Invoke(CurrentRound, ArmedStationId, color, CurrentRoundIsGo);
        }

        private void Finish()
        {
            IsRunning = false;
            int avgMs = (int)(AverageSeconds() * 1000f);
            int bestMs = (int)(BestSeconds() * 1000f);
            OnBroadcast?.Invoke(TrainingProtocol.FormatEnd(HitCount, MissCount, avgMs, bestMs));
            OnSessionFinished?.Invoke();
        }
    }
}
