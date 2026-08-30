using System.Collections;
using System.Collections.Generic;
using Entrenamiento.Core.Models;
using Entrenamiento.Core.Rules;
using Entrenamiento.Presentation;
using Entrenamiento.Transport;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.App
{
    /// <summary>
    /// Bootstrap de la sesión de entrenamiento real entre teléfonos.
    ///
    /// Flujo: pantalla de rol (Host / Estación) ->
    ///  - Host: acepta/rechaza estaciones, configura (rondas, modo clásico o
    ///    go/no-go, límite por ronda, colores, participar como estación),
    ///    dirige la sesión con SessionCoordinator y ve progreso + resumen.
    ///  - Estación: espera; al armarse, la pantalla se pinta del color; el
    ///    toque manda HIT con el tiempo medido localmente.
    ///
    /// En el Editor usa SimulatedTransport con 2 estaciones fantasma que piden
    /// unirse (hay que aceptarlas), tocan las rondas go con demora aleatoria y
    /// suelen quedarse quietas en las no-go.
    /// </summary>
    public class TrainingNearbyBootstrap : MonoBehaviour
    {
        [Header("Paneles")]
        [SerializeField] private GameObject rolePanel;
        [SerializeField] private GameObject hostConfigPanel;
        [SerializeField] private GameObject hostProgressPanel;
        [SerializeField] private GameObject stationWaitPanel;
        [SerializeField] private GameObject summaryPanel;

        [Header("Rol")]
        [SerializeField] private Button hostRoleButton;
        [SerializeField] private Button stationRoleButton;

        [Header("Config de host")]
        [SerializeField] private TMP_Text roundsValueLabel;
        [SerializeField] private Button roundsMinusButton;
        [SerializeField] private Button roundsPlusButton;
        [SerializeField] private Button modeButton;
        [SerializeField] private Button timeoutButton;
        [SerializeField] private Button colorModeButton;
        [SerializeField] private Button participateButton;
        [SerializeField] private TMP_Text participateLabel;
        [SerializeField] private TMP_Text connectedLabel;
        [SerializeField] private Button startSessionButton;

        [Header("Solicitud de conexión (host)")]
        [SerializeField] private GameObject requestCard;
        [SerializeField] private TMP_Text requestLabel;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button rejectButton;

        [Header("Progreso / estado")]
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private TMP_Text stationStatusLabel;
        [SerializeField] private TMP_Text debugLabel;

        [Header("Resumen")]
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private Button restartButton;

        [Header("Pantalla de color")]
        [SerializeField] private StationView colorView;

        [Header("Overlay (cuenta regresiva / tiempo / feedback)")]
        [SerializeField] private TMP_Text overlayLabel;

        [Header("Receiver (mismo GameObject)")]
        [SerializeField] private NearbyMessageReceiver messageReceiver;

        private enum Role { None, Host, Station }

        private Role _role = Role.None;
        private ILocalTransport _transport;
        private NearbyTransport _nearby;
        private SimulatedTransport _sim;

        private SessionCoordinator _coordinator;
        private readonly StationAgent _localAgent = new StationAgent();
        private readonly List<string> _remoteStations = new List<string>();
        private readonly List<(string id, string name)> _pendingRequests = new List<(string, string)>();
        private readonly System.Random _rng = new System.Random();

        private TMP_Text _modeLabel;
        private TMP_Text _timeoutLabel;
        private TMP_Text _colorModeLabel;

        private int _totalRounds = 10;
        private bool _participate = true;
        private int _modeIndex;      // 0 = clásico, 1 = go/no-go
        private int _timeoutIndex;   // índice en TimeoutValues
        private int _colorIndex;     // 0 = variados, 1..4 = StationColor fijo

        private string _hostEndpointId;
        private string _lastRoundText = "";
        private Coroutine _timeoutCoroutine;

        private const int MinRounds = 3;
        private const int MaxRounds = 50;
        private static readonly float[] TimeoutValues = { 0f, 3f, 5f, 10f };
        private static readonly string[] TimeoutNames = { "SIN LÍMITE", "3s", "5s", "10s" };
        private static readonly string[] ColorNames = { "VARIADOS", "ROJO", "VERDE", "AZUL", "AMARILLO" };

        // ------------------------------------------------------------------
        // Setup
        // ------------------------------------------------------------------

        private void Start()
        {
            NearbyPermissions.RequestAll();
            CreateTransport();

            // includeInactive=true: el panel de config arranca desactivado.
            _modeLabel = modeButton.GetComponentInChildren<TMP_Text>(true);
            _timeoutLabel = timeoutButton.GetComponentInChildren<TMP_Text>(true);
            _colorModeLabel = colorModeButton.GetComponentInChildren<TMP_Text>(true);

            hostRoleButton.onClick.AddListener(() => ChooseRole(Role.Host));
            stationRoleButton.onClick.AddListener(() => ChooseRole(Role.Station));
            roundsMinusButton.onClick.AddListener(() => ChangeRounds(-1));
            roundsPlusButton.onClick.AddListener(() => ChangeRounds(+1));
            modeButton.onClick.AddListener(CycleMode);
            timeoutButton.onClick.AddListener(CycleTimeout);
            colorModeButton.onClick.AddListener(CycleColorMode);
            participateButton.onClick.AddListener(ToggleParticipate);
            startSessionButton.onClick.AddListener(StartSession);
            acceptButton.onClick.AddListener(AcceptPendingRequest);
            rejectButton.onClick.AddListener(RejectPendingRequest);
            restartButton.onClick.AddListener(Restart);
            colorView.OnTapped += HandleColorTapped;

            _localAgent.OnArmed += HandleLocalArmed;
            _localAgent.OnRoundTimedOut += HandleLocalRoundTimedOut;
            _localAgent.OnSessionStarted += HandleStationSessionStarted;
            _localAgent.OnSessionEnded += HandleStationSessionEnded;

            ShowOnly(rolePanel);
            colorView.gameObject.SetActive(false);
            overlayLabel.gameObject.SetActive(false);
            requestCard.SetActive(false);
            RefreshConfigUi();
        }

        private void CreateTransport()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _nearby = new NearbyTransport(gameObject.name);
            messageReceiver.SetTransport(_nearby);
            _transport = _nearby;
#else
            _sim = new SimulatedTransport();
            _transport = _sim;
            SetDebug("Modo Editor: transporte simulado");
#endif
            _transport.OnStationConnected += HandlePeerConnected;
            _transport.OnStationDisconnected += HandlePeerDisconnected;
            _transport.OnMessageReceived += HandleIncomingMessage;

            if (_nearby != null)
            {
                _nearby.OnStatus += SetDebug;
                _nearby.OnConnectionRequest += HandleConnectionRequest;
            }
        }

        private void OnDestroy()
        {
            if (_transport != null)
            {
                _transport.OnStationConnected -= HandlePeerConnected;
                _transport.OnStationDisconnected -= HandlePeerDisconnected;
                _transport.OnMessageReceived -= HandleIncomingMessage;
            }

            _nearby?.StopAll();
        }

        // ------------------------------------------------------------------
        // Rol
        // ------------------------------------------------------------------

        private void ChooseRole(Role role)
        {
            _role = role;

            if (role == Role.Host)
            {
                _nearby?.StartHost(SystemInfo.deviceName);
                ShowOnly(hostConfigPanel);
                RefreshConfigUi();

#if UNITY_EDITOR
                StartCoroutine(SimulateStationsRequesting());
#endif
            }
            else
            {
                _nearby?.StartStation(SystemInfo.deviceName);
                ShowOnly(stationWaitPanel);
                stationStatusLabel.text = _sim != null
                    ? "El rol Estación necesita un celular real.\nEn el Editor probá el rol Host."
                    : "Buscando al host...";
            }
        }

        // ------------------------------------------------------------------
        // Solicitudes de conexión (host decide)
        // ------------------------------------------------------------------

        private void HandleConnectionRequest(string endpointId, string endpointName)
        {
            // Con una sesión en curso no se aceptan nuevas estaciones.
            if (_coordinator != null && _coordinator.IsRunning)
            {
                _nearby?.RejectStation(endpointId);
                return;
            }

            _pendingRequests.Add((endpointId, endpointName));
            RefreshRequestUi();
        }

        private void AcceptPendingRequest()
        {
            if (_pendingRequests.Count == 0)
            {
                return;
            }

            var (id, _) = _pendingRequests[0];
            _pendingRequests.RemoveAt(0);

            if (_nearby != null)
            {
                _nearby.AcceptStation(id);
            }
            else
            {
                _sim?.SimulateStationConnected(id); // Editor: conexión directa
            }

            RefreshRequestUi();
        }

        private void RejectPendingRequest()
        {
            if (_pendingRequests.Count == 0)
            {
                return;
            }

            var (id, _) = _pendingRequests[0];
            _pendingRequests.RemoveAt(0);
            _nearby?.RejectStation(id);
            RefreshRequestUi();
        }

        private void RefreshRequestUi()
        {
            bool show = _role == Role.Host && _pendingRequests.Count > 0 &&
                        (_coordinator == null || !_coordinator.IsRunning);
            requestCard.SetActive(show);

            if (show)
            {
                var (_, name) = _pendingRequests[0];
                string extra = _pendingRequests.Count > 1 ? $"  (+{_pendingRequests.Count - 1} en espera)" : "";
                requestLabel.text = $"\"{name}\" quiere unirse{extra}";
            }
        }

        // ------------------------------------------------------------------
        // Config de host
        // ------------------------------------------------------------------

        private void ChangeRounds(int delta)
        {
            _totalRounds = Mathf.Clamp(_totalRounds + delta, MinRounds, MaxRounds);
            RefreshConfigUi();
        }

        private void CycleMode()
        {
            _modeIndex = (_modeIndex + 1) % 2;
            RefreshConfigUi();
        }

        private void CycleTimeout()
        {
            _timeoutIndex = (_timeoutIndex + 1) % TimeoutValues.Length;
            RefreshConfigUi();
        }

        private void CycleColorMode()
        {
            _colorIndex = (_colorIndex + 1) % ColorNames.Length;
            RefreshConfigUi();
        }

        private void ToggleParticipate()
        {
            _participate = !_participate;
            RefreshConfigUi();
        }

        private void RefreshConfigUi()
        {
            bool goNoGo = _modeIndex == 1;

            roundsValueLabel.text = $"Rondas: {_totalRounds}";
            _modeLabel.text = goNoGo ? "Modo: GO / NO-GO" : "Modo: CLÁSICO";
            _timeoutLabel.text = goNoGo && TimeoutValues[_timeoutIndex] <= 0f
                ? "Límite por ronda: 5s (mín. del modo)"
                : $"Límite por ronda: {TimeoutNames[_timeoutIndex]}";
            _colorModeLabel.text = goNoGo
                ? "Colores: VERDE=GO / ROJO=NO"
                : $"Colores: {ColorNames[_colorIndex]}";
            colorModeButton.interactable = !goNoGo;

            participateLabel.text = _participate ? "Participo como estación: SÍ" : "Participo como estación: NO";
            connectedLabel.text = _remoteStations.Count == 0
                ? "Esperando estaciones..."
                : $"Estaciones conectadas: {_remoteStations.Count}\n{string.Join(", ", _remoteStations)}";
            startSessionButton.interactable = _remoteStations.Count > 0 || _participate;
        }

        private void StartSession()
        {
            var ids = new List<string>(_remoteStations);
            if (_participate)
            {
                ids.Add(SessionCoordinator.LocalStationId);
            }

            if (ids.Count == 0)
            {
                return;
            }

            bool goNoGo = _modeIndex == 1;
            float timeout = TimeoutValues[_timeoutIndex];
            if (goNoGo && timeout <= 0f)
            {
                timeout = 5f; // el modo necesita timeout para resolver los señuelos
            }

            var config = new SessionConfig
            {
                TotalRounds = _totalRounds,
                TimeoutSeconds = timeout,
                NoGoProbability = goNoGo ? 0.35f : 0f,
                FixedColor = (!goNoGo && _colorIndex > 0) ? (StationColor?)(StationColor)_colorIndex : null
            };

            _coordinator = new SessionCoordinator(ids, config, _rng);
            _coordinator.OnSendToStation += RouteToStation;
            _coordinator.OnBroadcast += RouteBroadcast;
            _coordinator.OnRoundStarted += HandleRoundStarted;
            _coordinator.OnRoundCompleted += HandleRoundCompleted;
            _coordinator.OnSessionFinished += HandleSessionFinished;

            requestCard.SetActive(false);
            ShowOnly(hostProgressPanel);
            _lastRoundText = "";
            progressLabel.text = "Preparados...";

            _coordinator.AnnounceStart();
            StartCoroutine(CountdownThenBegin());
        }

        private IEnumerator CountdownThenBegin()
        {
            yield return ShowCountdown();
            _coordinator.BeginRounds();
        }

        // ------------------------------------------------------------------
        // Ruteo de mensajes del coordinador (host)
        // ------------------------------------------------------------------

        private void RouteToStation(string stationId, string payload)
        {
            if (stationId == SessionCoordinator.LocalStationId)
            {
                _localAgent.HandleIncomingPayload(payload);
                return;
            }

            _transport.SendToStation(stationId, payload);

#if UNITY_EDITOR
            if (_sim != null)
            {
                StartCoroutine(SimulateStationReaction(stationId, payload));
            }
#endif
        }

        private void RouteBroadcast(string payload)
        {
            _transport.Broadcast(payload);

            if (_participate)
            {
                _localAgent.HandleIncomingPayload(payload);
            }
        }

        // ------------------------------------------------------------------
        // Eventos de sesión (host)
        // ------------------------------------------------------------------

        private void HandleRoundStarted(int round, string stationId, StationColor color, bool isGo)
        {
            string who = stationId == SessionCoordinator.LocalStationId ? "ESTE TELÉFONO" : stationId;
            string goText = _coordinator.Config.IsGoNoGo
                ? (isGo ? "\n<color=#43A047>GO: ¡a tocar!</color>" : "\n<color=#E53935>Señuelo (no tocar)</color>")
                : "";

            progressLabel.text = $"Ronda {round}/{_coordinator.TotalRounds}\n" +
                                 $"Estación activa: {who}{goText}\n\n{_lastRoundText}";

            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
                _timeoutCoroutine = null;
            }

            if (_coordinator.Config.TimeoutSeconds > 0f)
            {
                _timeoutCoroutine = StartCoroutine(RoundTimeoutTimer(round, _coordinator.Config.TimeoutSeconds));
            }
        }

        private IEnumerator RoundTimeoutTimer(int round, float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if (_coordinator != null && _coordinator.IsRunning &&
                _coordinator.CurrentRound == round && _coordinator.ArmedStationId != null)
            {
                _coordinator.HandleRoundTimeout();
            }
        }

        private void HandleRoundCompleted(ReactionEvent e, int round)
        {
            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
                _timeoutCoroutine = null;
            }

            _lastRoundText = $"Última: {DescribeResult(e)}\n" +
                             $"Aciertos: {_coordinator.HitCount}  Errores: {_coordinator.MissCount}";
        }

        private void HandleSessionFinished()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Rondas: {_coordinator.Results.Count}   " +
                          $"Aciertos: {_coordinator.HitCount}   Errores: {_coordinator.MissCount}\n");

            for (int i = 0; i < _coordinator.Results.Count; i++)
            {
                sb.AppendLine($"R{i + 1}: {DescribeResult(_coordinator.Results[i])}");
            }

            sb.AppendLine($"\nPromedio: {_coordinator.AverageSeconds():F3}s");
            sb.AppendLine($"Mejor: {_coordinator.BestSeconds():F3}s");

            summaryLabel.text = sb.ToString();
            colorView.gameObject.SetActive(false);
            ShowOnly(summaryPanel);
        }

        private string DescribeResult(ReactionEvent e)
        {
            string who = e.StationId == SessionCoordinator.LocalStationId ? "host" : e.StationId;

            if (e.Result == ReactionResult.Hit)
            {
                return e.ReactionTimeSeconds > 0f
                    ? $"<color=#A8D84F>OK</color> {who}  {e.ReactionTimeSeconds:F3}s"
                    : $"<color=#A8D84F>OK</color> {who}  (quieto en señuelo)";
            }

            return e.ReactionTimeSeconds > 0f
                ? $"<color=#E53935>X</color> {who}  (tocó el señuelo)"
                : $"<color=#E53935>X</color> {who}  (no llegó a tiempo)";
        }

        // ------------------------------------------------------------------
        // Lado estación (remota o local del host)
        // ------------------------------------------------------------------

        private void HandleLocalArmed(StationColor color, bool isGo)
        {
            colorView.SetColor(color);
            colorView.gameObject.SetActive(true);

            // Respeta el toggle de vibración de la pantalla de Ajustes.
            AppSettings.Vibrate();
        }

        private void HandleLocalRoundTimedOut(bool wasGo)
        {
            colorView.gameObject.SetActive(false);

            if (wasGo)
            {
                StartCoroutine(ShowOverlayText("¡MUY LENTO!", UiTheme.Danger, 0.9f));
                if (_role == Role.Station)
                {
                    stationStatusLabel.text = "No llegaste a tiempo.\nAtento a la próxima...";
                }
            }
            else
            {
                StartCoroutine(ShowOverlayText("¡BIEN, QUIETO!", UiTheme.AccentLime, 0.9f));
                if (_role == Role.Station)
                {
                    stationStatusLabel.text = "¡Bien! Era señuelo.\nAtento a la próxima...";
                }
            }
        }

        private void HandleColorTapped()
        {
            bool wasGo = _localAgent.LastArmWasGo;
            string hitMessage = _localAgent.RegisterTap();
            if (hitMessage == null)
            {
                return;
            }

            colorView.gameObject.SetActive(false);

            if (wasGo)
            {
                StartCoroutine(ShowReactionTime(_localAgent.LastElapsedMs));
            }
            else
            {
                StartCoroutine(ShowOverlayText("¡ERA ROJO!", UiTheme.Danger, 1.0f));
            }

            if (_role == Role.Host)
            {
                _coordinator?.HandleStationPayload(SessionCoordinator.LocalStationId, hitMessage);
            }
            else if (_hostEndpointId != null)
            {
                _transport.SendToStation(_hostEndpointId, hitMessage);
                stationStatusLabel.text = wasGo
                    ? "¡Bien! Esperando próxima ronda..."
                    : "Uy, era señuelo...\nAtento a la próxima.";
            }
        }

        private void HandleStationSessionStarted(int totalRounds)
        {
            if (_role == Role.Station)
            {
                stationStatusLabel.text = $"¡Sesión iniciada! {totalRounds} rondas.";
                StartCoroutine(StationCountdown());
            }
        }

        private IEnumerator StationCountdown()
        {
            yield return ShowCountdown();
            stationStatusLabel.text = "Atento a tu color...";
        }

        private void HandleStationSessionEnded(int hits, int misses, float avgSeconds, float bestSeconds)
        {
            if (_role != Role.Station)
            {
                return;
            }

            colorView.gameObject.SetActive(false);
            summaryLabel.text = $"Sesión terminada\n\nAciertos: {hits}   Errores: {misses}\n" +
                                $"Promedio general: {avgSeconds:F3}s\nMejor tiempo: {bestSeconds:F3}s";
            ShowOnly(summaryPanel);
        }

        // ------------------------------------------------------------------
        // Transporte
        // ------------------------------------------------------------------

        private void HandlePeerConnected(string peerId)
        {
            if (_role == Role.Station)
            {
                _hostEndpointId = peerId;
                stationStatusLabel.text = "Conectado al host.\nEsperando que arranque la sesión...";
                return;
            }

            if (!_remoteStations.Contains(peerId))
            {
                _remoteStations.Add(peerId);
            }

            RefreshConfigUi();
        }

        private void HandlePeerDisconnected(string peerId)
        {
            _remoteStations.Remove(peerId);

            if (_role == Role.Station && peerId == _hostEndpointId)
            {
                _hostEndpointId = null;
                stationStatusLabel.text = "Se perdió la conexión con el host.";
                ShowOnly(stationWaitPanel);
            }

            RefreshConfigUi();
        }

        private void HandleIncomingMessage(string senderId, string payload)
        {
            if (_role == Role.Host)
            {
                _coordinator?.HandleStationPayload(senderId, payload);
            }
            else if (_role == Role.Station)
            {
                _localAgent.HandleIncomingPayload(payload);
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private void ShowOnly(GameObject panel)
        {
            rolePanel.SetActive(panel == rolePanel);
            hostConfigPanel.SetActive(panel == hostConfigPanel);
            hostProgressPanel.SetActive(panel == hostProgressPanel);
            stationWaitPanel.SetActive(panel == stationWaitPanel);
            summaryPanel.SetActive(panel == summaryPanel);
        }

        private void SetDebug(string text)
        {
            if (debugLabel != null)
            {
                debugLabel.text = text;
            }
        }

        private void Restart()
        {
            _nearby?.StopAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ------------------------------------------------------------------
        // Overlay: cuenta regresiva, tiempo de reacción y feedback
        // ------------------------------------------------------------------

        private IEnumerator ShowCountdown()
        {
            overlayLabel.color = UiTheme.AccentLime;
            overlayLabel.gameObject.SetActive(true);

            for (int n = 3; n >= 1; n--)
            {
                overlayLabel.text = n.ToString();
                yield return PunchOverlay(1f);
            }

            overlayLabel.text = "¡YA!";
            yield return PunchOverlay(0.5f);
            overlayLabel.gameObject.SetActive(false);
        }

        private IEnumerator ShowReactionTime(int elapsedMs)
        {
            yield return ShowOverlayText($"{elapsedMs / 1000f:F3}s", UiTheme.AccentLime, 1.1f);
        }

        private IEnumerator ShowOverlayText(string text, Color color, float holdSeconds)
        {
            overlayLabel.color = color;
            overlayLabel.text = text;
            overlayLabel.gameObject.SetActive(true);
            yield return PunchOverlay(holdSeconds);
            overlayLabel.gameObject.SetActive(false);
        }

        /// <summary>Efecto "punch": arranca grande y se asienta, luego espera.</summary>
        private IEnumerator PunchOverlay(float holdSeconds)
        {
            var t = overlayLabel.transform;
            float elapsed = 0f;
            const float punchDuration = 0.18f;

            while (elapsed < punchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / punchDuration);
                float scale = Mathf.Lerp(1.6f, 1f, k);
                t.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            t.localScale = Vector3.one;
            yield return new WaitForSeconds(Mathf.Max(0f, holdSeconds - punchDuration));
        }

        // ------------------------------------------------------------------
        // Simulación solo-Editor
        // ------------------------------------------------------------------

#if UNITY_EDITOR
        private IEnumerator SimulateStationsRequesting()
        {
            yield return new WaitForSeconds(0.7f);
            _pendingRequests.Add(("sim-1", "Simulada 1"));
            RefreshRequestUi();
            yield return new WaitForSeconds(0.6f);
            _pendingRequests.Add(("sim-2", "Simulada 2"));
            RefreshRequestUi();
        }

        private IEnumerator SimulateStationReaction(string stationId, string payload)
        {
            if (!TrainingProtocol.TryParse(payload, out string type, out string[] args) ||
                type != TrainingProtocol.TypeArm ||
                args.Length < 2 ||
                !TrainingProtocol.TryParseInt(args[0], out int round))
            {
                yield break;
            }

            bool isGo = args.Length < 3 ||
                        !TrainingProtocol.TryParseInt(args[2], out int goFlag) || goFlag != 0;

            if (!isGo && Random.value < 0.7f)
            {
                // La estación simulada se queda quieta: el timeout resuelve la ronda.
                yield break;
            }

            float delay = isGo ? Random.Range(0.3f, 1.2f) : Random.Range(0.25f, 0.6f);
            yield return new WaitForSeconds(delay);
            _sim.SimulateIncomingTouch(stationId, TrainingProtocol.FormatHit(round, (int)(delay * 1000f)));
        }
#endif
    }
}
