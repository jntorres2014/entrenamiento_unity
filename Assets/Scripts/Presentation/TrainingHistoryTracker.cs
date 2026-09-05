using System.Reflection;
using Entrenamiento.Core.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Registra automáticamente sesiones con pods y SOLO en el historial local.
    /// SOLO se observa por reflexión para no acoplar su controlador visual al store.
    /// </summary>
    public sealed class TrainingHistoryTracker : MonoBehaviour
    {
        private SessionCoordinator _coordinator;
        private SoloTrainingModeController _solo;
        private bool _soloRecorded;

        private FieldInfo _soloStateField;
        private FieldInfo _soloExerciseField;
        private FieldInfo _soloHitsField;
        private FieldInfo _soloMissesField;
        private MethodInfo _soloAverageMethod;
        private MethodInfo _soloBestMethod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<TrainingHistoryTracker>() == null)
                {
                    canvas.gameObject.AddComponent<TrainingHistoryTracker>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _solo = GetComponent<SoloTrainingModeController>();
            CacheSoloReflection();
        }

        private void Update()
        {
            SessionCoordinator current = ExerciseRuntimeRegistry.CurrentCoordinator;
            if (!ReferenceEquals(current, _coordinator))
            {
                UnsubscribeCoordinator();
                _coordinator = current;
                if (_coordinator != null) _coordinator.OnSessionFinished += HandleCoordinatorFinished;
            }

            TrackSoloFinish();
        }

        private void HandleCoordinatorFinished()
        {
            if (_coordinator == null) return;
            TrainingHistoryStore.Add(
                ExerciseSelection.Name(_coordinator.Config.Exercise),
                "PODS",
                _coordinator.HitCount,
                _coordinator.MissCount,
                _coordinator.AverageSeconds(),
                _coordinator.BestSeconds());
        }

        private void CacheSoloReflection()
        {
            var type = typeof(SoloTrainingModeController);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            _soloStateField = type.GetField("_state", flags);
            _soloExerciseField = type.GetField("_exercise", flags);
            _soloHitsField = type.GetField("_hits", flags);
            _soloMissesField = type.GetField("_misses", flags);
            _soloAverageMethod = type.GetMethod("AverageSeconds", flags);
            _soloBestMethod = type.GetMethod("BestSeconds", flags);
        }

        private void TrackSoloFinish()
        {
            if (_solo == null) _solo = GetComponent<SoloTrainingModeController>();
            if (_solo == null || _soloStateField == null) return;

            object state = _soloStateField.GetValue(_solo);
            bool finished = state != null && state.ToString() == "Finished";

            if (!finished)
            {
                _soloRecorded = false;
                return;
            }

            if (_soloRecorded) return;
            _soloRecorded = true;

            ExerciseMode exercise = ExerciseMode.Reaction;
            if (_soloExerciseField != null)
            {
                object value = _soloExerciseField.GetValue(_solo);
                if (value is ExerciseMode mode) exercise = mode;
            }

            int hits = ReadInt(_soloHitsField, _solo);
            int misses = ReadInt(_soloMissesField, _solo);
            float average = InvokeFloat(_soloAverageMethod, _solo);
            float best = InvokeFloat(_soloBestMethod, _solo);

            TrainingHistoryStore.Add(
                ExerciseSelection.Name(exercise),
                "SOLO",
                hits,
                misses,
                average,
                best);
        }

        private static int ReadInt(FieldInfo field, object instance)
        {
            if (field == null || instance == null) return 0;
            object value = field.GetValue(instance);
            return value is int i ? i : 0;
        }

        private static float InvokeFloat(MethodInfo method, object instance)
        {
            if (method == null || instance == null) return 0f;
            object value = method.Invoke(instance, null);
            return value is float f ? f : 0f;
        }

        private void UnsubscribeCoordinator()
        {
            if (_coordinator != null)
                _coordinator.OnSessionFinished -= HandleCoordinatorFinished;
        }

        private void OnDestroy()
        {
            UnsubscribeCoordinator();
        }
    }
}
