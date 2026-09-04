using System.Collections;
using System.Collections.Generic;
using Entrenamiento.Core.Models;
using Entrenamiento.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Modo SOLO: un único teléfono funciona como entrenador y como cuatro pods
    /// virtuales. Usa cámara fija, cinco zonas calibradas y los mismos seis
    /// ExerciseMode que el entrenamiento con teléfonos/pods.
    /// </summary>
    public sealed class SoloTrainingModeController : MonoBehaviour
    {
        private enum SoloState
        {
            Home,
            SelectingExercise,
            Options,
            StartingCamera,
            Placing,
            BackgroundCountdown,
            WaitingCenter,
            Countdown,
            Running,
            ReturningCenter,
            Feedback,
            Finished,
            Fatal
        }

        private enum SoloZone
        {
            None,
            Front,
            Left,
            Center,
            Right,
            Back
        }

        private sealed class SoloAttempt
        {
            public int Round;
            public SoloZone Expected;
            public SoloZone Actual;
            public StationColor CueColor;
            public bool Hit;
            public float Seconds;
            public string Note;
        }

        private static readonly SoloZone[] PlacementOrder =
        {
            SoloZone.Center,
            SoloZone.Front,
            SoloZone.Left,
            SoloZone.Right,
            SoloZone.Back
        };

        private static readonly SoloZone[] DirectionZones =
        {
            SoloZone.Front,
            SoloZone.Left,
            SoloZone.Right,
            SoloZone.Back
        };

        private readonly Dictionary<SoloZone, Vector2> _zonePositions = new Dictionary<SoloZone, Vector2>();
        private readonly Dictionary<SoloZone, RectTransform> _zoneRects = new Dictionary<SoloZone, RectTransform>();
        private readonly Dictionary<SoloZone, Image> _zoneImages = new Dictionary<SoloZone, Image>();
        private readonly List<SoloAttempt> _attempts = new List<SoloAttempt>();
        private readonly HashSet<SoloZone> _speedVisited = new HashSet<SoloZone>();

        private Canvas _canvas;
        private GameObject _rolePanel;
        private Button _homeButton;
        private Sprite _roundedSprite;
        private SoloState _state = SoloState.Home;
        private ExerciseMode _exercise = ExerciseMode.Reaction;

        private GameObject _root;
        private GameObject _selectorPanel;
        private GameObject _optionsPanel;
        private GameObject _cameraPanel;
        private RawImage _cameraImage;
        private RectTransform _cameraRect;
        private TMP_Text _title;
        private TMP_Text _status;
        private TMP_Text _cue;
        private TMP_Text _counter;
        private TMP_Text _result;
        private TMP_Text _roundsLabel;
        private Button _primaryButton;
        private TMP_Text _primaryLabel;
        private RectTransform _feetMarker;
        private Image _feetMarkerImage;
        private CameraPlayerTracker _tracker;

        private int _rounds = 10;
        private int _currentRound;
        private int _placementIndex;
        private int _hits;
        private int _misses;
        private float _nextVisionSample;
        private float _centerInsideSince = -1f;
        private float _candidateInsideSince = -1f;
        private SoloZone _candidateZone = SoloZone.None;
        private SoloZone _currentTarget = SoloZone.None;
        private StationColor _currentCueColor = StationColor.None;
        private float _stimulusStartedAt;
        private float _roundDeadline;
        private bool _fakeChanged;
        private float _fakeChangeAt;
        private float _footballNoGoEndAt;
        private SoloZone _lastTarget = SoloZone.None;

        private const float VisionInterval = 0.10f;
        private const float ZoneDwellSeconds = 0.18f;
        private const float CenterDwellSeconds = 0.38f;
        private const float CognitiveChangeDelay = 0.65f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<SoloTrainingModeController>() == null)
                {
                    canvas.gameObject.AddComponent<SoloTrainingModeController>();
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
            for (int i = 0; i < 90; i++)
            {
                _rolePanel = FindDeep("RolePanel");
                if (_rolePanel != null && FindDeep("HomeCVisuals") != null) break;
                yield return null;
            }

            if (_rolePanel == null || FindDeep("SoloTrainingButton") != null) yield break;

            CaptureRoundedSprite();

            // La tarjeta inferior de Home C pasa a contener los dos modos de un teléfono.
            var statusCard = FindDeep("ARStatusCard");
            if (statusCard != null) statusCard.SetActive(false);

            var cameraTraining = FindDeep("CameraTrainingButton");
            if (cameraTraining != null)
            {
                var cameraRect = cameraTraining.GetComponent<RectTransform>();
                if (cameraRect != null) SetRect(cameraRect, 0.072f, 0.070f, 0.928f, 0.142f);

                var cameraLabel = cameraTraining.GetComponentInChildren<TMP_Text>(true);
                if (cameraLabel != null)
                {
                    cameraLabel.text = "CAMERA TRAINING  ·  CALIBRACIÓN LIBRE";
                    cameraLabel.alignment = TextAlignmentOptions.Center;
                    cameraLabel.fontSizeMax = 18f;
                    cameraLabel.fontSizeMin = 12f;
                }
            }

            _homeButton = CreateButton(_rolePanel.transform, "SoloTrainingButton",
                "SOLO  ·  1 TELÉFONO   →\n<size=63%><color=#A8B2C1>6 EJERCICIOS  •  CÁMARA  •  SIN ARCORE</color></size>", UiTheme.Accent);
            SetRect(_homeButton.GetComponent<RectTransform>(), 0.072f, 0.154f, 0.928f, 0.245f);
            var label = _homeButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.rectTransform.offsetMin = new Vector2(28f, 6f);
                label.rectTransform.offsetMax = new Vector2(-24f, -6f);
                label.fontSizeMax = 23f;
                label.fontSizeMin = 13f;
            }

            _homeButton.onClick.AddListener(OpenSoloMode);
            _homeButton.transform.SetAsLastSibling();
        }

        private void OpenSoloMode()
        {
            if (_state != SoloState.Home) return;
            HideNormalUi();
            EnsureRoot();
            ShowExerciseSelector();
        }

        private void HideNormalUi()
        {
            string[] panels = { "RolePanel", "HostConfigPanel", "HostProgressPanel", "StationWaitPanel", "SummaryPanel" };
            foreach (string panelName in panels)
            {
                var panel = FindDeep(panelName);
                if (panel != null) panel.SetActive(false);
            }

            SetObjectActive("ModernBackdrop", false);
            SetObjectActive("ModernBackButton", false);

            DisableIfPresent<TrainingModernUiController>();
            DisableIfPresent<TrainingUiMagic>();
            DisableIfPresent<TrainingHomeCView>();
            DisableIfPresent<TrainingFlowCView>();
            DisableIfPresent<ExerciseSelectionController>();
            DisableIfPresent<ExerciseRuntimeEnhancer>();
            DisableIfPresent<CameraTrainingModeController>();
            DisableIfPresent<ARTrainingModeController>();
        }

        private void DisableIfPresent<T>() where T : Behaviour
        {
            var component = GetComponent<T>();
            if (component != null) component.enabled = false;
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                _root.SetActive(true);
                _root.transform.SetAsLastSibling();
                return;
            }

            CaptureRoundedSprite();
            _root = new GameObject("SoloTrainingUI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _root.transform.SetParent(_canvas.transform, false);
            Stretch(_root.GetComponent<RectTransform>());
            var bg = _root.GetComponent<Image>();
            bg.color = UiTheme.Background;
            bg.raycastTarget = true;
            _root.transform.SetAsLastSibling();

            BuildSelectorPanel();
            BuildOptionsPanel();
            BuildCameraPanel();
        }

        private void BuildSelectorPanel()
        {
            _selectorPanel = CreatePanel(_root.transform, "SoloExerciseSelection", UiTheme.Background);

            var eyebrow = CreateText(_selectorPanel.transform, "Eyebrow", "SOLO  /  1 TELÉFONO", 18f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(eyebrow.rectTransform, 0.055f, 0.93f, 0.80f, 0.97f);
            eyebrow.color = UiTheme.Accent;
            eyebrow.characterSpacing = 2f;

            var title = CreateText(_selectorPanel.transform, "Title", "Elegí tu entrenamiento", 42f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.055f, 0.855f, 0.94f, 0.925f);

            var subtitle = CreateText(_selectorPanel.transform, "Subtitle",
                "El teléfono mira la cancha, detecta tus desplazamientos y reemplaza los pods físicos.",
                18.5f, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(subtitle.rectTransform, 0.055f, 0.795f, 0.94f, 0.855f);
            subtitle.color = UiTheme.TextSecondary;

            CreateExerciseCard(ExerciseMode.Reaction, "01", "REACCIÓN", "Un objetivo al azar. Reaccioná y volvé al centro.", UiTheme.Positive, 0.055f, 0.585f, 0.485f, 0.765f);
            CreateExerciseCard(ExerciseMode.AllSame, "02", "VELOCIDAD", "Visitá las cuatro zonas lo más rápido posible, en cualquier orden.", UiTheme.Info, 0.515f, 0.585f, 0.945f, 0.765f);
            CreateExerciseCard(ExerciseMode.Colors, "03", "COLORES", "Cada zona tiene un color. Andá solamente al color indicado.", UiTheme.Accent, 0.055f, 0.365f, 0.485f, 0.545f);
            CreateExerciseCard(ExerciseMode.Decision, "04", "DECISIÓN", "Verde frente · rojo atrás · azul izquierda · amarillo derecha.", UiTheme.AccentLime, 0.515f, 0.365f, 0.945f, 0.545f);
            CreateExerciseCard(ExerciseMode.CognitiveFake, "05", "FINTA", "El estímulo cambia después de que empezaste a reaccionar.", new Color32(0xC0, 0x75, 0xFF, 0xFF), 0.055f, 0.145f, 0.485f, 0.325f);
            CreateExerciseCard(ExerciseMode.Football, "06", "FÚTBOL", "Derecho · izquierdo · quieto. La cámara valida zona y movimiento.", new Color32(0x4C, 0xC9, 0x9A, 0xFF), 0.515f, 0.145f, 0.945f, 0.325f);

            var back = CreateButton(_selectorPanel.transform, "SoloSelectorBack", "←  VOLVER", UiTheme.CardElevated);
            SetRect(back.GetComponent<RectTransform>(), 0.055f, 0.045f, 0.34f, 0.105f);
            back.onClick.AddListener(ExitSoloMode);
        }

        private void BuildOptionsPanel()
        {
            _optionsPanel = CreatePanel(_root.transform, "SoloOptionsPanel", UiTheme.Background);

            var eyebrow = CreateText(_optionsPanel.transform, "Eyebrow", "SOLO  /  CONFIGURACIÓN", 18f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(eyebrow.rectTransform, 0.055f, 0.93f, 0.80f, 0.97f);
            eyebrow.color = UiTheme.Accent;

            var title = CreateText(_optionsPanel.transform, "SoloOptionsTitle", "", 42f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.055f, 0.845f, 0.94f, 0.925f);

            var ruleCard = CreateImage(_optionsPanel.transform, "RuleCard", UiTheme.CardElevated, false);
            SetRect(ruleCard.rectTransform, 0.055f, 0.615f, 0.945f, 0.805f);
            var rule = CreateText(ruleCard.transform, "SoloRuleText", "", 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            SetRect(rule.rectTransform, 0.055f, 0.16f, 0.945f, 0.84f);
            rule.color = UiTheme.TextPrimary;

            var roundsCard = CreateImage(_optionsPanel.transform, "RoundsCard", UiTheme.Surface, false);
            SetRect(roundsCard.rectTransform, 0.055f, 0.425f, 0.945f, 0.575f);
            var roundsCaption = CreateText(roundsCard.transform, "Caption", "RONDAS", 17f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(roundsCaption.rectTransform, 0.055f, 0.55f, 0.35f, 0.86f);
            roundsCaption.color = UiTheme.TextMuted;

            _roundsLabel = CreateText(roundsCard.transform, "RoundsValue", "10", 38f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(_roundsLabel.rectTransform, 0.055f, 0.12f, 0.35f, 0.58f);

            var minus = CreateButton(roundsCard.transform, "SoloRoundsMinus", "−", UiTheme.Neutral);
            SetRect(minus.GetComponent<RectTransform>(), 0.58f, 0.20f, 0.73f, 0.80f);
            minus.onClick.AddListener(() => ChangeRounds(-1));
            var plus = CreateButton(roundsCard.transform, "SoloRoundsPlus", "+", UiTheme.Accent);
            SetRect(plus.GetComponent<RectTransform>(), 0.77f, 0.20f, 0.92f, 0.80f);
            plus.onClick.AddListener(() => ChangeRounds(+1));

            var note = CreateText(_optionsPanel.transform, "SoloCameraNote",
                "Vas a apoyar el teléfono, marcar CENTRO + 4 direcciones y dejar el campo vacío durante 5 segundos para calibrar.",
                19f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetRect(note.rectTransform, 0.075f, 0.245f, 0.925f, 0.385f);
            note.color = UiTheme.TextSecondary;

            var start = CreateButton(_optionsPanel.transform, "SoloStartCamera", "PREPARAR CÁMARA   →", UiTheme.Accent);
            SetRect(start.GetComponent<RectTransform>(), 0.075f, 0.105f, 0.925f, 0.195f);
            start.onClick.AddListener(StartSelectedExercise);

            var back = CreateButton(_optionsPanel.transform, "SoloOptionsBack", "←  EJERCICIOS", UiTheme.CardElevated);
            SetRect(back.GetComponent<RectTransform>(), 0.055f, 0.035f, 0.36f, 0.085f);
            back.onClick.AddListener(ShowExerciseSelector);
        }

        private void BuildCameraPanel()
        {
            _cameraPanel = CreatePanel(_root.transform, "SoloCameraPanel", Color.black);

            var cameraGo = new GameObject("SoloCameraFeed", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            cameraGo.transform.SetParent(_cameraPanel.transform, false);
            _cameraRect = cameraGo.GetComponent<RectTransform>();
            Stretch(_cameraRect);
            _cameraImage = cameraGo.GetComponent<RawImage>();
            _cameraImage.color = Color.white;
            _cameraImage.raycastTarget = false;

            var shade = CreateImage(_cameraPanel.transform, "Shade", new Color(0f, 0f, 0f, 0.10f), false);
            Stretch(shade.rectTransform);

            var top = CreateImage(_cameraPanel.transform, "TopCard", new Color(0.035f, 0.05f, 0.07f, 0.93f), false);
            SetRect(top.rectTransform, 0.035f, 0.805f, 0.965f, 0.972f);

            _title = CreateText(top.transform, "Title", "SOLO", 30f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            SetRect(_title.rectTransform, 0.045f, 0.58f, 0.80f, 0.92f);
            _title.color = UiTheme.Accent;

            _status = CreateText(top.transform, "Status", "", 19.5f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetRect(_status.rectTransform, 0.045f, 0.07f, 0.95f, 0.60f);
            _status.color = UiTheme.TextPrimary;

            _counter = CreateText(_cameraPanel.transform, "Counter", "", 30f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_counter.rectTransform, 0.08f, 0.705f, 0.92f, 0.785f);

            _cue = CreateText(_cameraPanel.transform, "Cue", "", 52f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_cue.rectTransform, 0.06f, 0.525f, 0.94f, 0.70f);

            _result = CreateText(_cameraPanel.transform, "Result", "", 30f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_result.rectTransform, 0.06f, 0.285f, 0.94f, 0.515f);

            _primaryButton = CreateButton(_cameraPanel.transform, "SoloPrimaryButton", "CONTINUAR", UiTheme.Accent);
            SetRect(_primaryButton.GetComponent<RectTransform>(), 0.075f, 0.045f, 0.925f, 0.135f);
            _primaryLabel = _primaryButton.GetComponentInChildren<TMP_Text>(true);
            _primaryButton.gameObject.SetActive(false);

            var back = CreateButton(_cameraPanel.transform, "SoloCameraBack", "←  SALIR", new Color32(0x20, 0x2A, 0x37, 0xF2));
            SetRect(back.GetComponent<RectTransform>(), 0.055f, 0.905f, 0.30f, 0.962f);
            back.onClick.AddListener(ExitSoloMode);
            back.transform.SetAsLastSibling();

            var marker = CreateImage(_cameraPanel.transform, "SoloFeetMarker", UiTheme.AccentLime, false);
            _feetMarker = marker.rectTransform;
            _feetMarker.anchorMin = new Vector2(0.5f, 0.5f);
            _feetMarker.anchorMax = new Vector2(0.5f, 0.5f);
            _feetMarker.sizeDelta = new Vector2(32f, 32f);
            _feetMarkerImage = marker;
            marker.gameObject.SetActive(false);

            _cameraPanel.SetActive(false);
        }

        private void CreateExerciseCard(ExerciseMode mode, string number, string heading, string detail, Color accent,
            float xMin, float yMin, float xMax, float yMax)
        {
            var button = CreateButton(_selectorPanel.transform, "SoloExercise_" + mode, string.Empty, UiTheme.CardElevated);
            SetRect(button.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);

            var line = CreateImage(button.transform, "Accent", accent, false);
            SetRect(line.rectTransform, 0.055f, 0.86f, 0.42f, 0.89f);

            var num = CreateText(button.transform, "Number", number, 15f, FontStyles.Bold, TextAlignmentOptions.TopRight);
            SetRect(num.rectTransform, 0.76f, 0.70f, 0.92f, 0.91f);
            num.color = UiTheme.TextMuted;

            var title = CreateText(button.transform, "Heading", heading, 24f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, 0.055f, 0.49f, 0.90f, 0.73f);

            var description = CreateText(button.transform, "Detail", detail, 16f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetRect(description.rectTransform, 0.055f, 0.10f, 0.91f, 0.49f);
            description.color = UiTheme.TextSecondary;
            description.enableWordWrapping = true;

            button.onClick.AddListener(() => SelectExercise(mode));
        }

        private void ShowExerciseSelector()
        {
            _state = SoloState.SelectingExercise;
            if (_selectorPanel != null) _selectorPanel.SetActive(true);
            if (_optionsPanel != null) _optionsPanel.SetActive(false);
            if (_cameraPanel != null) _cameraPanel.SetActive(false);
        }

        private void SelectExercise(ExerciseMode mode)
        {
            _exercise = mode;
            ExerciseSelection.Current = mode;
            _rounds = DefaultRounds(mode);
            ShowOptions();
        }

        private void ShowOptions()
        {
            _state = SoloState.Options;
            _selectorPanel.SetActive(false);
            _optionsPanel.SetActive(true);
            _cameraPanel.SetActive(false);

            var title = FindText(_optionsPanel, "SoloOptionsTitle");
            if (title != null) title.text = ExerciseSelection.Name(_exercise);
            var rule = FindText(_optionsPanel, "SoloRuleText");
            if (rule != null)
            {
                rule.text = SoloRule(_exercise);
                rule.color = AccentForExercise(_exercise);
            }
            RefreshRoundsLabel();
        }

        private int DefaultRounds(ExerciseMode mode)
        {
            switch (mode)
            {
                case ExerciseMode.AllSame: return 4;
                case ExerciseMode.Decision: return 12;
                case ExerciseMode.Football: return 12;
                default: return 10;
            }
        }

        private void ChangeRounds(int delta)
        {
            _rounds = Mathf.Clamp(_rounds + delta, 3, 30);
            RefreshRoundsLabel();
        }

        private void RefreshRoundsLabel()
        {
            if (_roundsLabel != null) _roundsLabel.text = _rounds.ToString();
        }

        private void StartSelectedExercise()
        {
            _optionsPanel.SetActive(false);
            _cameraPanel.SetActive(true);
            _cameraPanel.transform.SetAsLastSibling();
            ClearCalibration();
            ClearSessionMetrics();
            _state = SoloState.StartingCamera;
            _title.text = "PREPARAR CÁMARA";
            _status.text = "Apoyá el teléfono mirando el espacio de entrenamiento. Intentá que pueda verte de cuerpo completo.";
            _counter.text = "";
            _cue.text = "";
            _result.text = "";
            _primaryButton.gameObject.SetActive(false);

            if (_tracker == null) _tracker = gameObject.AddComponent<CameraPlayerTracker>();
            StartCoroutine(StartCameraRoutine());
        }

        private IEnumerator StartCameraRoutine()
        {
            bool success = false;
            string error = null;
            yield return StartCoroutine(_tracker.StartCamera(_cameraImage, _cameraRect, (ok, message) =>
            {
                success = ok;
                error = message;
            }));

            if (!success)
            {
                ShowFatal("CÁMARA NO DISPONIBLE", error ?? "No pude iniciar la cámara.");
                yield break;
            }

            _state = SoloState.Placing;
            _placementIndex = 0;
            ShowPlacementInstruction();
        }

        private void ShowPlacementInstruction()
        {
            if (_placementIndex >= PlacementOrder.Length)
            {
                _title.text = "CANCHA MARCADA";
                _status.text = "No muevas más el teléfono. Tocá FIJAR CÁMARA y salí completamente de la imagen durante 5 segundos.";
                _counter.text = "5 / 5 ZONAS";
                _cue.text = "LISTO";
                _cue.color = UiTheme.AccentLime;
                _primaryLabel.text = "FIJAR CÁMARA";
                _primaryButton.gameObject.SetActive(true);
                _primaryButton.onClick.RemoveAllListeners();
                _primaryButton.onClick.AddListener(() => StartCoroutine(CaptureBackgroundCountdown()));
                return;
            }

            SoloZone zone = PlacementOrder[_placementIndex];
            _title.text = "MARCAR CANCHA";
            _status.text = PlacementHelp(zone);
            _counter.text = $"{_placementIndex} / {PlacementOrder.Length} ZONAS";
            _cue.text = ZoneName(zone);
            _cue.color = ColorForZone(zone);
        }

        private void Update()
        {
            if (_state == SoloState.Home || _state == SoloState.SelectingExercise || _state == SoloState.Options) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ExitSoloMode();
                return;
            }

            if (_tracker != null) _tracker.UpdatePreviewGeometry();

            if (_state == SoloState.Placing)
            {
                UpdatePlacement();
                return;
            }

            if (_state != SoloState.WaitingCenter &&
                _state != SoloState.ReturningCenter &&
                _state != SoloState.Running)
            {
                return;
            }

            if (Time.unscaledTime < _nextVisionSample) return;
            _nextVisionSample = Time.unscaledTime + VisionInterval;

            if (_tracker == null || !_tracker.TryDetectFeet(out Vector2 feet, out bool tooClose))
            {
                HideFeetMarker();
                ResetDwell();
                return;
            }

            if (tooClose)
            {
                HideFeetMarker();
                ResetDwell();
                _status.text = "Estás demasiado cerca. Alejate hasta que la cámara pueda ver tu desplazamiento completo.";
                return;
            }

            ShowFeetMarker(feet);
            SoloZone zone = ZoneAt(feet);

            if (_state == SoloState.WaitingCenter || _state == SoloState.ReturningCenter)
            {
                UpdateCenterGate(zone);
            }
            else if (_state == SoloState.Running)
            {
                UpdateRunning(zone);
            }
        }

        private void UpdatePlacement()
        {
            if (_placementIndex >= PlacementOrder.Length) return;
            if (!TryGetPointerDown(out Vector2 screenPosition)) return;

            float y01 = screenPosition.y / Mathf.Max(1f, Screen.height);
            if (y01 < 0.17f || y01 > 0.79f) return;

            Vector2 normalized = new Vector2(
                screenPosition.x / Mathf.Max(1f, Screen.width),
                screenPosition.y / Mathf.Max(1f, Screen.height));

            float minDistance = Mathf.Min(Screen.width, Screen.height) * 0.16f;
            foreach (var existing in _zonePositions.Values)
            {
                Vector2 existingPx = new Vector2(existing.x * Screen.width, existing.y * Screen.height);
                if (Vector2.Distance(existingPx, screenPosition) < minDistance)
                {
                    StartCoroutine(TemporaryStatus("Separá un poco más las zonas para que la cámara pueda distinguirlas."));
                    return;
                }
            }

            SoloZone zone = PlacementOrder[_placementIndex];
            CreateZoneMarker(zone, normalized);
            _placementIndex++;
            ShowPlacementInstruction();
        }

        private void CreateZoneMarker(SoloZone zone, Vector2 normalized)
        {
            _zonePositions[zone] = normalized;
            var image = CreateImage(_cameraPanel.transform, "SoloZone_" + zone, WithAlpha(ColorForZone(zone), 0.46f), false);
            var rect = image.rectTransform;
            rect.anchorMin = normalized;
            rect.anchorMax = normalized;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = zone == SoloZone.Center ? new Vector2(132f, 132f) : new Vector2(116f, 116f);

            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.65f);
            outline.effectDistance = new Vector2(3f, -3f);

            var label = CreateText(image.transform, "Label", ShortZoneName(zone), 19f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.color = Color.white;

            _zoneRects[zone] = rect;
            _zoneImages[zone] = image;
            image.transform.SetAsLastSibling();
            BringHudForward();
        }

        private IEnumerator CaptureBackgroundCountdown()
        {
            _state = SoloState.BackgroundCountdown;
            _primaryButton.gameObject.SetActive(false);
            _title.text = "FIJAR CÁMARA";
            _status.text = "Salí de la imagen. Voy a aprender cómo se ve la cancha vacía.";
            _result.text = "";

            for (int n = 5; n >= 1; n--)
            {
                _counter.text = n.ToString();
                _cue.text = "CAMPO VACÍO";
                _cue.color = UiTheme.TextPrimary;
                yield return new WaitForSecondsRealtime(1f);
            }

            _counter.text = "QUIETO";
            yield return new WaitForSecondsRealtime(0.30f);

            if (_tracker == null || !_tracker.CaptureBackground())
            {
                ShowFatal("NO PUDE CALIBRAR", "No recibí una imagen válida. Volvé a intentarlo sin mover el teléfono.");
                yield break;
            }

            _state = SoloState.WaitingCenter;
            _title.text = "CAMPO CALIBRADO";
            _status.text = "Entrá a la cancha y parate sobre CENTRO. Empiezo cuando te detecte estable ahí.";
            _counter.text = ExerciseSelection.Name(_exercise);
            _cue.text = "CENTRO";
            _cue.color = Color.white;
            _centerInsideSince = -1f;
            HighlightOnly(SoloZone.Center, Color.white);
        }

        private void UpdateCenterGate(SoloZone zone)
        {
            if (zone != SoloZone.Center)
            {
                _centerInsideSince = -1f;
                _status.text = _state == SoloState.WaitingCenter
                    ? "Jugador detectado. Ubicate en CENTRO para empezar."
                    : "VOLVÉ AL CENTRO para habilitar el próximo estímulo.";
                return;
            }

            if (_centerInsideSince < 0f)
            {
                _centerInsideSince = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - _centerInsideSince < CenterDwellSeconds) return;

            _centerInsideSince = -1f;
            if (_state == SoloState.WaitingCenter)
            {
                StartCoroutine(SessionCountdown());
            }
            else if (_state == SoloState.ReturningCenter)
            {
                BeginRound();
            }
        }

        private IEnumerator SessionCountdown()
        {
            _state = SoloState.Countdown;
            ResetZoneVisuals();
            _title.text = ExerciseSelection.Name(_exercise);
            _status.text = "Posición inicial confirmada. Preparado.";
            _result.text = "";

            for (int n = 3; n >= 1; n--)
            {
                _cue.text = n.ToString();
                _cue.color = UiTheme.AccentLime;
                yield return new WaitForSecondsRealtime(0.70f);
            }

            _cue.text = "¡YA!";
            yield return new WaitForSecondsRealtime(0.35f);

            ClearSessionMetrics();
            BeginRound();
        }

        private void BeginRound()
        {
            if (_currentRound >= _rounds)
            {
                FinishSession();
                return;
            }

            _currentRound++;
            _state = SoloState.Running;
            _candidateZone = SoloZone.None;
            _candidateInsideSince = -1f;
            _currentTarget = SoloZone.None;
            _currentCueColor = StationColor.None;
            _fakeChanged = false;
            _speedVisited.Clear();
            _result.text = "";
            _counter.text = $"RONDA  {_currentRound} / {_rounds}";
            _stimulusStartedAt = Time.unscaledTime;

            switch (_exercise)
            {
                case ExerciseMode.Reaction:
                    BeginReactionRound();
                    break;
                case ExerciseMode.AllSame:
                    BeginSpeedRound();
                    break;
                case ExerciseMode.Colors:
                    BeginColorsRound();
                    break;
                case ExerciseMode.Decision:
                    BeginDecisionRound();
                    break;
                case ExerciseMode.CognitiveFake:
                    BeginFakeRound();
                    break;
                case ExerciseMode.Football:
                    BeginFootballRound();
                    break;
            }
        }

        private void BeginReactionRound()
        {
            _currentTarget = PickDirection();
            _currentCueColor = StationColor.Green;
            _cue.text = ZoneName(_currentTarget);
            _cue.color = UiTheme.Positive;
            _status.text = "REACCIÓN  •  movete al objetivo verde.";
            HighlightOnly(_currentTarget, UiTheme.Positive);
            _roundDeadline = Time.unscaledTime + 5f;
        }

        private void BeginSpeedRound()
        {
            _cue.text = "4 ZONAS";
            _cue.color = UiTheme.Info;
            _status.text = "VELOCIDAD  •  visitá FRENTE, ATRÁS, IZQUIERDA y DERECHA en cualquier orden.";
            foreach (SoloZone zone in DirectionZones) SetZoneVisual(zone, UiTheme.Info, 0.72f, 1.06f);
            SetZoneVisual(SoloZone.Center, Color.white, 0.20f, 1f);
            _roundDeadline = Time.unscaledTime + 15f;
        }

        private void BeginColorsRound()
        {
            _currentCueColor = RandomDirectionColor();
            _currentTarget = ZoneForColor(_currentCueColor);
            _cue.text = ColorName(_currentCueColor);
            _cue.color = ColorForStation(_currentCueColor);
            _status.text = "COLORES  •  andá solamente a la zona del color indicado.";
            ShowFixedColorMap(0.62f);
            _roundDeadline = Time.unscaledTime + 5f;
        }

        private void BeginDecisionRound()
        {
            _currentCueColor = RandomDirectionColor();
            _currentTarget = ZoneForColor(_currentCueColor);
            _cue.text = ColorName(_currentCueColor);
            _cue.color = ColorForStation(_currentCueColor);
            _status.text = "DECISIÓN  •  recordá: verde frente · rojo atrás · azul izquierda · amarillo derecha.";
            ShowFixedColorMap(0.34f);
            _roundDeadline = Time.unscaledTime + 5f;
        }

        private void BeginFakeRound()
        {
            _currentCueColor = RandomDirectionColor();
            _currentTarget = ZoneForColor(_currentCueColor);
            _cue.text = ColorName(_currentCueColor);
            _cue.color = ColorForStation(_currentCueColor);
            _status.text = "FINTA  •  empezá a reaccionar, pero preparate para cambiar la decisión.";
            ShowFixedColorMap(0.28f);
            _fakeChangeAt = Time.unscaledTime + CognitiveChangeDelay;
            _roundDeadline = Time.unscaledTime + 6.5f;
        }

        private void BeginFootballRound()
        {
            int pick = Random.Range(0, 3);
            if (pick == 2)
            {
                _currentCueColor = StationColor.Red;
                _currentTarget = SoloZone.None;
                _cue.text = "QUIETO";
                _cue.color = UiTheme.Danger;
                _status.text = "FÚTBOL  •  ROJO = no salgas del centro.";
                ResetZoneVisuals();
                SetZoneVisual(SoloZone.Center, Color.white, 0.75f, 1.08f);
                _footballNoGoEndAt = Time.unscaledTime + 2.5f;
                _roundDeadline = _footballNoGoEndAt;
            }
            else
            {
                _currentCueColor = pick == 0 ? StationColor.Green : StationColor.Blue;
                _currentTarget = PickDirection();
                string foot = _currentCueColor == StationColor.Green ? "PIE DERECHO" : "PIE IZQUIERDO";
                _cue.text = foot + "\n<size=62%>" + ZoneName(_currentTarget) + "</size>";
                _cue.color = ColorForStation(_currentCueColor);
                _status.text = "FÚTBOL  •  la cámara valida la zona; cumplí la consigna del pie indicada.";
                HighlightOnly(_currentTarget, ColorForStation(_currentCueColor));
                _roundDeadline = Time.unscaledTime + 5f;
            }
        }

        private void UpdateRunning(SoloZone detectedZone)
        {
            if (_exercise == ExerciseMode.CognitiveFake && !_fakeChanged && Time.unscaledTime >= _fakeChangeAt)
            {
                TriggerFakeChange();
            }

            if (_exercise == ExerciseMode.Football && _currentCueColor == StationColor.Red)
            {
                UpdateFootballNoGo(detectedZone);
                return;
            }

            if (Time.unscaledTime >= _roundDeadline)
            {
                CompleteAttempt(false, SoloZone.None, 0f, "Tiempo agotado");
                return;
            }

            if (_exercise == ExerciseMode.AllSame)
            {
                UpdateSpeedRound(detectedZone);
                return;
            }

            if (_exercise == ExerciseMode.CognitiveFake && !_fakeChanged && IsDirection(detectedZone))
            {
                // Llegar a una dirección antes de la finta es anticipación incorrecta.
                if (UpdateCandidateDwell(detectedZone))
                {
                    CompleteAttempt(false, detectedZone, Time.unscaledTime - _stimulusStartedAt, "Te anticipaste al cambio");
                }
                return;
            }

            if (!IsDirection(detectedZone))
            {
                _candidateZone = SoloZone.None;
                _candidateInsideSince = -1f;
                return;
            }

            if (!UpdateCandidateDwell(detectedZone)) return;

            float reaction = Time.unscaledTime - _stimulusStartedAt;
            bool correct = detectedZone == _currentTarget;
            CompleteAttempt(correct, detectedZone, reaction,
                correct ? "Correcto" : $"Esperaba {ZoneName(_currentTarget)}");
        }

        private void UpdateSpeedRound(SoloZone detectedZone)
        {
            if (!IsDirection(detectedZone))
            {
                _candidateZone = SoloZone.None;
                _candidateInsideSince = -1f;
                return;
            }

            if (_speedVisited.Contains(detectedZone)) return;
            if (!UpdateCandidateDwell(detectedZone)) return;

            _speedVisited.Add(detectedZone);
            AppSettings.Vibrate();
            SetZoneVisual(detectedZone, UiTheme.Positive, 0.24f, 0.92f);
            _candidateZone = SoloZone.None;
            _candidateInsideSince = -1f;
            _result.text = $"{_speedVisited.Count} / 4";
            _result.color = UiTheme.AccentLime;

            if (_speedVisited.Count >= 4)
            {
                float elapsed = Time.unscaledTime - _stimulusStartedAt;
                CompleteAttempt(true, SoloZone.None, elapsed, "4 zonas completas");
            }
        }

        private void UpdateFootballNoGo(SoloZone detectedZone)
        {
            if (IsDirection(detectedZone))
            {
                if (UpdateCandidateDwell(detectedZone))
                {
                    CompleteAttempt(false, detectedZone, 0f, "Te moviste en ROJO");
                }
                return;
            }

            _candidateZone = SoloZone.None;
            _candidateInsideSince = -1f;

            if (Time.unscaledTime >= _footballNoGoEndAt)
            {
                CompleteAttempt(true, SoloZone.Center, 0f, "Bien, quieto");
            }
        }

        private void TriggerFakeChange()
        {
            _fakeChanged = true;
            StationColor previous = _currentCueColor;
            do
            {
                _currentCueColor = RandomDirectionColor();
            } while (_currentCueColor == previous);

            _currentTarget = ZoneForColor(_currentCueColor);
            _stimulusStartedAt = Time.unscaledTime;
            _roundDeadline = Time.unscaledTime + 5f;
            _candidateZone = SoloZone.None;
            _candidateInsideSince = -1f;

            _cue.text = "CAMBIO  →  " + ColorName(_currentCueColor);
            _cue.color = ColorForStation(_currentCueColor);
            _status.text = "CAMBIÓ LA DECISIÓN  •  corregí tu movimiento hacia la nueva dirección.";
            ShowFixedColorMap(0.30f);
            StartCoroutine(PunchCue());
        }

        private bool UpdateCandidateDwell(SoloZone zone)
        {
            if (_candidateZone != zone)
            {
                _candidateZone = zone;
                _candidateInsideSince = Time.unscaledTime;
                return false;
            }

            if (_candidateInsideSince < 0f)
            {
                _candidateInsideSince = Time.unscaledTime;
                return false;
            }

            return Time.unscaledTime - _candidateInsideSince >= ZoneDwellSeconds;
        }

        private void CompleteAttempt(bool hit, SoloZone actual, float seconds, string note)
        {
            if (_state != SoloState.Running) return;
            _state = SoloState.Feedback;

            var attempt = new SoloAttempt
            {
                Round = _currentRound,
                Expected = _exercise == ExerciseMode.AllSame ? SoloZone.None : _currentTarget,
                Actual = actual,
                CueColor = _currentCueColor,
                Hit = hit,
                Seconds = Mathf.Max(0f, seconds),
                Note = note
            };
            _attempts.Add(attempt);

            if (hit) _hits++;
            else _misses++;

            AppSettings.Vibrate();
            StartCoroutine(FeedbackRoutine(hit, seconds, note));
        }

        private IEnumerator FeedbackRoutine(bool hit, float seconds, string note)
        {
            _cue.text = hit ? "CORRECTO" : "ERROR";
            _cue.color = hit ? UiTheme.AccentLime : UiTheme.Danger;
            _result.text = seconds > 0f ? $"{seconds:F2}s\n<size=55%>{note}</size>" : note;
            _result.color = hit ? UiTheme.TextPrimary : UiTheme.Danger;
            _status.text = hit ? "Respuesta registrada." : "La próxima vuelve a salir desde CENTRO.";
            ResetZoneVisuals();

            yield return new WaitForSecondsRealtime(0.70f);

            if (_currentRound >= _rounds)
            {
                FinishSession();
                yield break;
            }

            _state = SoloState.ReturningCenter;
            _cue.text = "CENTRO";
            _cue.color = Color.white;
            _result.text = "";
            _status.text = "VOLVÉ AL CENTRO para habilitar el próximo estímulo.";
            _centerInsideSince = -1f;
            HighlightOnly(SoloZone.Center, Color.white);
        }

        private void FinishSession()
        {
            _state = SoloState.Finished;
            HideFeetMarker();
            ResetZoneVisuals();

            float average = AverageSeconds();
            float best = BestSeconds();
            float accuracy = _rounds > 0 ? (_hits * 100f / _rounds) : 0f;

            _title.text = "ENTRENAMIENTO COMPLETO";
            _counter.text = ExerciseSelection.Name(_exercise);
            _cue.text = $"{accuracy:F0}% PRECISIÓN";
            _cue.color = accuracy >= 80f ? UiTheme.AccentLime : UiTheme.Accent;
            _status.text = "Resultados de esta sesión con un solo teléfono.";
            _result.text = BuildSummary(average, best);
            _result.color = UiTheme.TextPrimary;

            _primaryLabel.text = "REPETIR SIN RECALIBRAR";
            _primaryButton.gameObject.SetActive(true);
            _primaryButton.onClick.RemoveAllListeners();
            _primaryButton.onClick.AddListener(RepeatSession);
        }

        private void RepeatSession()
        {
            ClearSessionMetrics();
            _primaryButton.gameObject.SetActive(false);
            _state = SoloState.ReturningCenter;
            _title.text = ExerciseSelection.Name(_exercise);
            _counter.text = "NUEVA SESIÓN";
            _cue.text = "CENTRO";
            _cue.color = Color.white;
            _result.text = "";
            _status.text = "Volvé al CENTRO. Reutilizo la calibración porque el teléfono no se movió.";
            _centerInsideSince = -1f;
            HighlightOnly(SoloZone.Center, Color.white);
        }

        private string BuildSummary(float average, float best)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"✓  {_hits} aciertos     ✕  {_misses} errores");
            sb.AppendLine($"Promedio  {average:F2}s     Mejor  {best:F2}s");

            if (_exercise == ExerciseMode.AllSame)
            {
                sb.AppendLine("\nCada ronda = completar las 4 zonas.");
                return sb.ToString();
            }

            if (_exercise == ExerciseMode.Football)
            {
                AppendFootballStats(sb);
                sb.AppendLine("\nLa cámara valida desplazamiento/zona; el pie indicado todavía depende del jugador.");
                return sb.ToString();
            }

            sb.AppendLine("\nPOR DIRECCIÓN");
            foreach (SoloZone zone in DirectionZones)
            {
                int total = 0;
                int good = 0;
                foreach (var attempt in _attempts)
                {
                    if (attempt.Expected != zone) continue;
                    total++;
                    if (attempt.Hit) good++;
                }

                if (total > 0)
                {
                    sb.AppendLine($"{ZoneName(zone),-10}  {(good * 100f / total):F0}%  ({good}/{total})");
                }
            }

            if (_exercise == ExerciseMode.CognitiveFake)
            {
                sb.AppendLine("\nTiempo medido desde el CAMBIO de estímulo.");
            }

            return sb.ToString();
        }

        private void AppendFootballStats(System.Text.StringBuilder sb)
        {
            int greenTotal = 0, greenHit = 0;
            int blueTotal = 0, blueHit = 0;
            int redTotal = 0, redHit = 0;

            foreach (var attempt in _attempts)
            {
                switch (attempt.CueColor)
                {
                    case StationColor.Green:
                        greenTotal++;
                        if (attempt.Hit) greenHit++;
                        break;
                    case StationColor.Blue:
                        blueTotal++;
                        if (attempt.Hit) blueHit++;
                        break;
                    case StationColor.Red:
                        redTotal++;
                        if (attempt.Hit) redHit++;
                        break;
                }
            }

            sb.AppendLine("\nFÚTBOL");
            if (greenTotal > 0) sb.AppendLine($"Derecho   {(greenHit * 100f / greenTotal):F0}% ({greenHit}/{greenTotal})");
            if (blueTotal > 0) sb.AppendLine($"Izquierdo {(blueHit * 100f / blueTotal):F0}% ({blueHit}/{blueTotal})");
            if (redTotal > 0) sb.AppendLine($"Quieto     {(redHit * 100f / redTotal):F0}% ({redHit}/{redTotal})");
        }

        private float AverageSeconds()
        {
            float sum = 0f;
            int count = 0;
            foreach (var attempt in _attempts)
            {
                if (!attempt.Hit || attempt.Seconds <= 0f) continue;
                sum += attempt.Seconds;
                count++;
            }
            return count == 0 ? 0f : sum / count;
        }

        private float BestSeconds()
        {
            float best = float.MaxValue;
            foreach (var attempt in _attempts)
            {
                if (!attempt.Hit || attempt.Seconds <= 0f) continue;
                best = Mathf.Min(best, attempt.Seconds);
            }
            return best == float.MaxValue ? 0f : best;
        }

        private void ClearSessionMetrics()
        {
            _attempts.Clear();
            _hits = 0;
            _misses = 0;
            _currentRound = 0;
            _lastTarget = SoloZone.None;
            _candidateZone = SoloZone.None;
            _candidateInsideSince = -1f;
            _centerInsideSince = -1f;
        }

        private void ClearCalibration()
        {
            foreach (var rect in _zoneRects.Values)
            {
                if (rect != null) Destroy(rect.gameObject);
            }
            _zonePositions.Clear();
            _zoneRects.Clear();
            _zoneImages.Clear();
            _placementIndex = 0;
            HideFeetMarker();
        }

        private SoloZone PickDirection()
        {
            if (DirectionZones.Length == 1) return DirectionZones[0];
            SoloZone selected;
            do
            {
                selected = DirectionZones[Random.Range(0, DirectionZones.Length)];
            } while (selected == _lastTarget);
            _lastTarget = selected;
            return selected;
        }

        private StationColor RandomDirectionColor()
        {
            int pick = Random.Range(0, 4);
            switch (pick)
            {
                case 0: return StationColor.Green;
                case 1: return StationColor.Red;
                case 2: return StationColor.Blue;
                default: return StationColor.Yellow;
            }
        }

        private static SoloZone ZoneForColor(StationColor color)
        {
            switch (color)
            {
                case StationColor.Green: return SoloZone.Front;
                case StationColor.Red: return SoloZone.Back;
                case StationColor.Blue: return SoloZone.Left;
                case StationColor.Yellow: return SoloZone.Right;
                default: return SoloZone.None;
            }
        }

        private SoloZone ZoneAt(Vector2 normalized)
        {
            Vector2 point = new Vector2(normalized.x * Screen.width, normalized.y * Screen.height);
            float radius = Mathf.Min(Screen.width, Screen.height) * 0.135f;
            float best = float.MaxValue;
            SoloZone result = SoloZone.None;

            foreach (var kv in _zonePositions)
            {
                Vector2 center = new Vector2(kv.Value.x * Screen.width, kv.Value.y * Screen.height);
                float distance = Vector2.Distance(point, center);
                if (distance <= radius && distance < best)
                {
                    best = distance;
                    result = kv.Key;
                }
            }
            return result;
        }

        private void ResetDwell()
        {
            _candidateZone = SoloZone.None;
            _candidateInsideSince = -1f;
            _centerInsideSince = -1f;
        }

        private void HighlightOnly(SoloZone zone, Color color)
        {
            foreach (SoloZone z in _zoneImages.Keys)
            {
                if (z == zone) SetZoneVisual(z, color, 0.88f, 1.14f);
                else SetZoneVisual(z, ColorForZone(z), 0.18f, 0.92f);
            }
            BringHudForward();
        }

        private void ResetZoneVisuals()
        {
            foreach (SoloZone zone in _zoneImages.Keys)
            {
                SetZoneVisual(zone, ColorForZone(zone), zone == SoloZone.Center ? 0.30f : 0.35f, 1f);
            }
            BringHudForward();
        }

        private void ShowFixedColorMap(float alpha)
        {
            SetZoneVisual(SoloZone.Front, ColorForStation(StationColor.Green), alpha, 1f);
            SetZoneVisual(SoloZone.Back, ColorForStation(StationColor.Red), alpha, 1f);
            SetZoneVisual(SoloZone.Left, ColorForStation(StationColor.Blue), alpha, 1f);
            SetZoneVisual(SoloZone.Right, ColorForStation(StationColor.Yellow), alpha, 1f);
            SetZoneVisual(SoloZone.Center, Color.white, 0.18f, 0.92f);
            BringHudForward();
        }

        private void SetZoneVisual(SoloZone zone, Color color, float alpha, float scale)
        {
            if (!_zoneImages.TryGetValue(zone, out Image image) || image == null) return;
            image.color = WithAlpha(color, alpha);
            image.rectTransform.localScale = Vector3.one * scale;
        }

        private void ShowFeetMarker(Vector2 normalized)
        {
            if (_feetMarker == null) return;
            _feetMarker.gameObject.SetActive(true);
            _feetMarker.anchorMin = normalized;
            _feetMarker.anchorMax = normalized;
            _feetMarker.anchoredPosition = Vector2.zero;
            if (_feetMarkerImage != null) _feetMarkerImage.color = UiTheme.AccentLime;
            _feetMarker.transform.SetAsLastSibling();
            BringHudForward();
        }

        private void HideFeetMarker()
        {
            if (_feetMarker != null) _feetMarker.gameObject.SetActive(false);
        }

        private void BringHudForward()
        {
            if (_counter != null) _counter.transform.SetAsLastSibling();
            if (_cue != null) _cue.transform.SetAsLastSibling();
            if (_result != null) _result.transform.SetAsLastSibling();
            if (_primaryButton != null) _primaryButton.transform.SetAsLastSibling();
            var back = FindDeep("SoloCameraBack");
            if (back != null) back.transform.SetAsLastSibling();
        }

        private IEnumerator PunchCue()
        {
            if (_cue == null) yield break;
            Transform t = _cue.transform;
            float elapsed = 0f;
            while (elapsed < 0.22f)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / 0.22f);
                float scale = Mathf.Lerp(1.18f, 1f, k);
                t.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        private IEnumerator TemporaryStatus(string message)
        {
            string previous = _status != null ? _status.text : string.Empty;
            Color previousColor = _status != null ? _status.color : UiTheme.TextPrimary;
            if (_status != null)
            {
                _status.text = message;
                _status.color = UiTheme.Danger;
            }
            yield return new WaitForSecondsRealtime(1.25f);
            if (_status != null)
            {
                _status.text = previous;
                _status.color = previousColor;
            }
        }

        private void ShowFatal(string title, string message)
        {
            _state = SoloState.Fatal;
            _title.text = title;
            _status.text = message;
            _counter.text = "";
            _cue.text = "";
            _result.text = "";
            _primaryButton.gameObject.SetActive(false);
        }

        private void ExitSoloMode()
        {
            if (_tracker != null) _tracker.StopCamera();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

        private GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return go;
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

            if (!string.IsNullOrEmpty(text))
            {
                var label = CreateText(go.transform, "Label", text, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(14f, 5f);
                label.rectTransform.offsetMax = new Vector2(-14f, -5f);
            }
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

        private static TMP_Text CreateText(Transform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = UiTheme.TextPrimary;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(11f, size * 0.50f);
            label.fontSizeMax = size;
            label.enableWordWrapping = true;
            return label;
        }

        private GameObject FindDeep(string objectName)
        {
            if (_canvas == null) return null;
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }
            return null;
        }

        private static TMP_Text FindText(GameObject root, string objectName)
        {
            if (root == null) return null;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == objectName) return text;
            }
            return null;
        }

        private void SetObjectActive(string objectName, bool active)
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

        private static string SoloRule(ExerciseMode mode)
        {
            switch (mode)
            {
                case ExerciseMode.Reaction:
                    return "La app elige una de las 4 direcciones. Llegá, registrá el tiempo y volvé al centro.";
                case ExerciseMode.AllSame:
                    return "Las 4 zonas quedan activas. Visitá todas en cualquier orden; medimos el tiempo total.";
                case ExerciseMode.Colors:
                    return "FRENTE verde · ATRÁS rojo · IZQUIERDA azul · DERECHA amarillo. Buscá el color indicado.";
                case ExerciseMode.Decision:
                    return "Memorizá la regla de colores y reaccioná a la dirección correcta sin que la app te la diga.";
                case ExerciseMode.CognitiveFake:
                    return "A los 0,65 s el color cambia. Frená la primera decisión y corregí hacia la nueva dirección.";
                case ExerciseMode.Football:
                    return "VERDE pie derecho · AZUL pie izquierdo · ROJO quieto. La cámara valida movimiento y zona.";
                default:
                    return ExerciseSelection.Rule(mode);
            }
        }

        private static Color AccentForExercise(ExerciseMode mode)
        {
            switch (mode)
            {
                case ExerciseMode.Reaction: return UiTheme.Positive;
                case ExerciseMode.AllSame: return UiTheme.Info;
                case ExerciseMode.Colors: return UiTheme.Accent;
                case ExerciseMode.Decision: return UiTheme.AccentLime;
                case ExerciseMode.CognitiveFake: return new Color32(0xC0, 0x75, 0xFF, 0xFF);
                case ExerciseMode.Football: return new Color32(0x4C, 0xC9, 0x9A, 0xFF);
                default: return UiTheme.Accent;
            }
        }

        private static string PlacementHelp(SoloZone zone)
        {
            switch (zone)
            {
                case SoloZone.Center: return "Tocá el piso donde vas a esperar entre estímulos. Este será tu CENTRO.";
                case SoloZone.Front: return "Tocá un objetivo hacia ADELANTE respecto del centro.";
                case SoloZone.Left: return "Tocá un objetivo hacia tu IZQUIERDA.";
                case SoloZone.Right: return "Tocá un objetivo hacia tu DERECHA.";
                case SoloZone.Back: return "Tocá un objetivo hacia ATRÁS respecto del centro.";
                default: return "Tocá la posición de la zona.";
            }
        }

        private static string ZoneName(SoloZone zone)
        {
            switch (zone)
            {
                case SoloZone.Front: return "FRENTE";
                case SoloZone.Left: return "IZQUIERDA";
                case SoloZone.Center: return "CENTRO";
                case SoloZone.Right: return "DERECHA";
                case SoloZone.Back: return "ATRÁS";
                default: return "";
            }
        }

        private static string ShortZoneName(SoloZone zone)
        {
            switch (zone)
            {
                case SoloZone.Front: return "FRENTE";
                case SoloZone.Left: return "IZQ";
                case SoloZone.Center: return "CENTRO";
                case SoloZone.Right: return "DER";
                case SoloZone.Back: return "ATRÁS";
                default: return "";
            }
        }

        private static bool IsDirection(SoloZone zone)
        {
            return zone == SoloZone.Front || zone == SoloZone.Left || zone == SoloZone.Right || zone == SoloZone.Back;
        }

        private static string ColorName(StationColor color)
        {
            switch (color)
            {
                case StationColor.Green: return "VERDE";
                case StationColor.Red: return "ROJO";
                case StationColor.Blue: return "AZUL";
                case StationColor.Yellow: return "AMARILLO";
                default: return "COLOR";
            }
        }

        private static Color ColorForZone(SoloZone zone)
        {
            switch (zone)
            {
                case SoloZone.Front: return ColorForStation(StationColor.Green);
                case SoloZone.Back: return ColorForStation(StationColor.Red);
                case SoloZone.Left: return ColorForStation(StationColor.Blue);
                case SoloZone.Right: return ColorForStation(StationColor.Yellow);
                case SoloZone.Center: return new Color32(0xF4, 0xF7, 0xFB, 0xFF);
                default: return UiTheme.Neutral;
            }
        }

        private static Color ColorForStation(StationColor color)
        {
            switch (color)
            {
                case StationColor.Green: return new Color32(0x45, 0xD4, 0x75, 0xFF);
                case StationColor.Red: return new Color32(0xEF, 0x53, 0x50, 0xFF);
                case StationColor.Blue: return new Color32(0x3D, 0x8B, 0xFF, 0xFF);
                case StationColor.Yellow: return new Color32(0xFF, 0xC8, 0x3D, 0xFF);
                default: return UiTheme.TextPrimary;
            }
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

        private void OnDestroy()
        {
            if (_tracker != null) _tracker.StopCamera();
        }
    }
}
