using System;
using System.Diagnostics;
using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.Rules
{
    /// <summary>
    /// Lógica del lado ESTACIÓN: interpreta START/ARM/CHANGE/OFF/END, mide
    /// localmente la reacción y genera HIT. CHANGE actualiza el estímulo sin
    /// cerrar la ronda y reinicia el cronómetro para medir la adaptación.
    /// </summary>
    public class StationAgent
    {
        public event Action<StationColor, bool> OnArmed;
        public event Action<bool> OnRoundTimedOut;
        public event Action<int> OnSessionStarted;
        public event Action<int, int, float, float> OnSessionEnded;

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private int _currentRound;

        public bool IsArmed { get; private set; }
        public bool LastArmWasGo { get; private set; } = true;
        public int LastElapsedMs { get; private set; }

        public void HandleIncomingPayload(string payload)
        {
            if (!TrainingProtocol.TryParse(payload, out string type, out string[] args))
            {
                return;
            }

            switch (type)
            {
                case TrainingProtocol.TypeStart:
                    if (args.Length >= 1 && TrainingProtocol.TryParseInt(args[0], out int total))
                    {
                        OnSessionStarted?.Invoke(total);
                    }
                    break;

                case TrainingProtocol.TypeArm:
                    ApplyStimulus(args, requireCurrentRound: false);
                    break;

                case TrainingProtocol.TypeChange:
                    ApplyStimulus(args, requireCurrentRound: true);
                    break;

                case TrainingProtocol.TypeOff:
                    if (IsArmed)
                    {
                        bool timedOut = true;
                        if (args.Length >= 2 && TrainingProtocol.TryParseInt(args[1], out int timeoutFlag))
                        {
                            timedOut = timeoutFlag != 0;
                        }

                        IsArmed = false;
                        _stopwatch.Stop();
                        if (timedOut)
                        {
                            OnRoundTimedOut?.Invoke(LastArmWasGo);
                        }
                    }
                    break;

                case TrainingProtocol.TypeEnd:
                    IsArmed = false;
                    if (args.Length >= 4 &&
                        TrainingProtocol.TryParseInt(args[0], out int hits) &&
                        TrainingProtocol.TryParseInt(args[1], out int misses) &&
                        TrainingProtocol.TryParseInt(args[2], out int avgMs) &&
                        TrainingProtocol.TryParseInt(args[3], out int bestMs))
                    {
                        OnSessionEnded?.Invoke(hits, misses, avgMs / 1000f, bestMs / 1000f);
                    }
                    break;
            }
        }

        private void ApplyStimulus(string[] args, bool requireCurrentRound)
        {
            if (args.Length < 2 ||
                !TrainingProtocol.TryParseInt(args[0], out int round) ||
                !TrainingProtocol.TryParseColor(args[1], out StationColor color))
            {
                return;
            }

            if (requireCurrentRound && (!IsArmed || round != _currentRound))
            {
                return;
            }

            bool isGo = true;
            if (args.Length >= 3 && TrainingProtocol.TryParseInt(args[2], out int goFlag))
            {
                isGo = goFlag != 0;
            }

            _currentRound = round;
            IsArmed = true;
            LastArmWasGo = isGo;
            _stopwatch.Restart();
            OnArmed?.Invoke(color, isGo);
        }

        public string RegisterTap()
        {
            if (!IsArmed)
            {
                return null;
            }

            _stopwatch.Stop();
            IsArmed = false;
            LastElapsedMs = (int)_stopwatch.ElapsedMilliseconds;
            return TrainingProtocol.FormatHit(_currentRound, LastElapsedMs);
        }
    }
}
