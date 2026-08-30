using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Pulso de escala continuo (por ejemplo, el "¡TOCÁ!" sobre la pantalla
    /// de color). Se reinicia cada vez que el objeto se activa.
    /// </summary>
    public class PulseScale : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.08f;
        [SerializeField] private float speed = 3.5f;

        private void OnEnable()
        {
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * speed) * amplitude;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
