using System;
using System.Collections.Generic;
using UnityEngine;

namespace Entrenamiento.Presentation
{
    [Serializable]
    public sealed class TrainingHistoryEntry
    {
        public string exercise;
        public string source;
        public long unixSeconds;
        public int hits;
        public int misses;
        public float averageSeconds;
        public float bestSeconds;

        public int Total => hits + misses;
        public float Accuracy => Total > 0 ? hits * 100f / Total : 0f;
    }

    [Serializable]
    internal sealed class TrainingHistoryData
    {
        public List<TrainingHistoryEntry> entries = new List<TrainingHistoryEntry>();
    }

    /// <summary>
    /// Historial local liviano. No requiere cuenta ni backend: guarda las
    /// últimas sesiones en PlayerPrefs para alimentar la pantalla Progreso.
    /// </summary>
    public static class TrainingHistoryStore
    {
        private const string PrefKey = "training_history_v1";
        private const int MaxEntries = 30;

        public static List<TrainingHistoryEntry> Load()
        {
            string json = PlayerPrefs.GetString(PrefKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return new List<TrainingHistoryEntry>();

            try
            {
                var data = JsonUtility.FromJson<TrainingHistoryData>(json);
                return data != null && data.entries != null
                    ? new List<TrainingHistoryEntry>(data.entries)
                    : new List<TrainingHistoryEntry>();
            }
            catch
            {
                return new List<TrainingHistoryEntry>();
            }
        }

        public static void Add(string exercise, string source, int hits, int misses, float averageSeconds, float bestSeconds)
        {
            var data = new TrainingHistoryData { entries = Load() };
            data.entries.Insert(0, new TrainingHistoryEntry
            {
                exercise = string.IsNullOrEmpty(exercise) ? "ENTRENAMIENTO" : exercise,
                source = string.IsNullOrEmpty(source) ? "APP" : source,
                unixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                hits = Mathf.Max(0, hits),
                misses = Mathf.Max(0, misses),
                averageSeconds = Mathf.Max(0f, averageSeconds),
                bestSeconds = Mathf.Max(0f, bestSeconds)
            });

            if (data.entries.Count > MaxEntries)
                data.entries.RemoveRange(MaxEntries, data.entries.Count - MaxEntries);

            PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
        }
    }
}
