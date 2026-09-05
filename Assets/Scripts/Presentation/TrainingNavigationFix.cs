using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    public sealed class TrainingNavigationFix : MonoBehaviour
    {
        private Button _modernBackButton;
        private bool _backHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "TrainingNearby") return;

            Canvas canvas = null;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in canvases)
            {
                if (candidate != null && candidate.isRootCanvas)
                {
                    canvas = candidate;
                    break;
                }
            }
            if (canvas == null) return;

            AddIfMissing<TrainingModernUiController>(canvas);
            AddIfMissing<TrainingUiMagic>(canvas);
            AddIfMissing<ARTrainingModeController>(canvas);
            AddIfMissing<CameraTrainingModeController>(canvas);
            AddIfMissing<TrainingHomeCView>(canvas);
            AddIfMissing<TrainingFlowCView>(canvas);
            AddIfMissing<ExerciseSelectionController>(canvas);
            AddIfMissing<ExerciseRuntimeEnhancer>(canvas);
            AddIfMissing<SoloTrainingModeController>(canvas);
            AddIfMissing<SoloHomeLayoutFix>(canvas);
            AddIfMissing<DeportivoProSessionSkin>(canvas);
            AddIfMissing<DeportivoProFidelityFix>(canvas);
            AddIfMissing<TrainingHistoryTracker>(canvas);
            AddIfMissing<TrainingProgressController>(canvas);
            AddIfMissing<ExerciseCueProLayoutFix>(canvas);
            AddIfMissing<TrainingOverlayOrderFix>(canvas);
            AddIfMissing<MobileBackGestureController>(canvas);
            AddIfMissing<MobileNavigationVisualFix>(canvas);
            AddIfMissing<ProOverlayNavigationController>(canvas);
            AddIfMissing<ProLogoLayoutFix>(canvas);
            AddIfMissing<AndroidSystemUiPro>(canvas);
            AddIfMissing<TrainingNavigationFix>(canvas);
        }

        private static void AddIfMissing<T>(Canvas canvas) where T : Component
        {
            if (canvas.GetComponent<T>() == null) canvas.gameObject.AddComponent<T>();
        }

        private void Update()
        {
            if (_backHooked && _modernBackButton != null) return;
            var go = FindDeep("ModernBackButton");
            if (go == null) return;
            _modernBackButton = go.GetComponent<Button>();
            if (_modernBackButton == null) return;

            _modernBackButton.onClick.RemoveAllListeners();
            _modernBackButton.onClick.AddListener(ReturnToModernHome);
            _backHooked = true;
        }

        private static void ReturnToModernHome()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private GameObject FindDeep(string objectName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == objectName) return t.gameObject;
            return null;
        }
    }
}
