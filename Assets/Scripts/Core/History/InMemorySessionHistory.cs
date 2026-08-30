using System;
using System.Collections.Generic;
using Entrenamiento.Core.Models;

namespace Entrenamiento.Core.History
{
    /// <summary>Fuente de sesiones pasadas para la pantalla de historial.</summary>
    public interface ISessionHistorySource
    {
        /// <summary>Últimas sesiones, de la más reciente a la más vieja.</summary>
        IReadOnlyList<SessionRecord> GetRecent(int maxCount);
    }

    /// <summary>
    /// Historial en memoria (se pierde al cerrar la app). Por ahora arranca con
    /// datos de ejemplo; cuando el flujo real termine una sesión, tiene que
    /// llamar a <see cref="Add"/> (y en algún momento esto migra a persistencia).
    /// Clase de lógica pura: no depende de Unity.
    /// </summary>
    public class InMemorySessionHistory : ISessionHistorySource
    {
        /// <summary>Instancia compartida de la app, precargada con ejemplos.</summary>
        public static InMemorySessionHistory Shared { get; } = CreateWithSampleData(DateTime.Now);

        private readonly List<SessionRecord> _records = new List<SessionRecord>();

        public void Add(SessionRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            _records.Add(record);
        }

        public void Clear()
        {
            _records.Clear();
        }

        public IReadOnlyList<SessionRecord> GetRecent(int maxCount)
        {
            var ordered = new List<SessionRecord>(_records);
            ordered.Sort((a, b) => b.EndedAt.CompareTo(a.EndedAt));

            if (ordered.Count > maxCount)
            {
                ordered.RemoveRange(maxCount, ordered.Count - maxCount);
            }

            return ordered;
        }

        /// <summary>Historial con datos de ejemplo relativos a <paramref name="now"/>.</summary>
        public static InMemorySessionHistory CreateWithSampleData(DateTime now)
        {
            var history = new InMemorySessionHistory();
            history.Add(new SessionRecord(now.AddHours(-2), SessionMode.Classic, 18, 2, 0.42f));
            history.Add(new SessionRecord(now.AddHours(-5), SessionMode.GoNoGo, 12, 4, 0.51f));
            history.Add(new SessionRecord(now.AddDays(-1).AddHours(-1), SessionMode.Classic, 15, 5, 0.47f));
            history.Add(new SessionRecord(now.AddDays(-1).AddHours(-3), SessionMode.GoNoGo, 9, 6, 0.58f));
            history.Add(new SessionRecord(now.AddDays(-3), SessionMode.Classic, 20, 0, 0.39f));
            history.Add(new SessionRecord(now.AddDays(-6), SessionMode.Classic, 10, 8, 0.63f));
            return history;
        }
    }
}
