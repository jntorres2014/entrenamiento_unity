using System;
using System.Diagnostics;
using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.Rules
{
    /// <summary>
    /// Lógica del lado ESTACIÓN: interpreta los mensajes del host
    /// (START/ARM/OFF/END), mide localmente el tiempo de reacción con
    /// Stopwatch, y arma el mensaje HIT cuando el deportista toca la pantalla.
    /// Clase pura: no sabe de Unity ni de transporte. También la usa el host
    /// cuando participa como estación (agente "local").
    /// </summary>
    public class StationAgent
    {
        /// <summary>El host indicó encenderse: (color, esGo). esGo=false es señuelo.</summary>
        public event Action<StationColor, bool> OnArmed;

        /// <summary>
        /// La ronda venció por timeout (OFF del host): (eraGo). Si eraGo, el
        /// deportista llegó tarde; si no, se quedó quieto correctamente.
        /// </summary>
        public event Action<bool> OnRoundTimedOut;

        /// <summary>La sesión arrancó (total de rondas).</summary>
        public event Action<int> OnSessionStarted;

        /// <summary>La sesión terminó: (aciertos, errores, promedio s, mejor s).</summary>
        public event Action<int, int, float, float> OnSessionEnded;

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private int _currentRound;

        public bool IsArmed { get; private set; }

        /// <summary>Si el último ARM fue go (tocar) o señuelo (quieto).</summary>
        public bool LastArmWasGo { get; private set; } = true;

        /// <summary>Tiempo del último toque registrado, en ms (para mostrar en UI).</summary>
        public int LastElapsedMs { get; private set; }

        /// <summary>Procesa un payload que llegó del host.</summary>
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
                    if (args.Length >= 2 &&
                        TrainingProtocol.TryParseInt(args[0], out int round) &&
                        TrainingProtocol.TryParseColor(args[1], out StationColor color))
                    {
                        // go es opcional por compatibilidad: sin el flag, es go.
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
                    break;

                case TrainingProtocol.TypeOff:
                    if (IsArmed)
                    {
                        IsArmed = false;
                        _stopwatch.Stop();
                        OnRoundTimedOut?.Invoke(LastArmWasGo);
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

        /// <summary>
        /// Registra el toque del deportista. Si la estación estaba armada,
        /// devuelve el mensaje HIT para mandar al host; si no, devuelve null.
        /// </summary>
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
