using System;
using System.Globalization;
using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.Rules
{
    /// <summary>
    /// Protocolo de aplicación entre host y estaciones. Mantiene los mensajes
    /// históricos y suma CHANGE para poder cambiar un estímulo sin cerrar la ronda.
    ///
    /// Host -> estación:
    ///   START|totalRounds
    ///   ARM|round|color|go
    ///   CHANGE|round|color|go
    ///   OFF|round
    ///   END|hits|misses|avgMs|bestMs
    /// Estación -> host:
    ///   HIT|round|elapsedMs
    /// </summary>
    public static class TrainingProtocol
    {
        public const char Separator = '|';

        public const string TypeStart = "START";
        public const string TypeArm = "ARM";
        public const string TypeChange = "CHANGE";
        public const string TypeOff = "OFF";
        public const string TypeEnd = "END";
        public const string TypeHit = "HIT";

        public static string FormatStart(int totalRounds) =>
            $"{TypeStart}{Separator}{totalRounds}";

        public static string FormatArm(int round, StationColor color, bool isGo) =>
            $"{TypeArm}{Separator}{round}{Separator}{color}{Separator}{(isGo ? 1 : 0)}";

        public static string FormatChange(int round, StationColor color, bool isGo) =>
            $"{TypeChange}{Separator}{round}{Separator}{color}{Separator}{(isGo ? 1 : 0)}";

        public static string FormatOff(int round) =>
            $"{TypeOff}{Separator}{round}";

        public static string FormatEnd(int hits, int misses, int avgMs, int bestMs) =>
            $"{TypeEnd}{Separator}{hits}{Separator}{misses}{Separator}{avgMs}{Separator}{bestMs}";

        public static string FormatHit(int round, int elapsedMs) =>
            $"{TypeHit}{Separator}{round}{Separator}{elapsedMs}";

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
