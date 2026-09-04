using System;
using System.Globalization;
using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.Rules
{
    /// <summary>
    /// Protocolo de aplicación entre host y estaciones.
    /// START incluye el preset para sincronizar la UI de los pods.
    /// OFF: 1=timeout, 0=apagado silencioso.
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
            FormatStart(totalRounds, ExerciseSelection.Current);

        public static string FormatStart(int totalRounds, ExerciseMode exercise) =>
            $"{TypeStart}{Separator}{totalRounds}{Separator}{exercise}";

        public static string FormatArm(int round, StationColor color, bool isGo) =>
            $"{TypeArm}{Separator}{round}{Separator}{color}{Separator}{(isGo ? 1 : 0)}";

        public static string FormatChange(int round, StationColor color, bool isGo) =>
            $"{TypeChange}{Separator}{round}{Separator}{color}{Separator}{(isGo ? 1 : 0)}";

        public static string FormatOff(int round) => FormatOff(round, true);

        public static string FormatOff(int round, bool timedOut) =>
            $"{TypeOff}{Separator}{round}{Separator}{(timedOut ? 1 : 0)}";

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

        public static bool TryParseExercise(string value, out ExerciseMode exercise) =>
            Enum.TryParse(value, out exercise);
    }
}
