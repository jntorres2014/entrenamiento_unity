using System.Collections;
using Entrenamiento.Core.Models;
using Entrenamiento.Core.Rules;
using Entrenamiento.Transport;
using UnityEngine;

namespace Entrenamiento.App
{
    /// <summary>
    /// Bootstrap de prueba para la Tarea 1: arma una TrainingSession con un
    /// SimulatedTransport, registra un par de estaciones, y corre un ciclo
    /// completo (activar estación -> simular toque -> ver acierto y tiempo
    /// de reacción en consola) sin necesidad de un segundo teléfono.
    ///
    /// Este es el único MonoBehaviour de la tarea: solo arma dependencias y
    /// dispara el flujo, no contiene reglas de negocio (eso vive en Core).
    /// </summary>
    public class TrainingBootstrap : MonoBehaviour
    {
        [Tooltip("Segundos simulados de demora antes de que la estación registre el toque")]
        [SerializeField] private float simulatedReactionDelaySeconds = 1.2f;

        private TrainingSession _session;
        private SimulatedTransport _transport;

        private void Start()
        {
            SetupSession();
            _transport.OnMessageReceived += HandleStationMessage;

            StartCoroutine(RunTestCycle());
        }

        private void OnDestroy()
        {
            if (_transport != null)
            {
                _transport.OnMessageReceived -= HandleStationMessage;
            }
        }

        private void SetupSession()
        {
            _session = new TrainingSession();
            _transport = new SimulatedTransport();

            var stationA = new Station("station-a", "Estación A");
            var stationB = new Station("station-b", "Estación B");

            _session.RegisterStation(stationA);
            _session.RegisterStation(stationB);

            _transport.SimulateStationConnected(stationA.Id);
            _transport.SimulateStationConnected(stationB.Id);

            _session.OnReactionRegistered += reactionEvent =>
            {
                Debug.Log($"[TrainingBootstrap] Evento registrado: {reactionEvent}. " +
                          $"Aciertos={_session.HitCount} Errores={_session.MissCount}");
            };
        }

        private IEnumerator RunTestCycle()
        {
            const string targetStationId = "station-a";

            Debug.Log($"[TrainingBootstrap] Activando {targetStationId} con color Red...");
            _session.ActivateStation(targetStationId, StationColor.Red);
            _transport.SendToStation(targetStationId, "ACTIVATE:Red");

            yield return new WaitForSeconds(simulatedReactionDelaySeconds);

            Debug.Log($"[TrainingBootstrap] Simulando toque en {targetStationId}...");
            _transport.SimulateIncomingTouch(targetStationId, "TOUCH");
        }

        private void HandleStationMessage(string stationId, string payload)
        {
            if (payload == "TOUCH")
            {
                _session.RegisterTouch(stationId);
            }
        }
    }
}
