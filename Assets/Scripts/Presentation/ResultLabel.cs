using Entrenamiento.Core.Models;
using TMPro;
using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// MonoBehaviour "fino": solo formatea y muestra texto. No contiene reglas
    /// de negocio; recibe un ReactionEvent ya calculado por Core.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ResultLabel : MonoBehaviour
    {
        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _text.text = "Esperando toque...";
        }

        public void ShowResult(ReactionEvent reactionEvent)
        {
            if (reactionEvent.Result == ReactionResult.Hit)
            {
                _text.text = $"¡Acierto! Tiempo: {reactionEvent.ReactionTimeSeconds:F3}s";
            }
            else
            {
                _text.text = "Error";
            }
        }

        /// <summary>
        /// Muestra un texto de estado simple, sin pasar por un ReactionEvent.
        /// Usado por pruebas que no son parte del flujo de entrenamiento en sí
        /// (por ejemplo, la prueba de puente Unity-Kotlin).
        /// </summary>
        public void SetText(string text)
        {
            _text.text = text;
        }
    }
}
