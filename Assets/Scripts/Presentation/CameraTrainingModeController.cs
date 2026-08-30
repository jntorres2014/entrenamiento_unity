using System;
using System.Collections;
using System.Collections.Generic;
using Entrenamiento.App;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Entrenamiento con cámara fija que NO depende de ARCore.
    /// El usuario apoya el teléfono, marca cuatro zonas sobre la imagen real,
    /// captura un fondo vacío y el sistema estima los pies del jugador mediante
    /// diferencia de imagen para validar la llegada a la zona activa.
    /// </summary>
    public sealed class CameraTrainingModeController : MonoBehaviour
    {
        private enum State
        {
            Home,
            StartingCamera,
            Placing,
            CameraCountdown,
            Ready,
            Running,
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

        private Canvas _canvas;
        private GameObject _rolePanel;
        private Button _homeButton;
        private State _state = State.Home;

        private GameObject _uiRoot;
        private RawImage _cameraImage;
        private RectTransform _cameraRect;
        private TMP_Text _title;
        private TMP_Text _status;
        private TMP_Text _counter;
        private TMP_Text _result;
        private Button _primaryButton;
        private TMP_Text _primaryLabel;
        private RectTransform _feetMarker;
        private Image _feetMarkerImage;

        private readonly List<RectTransform> _zones = new List<RectTransform>();
        private readonly List<Image> _zoneImages = new List<Image>();
        private readonly List<Vector2> _zoneScreenNormalized = new List<Vector2>();

        private WebCamTexture _webCam;
        private Color32[] _pixels;
        private byte[] _backgroundGray;
        private int _cameraWidth;
        private int _cameraHeight;
        private int _sampleStep = 5;
        private float _nextVisionSample;
        private int _lastVideoAngle = -1;
        private bool _lastVideoMirror;

        private int _activeZone = -1;
        private int _lastZone = -1;
        private int _points;
        private const int TargetCount = 10;
        private float _targetStartedAt;
        private float _totalReactionTime;
        private float _bestReactionTime = float.MaxValue;
        private float _insideSince = -1f;

        private const float VisionInterval = 0.10f;
        private const int MotionThreshold = 34;
        private const int MinimumMotionSamples = 55;
        private const float RequiredInsideSeconds = 0.18f;
        private const float PlacementMinY = 0.19f;
        private const float PlacementMaxY = 0.79f;

        private Sprite _roundedSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<CameraTrainingModeController>() == null)
                {
                    canvas.gameObject.AddComponent<CameraTrainingModeController>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            StartCoroutine(AddHomeButtonWhenReady());
        }

        private IEnumerator AddHomeButtonWhenReady()
        {
            // La Home C se arma en runtime. Esperamos a que termine para colocar
            // Camera Training encima de la tarjeta inferior sin alterar su layout.
            for (int i = 0; i < 60; i++)
            {
                _rolePanel = FindDeep("RolePanel");
                if (_rolePanel != null && FindDeep("HomeCVisuals") != null)
                {
                    break;
                }
                yield return null;
            }

            if (_rolePanel == null || _homeButton != null || FindDeep("CameraTrainingButton") != null)
            {
                yield break;
            }

            CaptureRoundedSprite();
            _homeButton = CreateButton(_rolePanel.transform, "CameraTrainingButton",
                "CAMERA TRAINING   →\n<size=64%><color=#A8B2C1>CÁMARA FIJA  •  SIN ARCORE</color></size>", UiTheme.Surface);

            var rect = _homeButton.GetComponent<RectTransform>();
            SetRect(rect, 0.072f, 0.092f, 0.928f, 0.228f);

            var label = _homeButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.fontSizeMax = 24f;
                label.fontSizeMin = 14f;
                label.rectTransform.offsetMin = new Vector2(34f, 8f);
                label.rectTransform.offsetMax = new Vector2(-26f, -8f);
            }

            _homeButton.onClick.AddListener(StartCameraMode);
            _homeButton.transform.SetAsLastSibling();
        }

        private void StartCameraMode()
        {
            if (_state != State.Home) return;

            _state = State.StartingCamera;
            HideNormalUi();
            CreateCameraUi();
            StartCoroutine(StartCamera());
        }

        private IEnumerator StartCamera()
        {
            _title.text = "CAMERA TRAINING";
            _status.text = "Preparando la cámara…";
            _primaryButton.gameObject.SetActive(false);

            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            }

            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                ShowFatal("PERMISO DE CÁMARA", "Necesito permiso para usar la cámara. Podés habilitarlo en Ajustes del teléfono y volver a intentar.");
                yield break;
            }

            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                ShowFatal("SIN CÁMARA", "No pude encontrar una cámara disponible en este dispositivo.");
                yield break;
            }

            string deviceName = devices[0].name;
            for (int i = 0; i < devices.Length; i++)
            {
                if (!devices[i].isFrontFacing)
                {
                    deviceName = devices[i].name;
                    break;
                }
            }

            _webCam = new WebCamTexture(deviceName, 640, 480, 30);
            _cameraImage.texture = _webCam;
            _webCam.Play();

            float timeoutAt = Time.realtimeSinceStartup + 8f;
            while ((_webCam.width <= 32 || !_webCam.didUpdateThisFrame) && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            if (_webCam.width <= 32 || _webCam.height <= 32)
            {
                ShowFatal("CÁMARA NO DISPONIBLE", "La cámara no llegó a iniciar. Cerrá otras apps que puedan estar usándola y probá nuevamente.");
                yield break;
            }

            _cameraWidth = _webCam.width;
            _cameraHeight = _webCam.height;
            _pixels = new Color32[_cameraWidth * _cameraHeight];
            _sampleStep = Mathf.Clamp(_cameraWidth / 110, 4, 10);
            UpdateCameraGeometry(true);

            _state = State.Placing;
            _title.text = "MARCAR ZONAS";
            _status.text = "Apoyá el teléfono mirando el campo. Después tocá 4 lugares donde quieras entrenar. Dejalos separados.";
            _counter.text = "0 / 4 ZONAS";
        }

        private void HideNormalUi()
        {
            string[] panels = { "RolePanel", "HostConfigPanel", "HostProgressPanel", "StationWaitPanel", "SummaryPanel" };
            foreach (string panelName in panels)
            {
                var panel = FindDeep(panelName);
                if (panel != null) panel.SetActive(false);
            }

            SetActive("ModernBackdrop", false);
            SetActive("ModernBackButton", false);

            var modern = GetComponent<TrainingModernUiController>();
            if (modern != null) modern.enabled = false;
            var magic = GetComponent<TrainingUiMagic>();
            if (magic != null) magic.enabled = false;
            var home = GetComponent<TrainingHomeCView>();
            if (home != null) home.enabled = false;
            var flow = GetComponent<TrainingFlowCView>();
            if (flow != null) flow.enabled = false;
        }

        private void CreateCameraUi()
        {
            CaptureRoundedSprite();

            _uiRoot = new GameObject("CameraTrainingUI", typeof(RectTransform), typeof(CanvasGroup));
            _uiRoot.transform.SetParent(_canvas.transform, false);
            _uiRoot.transform.SetAsLastSibling();
            Stretch(_uiRoot.GetComponent<RectTransform>());

            var cameraGo = new GameObject("CameraFeed", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            cameraGo.transform.SetParent(_uiRoot.transform, false);
            _cameraRect = cameraGo.GetComponent<RectTransform>();
            Stretch(_cameraRect);
            _cameraImage = cameraGo.GetComponent<RawImage>();
            _cameraImage.color = Color.white;
            _cameraImage.raycastTarget = false;

            var shade = CreateImage(_uiRoot.transform, "CameraShade", new Color(0f, 0f, 0f, 0.10f), false);
            Stretch(shade.rectTransform);

            var topCard = CreateImage(_uiRoot.transform, "CameraTopCard", new Color(0.035f, 0.05f, 0.07f, 0.92f), false);
            SetRect(topCard.rectTransform, 0.035f, 0.815f, 0.965f, 0.972f);

            _title = CreateText(topCard.transform, "Title", "CAMERA TRAINING", 31f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            SetRect(_title.rectTransform, 0.045f, 0.57f, 0.78f, 0.92f);
            _title.color = UiTheme.Accent;

            _status = CreateText(topCard.transform, "Status", "", 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetRect(_status.rectTransform, 0.045f, 0.08f, 0.95f, 0.58f);
            _status.color = UiTheme.TextPrimary;

            _counter = CreateText(_uiRoot.transform, "Counter", "", 34f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_counter.rectTransform, 0.08f, 0.72f, 0.92f, 0.80f);
            _counter.color = UiTheme.TextPrimary;

            _result = CreateText(_uiRoot.transform, "Result", "", 47f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_result.rectTransform, 0.07f, 0.31f, 0.93f, 0.68f);
            _result.color = UiTheme.TextPrimary;

            _primaryButton = CreateButton(_uiRoot.transform, "CameraPrimaryButton", "CONTINUAR", UiTheme.Accent);
            SetRect(_primaryButton.GetComponent<RectTransform>(), 0.075f, 0.045f, 0.925f, 0.135f);
            _primaryLabel = _primaryButton.GetComponentInChildren<TMP_Text>(true);
            _primaryButton.gameObject.SetActive(false);

            var back = CreateButton(_uiRoot.transform, "CameraBackButton", "←  ATRÁS", new Color32(0x20, 0x2A, 0x37, 0xF2));
            SetRect(back.GetComponent<RectTransform>(), 0.055f, 0.905f, 0.30f, 0.962f);
            back.onClick.AddListener(ExitCameraMode);
            back.transform.SetAsLastSibling();

            var marker = CreateImage(_uiRoot.transform, "DetectedFeetMarker", Color.white, false);
            _feetMarker = marker.rectTransform;
            _feetMarker.anchorMin = new Vector2(0.5f, 0.5f);
            _feetMarker.anchorMax = new Vector2(0.5f, 0.5f);
            _feetMarker.sizeDelta = new Vector2(34f, 34f);
            _feetMarkerImage = marker;
            marker.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_state == State.Home) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ExitCameraMode();
                return;
            }

            UpdateCameraGeometry(false);

            if (_state == State.Placing)
            {
                UpdatePlacement();
            }
            else if (_state == State.Running)
            {
                UpdateRunning();
            }
        }

        private void UpdatePlacement()
        {
            if (_zones.Count >= 4) return;
            if (!TryGetPointerDown(out Vector2 screenPosition)) return;

            float y01 = screenPosition.y / Mathf.Max(1f, Screen.height);
            if (y01 < PlacementMinY || y01 > PlacementMaxY) return;

            Vector2 normalized = new Vector2(
                screenPosition.x / Mathf.Max(1f, Screen.width),
                screenPosition.y / Mathf.Max(1f, Screen.height));

            float minDistance = Mathf.Min(Screen.width, Screen.height) * 0.23f;
            foreach (var existing in _zoneScreenNormalized)
            {
                Vector2 a = new Vector2(existing.x * Screen.width, existing.y * Screen.height);
                if (Vector2.Distance(a, screenPosition) < minDistance)
                {
                    StartCoroutine(TemporaryStatus("Separá un poco más las zonas para que el recorrido sea claro."));
                    return;
                }
            }

            CreateZone(normalized, _zones.Count);
            _counter.text = $"{_zones.Count} / 4 ZONAS";

            if (_zones.Count == 4)
            {
                _status.text = "Campo listo ✓  Ahora no muevas más el teléfono. Tocá FIJAR CÁMARA y salí de la imagen durante la cuenta regresiva.";
                _primaryLabel.text = "FIJAR CÁMARA";
                _primaryButton.gameObject.SetActive(true);
                _primaryButton.onClick.RemoveAllListeners();
                _primaryButton.onClick.AddListener(BeginCameraLockCountdown);
            }
        }

        private void CreateZone(Vector2 normalized, int index)
        {
            _zoneScreenNormalized.Add(normalized);

            var zone = CreateImage(_uiRoot.transform, $"CameraZone{index + 1}", WithAlpha(ZoneColors[index], 0.58f), false);
            var rect = zone.rectTransform;
            rect.anchorMin = normalized;
            rect.anchorMax = normalized;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(150f, 150f);

            var outline = zone.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.70f);
            outline.effectDistance = new Vector2(3f, -3f);

            var label = CreateText(zone.transform, "ZoneLabel", (index + 1).ToString(), 34f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.color = Color.white;

            _zones.Add(rect);
            _zoneImages.Add(zone);
            zone.transform.SetAsLastSibling();
            if (_primaryButton != null) _primaryButton.transform.SetAsLastSibling();
        }

        private void BeginCameraLockCountdown()
        {
            if (_state != State.Placing && _state != State.Ready) return;
            StartCoroutine(CameraLockCountdown());
        }

        private IEnumerator CameraLockCountdown()
        {
            _state = State.CameraCountdown;
            _primaryButton.gameObject.SetActive(false);
            _result.text = "";
            _title.text = "FIJAR CÁMARA";
            _status.text = "Dejá el campo vacío. Voy a aprender cómo se ve el fondo sin jugadores.";

            for (int n = 5; n >= 1; n--)
            {
                _counter.text = $"{n}";
                yield return new WaitForSecondsRealtime(1f);
            }

            _counter.text = "QUIETO";
            yield return new WaitForSecondsRealtime(0.35f);

            if (!CaptureBackground())
            {
                ShowFatal("NO PUDE CALIBRAR", "La cámara no entregó una imagen válida. Probá de nuevo sin mover el teléfono.");
                yield break;
            }

            _state = State.Ready;
            _title.text = "CAMPO CALIBRADO";
            _status.text = "Listo ✓  Entrá completamente en la imagen. Cuando empiece, corré hacia el color indicado.";
            _counter.text = "4 ZONAS LISTAS";
            _primaryLabel.text = "EMPEZAR";
            _primaryButton.gameObject.SetActive(true);
            _primaryButton.onClick.RemoveAllListeners();
            _primaryButton.onClick.AddListener(() => StartCoroutine(StartExercise()));
        }

        private bool CaptureBackground()
        {
            if (_webCam == null || !_webCam.isPlaying || _webCam.width <= 32) return false;
            EnsurePixelBuffer();
            if (_pixels == null) return false;

            _webCam.GetPixels32(_pixels);
            _backgroundGray = new byte[_pixels.Length];
            for (int i = 0; i < _pixels.Length; i++)
            {
                _backgroundGray[i] = Gray(_pixels[i]);
            }
            return true;
        }

        private IEnumerator StartExercise()
        {
            if (_backgroundGray == null || _backgroundGray.Length == 0) yield break;

            _primaryButton.gameObject.SetActive(false);
            _result.text = "";
            _title.text = "PREPARATE";
            _status.text = "Mantenete visible para la cámara y esperá el objetivo.";

            for (int n = 3; n >= 1; n--)
            {
                _counter.text = n.ToString();
                yield return new WaitForSecondsRealtime(0.75f);
            }

            _counter.text = "¡YA!";
            yield return new WaitForSecondsRealtime(0.45f);

            _points = 0;
            _totalReactionTime = 0f;
            _bestReactionTime = float.MaxValue;
            _lastZone = -1;
            _activeZone = -1;
            _insideSince = -1f;
            _state = State.Running;
            SelectNextTarget();
        }

        private void SelectNextTarget()
        {
            if (_points >= TargetCount)
            {
                FinishExercise();
                return;
            }

            int next;
            do
            {
                next = UnityEngine.Random.Range(0, _zones.Count);
            } while (_zones.Count > 1 && next == _lastZone);

            _lastZone = _activeZone;
            _activeZone = next;
            _insideSince = -1f;
            _targetStartedAt = Time.unscaledTime;

            for (int i = 0; i < _zoneImages.Count; i++)
            {
                bool active = i == _activeZone;
                _zoneImages[i].color = WithAlpha(ZoneColors[i], active ? 0.96f : 0.28f);
                _zones[i].localScale = active ? Vector3.one * 1.18f : Vector3.one;
            }

            _title.text = "OBJETIVO";
            _counter.text = $"{_points + 1} / {TargetCount}";
            _result.text = ZoneNames[_activeZone];
            _result.color = ZoneColors[_activeZone];
            _status.text = "Movete hasta que tus pies entren en la zona marcada.";
        }

        private void UpdateRunning()
        {
            if (_webCam == null || !_webCam.isPlaying || !_webCam.didUpdateThisFrame) return;
            if (Time.unscaledTime < _nextVisionSample) return;
            _nextVisionSample = Time.unscaledTime + VisionInterval;

            if (!TryDetectFeet(out Vector2 feetScreenNormalized, out bool tooClose))
            {
                if (_feetMarker != null) _feetMarker.gameObject.SetActive(false);
                _insideSince = -1f;
                return;
            }

            if (tooClose)
            {
                if (_feetMarker != null) _feetMarker.gameObject.SetActive(false);
                _insideSince = -1f;
                _status.text = "⚠ Estás demasiado cerca de la cámara. Alejate para que pueda verte completo.";
                return;
            }

            ShowFeetMarker(feetScreenNormalized);

            Vector2 feetPixels = new Vector2(feetScreenNormalized.x * Screen.width, feetScreenNormalized.y * Screen.height);
            Vector2 zone = _zoneScreenNormalized[_activeZone];
            Vector2 zonePixels = new Vector2(zone.x * Screen.width, zone.y * Screen.height);
            float hitRadius = Mathf.Min(Screen.width, Screen.height) * 0.145f;
            bool inside = Vector2.Distance(feetPixels, zonePixels) <= hitRadius;

            if (!inside)
            {
                _insideSince = -1f;
                _status.text = "Jugador detectado ✓  Andá hacia " + ZoneNames[_activeZone] + ".";
                return;
            }

            if (_insideSince < 0f)
            {
                _insideSince = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - _insideSince < RequiredInsideSeconds) return;

            float reaction = Time.unscaledTime - _targetStartedAt;
            _totalReactionTime += reaction;
            _bestReactionTime = Mathf.Min(_bestReactionTime, reaction);
            _points++;
            _insideSince = -1f;
            AppSettings.Vibrate();
            StartCoroutine(TargetFeedback(reaction));
        }

        private IEnumerator TargetFeedback(float reaction)
        {
            _state = State.Ready;
            _result.text = $"✓  {reaction:F2}s";
            _result.color = UiTheme.AccentLime;
            _status.text = "Objetivo alcanzado";

            if (_activeZone >= 0 && _activeZone < _zones.Count)
            {
                var zone = _zones[_activeZone];
                Vector3 from = Vector3.one * 1.32f;
                float elapsed = 0f;
                while (elapsed < 0.24f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / 0.24f);
                    zone.localScale = Vector3.Lerp(from, Vector3.one * 1.18f, t);
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(0.32f);
            _state = State.Running;
            SelectNextTarget();
        }

        private void FinishExercise()
        {
            _state = State.Finished;
            _activeZone = -1;
            if (_feetMarker != null) _feetMarker.gameObject.SetActive(false);

            for (int i = 0; i < _zoneImages.Count; i++)
            {
                _zoneImages[i].color = WithAlpha(ZoneColors[i], 0.32f);
                _zones[i].localScale = Vector3.one;
            }

            float average = _points > 0 ? _totalReactionTime / _points : 0f;
            float best = _bestReactionTime < float.MaxValue ? _bestReactionTime : 0f;

            _title.text = "ENTRENAMIENTO COMPLETO";
            _counter.text = $"{_points} / {TargetCount} OBJETIVOS";
            _result.text = $"PROMEDIO  {average:F2}s\n<size=62%>MEJOR  {best:F2}s</size>";
            _result.color = UiTheme.TextPrimary;
            _status.text = "Camera Training funciona sin ARCore. Si la detección se siente corrida, la ajustamos con tu prueba real.";

            _primaryLabel.text = "REPETIR";
            _primaryButton.gameObject.SetActive(true);
            _primaryButton.onClick.RemoveAllListeners();
            _primaryButton.onClick.AddListener(BeginCameraLockCountdown);
        }

        private bool TryDetectFeet(out Vector2 feetScreenNormalized, out bool tooClose)
        {
            feetScreenNormalized = default;
            tooClose = false;

            if (_backgroundGray == null || _webCam == null) return false;
            EnsurePixelBuffer();
            if (_pixels == null || _backgroundGray.Length != _pixels.Length) return false;

            _webCam.GetPixels32(_pixels);

            int motionCount = 0;
            int visibleSamples = 0;
            float minX = 1f;
            float maxX = 0f;
            float minY = 1f;
            float maxY = 0f;

            int marginX = Mathf.Max(_sampleStep, _cameraWidth / 30);
            int marginY = Mathf.Max(_sampleStep, _cameraHeight / 30);

            for (int y = marginY; y < _cameraHeight - marginY; y += _sampleStep)
            {
                int row = y * _cameraWidth;
                for (int x = marginX; x < _cameraWidth - marginX; x += _sampleStep)
                {
                    int index = row + x;
                    Vector2 screen = TexturePointToScreenNormalized(x, y);
                    if (screen.x < 0f || screen.x > 1f || screen.y < 0f || screen.y > 1f) continue;
                    visibleSamples++;

                    int delta = Mathf.Abs(Gray(_pixels[index]) - _backgroundGray[index]);
                    if (delta < MotionThreshold) continue;

                    motionCount++;
                    minX = Mathf.Min(minX, screen.x);
                    maxX = Mathf.Max(maxX, screen.x);
                    minY = Mathf.Min(minY, screen.y);
                    maxY = Mathf.Max(maxY, screen.y);
                }
            }

            if (motionCount < MinimumMotionSamples || visibleSamples <= 0) return false;

            float width = Mathf.Max(0f, maxX - minX);
            float height = Mathf.Max(0f, maxY - minY);
            float area = width * height;
            float motionRatio = (float)motionCount / visibleSamples;

            // Cuando el cuerpo ocupa casi toda la imagen, la estimación de pies deja
            // de ser confiable. Lo tratamos como zona muerta cercana a la cámara.
            tooClose = width > 0.86f || height > 0.90f || area > 0.58f || motionRatio > 0.72f;
            if (tooClose) return true;

            float footBand = Mathf.Clamp(height * 0.16f, 0.035f, 0.10f);
            float footLimit = minY + footBand;
            float sumX = 0f;
            float sumY = 0f;
            int feetSamples = 0;

            for (int y = marginY; y < _cameraHeight - marginY; y += _sampleStep)
            {
                int row = y * _cameraWidth;
                for (int x = marginX; x < _cameraWidth - marginX; x += _sampleStep)
                {
                    int index = row + x;
                    int delta = Mathf.Abs(Gray(_pixels[index]) - _backgroundGray[index]);
                    if (delta < MotionThreshold) continue;

                    Vector2 screen = TexturePointToScreenNormalized(x, y);
                    if (screen.x < 0f || screen.x > 1f || screen.y < 0f || screen.y > footLimit) continue;

                    sumX += screen.x;
                    sumY += screen.y;
                    feetSamples++;
                }
            }

            if (feetSamples < 8) return false;

            feetScreenNormalized = new Vector2(sumX / feetSamples, sumY / feetSamples);
            return true;
        }

        private Vector2 TexturePointToScreenNormalized(int x, int y)
        {
            float u = x / Mathf.Max(1f, _cameraWidth - 1f);
            float v = y / Mathf.Max(1f, _cameraHeight - 1f);

            if (_webCam != null && _webCam.videoVerticallyMirrored)
            {
                v = 1f - v;
            }

            float cx = u - 0.5f;
            float cy = v - 0.5f;
            float rx = cx;
            float ry = cy;
            int angle = _webCam != null ? NormalizeAngle(_webCam.videoRotationAngle) : 0;

            // RawImage se rota -videoRotationAngle, por lo que aplicamos la misma
            // transformación al punto detectado para llevarlo al espacio de pantalla.
            switch (angle)
            {
                case 90:
                    rx = cy;
                    ry = -cx;
                    break;
                case 180:
                    rx = -cx;
                    ry = -cy;
                    break;
                case 270:
                    rx = -cy;
                    ry = cx;
                    break;
            }

            float scale = GetCameraCoverScale(angle);
            return new Vector2(rx * scale + 0.5f, ry * scale + 0.5f);
        }

        private void UpdateCameraGeometry(bool force)
        {
            if (_webCam == null || _cameraRect == null || !_webCam.isPlaying) return;

            int angle = NormalizeAngle(_webCam.videoRotationAngle);
            bool mirror = _webCam.videoVerticallyMirrored;
            if (!force && angle == _lastVideoAngle && mirror == _lastVideoMirror) return;

            _lastVideoAngle = angle;
            _lastVideoMirror = mirror;
            _cameraRect.localEulerAngles = new Vector3(0f, 0f, -angle);
            float scale = GetCameraCoverScale(angle);
            _cameraRect.localScale = new Vector3(scale, scale, 1f);
            _cameraImage.uvRect = mirror ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
        }

        private static float GetCameraCoverScale(int angle)
        {
            if (angle != 90 && angle != 270) return 1f;
            float ratio = Screen.width > 0 && Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
            if (ratio <= 0f) return 1f;
            return Mathf.Max(ratio, 1f / ratio);
        }

        private static int NormalizeAngle(int angle)
        {
            angle %= 360;
            if (angle < 0) angle += 360;
            if (angle < 45 || angle >= 315) return 0;
            if (angle < 135) return 90;
            if (angle < 225) return 180;
            return 270;
        }

        private void EnsurePixelBuffer()
        {
            if (_webCam == null || _webCam.width <= 32 || _webCam.height <= 32) return;

            if (_cameraWidth != _webCam.width || _cameraHeight != _webCam.height ||
                _pixels == null || _pixels.Length != _webCam.width * _webCam.height)
            {
                _cameraWidth = _webCam.width;
                _cameraHeight = _webCam.height;
                _pixels = new Color32[_cameraWidth * _cameraHeight];
                _sampleStep = Mathf.Clamp(_cameraWidth / 110, 4, 10);
            }
        }

        private void ShowFeetMarker(Vector2 normalized)
        {
            if (_feetMarker == null) return;

            _feetMarker.gameObject.SetActive(true);
            _feetMarker.anchorMin = normalized;
            _feetMarker.anchorMax = normalized;
            _feetMarker.anchoredPosition = Vector2.zero;
            if (_feetMarkerImage != null)
            {
                _feetMarkerImage.color = UiTheme.AccentLime;
            }
            _feetMarker.transform.SetAsLastSibling();
            if (_primaryButton != null) _primaryButton.transform.SetAsLastSibling();
        }

        private void ShowFatal(string title, string message)
        {
            _state = State.Ready;
            _title.text = title;
            _status.text = message;
            _counter.text = "";
            _result.text = "";
            _primaryButton.gameObject.SetActive(false);
        }

        private IEnumerator TemporaryStatus(string message)
        {
            string previous = _status.text;
            Color previousColor = _status.color;
            _status.text = message;
            _status.color = UiTheme.Danger;
            yield return new WaitForSecondsRealtime(1.25f);
            _status.text = previous;
            _status.color = previousColor;
        }

        private void ExitCameraMode()
        {
            StopCamera();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void StopCamera()
        {
            if (_webCam != null)
            {
                if (_webCam.isPlaying) _webCam.Stop();
                Destroy(_webCam);
                _webCam = null;
            }
        }

        private void OnDestroy()
        {
            StopCamera();
        }

        private void CaptureRoundedSprite()
        {
            if (_roundedSprite != null) return;
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                var image = button.GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    _roundedSprite = image.sprite;
                    break;
                }
            }
        }

        private Button CreateButton(Transform parent, string name, string text, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;
            if (_roundedSprite != null)
            {
                image.sprite = _roundedSprite;
                image.type = Image.Type.Sliced;
            }

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = UiTheme.Disabled;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            go.AddComponent<ButtonPressScale>();

            var label = CreateText(go.transform, "Label", text, 25f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(18f, 7f);
            label.rectTransform.offsetMax = new Vector2(-18f, -7f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 28f;
            return button;
        }

        private Image CreateImage(Transform parent, string name, Color color, bool raycastTarget)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            if (_roundedSprite != null)
            {
                image.sprite = _roundedSprite;
                image.type = Image.Type.Sliced;
            }
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
            label.raycastTarget = false;
            label.enableWordWrapping = true;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(12f, size * 0.55f);
            label.fontSizeMax = size;
            return label;
        }

        private GameObject FindDeep(string objectName)
        {
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }
            return null;
        }

        private void SetActive(string objectName, bool active)
        {
            var go = FindDeep(objectName);
            if (go != null) go.SetActive(active);
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

        private static byte Gray(Color32 c)
        {
            return (byte)((c.r * 77 + c.g * 150 + c.b * 29) >> 8);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static void SetRect(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
