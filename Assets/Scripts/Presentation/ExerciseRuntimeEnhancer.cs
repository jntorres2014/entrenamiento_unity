using System.Collections;
using Entrenamiento.Core.Models;
using Entrenamiento.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Capa Unity para los presets: muestra la consigna en el host, programa
    /// Finta Cognitiva y etiqueta el resumen final con el ejercicio ejecutado.
    /// </summary>
    public sealed class ExerciseRuntimeEnhancer : MonoBehaviour
    {
        private Canvas _canvas;
        private SessionCoordinator _coordinator;
        private TMP_Text _cueLabel;
        private Image _cueCard;
        private Coroutine _fakeCoroutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<ExerciseRuntimeEnhancer>() == null)
                {
                    canvas.gameObject.AddComponent<ExerciseRuntimeEnhancer>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void Update()
        {
            var current = ExerciseRuntimeRegistry.CurrentCoordinator;
            if (current == _coordinator) return;

            Unsubscribe();
            _coordinator = current;
            Subscribe();
        }

        private void Subscribe()
        {
            if (_coordinator == null) return;
            _coordinator.OnRoundStarted += HandleRoundStarted;
            _coordinator.OnStimulusChanged += HandleStimulusChanged;
            _coordinator.OnSessionFinished += HandleSessionFinished;
        }

        private void Unsubscribe()
        {
            if (_coordinator == null) return;
            _coordinator.OnRoundStarted -= HandleRoundStarted;
            _coordinator.OnStimulusChanged -= HandleStimulusChanged;
            _coordinator.OnSessionFinished -= HandleSessionFinished;
        }

        private void HandleRoundStarted(int round, string stationId, StationColor color, bool isGo)
        {
            EnsureCueCard();
            ShowCue(CueFor(_coordinator.Config.Exercise, color, isGo), color);

            if (_fakeCoroutine != null)
            {
                StopCoroutine(_fakeCoroutine);
                _fakeCoroutine = null;
            }

            if (_coordinator.Config.Exercise == ExerciseMode.CognitiveFake)
            {
                _fakeCoroutine = StartCoroutine(ChangeCognitiveStimulusAfterDelay(round));
            }
        }

        private IEnumerator ChangeCognitiveStimulusAfterDelay(int round)
        {
            float delay = Mathf.Max(0.20f, _coordinator.Config.CognitiveChangeDelaySeconds);
            yield return new WaitForSecondsRealtime(delay);

            if (_coordinator != null &&
                _coordinator.IsRunning &&
                _coordinator.CurrentRound == round &&
                _coordinator.Config.Exercise == ExerciseMode.CognitiveFake)
            {
                _coordinator.TriggerCognitiveFakeChange();
            }
            _fakeCoroutine = null;
        }

        private void HandleStimulusChanged(int round, string stationId, StationColor color, bool isGo)
        {
            EnsureCueCard();
            ShowCue("CAMBIO  →  " + CueFor(ExerciseMode.Decision, color, isGo), color);
            StartCoroutine(PunchCue());
        }

        private void HandleSessionFinished()
        {
            if (_fakeCoroutine != null)
            {
                StopCoroutine(_fakeCoroutine);
                _fakeCoroutine = null;
            }
            if (_cueCard != null) _cueCard.gameObject.SetActive(false);

            var summaryGo = FindDeep("SummaryLabel");
            var summary = summaryGo != null ? summaryGo.GetComponent<TMP_Text>() : null;
            if (summary != null && _coordinator != null)
            {
                string title = ExerciseSelection.Name(_coordinator.Config.Exercise);
                string rule = ExerciseSelection.Rule(_coordinator.Config.Exercise);
                summary.text = $"<color=#FF7A32><b>{title}</b></color>\n<size=70%><color=#A8B2C1>{rule}</color></size>\n\n" + summary.text;
            }
        }

        private void EnsureCueCard()
        {
            if (_cueCard != null || _canvas == null) return;

            var progress = FindDeep("HostProgressPanel");
            if (progress == null) return;

            var cardGo = new GameObject("ExerciseCueCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardGo.transform.SetParent(progress.transform, false);
            _cueCard = cardGo.GetComponent<Image>();
            _cueCard.color = new Color(UiTheme.Surface.r, UiTheme.Surface.g, UiTheme.Surface.b, 0.96f);
            _cueCard.raycastTarget = false;
            SetRect(_cueCard.rectTransform, 0.075f, 0.205f, 0.925f, 0.285f);

            var labelGo = new GameObject("ExerciseCueLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(cardGo.transform, false);
            _cueLabel = labelGo.GetComponent<TextMeshProUGUI>();
            _cueLabel.fontSize = 28f;
            _cueLabel.fontStyle = FontStyles.Bold;
            _cueLabel.alignment = TextAlignmentOptions.Center;
            _cueLabel.color = UiTheme.TextPrimary;
            _cueLabel.raycastTarget = false;
            _cueLabel.enableAutoSizing = true;
            _cueLabel.fontSizeMin = 17f;
            _cueLabel.fontSizeMax = 31f;
            Stretch(_cueLabel.rectTransform);
            _cueLabel.rectTransform.offsetMin = new Vector2(20f, 6f);
            _cueLabel.rectTransform.offsetMax = new Vector2(-20f, -6f);
        }

        private void ShowCue(string text, StationColor color)
        {
            if (_cueCard == null || _cueLabel == null) return;
            _cueCard.gameObject.SetActive(true);
            _cueCard.transform.SetAsLastSibling();
            _cueLabel.text = text;
            _cueLabel.color = UiTheme.TextPrimary;

            var outline = _cueCard.GetComponent<Outline>();
            if (outline == null) outline = _cueCard.gameObject.AddComponent<Outline>();
            outline.effectColor = ColorFor(color);
            outline.effectDistance = new Vector2(3f, -3f);
        }

        private IEnumerator PunchCue()
        {
            if (_cueCard == null) yield break;
            Transform t = _cueCard.transform;
            float elapsed = 0f;
            while (elapsed < 0.22f)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / 0.22f);
                float scale = Mathf.Lerp(1.08f, 1f, k);
                t.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        private static string CueFor(ExerciseMode mode, StationColor color, bool isGo)
        {
            switch (mode)
            {
                case ExerciseMode.Reaction:
                    return "REACCIÓN  •  TOCÁ EL POD VERDE";
                case ExerciseMode.AllSame:
                    return "VELOCIDAD  •  APAGÁ TODOS LOS PODS AZULES";
                case ExerciseMode.Colors:
                    return "TOCÁ SOLO  •  " + ColorName(color);
                case ExerciseMode.Decision:
                case ExerciseMode.CognitiveFake:
                    return DecisionName(color);
                case ExerciseMode.Football:
                    if (color == StationColor.Green) return "PIE DERECHO";
                    if (color == StationColor.Blue) return "PIE IZQUIERDO";
                    return "ROJO  •  NO TOCAR";
                default:
                    return isGo ? "¡REACCIONÁ!" : "QUIETO";
            }
        }

        private static string DecisionName(StationColor color)
        {
            switch (color)
            {
                case StationColor.Green: return "VERDE  •  AVANZAR";
                case StationColor.Red: return "ROJO  •  RETROCEDER";
                case StationColor.Blue: return "AZUL  •  IZQUIERDA";
                case StationColor.Yellow: return "AMARILLO  •  DERECHA";
                default: return "CAMBIO DE DIRECCIÓN";
            }
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

        private static Color ColorFor(StationColor color)
        {
            switch (color)
            {
                case StationColor.Green: return new Color32(0x45, 0xD4, 0x75, 0xFF);
                case StationColor.Red: return new Color32(0xEF, 0x53, 0x50, 0xFF);
                case StationColor.Blue: return new Color32(0x3D, 0x8B, 0xFF, 0xFF);
                case StationColor.Yellow: return new Color32(0xFF, 0xC8, 0x3D, 0xFF);
                default: return UiTheme.Accent;
            }
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
            Unsubscribe();
        }
    }
}
