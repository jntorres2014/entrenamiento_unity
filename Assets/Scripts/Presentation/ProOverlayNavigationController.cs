using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Evita que el handler legacy cierre la app mientras hay overlays Pro y
    /// resuelve Atrás del sistema en Ejercicios, SOLO y Progreso.
    /// </summary>
    public sealed class ProOverlayNavigationController : MonoBehaviour
    {
        private Canvas _canvas;
        private TrainingModernUiController _modernUi;
        private bool _disabledModernForOverlay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<ProOverlayNavigationController>() == null)
                {
                    canvas.gameObject.AddComponent<ProOverlayNavigationController>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _modernUi = GetComponent<TrainingModernUiController>();
        }

        private void Update()
        {
            bool progress = IsActive("TrainingProgressPanel");
            bool podExercises = IsActive("ExerciseSelectionPanel");
            bool soloExercises = IsActive("SoloExerciseSelection");
            bool soloOptions = IsActive("SoloOptionsPanel");
            bool ownsBack = progress || podExercises || soloExercises || soloOptions;

            if ((progress || podExercises) && _modernUi != null && _modernUi.enabled)
            {
                _modernUi.enabled = false;
                _disabledModernForOverlay = true;
            }
            else if (!progress && !podExercises && _disabledModernForOverlay && _modernUi != null)
            {
                _modernUi.enabled = true;
                _disabledModernForOverlay = false;
            }

            if (!ownsBack || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

            string[] buttons =
            {
                "ProgressBackButton",
                "SoloOptionsBack",
                "SoloSelectorBack",
                "ExerciseBackButton"
            };

            foreach (string name in buttons)
            {
                var go = FindDeep(name);
                if (go == null || !go.activeInHierarchy) continue;
                var button = go.GetComponent<Button>();
                if (button == null || !button.interactable) continue;
                button.onClick.Invoke();
                return;
            }
        }

        private bool IsActive(string objectName)
        {
            var go = FindDeep(objectName);
            return go != null && go.activeInHierarchy;
        }

        private GameObject FindDeep(string objectName)
        {
            if (_canvas == null) return null;
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true))
                if (t.name == objectName) return t.gameObject;
            return null;
        }
    }
}
