using System;
using System.Globalization;
using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.History
{
    /// <summary>
    /// Textos del historial (fechas relativas, tiempos, nombres de modo) en
    /// rioplatense. Lógica pura y testeable: la vista solo concatena y colorea.
    /// </summary>
    public static class SessionHistoryFormat
    {
        private static readonly string[] Months =
        {
            "ENE", "FEB", "MAR", "ABR", "MAY", "JUN",
            "JUL", "AGO", "SEP", "OCT", "NOV", "DIC"
        };

        public static string ModeName(SessionMode mode)
        {
            return mode == SessionMode.GoNoGo ? "GO / NO-GO" : "CLÁSICO";
        }

        /// <summary>"HOY 18:30", "AYER 19:10" o "12 JUL · 18:30".</summary>
        public static string RelativeDate(DateTime now, DateTime endedAt)
        {
            string time = endedAt.ToString("HH:mm", CultureInfo.InvariantCulture);

            if (endedAt.Date == now.Date)
            {
                return $"HOY {time}";
            }

            if (endedAt.Date == now.Date.AddDays(-1))
            {
                return $"AYER {time}";
            }

            return $"{endedAt.Day} {Months[endedAt.Month - 1]} · {time}";
        }

        /// <summary>"PROM 0,48 s"; "—" si no hubo aciertos medibles.</summary>
        public static string AverageTime(float seconds)
        {
            if (seconds <= 0f)
            {
                return "—";
            }

            string value = seconds.ToString("0.00", CultureInfo.InvariantCulture)
                .Replace('.', ',');
            return $"PROM {value} s";
        }

        public static string Hits(int hits)
        {
            return hits == 1 ? "1 acierto" : $"{hits} aciertos";
        }

        public static string Misses(int misses)
        {
            return misses == 1 ? "1 error" : $"{misses} errores";
        }
    }
}
