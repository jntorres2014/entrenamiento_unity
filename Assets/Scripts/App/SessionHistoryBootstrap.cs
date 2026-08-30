using System;
using System.Collections.Generic;
using Entrenamiento.Core.History;
using Entrenamiento.Core.Models;
using Entrenamiento.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.App
{
    /// <summary>
    /// Bootstrap de la pantalla de historial de sesiones: llena la lista con
    /// las últimas sesiones de InMemorySessionHistory (datos de ejemplo por
    /// ahora), muestra el estado vacío si no hay ninguna, y vuelve a la escena
    /// anterior con el botón VOLVER. MonoBehaviour fino: formato y datos
    /// viven en Core.
    /// </summary>
    public class SessionHistoryBootstrap : MonoBehaviour
    {
        [Header("Lista")]
        [SerializeField] private GameObject listRoot;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private SessionHistoryRowView rowTemplate;

        [Header("Estado vacío")]
        [SerializeField] private GameObject emptyState;

        [Header("Navegación")]
        [SerializeField] private Button backButton;
        [SerializeField] private string backSceneName = "TrainingNearby";

        [Header("Datos (en memoria por ahora)")]
        [Tooltip("Activar para probar el estado vacío sin tocar los datos.")]
        [SerializeField] private bool forceEmptyState;

        private const int MaxRows = 20;

        private void Start()
        {
            backButton.onClick.AddListener(GoBack);

            IReadOnlyList<SessionRecord> records = forceEmptyState
                ? System.Array.Empty<SessionRecord>()
                : InMemorySessionHistory.Shared.GetRecent(MaxRows);

            Populate(records);
        }

        private void Populate(IReadOnlyList<SessionRecord> records)
        {
            bool hasAny = records.Count > 0;
            emptyState.SetActive(!hasAny);
            listRoot.SetActive(hasAny);

            if (!hasAny)
            {
                return;
            }

            DateTime now = DateTime.Now;

            foreach (SessionRecord record in records)
            {
                SessionHistoryRowView row = Instantiate(rowTemplate, listContent);
                row.gameObject.SetActive(true);
                row.Bind(record, now);
            }
        }

        private void GoBack()
        {
            if (!Application.CanStreamedLevelBeLoaded(backSceneName))
            {
                Debug.LogWarning(
                    $"[SessionHistory] La escena '{backSceneName}' no está en Build Settings; " +
                    "no se puede volver.");
                return;
            }

            SceneManager.LoadScene(backSceneName);
        }
    }
}
