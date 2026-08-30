using Entrenamiento.Core.Models;
using Entrenamiento.Core.Rules;
using Entrenamiento.Presentation;
using Entrenamiento.Transport;
using UnityEngine;

namespace Entrenamiento.App
{
    /// <summary>
    /// Bootstrap de la Tarea 2: prueba de un solo dispositivo. Arma TrainingSession
    /// y SimulatedTransport (igual que en la Tarea 1), pero en vez de simular el
    /// toque con un delay automático, espera el toque real del usuario sobre
    /// StationView y recién ahí dispara el flujo de registro de evento.
    ///
    /// Sigue usando ILocalTransport.SimulateIncomingTouch como punto de entrada:
    /// en la versión real, ese mismo evento (OnMessageReceived) llegaría desde
    /// una estación remota a través de Nearby Connections. Acá el "mensaje"
    /// se dispara localmente porque el anfitrión también actúa como estación.
    /// </summary>
    public class StationTestBootstrap : MonoBehaviour
    {
        [SerializeField] private StationView stationView;
        [SerializeField] private ResultLabel resultLabel;

        private const string StationId = "station-a";

        private TrainingSession _session;
        private SimulatedTransport _transport;

        private void Start()
        {
            if (stationView == null || resultLabel == null)
            {
                Debug.LogError("[StationTestBootstrap] Faltan referencias: asigná StationView y ResultLabel en el Inspector.");
                return;
            }

            SetupSession();

            stationView.OnTapped += HandleStationTapped;
            _transport.OnMessageReceived += HandleStationMessage;
            _session.OnReactionRegistered += HandleReactionRegistered;

            ActivateStation();
        }

        private void OnDestroy()
        {
            if (stationView != null)
            {
                stationView.OnTapped -= HandleStationTapped;
            }

            if (_transport != null)
            {
                _transport.OnMessageReceived -= HandleStationMessage;
            }

            if (_session != null)
            {
                _session.OnReactionRegistered -= HandleReactionRegistered;
            }
        }

        private void SetupSession()
        {
            _session = new TrainingSession();
            _transport = new SimulatedTransport();

            var station = new Station(StationId, "Estación A");
            _session.RegisterStation(station);
            _transport.SimulateStationConnected(StationId);
        }

        private void ActivateStation()
        {
            Debug.Log($"[StationTestBootstrap] Activando {StationId} con color Red. Tocá la pantalla.");
            _session.ActivateStation(StationId, StationColor.Red);
            stationView.SetColor(StationColor.Red);
        }

        private void HandleStationTapped()
        {
            // En un dispositivo real remoto, este toque viajaría por Nearby Connections
            // hasta el anfitrión. Acá simulamos esa llegada localmente.
            _transport.SimulateIncomingTouch(StationId, "TOUCH");
        }

        private void HandleStationMessage(string stationId, string payload)
        {
            if (payload == "TOUCH")
            {
                _session.RegisterTouch(stationId);
            }
        }

        private void HandleReactionRegistered(ReactionEvent reactionEvent)
        {
            Debug.Log($"[StationTestBootstrap] {reactionEvent}. Aciertos={_session.HitCount} Errores={_session.MissCount}");
            resultLabel.ShowResult(reactionEvent);
        }
    }
}
