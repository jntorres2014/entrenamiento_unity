using System;
using System.Globalization;
using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.Rules
{
    /// <summary>
    /// Protocolo de aplicación de la sesión de entrenamiento: los mensajes de
    /// texto que viajan por ILocalTransport entre el host y las estaciones.
    /// Es independiente del transporte (funciona igual sobre SimulatedTransport
    /// o Nearby Connections).
    ///
    /// Mensajes host -> estación:
    ///   START|totalRounds            La sesión arranca.
    ///   ARM|round|color|go           Encendete con este color (round 1-based).
    ///                                go=1: hay que tocar; go=0: señuelo (NO tocar).
    ///   OFF|round                    La ronda venció por timeout: apagate.
    ///   END|hits|misses|avgMs|bestMs La sesión terminó, resumen simple.
    ///
    /// Mensajes estación -> host:
    ///   HIT|round|elapsedMs          El deportista tocó; tiempo medido LOCALMENTE
    ///                                por la estación (más preciso que medir en el
    ///                                host, porque no incluye la latencia de red).
    /// </summary>
    public static class TrainingProtocol
    {
        public const char Separator = '|';

        public const string TypeStart = "START";
        public const string TypeArm = "ARM";
        public const string TypeOff = "OFF";
        public const string TypeEnd = "END";
        public const string TypeHit = "HIT";

        // ----------------- Formateo -----------------

        public static string FormatStart(int totalRounds) =>
            $"{TypeStart}{Separator}{totalRounds}";

        public static string FormatArm(int round, StationColor color, bool isGo) =>
            $"{TypeArm}{Separator}{round}{Separator}{color}{Separator}{(isGo ? 1 : 0)}";

        public static string FormatOff(int round) =>
            $"{TypeOff}{Separator}{round}";

        public static string FormatEnd(int hits, int misses, int avgMs, int bestMs) =>
            $"{TypeEnd}{Separator}{hits}{Separator}{misses}{Separator}{avgMs}{Separator}{bestMs}";

        public static string FormatHit(int round, int elapsedMs) =>
            $"{TypeHit}{Separator}{round}{Separator}{elapsedMs}";

        // ----------------- Parseo -----------------

        /// <summary>
        /// Separa un mensaje en tipo + argumentos. Devuelve false si está vacío.
        /// </summary>
        public static bool TryParse(string payload, out string type, out string[] args)
        {
            type = null;
            args = null;

            if (string.IsNullOrEmpty(payload))
            {
                return false;
            }

            string[] parts = payload.Split(Separator);
            type = parts[0];
            args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);
            return true;
        }

        public static bool TryParseInt(string value, out int result) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

        public static bool TryParseColor(string value, out StationColor color) =>
            Enum.TryParse(value, out color);
    }
}
