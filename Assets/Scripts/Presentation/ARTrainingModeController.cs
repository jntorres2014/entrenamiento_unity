using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Modo de entrenamiento AR pensado para usar el teléfono apoyado, sin trípode.
    /// Flujo: detectar piso -> colocar 4 zonas -> apoyar/fijar cámara -> capturar
    /// fondo vacío -> iniciar -> detectar movimiento del jugador y proyectar sus
    /// pies al piso para validar la llegada al objetivo activo.
    /// </summary>
    public sealed class ARTrainingModeController : MonoBehaviour
    {
        private enum State
        {
            Home,
            Scanning,
            Placing,
            CameraCountdown,
            Ready,
            Running,
            PausedCameraMoved,
            Finished
        }

        private static readonly Color[] ZoneColors =
        {
            new Color32(0x3D, 0x8B, 0xFF, 0xFF),
            new Color32(0x45, 0xD4, 0x75, 0xFF),
            new Color32(0xFF, 0xC8, 0x3D, 0xFF),
            new Color32(0xEF, 0x53, 0x50, 0xFF)
        };

        private static readonly string[] ZoneNames = { "AZUL", "VERDE", "AMARILLO", "ROJO" };
        private static readonly List<ARRaycastHit> RaycastHits = new List<ARRaycastHit>();

        private Canvas _canvas;
        private State _state = State.Home;
        private GameObject _rolePanel;
        private GameObject _arUiRoot;
        private GameObject _modernBackdrop;
        private GameObject _modernBackButton;
        private TrainingModernUiController _modernUi;
        private TrainingUiMagic _uiMagic;

        private ARSession _arSession;
        private XROrigin _xrOrigin;
        private ARRaycastManager _raycastManager;
        private ARPlaneManager _planeManager;
        private ARCameraManager _cameraManager;
        private Camera _arCamera;

        private TMP_Text _title;
        private TMP_Text _status;
        private TMP_Text _counter;
        private TMP_Text _result;
        private Button _primaryButton;
        private TMP_Text _primaryLabel;
        private Button _backButton;

        private readonly List<Transform> _zones = new List<Transform>();
        private readonly List<Renderer> _zoneRenderers = new List<Renderer>();
        private int _activeZone = -1;
        private int _lastZone = -1;
        private int _points;
        private int _targetCount = 10;
        private float _targetStartedAt;
        private float _bestTime = float.MaxValue;
        private float _totalTime;
        private float _insideSince = -1f;

        private Vector3 _lockedCameraPosition;
        private Quaternion _lockedCameraRotation;
        private const float MaxCameraMovementMeters = 0.15f;
        private const float MaxCameraRotationDegrees = 10f;
        private const float MinimumZoneDistanceFromCamera = 1.20f;
        private const float ZoneHitRadius = 0.85f;
        private const float RequiredInsideSeconds = 0.18f;

        // Detector simple de persona para cámara fija: diferencia contra fondo vacío.
        private byte[] _backgroundFrame;
        private int _frameWidth;
        private int _frameHeight;
        private float _nextVisionSample;
        private const float VisionSampleInterval = 0.10f;
        private const byte MotionThreshold = 30;
        private const int MinMotionPixels = 35;

        private Material _zoneMaterialTemplate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<ARTrainingModeController>() == null)
                {
                    canvas.gameObject.AddComponent<ARTrainingModeController>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _rolePanel = FindDeep("RolePanel");
            _modernBackdrop = FindDeep("ModernBackdrop");
            _modernBackButton = FindDeep("ModernBackButton");
            _modernUi = GetComponent<TrainingModernUiController>();
            _uiMagic = GetComponent<TrainingUiMagic>();

            AddARButtonToHome();
        }

        private void AddARButtonToHome()
        {
            if (_rolePanel == null || FindDeep("ARTrainingButton") != null) return;

            Button host = FindButton("HostRoleButton");
            Button station = FindButton("StationRoleButton");

            if (host != null) SetRoleButtonRect(host.GetComponent<RectTransform>(), 0.56f, 0.72f);
            if (station != null) SetRoleButtonRect(station.GetComponent<RectTransform>(), 0.35f, 0.51f);

            var button = CreateButton(_rolePanel.transform, "ARTrainingButton", "✨  AR TRAINING\n<size=70%>Entrenar usando el espacio real</size>", UiTheme.Accent);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.14f);
            rect.anchorMax = new Vector2(0.92f, 0.30f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            button.onClick.AddListener(StartARMode);
        }

        private static void SetRoleButtonRect(RectTransform rect, float yMin, float yMax)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0.08f, yMin);
            rect.anchorMax = new Vector2(0.92f, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void StartARMode()
        {
#if UNITY_EDITOR
            Debug.Log("[AR] El modo AR necesita ejecutarse en un dispositivo Android compatible con ARCore.");
#endif
            HideNormalUi();
            CreateARUi();
            CreateARSessionObjects();
            _state = State.Scanning;
            _status.text = "Mové lentamente el teléfono apuntando al piso.\nCuando detectemos una superficie, vas a poder ubicar las 4 zonas.";
            _title.text = "ESCANEAR CAMPO";
            _primaryButton.gameObject.SetActive(false);
            StartCoroutine(CheckAvailability());
        }

        private IEnumerator CheckAvailability()
        {
            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
            {
                yield return ARSession.CheckAvailability();
            }

            if (ARSession.state == ARSessionState.Unsupported)
            {
                _status.text = "Este dispositivo no informa soporte para ARCore.\nPodés volver atrás y seguir usando el entrenamiento clásico.";
                _title.text = "AR NO DISPONIBLE";
                yield break;
            }

            if (_arSession != null) _arSession.enabled = true;
            if (_xrOrigin != null) _xrOrigin.gameObject.SetActive(true);
        }

        private void HideNormalUi()
        {
            string[] panels = { "RolePanel", "HostConfigPanel", "HostProgressPanel", "StationWaitPanel", "SummaryPanel" };
            foreach (string name in panels)
            {
                var panel = FindDeep(name);
                if (panel != null) panel.SetActive(false);
            }

            if (_modernUi != null) _modernUi.enabled = false;
            if (_uiMagic != null) _uiMagic.enabled = false;
            if (_modernBackdrop != null) _modernBackdrop.SetActive(false);
            if (_modernBackButton != null) _modernBackButton.SetActive(false);
        }

        private void CreateARSessionObjects()
        {
            var oldCamera = Camera.main;
            if (oldCamera != null)
            {
                oldCamera.gameObject.SetActive(false);
            }

            var sessionGo = new GameObject("AR Session");
            _arSession = sessionGo.AddComponent<ARSession>();
            sessionGo.AddComponent<ARInputManager>();

            var originGo = new GameObject("XR Origin AR");
            _xrOrigin = originGo.AddComponent<XROrigin>();
            _raycastManager = originGo.AddComponent<ARRaycastManager>();
            _planeManager = originGo.AddComponent<ARPlaneManager>();
            _planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;

            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(originGo.transform, false);

            var cameraGo = new GameObject("AR Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(cameraOffset.transform, false);
            _arCamera = cameraGo.AddComponent<Camera>();
            _arCamera.nearClipPlane = 0.05f;
            _arCamera.farClipPlane = 30f;
            _cameraManager = cameraGo.AddComponent<ARCameraManager>();
            cameraGo.AddComponent<ARCameraBackground>();
            cameraGo.AddComponent<TrackedPoseDriver>();

            _xrOrigin.Camera = _arCamera;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null)
            {
                _zoneMaterialTemplate = new Material(shader);
            }
        }

        private void CreateARUi()
        {
            _arUiRoot = new GameObject("ARTrainingUI", typeof(RectTransform), typeof(CanvasGroup));
            _arUiRoot.transform.SetParent(_canvas.transform, false);
            var rootRect = _arUiRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var topCard = CreateImage(_arUiRoot.transform, "TopCard", new Color(0.03f, 0.05f, 0.08f, 0.90f));
            var topRect = topCard.rectTransform;
            topRect.anchorMin = new Vector2(0.04f, 0.77f);
            topRect.anchorMax = new Vector2(0.96f, 0.96f);
            topRect.offsetMin = Vector2.zero;
            topRect.offsetMax = Vector2.zero;

            _title = CreateText(topCard.transform, "Title", "AR TRAINING", 34, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            SetRect(_title.rectTransform, 0.04f, 0.55f, 0.96f, 0.94f);
            _title.color = UiTheme.Accent;

            _status = CreateText(topCard.transform, "Status", "", 23, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetRect(_status.rectTransform, 0.04f, 0.08f, 0.96f, 0.58f);
            _status.color = UiTheme.TextPrimary;

            _counter = CreateText(_arUiRoot.transform, "Counter", "", 42, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_counter.rectTransform, 0.08f, 0.60f, 0.92f, 0.76f);
            _counter.color = UiTheme.TextPrimary;

            _result = CreateText(_arUiRoot.transform, "Result", "", 52, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_result.rectTransform, 0.06f, 0.32f, 0.94f, 0.58f);
            _result.color = UiTheme.TextPrimary;

            _primaryButton = CreateButton(_arUiRoot.transform, "ARPrimaryButton", "CONTINUAR", UiTheme.Accent);
            var primaryRect = _primaryButton.GetComponent<RectTransform>();
            primaryRect.anchorMin = new Vector2(0.08f, 0.06f);
            primaryRect.anchorMax = new Vector2(0.92f, 0.15f);
            primaryRect.offsetMin = Vector2.zero;
            primaryRect.offsetMax = Vector2.zero;
            _primaryLabel = _primaryButton.GetComponentInChildren<TMP_Text>(true);

            _backButton = CreateButton(_arUiRoot.transform, "ARBackButton", "←  ATRÁS", new Color32(0x20, 0x2A, 0x37, 0xF2));
            var backRect = _backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.04f, 0.89f);
            backRect.anchorMax = new Vector2(0.30f, 0.96f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;
            _backButton.transform.SetAsLastSibling();
            _backButton.onClick.AddListener(ExitARMode);
        }

        private void Update()
        {
            if (_state == State.Home) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ExitARMode();
                return;
            }

            switch (_state)
            {
                case State.Scanning:
                    UpdateScanning();
                    break;
                case State.Placing:
                    UpdatePlacement();
                    break;
                case State.Ready:
                    UpdateCameraStability(false);
                    break;
                case State.Running:
                    UpdateCameraStability(true);
                    UpdateRunning();
                    break;
                case State.PausedCameraMoved:
                    break;
            }
        }

        private void UpdateScanning()
        {
            if (_planeManager == null) return;

            int planeCount = 0;
            foreach (var plane in _planeManager.trackables)
            {
                if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp) planeCount++;
            }

            if (planeCount <= 0) return;

            _state = State.Placing;
            _title.text = "UBICAR ZONAS";
            _status.text = "Piso detectado ✓\nTocá cuatro lugares del campo. Dejalos separados y a más de 1,2 m de la cámara.";
            _counter.text = "0 / 4 ZONAS";
        }

        private void UpdatePlacement()
        {
            if (_zones.Count >= 4) return;
            if (!TryGetPointerDown(out Vector2 screenPosition)) return;
            if (_raycastManager == null) return;

            RaycastHits.Clear();
            if (!_raycastManager.Raycast(screenPosition, RaycastHits, TrackableType.PlaneWithinPolygon)) return;

            Pose pose = RaycastHits[0].pose;
            if (_arCamera != null && Vector3.Distance(_arCamera.transform.position, pose.position) < MinimumZoneDistanceFromCamera)
            {
                StartCoroutine(TemporaryStatus("Esa zona está demasiado cerca de la cámara. Elegí un punto un poco más lejos."));
                return;
            }

            foreach (var zone in _zones)
            {
                if (Vector3.Distance(zone.position, pose.position) < 0.9f)
                {
                    StartCoroutine(TemporaryStatus("Separá un poco más las zonas para que el recorrido sea claro."));
                    return;
                }
            }

            CreateZone(pose.position, _zones.Count);
            _counter.text = $"{_zones.Count} / 4 ZONAS";

            if (_zones.Count == 4)
            {
                _status.text = "Campo listo ✓\nAhora apoyá el teléfono contra una mochila, botella, banco o cualquier soporte firme.";
                _primaryLabel.text = "FIJAR CÁMARA";
                _primaryButton.gameObject.SetActive(true);
                _primaryButton.onClick.RemoveAllListeners();
                _primaryButton.onClick.AddListener(BeginCameraLockCountdown);
            }
        }

        private void CreateZone(Vector3 position, int index)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = $"AR Zone {index + 1} - {ZoneNames[index]}";
            go.transform.position = position + Vector3.up * 0.025f;
            go.transform.localScale = new Vector3(0.65f, 0.025f, 0.65f);

            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            if (_zoneMaterialTemplate != null)
            {
                renderer.material = new Material(_zoneMaterialTemplate);
            }
            SetRendererColor(renderer, ZoneColors[index]);

            _zones.Add(go.transform);
            _zoneRenderers.Add(renderer);
        }

        private void BeginCameraLockCountdown()
        {
            if (_state != State.Placing || _zones.Count < 4) return;
            _primaryButton.gameObject.SetActive(false);
            StartCoroutine(CameraLockRoutine());
        }

        private IEnumerator CameraLockRoutine()
        {
            _state = State.CameraCountdown;
            _title.text = "FIJAR CÁMARA";
            _status.text = "Apoyá el teléfono y alejate del campo. No lo muevas mientras termina la cuenta.";

            for (int i = 5; i >= 1; i--)
            {
                _result.text = i.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }

            _result.text = "QUIETO";
            yield return new WaitForSecondsRealtime(1f);

            _lockedCameraPosition = _arCamera.transform.position;
            _lockedCameraRotation = _arCamera.transform.rotation;

            bool captured = CaptureBackgroundFrame();
            _result.text = captured ? "✓" : "";
            _title.text = "CAMPO CALIBRADO";
            _status.text = captured
                ? "Cámara estable y fondo capturado ✓\nEntrá al campo cuando empiece la cuenta regresiva."
                : "Cámara estable ✓\nNo pudimos capturar el fondo todavía; intentaremos nuevamente al iniciar.";

            _state = State.Ready;
            _primaryLabel.text = "INICIAR ENTRENAMIENTO";
            _primaryButton.gameObject.SetActive(true);
            _primaryButton.onClick.RemoveAllListeners();
            _primaryButton.onClick.AddListener(() => StartCoroutine(StartGameRoutine()));
        }

        private IEnumerator StartGameRoutine()
        {
            if (_state != State.Ready) yield break;
            _primaryButton.gameObject.SetActive(false);

            if (_backgroundFrame == null)
            {
                CaptureBackgroundFrame();
            }

            _points = 0;
            _totalTime = 0f;
            _bestTime = float.MaxValue;
            _insideSince = -1f;
            _result.text = "";
            _status.text = "Preparado...";

            for (int i = 3; i >= 1; i--)
            {
                _counter.text = i.ToString();
                yield return Punch(_counter.transform, 0.45f);
                yield return new WaitForSecondsRealtime(0.35f);
            }

            _counter.text = "¡YA!";
            yield return Punch(_counter.transform, 0.35f);
            _state = State.Running;
            ActivateNextZone();
        }

        private void ActivateNextZone()
        {
            if (_points >= _targetCount)
            {
                FinishGame();
                return;
            }

            do
            {
                _activeZone = UnityEngine.Random.Range(0, _zones.Count);
            } while (_zones.Count > 1 && _activeZone == _lastZone);

            _lastZone = _activeZone;
            _insideSince = -1f;
            _targetStartedAt = Time.unscaledTime;

            for (int i = 0; i < _zoneRenderers.Count; i++)
            {
                SetRendererColor(_zoneRenderers[i], WithAlpha(ZoneColors[i], i == _activeZone ? 1f : 0.35f));
                _zones[i].localScale = i == _activeZone
                    ? new Vector3(0.82f, 0.03f, 0.82f)
                    : new Vector3(0.58f, 0.025f, 0.58f);
            }

            _counter.text = $"{_points + 1} / {_targetCount}";
            _result.text = ZoneNames[_activeZone];
            _result.color = ZoneColors[_activeZone];
            _status.text = "Corré hasta la zona indicada.";
        }

        private void UpdateRunning()
        {
            if (_backgroundFrame == null || _cameraManager == null || _raycastManager == null) return;
            if (Time.unscaledTime < _nextVisionSample) return;
            _nextVisionSample = Time.unscaledTime + VisionSampleInterval;

            if (!TryDetectPlayerFeet(out Vector2 feetScreen))
            {
                _insideSince = -1f;
                return;
            }

            RaycastHits.Clear();
            if (!_raycastManager.Raycast(feetScreen, RaycastHits, TrackableType.PlaneWithinPolygon))
            {
                _insideSince = -1f;
                return;
            }

            Vector3 floorPoint = RaycastHits[0].pose.position;
            float cameraDistance = Vector3.Distance(_arCamera.transform.position, floorPoint);
            if (cameraDistance < MinimumZoneDistanceFromCamera)
            {
                _insideSince = -1f;
                _status.text = "⚠ Alejate de la cámara para volver al campo válido.";
                return;
            }

            _status.text = "Jugador detectado ✓";
            float distanceToTarget = Vector3.Distance(floorPoint, _zones[_activeZone].position);

            if (distanceToTarget <= ZoneHitRadius)
            {
                if (_insideSince < 0f) _insideSince = Time.unscaledTime;
                if (Time.unscaledTime - _insideSince >= RequiredInsideSeconds)
                {
                    RegisterPoint();
                }
            }
            else
            {
                _insideSince = -1f;
            }
        }

        private void RegisterPoint()
        {
            float elapsed = Time.unscaledTime - _targetStartedAt;
            _points++;
            _totalTime += elapsed;
            _bestTime = Mathf.Min(_bestTime, elapsed);
            _insideSince = -1f;

            AppSettings.Vibrate();
            StartCoroutine(PointFeedback(elapsed));
        }

        private IEnumerator PointFeedback(float elapsed)
        {
            _result.text = $"✓  {elapsed:F2}s";
            _result.color = UiTheme.Positive;
            yield return Punch(_result.transform, 0.25f);
            yield return new WaitForSecondsRealtime(0.28f);
            ActivateNextZone();
        }

        private void FinishGame()
        {
            _state = State.Finished;
            float average = _points > 0 ? _totalTime / _points : 0f;
            _title.text = "ENTRENAMIENTO COMPLETADO";
            _counter.text = $"{_points} OBJETIVOS";
            _result.color = UiTheme.AccentLime;
            _result.text = $"PROMEDIO  {average:F2}s\nMEJOR  {(_bestTime < float.MaxValue ? _bestTime : 0f):F2}s";
            _status.text = "Buen trabajo. Podés repetir el circuito manteniendo las mismas zonas.";
            _primaryLabel.text = "REPETIR";
            _primaryButton.gameObject.SetActive(true);
            _primaryButton.onClick.RemoveAllListeners();
            _primaryButton.onClick.AddListener(() => StartCoroutine(StartGameRoutineFromFinished()));
        }

        private IEnumerator StartGameRoutineFromFinished()
        {
            _state = State.Ready;
            yield return StartGameRoutine();
        }

        private void UpdateCameraStability(bool pauseIfMoved)
        {
            if (_arCamera == null) return;

            float moved = Vector3.Distance(_lockedCameraPosition, _arCamera.transform.position);
            float rotated = Quaternion.Angle(_lockedCameraRotation, _arCamera.transform.rotation);

            if (moved <= MaxCameraMovementMeters && rotated <= MaxCameraRotationDegrees) return;

            if (!pauseIfMoved)
            {
                _status.text = "⚠ La cámara se movió. Volvé a apoyarla antes de comenzar.";
                return;
            }

            if (_state == State.Running)
            {
                _state = State.PausedCameraMoved;
                _title.text = "CÁMARA MOVIDA";
                _result.text = "⏸";
                _result.color = UiTheme.Danger;
                _status.text = "Pausamos para evitar puntos falsos. Apoyá nuevamente el teléfono y recalibrá.";
                _primaryLabel.text = "RECALIBRAR";
                _primaryButton.gameObject.SetActive(true);
                _primaryButton.onClick.RemoveAllListeners();
                _primaryButton.onClick.AddListener(() =>
                {
                    _state = State.Placing;
                    BeginCameraLockCountdown();
                });
            }
        }

        private bool CaptureBackgroundFrame()
        {
            if (_cameraManager == null || !_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) return false;

            try
            {
                int targetWidth = 160;
                int targetHeight = Mathf.Max(90, Mathf.RoundToInt(targetWidth * (image.height / (float)image.width)));
                var conversion = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(targetWidth, targetHeight),
                    outputFormat = TextureFormat.R8,
                    transformation = XRCpuImage.Transformation.MirrorY
                };

                int size = image.GetConvertedDataSize(conversion);
                using var data = new NativeArray<byte>(size, Allocator.Temp);
                image.Convert(conversion, data);

                _backgroundFrame = data.ToArray();
                _frameWidth = targetWidth;
                _frameHeight = targetHeight;
                return _backgroundFrame.Length > 0;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AR] No se pudo capturar fondo: {ex.Message}");
                return false;
            }
            finally
            {
                image.Dispose();
            }
        }

        private bool TryDetectPlayerFeet(out Vector2 screenPoint)
        {
            screenPoint = default;
            if (_backgroundFrame == null || _cameraManager == null || !_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                return false;
            }

            try
            {
                var conversion = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(_frameWidth, _frameHeight),
                    outputFormat = TextureFormat.R8,
                    transformation = XRCpuImage.Transformation.MirrorY
                };

                int size = image.GetConvertedDataSize(conversion);
                using var data = new NativeArray<byte>(size, Allocator.Temp);
                image.Convert(conversion, data);
                if (data.Length != _backgroundFrame.Length) return false;

                var candidates = new List<Vector2Int>(256);

                int minY = Mathf.RoundToInt(_frameHeight * 0.18f);
                int maxY = Mathf.RoundToInt(_frameHeight * 0.96f);
                int minX = Mathf.RoundToInt(_frameWidth * 0.04f);
                int maxX = Mathf.RoundToInt(_frameWidth * 0.96f);

                for (int y = minY; y < maxY; y += 2)
                {
                    for (int x = minX; x < maxX; x += 2)
                    {
                        int idx = y * _frameWidth + x;
                        int diff = Mathf.Abs(data[idx] - _backgroundFrame[idx]);
                        if (diff >= MotionThreshold)
                        {
                            candidates.Add(new Vector2Int(x, y));
                        }
                    }
                }

                if (candidates.Count < MinMotionPixels) return false;

                // Para estimar los pies tomamos la franja más baja del movimiento.
                candidates.Sort((a, b) => b.y.CompareTo(a.y));
                int take = Mathf.Clamp(candidates.Count / 8, 12, 80);
                float sx = 0f;
                float sy = 0f;
                for (int i = 0; i < take; i++)
                {
                    sx += candidates[i].x;
                    sy += candidates[i].y;
                }

                float u = (sx / take) / _frameWidth;
                float v = (sy / take) / _frameHeight;
                screenPoint = CameraImageToScreen(u, v);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AR] Detector de movimiento: {ex.Message}");
                return false;
            }
            finally
            {
                image.Dispose();
            }
        }

        private static Vector2 CameraImageToScreen(float u, float v)
        {
            // ARCore suele entregar la imagen del sensor en orientación landscape.
            // La transformamos a la orientación actual de la pantalla para poder
            // usar el punto con ARRaycastManager.
            float x;
            float y;

            switch (Screen.orientation)
            {
                case ScreenOrientation.PortraitUpsideDown:
                    x = 1f - v;
                    y = u;
                    break;
                case ScreenOrientation.LandscapeLeft:
                    x = 1f - u;
                    y = 1f - v;
                    break;
                case ScreenOrientation.LandscapeRight:
                    x = u;
                    y = v;
                    break;
                default: // Portrait y AutoRotation terminan normalmente acá.
                    x = v;
                    y = 1f - u;
                    break;
            }

            return new Vector2(x * Screen.width, y * Screen.height);
        }

        private static bool TryGetPointerDown(out Vector2 position)
        {
            position = default;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }

            return false;
        }

        private IEnumerator TemporaryStatus(string message)
        {
            string previous = _status.text;
            _status.text = message;
            _status.color = UiTheme.Danger;
            yield return new WaitForSecondsRealtime(1.3f);
            _status.color = UiTheme.TextPrimary;
            _status.text = previous;
        }

        private static IEnumerator Punch(Transform target, float duration)
        {
            Vector3 original = Vector3.one;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float scale = Mathf.Lerp(1.35f, 1f, Mathf.SmoothStep(0f, 1f, k));
                target.localScale = original * scale;
                yield return null;
            }
            target.localScale = original;
        }

        private void ExitARMode()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private GameObject FindDeep(string objectName)
        {
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }
            return null;
        }

        private Button FindButton(string objectName)
        {
            var go = FindDeep(objectName);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static Button CreateButton(Transform parent, string name, string text, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;

            // Copiamos un sprite redondeado disponible si existe en la misma UI.
            var existing = parent.GetComponentInChildren<Button>(true);
            if (existing != null)
            {
                var sourceImage = existing.GetComponent<Image>();
                if (sourceImage != null && sourceImage.sprite != null)
                {
                    image.sprite = sourceImage.sprite;
                    image.type = sourceImage.type;
                }
            }

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            go.AddComponent<ButtonPressScale>();

            var label = CreateText(go.transform, "Label", text, 27, FontStyles.Bold, TextAlignmentOptions.Center);
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16;
            label.fontSizeMax = 30;
            SetRect(label.rectTransform, 0.04f, 0.05f, 0.96f, 0.95f);
            return button;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = UiTheme.TextPrimary;
            label.enableWordWrapping = true;
            return label;
        }

        private static void SetRect(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null || renderer.material == null) return;
            if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", color);
            else if (renderer.material.HasProperty("_Color")) renderer.material.SetColor("_Color", color);
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
