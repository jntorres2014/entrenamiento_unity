using System;
using System.Collections.Generic;
using Entrenamiento.Core.Models;
using Entrenamiento.Core.Rules;
using Entrenamiento.Presentation;
using Entrenamiento.Transport;
using UnityEngine;

namespace Entrenamiento.App
{
    /// <summary>
    /// Bootstrap de la Tarea 3: prueba de 2 a 4 estaciones simuladas en el mismo
    /// dispositivo. Cada StationView asignado en el Inspector representa una
    /// estación distinta. En cada ronda se activa una al azar (evitando repetir
    /// la anterior); tocar la correcta registra un acierto y arranca la siguiente
    /// ronda, tocar una incorrecta registra un error sin interrumpir la ronda.
    ///
    /// Sigue usando SimulatedTransport: no hay comunicación real entre
    /// dispositivos en esta tarea.
    /// </summary>
    public class MultiStationTestBootstrap : MonoBehaviour
    {
        [Tooltip("Entre 2 y 4 StationView, cada uno representa una estación distinta")]
        [SerializeField] private StationView[] stationViews;

        [SerializeField] private ResultLabel resultLabel;

        [SerializeField] private StationColor activeColor = StationColor.Red;

        private readonly Dictionary<StationView, string> _viewToStationId = new Dictionary<StationView, string>();
        private readonly Dictionary<string, StationView> _stationIdToView = new Dictionary<string, StationView>();
        private readonly Dictionary<StationView, Action> _tapHandlers = new Dictionary<StationView, Action>();

        private TrainingSession _session;
        private SimulatedTransport _transport;
        private readonly System.Random _rng = new System.Random();

        private void Start()
        {
            if (stationViews == null || stationViews.Length < 2 || stationViews.Length > 4)
            {
                Debug.LogError("[MultiStationTestBootstrap] Asigná entre 2 y 4 StationView en el Inspector.");
                return;
            }

            if (resultLabel == null)
            {
                Debug.LogError("[MultiStationTestBootstrap] Falta asignar ResultLabel en el Inspector.");
                return;
            }

            SetupSession();
            ActivateNextRound();
        }

        private void OnDestroy()
        {
            foreach (var kvp in _tapHandlers)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.OnTapped -= kvp.Value;
                }
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

            for (int i = 0; i < stationViews.Length; i++)
            {
                string stationId = $"station-{i}";
                var station = new Station(stationId, $"Estación {i + 1}");

                _session.RegisterStation(station);
                _transport.SimulateStationConnected(stationId);

                _viewToStationId[stationViews[i]] = stationId;
                _stationIdToView[stationId] = stationViews[i];

                var view = stationViews[i];
                view.SetColor(StationColor.None);

                Action tapHandler = () => HandleStationTapped(view);
                _tapHandlers[view] = tapHandler;
                view.OnTapped += tapHandler;
            }

            _transport.OnMessageReceived += HandleStationMessage;
            _session.OnReactionRegistered += HandleReactionRegistered;
        }

        private void ActivateNextRound()
        {
            foreach (var view in stationViews)
            {
                view.SetColor(StationColor.None);
            }

            string activeStationId = _session.ActivateRandomStation(activeColor, _rng);
            _stationIdToView[activeStationId].SetColor(activeColor);

            Debug.Log($"[MultiStationTestBootstrap] Estación activa: {activeStationId}");
        }

        private void HandleStationTapped(StationView view)
        {
            string stationId = _viewToStationId[view];
            _transport.SimulateIncomingTouch(stationId, "TOUCH");
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
            Debug.Log($"[MultiStationTestBootstrap] {reactionEvent}. Aciertos={_session.HitCount} Errores={_session.MissCount}");
            resultLabel.ShowResult(reactionEvent);

            if (reactionEvent.Result == ReactionResult.Hit)
            {
                ActivateNextRound();
            }
        }
    }
}
