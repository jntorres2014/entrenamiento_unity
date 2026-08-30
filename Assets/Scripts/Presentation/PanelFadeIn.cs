using System.Collections;
using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Transición suave: cada vez que el panel se activa, hace fade-in
    /// (alpha 0 -> 1) usando CanvasGroup.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PanelFadeIn : MonoBehaviour
    {
        [SerializeField] private float duration = 0.25f;

        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
            }

            StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            float t = 0f;
            _group.alpha = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }

            _group.alpha = 1f;
        }
    }
}
