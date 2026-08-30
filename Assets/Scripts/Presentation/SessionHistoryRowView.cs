using System;
using Entrenamiento.Core.History;
using Entrenamiento.Core.Models;
using TMPro;
using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Fila del historial de sesiones: muestra modo, fecha, aciertos/errores y
    /// tiempo promedio de un SessionRecord. Vista fina: los textos vienen de
    /// SessionHistoryFormat y los colores de UiTheme.
    /// </summary>
    public class SessionHistoryRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text modeLabel;
        [SerializeField] private TMP_Text dateLabel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text averageLabel;

        public void Bind(SessionRecord record, DateTime now)
        {
            modeLabel.text = SessionHistoryFormat.ModeName(record.Mode);
            dateLabel.text = SessionHistoryFormat.RelativeDate(now, record.EndedAt);

            // Rich text con los colores del tema (UiTheme sigue siendo la
            // única fuente de verdad; acá solo se convierten a hex).
            string hitsHex = ColorUtility.ToHtmlStringRGB(UiTheme.AccentLime);
            string missesHex = ColorUtility.ToHtmlStringRGB(UiTheme.Danger);
            scoreLabel.text =
                $"<color=#{hitsHex}>{SessionHistoryFormat.Hits(record.Hits)}</color>" +
                $"<color=#{ColorUtility.ToHtmlStringRGBA(UiTheme.TextSecondary)}>  ·  </color>" +
                $"<color=#{missesHex}>{SessionHistoryFormat.Misses(record.Misses)}</color>";

            averageLabel.text = SessionHistoryFormat.AverageTime(record.AverageReactionSeconds);
        }
    }
}
