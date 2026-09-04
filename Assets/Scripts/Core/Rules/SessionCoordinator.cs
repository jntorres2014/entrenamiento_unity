using System;
using System.Collections.Generic;
using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.Rules
{
    /// <summary>
    /// Coordinador del host. Soporta seis presets de entrenamiento y conserva
    /// la API usada por TrainingNearbyBootstrap.
    /// </summary>
    public class SessionCoordinator
    {
        public const string LocalStationId = "local";

        private static readonly StationColor[] Palette =
        {
            StationColor.Red, StationColor.Green, StationColor.Blue, StationColor.Yellow
        };

        public event Action<string, string> OnSendToStation;
        public event Action<string> OnBroadcast;
        public event Action<int, string, StationColor, bool> OnRoundStarted;
        public event Action<int, string, StationColor, bool> OnStimulusChanged;
        public event Action<ReactionEvent, int> OnRoundCompleted;
        public event Action OnSessionFinished;

        private readonly List<string> _stationIds;
        private readonly Dictionary<string, StationColor> _stationColors = new Dictionary<string, StationColor>();
        private readonly HashSet<string> _activeStations = new HashSet<string>();
        private readonly Dictionary<string, bool> _roundGoByStation = new Dictionary<string, bool>();
        private readonly List<ReactionEvent> _results = new List<ReactionEvent>();
        private readonly Random _rng;

        private string _targetStationId;
        private string _lastSingleStationId;
        private StationColor _currentStimulusColor = StationColor.None;
        private float _roundMaxElapsed;
        private bool _cognitiveChangeApplied;

        public SessionConfig Config { get; }
        public int TotalRounds => Config.TotalRounds;
        public int CurrentRound { get; private set; }
        public string ArmedStationId { get; private set; }
        public bool CurrentRoundIsGo { get; private set; }
        public bool IsRunning { get; private set; }
        public IReadOnlyList<ReactionEvent> Results => _results;
        public int HitCount { get; private set; }
        public int MissCount { get; private set; }
        public StationColor CurrentStimulusColor => _currentStimulusColor;
        public int ActiveStationCount => _activeStations.Count;

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

            NormalizePresetConfig();

            for (int i = 0; i < _stationIds.Count; i++)
            {
                _stationColors[_stationIds[i]] = Palette[i % Palette.Length];
            }

            ExerciseRuntimeRegistry.CurrentCoordinator = this;
        }

        private void NormalizePresetConfig()
        {
            switch (Config.Exercise)
            {
                case ExerciseMode.Reaction:
                    Config.NoGoProbability = 0f;
                    Config.FixedColor = StationColor.Green;
                    break;
                case ExerciseMode.AllSame:
                    Config.NoGoProbability = 0f;
                    Config.FixedColor = StationColor.Blue;
                    break;
                case ExerciseMode.Colors:
                case ExerciseMode.Decision:
                case ExerciseMode.CognitiveFake:
                    Config.NoGoProbability = 0f;
                    Config.FixedColor = null;
                    break;
                case ExerciseMode.Football:
                    Config.NoGoProbability = 0f;
                    Config.FixedColor = null;
                    if (Config.TimeoutSeconds <= 0f)
                    {
                        Config.TimeoutSeconds = 3f;
                    }
                    break;
            }

            if (Config.Exercise == ExerciseMode.CognitiveFake && Config.CognitiveChangeDelaySeconds <= 0f)
            {
                Config.CognitiveChangeDelaySeconds = 0.65f;
            }
        }

        public void Start()
        {
            AnnounceStart();
            BeginRounds();
        }

        public void AnnounceStart()
        {
            if (IsRunning) return;

            IsRunning = true;
            CurrentRound = 0;
            HitCount = 0;
            MissCount = 0;
            _results.Clear();
            _activeStations.Clear();
            OnBroadcast?.Invoke(TrainingProtocol.FormatStart(TotalRounds));
        }

        public void BeginRounds()
        {
            if (!IsRunning || CurrentRound > 0) return;
            NextRound();
        }

        public void HandleStationPayload(string stationId, string payload)
        {
            if (!IsRunning ||
                !TrainingProtocol.TryParse(payload, out string type, out string[] args) ||
                type != TrainingProtocol.TypeHit ||
                args.Length < 2 ||
                !TrainingProtocol.TryParseInt(args[0], out int round) ||
                !TrainingProtocol.TryParseInt(args[1], out int elapsedMs) ||
                round != CurrentRound ||
                !_activeStations.Contains(stationId))
            {
                return;
            }

            float elapsedSeconds = Math.Max(0, elapsedMs) / 1000f;

            switch (Config.Exercise)
            {
                case ExerciseMode.AllSame:
                    HandleAllSameHit(stationId, elapsedSeconds);
                    return;

                case ExerciseMode.Colors:
                    HandleColorChoiceHit(stationId, elapsedSeconds);
                    return;

                default:
                    if (stationId != ArmedStationId)
                    {
                        return;
                    }

                    bool isGo = _roundGoByStation.TryGetValue(stationId, out bool go) ? go : CurrentRoundIsGo;
                    CancelActiveStations(false);
                    ResolveRound(new ReactionEvent(
                        stationId,
                        isGo ? ReactionResult.Hit : ReactionResult.Miss,
                        elapsedSeconds));
                    return;
            }
        }

        private void HandleAllSameHit(string stationId, float elapsedSeconds)
        {
            _roundMaxElapsed = Math.Max(_roundMaxElapsed, elapsedSeconds);
            _activeStations.Remove(stationId);
            _roundGoByStation.Remove(stationId);
            OnSendToStation?.Invoke(stationId, TrainingProtocol.FormatOff(CurrentRound, false));

            if (_activeStations.Count == 0)
            {
                ArmedStationId = null;
                ResolveRound(new ReactionEvent("ALL", ReactionResult.Hit, _roundMaxElapsed));
                return;
            }

            ArmedStationId = FirstActiveStation();
        }

        private void HandleColorChoiceHit(string stationId, float elapsedSeconds)
        {
            bool correct = stationId == _targetStationId;
            CancelActiveStations(false);
            ResolveRound(new ReactionEvent(
                stationId,
                correct ? ReactionResult.Hit : ReactionResult.Miss,
                elapsedSeconds));
        }

        public void HandleRoundTimeout()
        {
            if (!IsRunning || _activeStations.Count == 0)
            {
                return;
            }

            if (Config.Exercise == ExerciseMode.Colors)
            {
                foreach (string id in SnapshotActiveStations())
                {
                    bool isTarget = id == _targetStationId;
                    OnSendToStation?.Invoke(id, TrainingProtocol.FormatOff(CurrentRound, isTarget));
                }
                _activeStations.Clear();
                _roundGoByStation.Clear();
                ResolveRound(new ReactionEvent(_targetStationId ?? "target", ReactionResult.Miss, 0f));
                return;
            }

            if (Config.Exercise == ExerciseMode.AllSame)
            {
                CancelActiveStations(true);
                ResolveRound(new ReactionEvent("ALL", ReactionResult.Miss, 0f));
                return;
            }

            string active = ArmedStationId;
            bool isGo = CurrentRoundIsGo;
            CancelActiveStations(true);
            ResolveRound(new ReactionEvent(
                active ?? "station",
                isGo ? ReactionResult.Miss : ReactionResult.Hit,
                0f));
        }

        /// <summary>
        /// Se llama desde la capa Unity durante Finta Cognitiva. Mantiene el pod
        /// activo, cambia su color y reinicia el cronómetro del StationAgent.
        /// </summary>
        public bool TriggerCognitiveFakeChange()
        {
            if (!IsRunning ||
                Config.Exercise != ExerciseMode.CognitiveFake ||
                _cognitiveChangeApplied ||
                ArmedStationId == null ||
                !_activeStations.Contains(ArmedStationId))
            {
                return false;
            }

            StationColor newColor = RandomColorDifferentFrom(_currentStimulusColor);
            _currentStimulusColor = newColor;
            _cognitiveChangeApplied = true;
            CurrentRoundIsGo = true;
            _roundGoByStation[ArmedStationId] = true;

            OnSendToStation?.Invoke(
                ArmedStationId,
                TrainingProtocol.FormatChange(CurrentRound, newColor, true));
            OnStimulusChanged?.Invoke(CurrentRound, ArmedStationId, newColor, true);
            return true;
        }

        private void ResolveRound(ReactionEvent reactionEvent)
        {
            ArmedStationId = null;
            _targetStationId = null;
            _activeStations.Clear();
            _roundGoByStation.Clear();
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
            _stationColors.TryGetValue(stationId, out var color) ? color : StationColor.None;

        private void NextRound()
        {
            CurrentRound++;
            _roundMaxElapsed = 0f;
            _cognitiveChangeApplied = false;
            _currentStimulusColor = StationColor.None;
            _targetStationId = null;
            _activeStations.Clear();
            _roundGoByStation.Clear();

            switch (Config.Exercise)
            {
                case ExerciseMode.Reaction:
                    ArmSingle(StationColor.Green, true);
                    break;

                case ExerciseMode.AllSame:
                    ArmAllSame();
                    break;

                case ExerciseMode.Colors:
                    ArmColorChoice();
                    break;

                case ExerciseMode.Decision:
                    ArmSingle(RandomColor(), true);
                    break;

                case ExerciseMode.CognitiveFake:
                    ArmSingle(RandomColor(), true);
                    break;

                case ExerciseMode.Football:
                    ArmFootball();
                    break;

                default:
                    ArmSingle(Config.FixedColor ?? StationColor.Green, true);
                    break;
            }
        }

        private void ArmSingle(StationColor color, bool isGo)
        {
            string stationId = PickSingleStation();
            _activeStations.Add(stationId);
            _roundGoByStation[stationId] = isGo;
            _targetStationId = stationId;
            ArmedStationId = stationId;
            CurrentRoundIsGo = isGo;
            _currentStimulusColor = color;

            OnSendToStation?.Invoke(stationId, TrainingProtocol.FormatArm(CurrentRound, color, isGo));
            OnRoundStarted?.Invoke(CurrentRound, stationId, color, isGo);
        }

        private void ArmAllSame()
        {
            CurrentRoundIsGo = true;
            _currentStimulusColor = StationColor.Blue;

            foreach (string stationId in _stationIds)
            {
                _activeStations.Add(stationId);
                _roundGoByStation[stationId] = true;
                OnSendToStation?.Invoke(
                    stationId,
                    TrainingProtocol.FormatArm(CurrentRound, StationColor.Blue, true));
            }

            ArmedStationId = FirstActiveStation();
            OnRoundStarted?.Invoke(CurrentRound, "ALL", StationColor.Blue, true);
        }

        private void ArmColorChoice()
        {
            int targetIndex = _rng.Next(_stationIds.Count);
            _targetStationId = _stationIds[targetIndex];
            CurrentRoundIsGo = true;

            StationColor targetColor = StationColor.None;
            for (int i = 0; i < _stationIds.Count; i++)
            {
                string stationId = _stationIds[i];
                StationColor color = Palette[i % Palette.Length];
                _activeStations.Add(stationId);

                // Todos quedan táctiles: el coordinador decide si fue el color correcto.
                _roundGoByStation[stationId] = true;
                OnSendToStation?.Invoke(
                    stationId,
                    TrainingProtocol.FormatArm(CurrentRound, color, true));

                if (i == targetIndex)
                {
                    targetColor = color;
                }
            }

            ArmedStationId = _targetStationId;
            _currentStimulusColor = targetColor;
            OnRoundStarted?.Invoke(CurrentRound, _targetStationId, targetColor, true);
        }

        private void ArmFootball()
        {
            int pick = _rng.Next(3);
            StationColor color = pick == 0
                ? StationColor.Green
                : pick == 1
                    ? StationColor.Blue
                    : StationColor.Red;
            bool isGo = color != StationColor.Red;
            ArmSingle(color, isGo);
        }

        private string PickSingleStation()
        {
            if (_stationIds.Count == 1)
            {
                _lastSingleStationId = _stationIds[0];
                return _stationIds[0];
            }

            var candidates = new List<string>(_stationIds);
            if (!string.IsNullOrEmpty(_lastSingleStationId))
            {
                candidates.Remove(_lastSingleStationId);
            }

            string selected = candidates[_rng.Next(candidates.Count)];
            _lastSingleStationId = selected;
            return selected;
        }

        private StationColor RandomColor()
        {
            return Palette[_rng.Next(Palette.Length)];
        }

        private StationColor RandomColorDifferentFrom(StationColor current)
        {
            if (current == StationColor.None) return RandomColor();

            StationColor selected;
            do
            {
                selected = RandomColor();
            } while (selected == current);
            return selected;
        }

        private string FirstActiveStation()
        {
            foreach (string id in _activeStations)
            {
                return id;
            }
            return null;
        }

        private List<string> SnapshotActiveStations()
        {
            return new List<string>(_activeStations);
        }

        private void CancelActiveStations(bool timedOut)
        {
            foreach (string id in SnapshotActiveStations())
            {
                OnSendToStation?.Invoke(id, TrainingProtocol.FormatOff(CurrentRound, timedOut));
            }
            _activeStations.Clear();
            _roundGoByStation.Clear();
        }

        private void Finish()
        {
            IsRunning = false;
            CancelActiveStations(false);
            int avgMs = (int)(AverageSeconds() * 1000f);
            int bestMs = (int)(BestSeconds() * 1000f);
            OnBroadcast?.Invoke(TrainingProtocol.FormatEnd(HitCount, MissCount, avgMs, bestMs));
            OnSessionFinished?.Invoke();
        }
    }
}
