using UnityEngine;
using UnityEngine.SceneManagement;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Mantiene el fondo semitransparente del overlay justo debajo del texto
    /// de cuenta regresiva/feedback, incluso después de activaciones sucesivas.
    /// </summary>
    public sealed class TrainingOverlayOrderFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<TrainingOverlayOrderFix>() == null)
                {
                    canvas.gameObject.AddComponent<TrainingOverlayOrderFix>();
                    break;
                }
            }
        }

        private void LateUpdate()
        {
            var overlay = FindDeep("Overlay");
            var backdrop = FindDeep("ModernOverlayBackdrop");
            if (overlay == null || backdrop == null || !backdrop.activeSelf) return;

            int overlayIndex = overlay.transform.GetSiblingIndex();
            int desired = Mathf.Max(0, overlayIndex - 1);
            if (backdrop.transform.GetSiblingIndex() != desired)
            {
                backdrop.transform.SetSiblingIndex(desired);
            }
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
