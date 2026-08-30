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

            if (canvas.GetComponent<TrainingModernUiController>() == null)
                canvas.gameObject.AddComponent<TrainingModernUiController>();
            if (canvas.GetComponent<TrainingUiMagic>() == null)
                canvas.gameObject.AddComponent<TrainingUiMagic>();
            if (canvas.GetComponent<ARTrainingModeController>() == null)
                canvas.gameObject.AddComponent<ARTrainingModeController>();
            if (canvas.GetComponent<CameraTrainingModeController>() == null)
                canvas.gameObject.AddComponent<CameraTrainingModeController>();
            if (canvas.GetComponent<TrainingHomeCView>() == null)
                canvas.gameObject.AddComponent<TrainingHomeCView>();
            if (canvas.GetComponent<TrainingFlowCView>() == null)
                canvas.gameObject.AddComponent<TrainingFlowCView>();
            if (canvas.GetComponent<TrainingOverlayOrderFix>() == null)
                canvas.gameObject.AddComponent<TrainingOverlayOrderFix>();
            if (canvas.GetComponent<TrainingNavigationFix>() == null)
                canvas.gameObject.AddComponent<TrainingNavigationFix>();
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
            {
                if (t.name == objectName) return t.gameObject;
            }
            return null;
        }
    }
}
