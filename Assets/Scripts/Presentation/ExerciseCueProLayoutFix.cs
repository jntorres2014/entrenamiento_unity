using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// ExerciseRuntimeEnhancer crea la consigna recién al iniciar la primera
    /// ronda. Este componente la integra al arena Deportivo Pro cuando aparece.
    /// </summary>
    public sealed class ExerciseCueProLayoutFix : MonoBehaviour
    {
        private Canvas _canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<ExerciseCueProLayoutFix>() == null)
                {
                    canvas.gameObject.AddComponent<ExerciseCueProLayoutFix>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void LateUpdate()
        {
            var progress = FindDeep("HostProgressPanel");
            if (progress == null || !progress.activeInHierarchy) return;

            var cue = FindDeep("ExerciseCueCard");
            if (cue == null || !cue.activeInHierarchy) return;

            var rect = cue.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.18f, 0.405f);
                rect.anchorMax = new Vector2(0.82f, 0.545f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            var image = cue.GetComponent<Image>();
            if (image != null)
                image.color = new Color(UiTheme.Surface.r, UiTheme.Surface.g, UiTheme.Surface.b, 0.96f);

            var label = cue.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = UiTheme.TextPrimary;
                label.fontStyle |= FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.fontSizeMax = 26f;
                label.fontSizeMin = 16f;
            }

            cue.transform.SetAsLastSibling();
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
