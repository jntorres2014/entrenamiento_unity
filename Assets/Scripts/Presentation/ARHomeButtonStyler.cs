using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Reaplica el acento del botón AR después de que la capa visual general
    /// termine de estilizar la portada.
    /// </summary>
    public sealed class ARHomeButtonStyler : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<ARHomeButtonStyler>() == null)
                {
                    canvas.gameObject.AddComponent<ARHomeButtonStyler>();
                    break;
                }
            }
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;

            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button != null && button.name == "ARTrainingButton")
                {
                    TrainingUiStyler.StylePrimary(button);
                    yield break;
                }
            }
        }
    }
}
