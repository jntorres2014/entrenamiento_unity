using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Capa de efectos livianos para TrainingNearby.
    /// Agrega transiciones, luces ambientales y celebración final sin plugins.
    /// </summary>
    public sealed class TrainingUiMagic : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _lastActivePanel;
        private GameObject _summaryPanel;
        private Image _orangeGlow;
        private Image _blueGlow;
        private bool _summaryCelebrated;
        private readonly List<GameObject> _particles = new List<GameObject>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<TrainingUiMagic>() == null)
                {
                    canvas.gameObject.AddComponent<TrainingUiMagic>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _summaryPanel = FindDeep("SummaryPanel");
            CreateAmbientGlows();
        }

        private void Start()
        {
            StartCoroutine(AnimateInitialPanel());
        }

        private IEnumerator AnimateInitialPanel()
        {
            yield return null;
            var panel = GetCurrentPanel();
            if (panel != null)
            {
                _lastActivePanel = panel;
                StartCoroutine(AnimatePanelIn(panel));
            }
        }

        private void Update()
        {
            AnimateGlows();

            var active = GetCurrentPanel();
            if (active != null && active != _lastActivePanel)
            {
                _lastActivePanel = active;
                StartCoroutine(AnimatePanelIn(active));
            }

            if (_summaryPanel != null && _summaryPanel.activeInHierarchy)
            {
                if (!_summaryCelebrated)
                {
                    _summaryCelebrated = true;
                    StartCoroutine(CelebrateSummary());
                }
            }
            else
            {
                _summaryCelebrated = false;
            }
        }

        private GameObject GetCurrentPanel()
        {
            string[] names = { "RolePanel", "HostConfigPanel", "HostProgressPanel", "StationWaitPanel", "SummaryPanel" };
            foreach (string name in names)
            {
                var go = FindDeep(name);
                if (go != null && go.activeInHierarchy) return go;
            }
            return null;
        }

        private GameObject FindDeep(string objectName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }
            return null;
        }

        private IEnumerator AnimatePanelIn(GameObject panel)
        {
            if (panel == null) yield break;

            var group = panel.GetComponent<CanvasGroup>();
            if (group == null) group = panel.AddComponent<CanvasGroup>();

            var rect = panel.transform as RectTransform;
            Vector3 baseScale = rect != null ? rect.localScale : Vector3.one;

            group.alpha = 0f;
            if (rect != null) rect.localScale = baseScale * 0.965f;

            float elapsed = 0f;
            const float duration = 0.28f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                group.alpha = eased;
                if (rect != null)
                {
                    rect.localScale = Vector3.Lerp(baseScale * 0.965f, baseScale, eased);
                }
                yield return null;
            }

            group.alpha = 1f;
            if (rect != null) rect.localScale = baseScale;
        }

        private void CreateAmbientGlows()
        {
            Sprite rounded = FindRoundedSprite();
            _orangeGlow = CreateGlow("OrangeGlow", UiTheme.Accent, rounded, new Vector2(-0.18f, 0.78f), new Vector2(430f, 430f), 0.075f);
            _blueGlow = CreateGlow("BlueGlow", UiTheme.Info, rounded, new Vector2(1.12f, 0.22f), new Vector2(500f, 500f), 0.055f);
        }

        private Image CreateGlow(string name, Color color, Sprite sprite, Vector2 anchor, Vector2 size, float alpha)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetSiblingIndex(Mathf.Min(1, go.transform.GetSiblingIndex()));

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, 18f);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            color.a = alpha;
            image.color = color;
            return image;
        }

        private Sprite FindRoundedSprite()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                var image = button.GetComponent<Image>();
                if (image != null && image.sprite != null) return image.sprite;
            }
            return null;
        }

        private void AnimateGlows()
        {
            float time = Time.unscaledTime;

            if (_orangeGlow != null)
            {
                var rt = _orangeGlow.rectTransform;
                rt.anchoredPosition = new Vector2(Mathf.Sin(time * 0.42f) * 24f, Mathf.Cos(time * 0.31f) * 18f);
                float s = 1f + Mathf.Sin(time * 0.75f) * 0.035f;
                rt.localScale = new Vector3(s, s, 1f);
            }

            if (_blueGlow != null)
            {
                var rt = _blueGlow.rectTransform;
                rt.anchoredPosition = new Vector2(Mathf.Cos(time * 0.36f) * 28f, Mathf.Sin(time * 0.27f) * 22f);
                float s = 1f + Mathf.Cos(time * 0.68f) * 0.04f;
                rt.localScale = new Vector3(s, s, 1f);
            }
        }

        private IEnumerator CelebrateSummary()
        {
            ClearParticles();

            Sprite sprite = FindRoundedSprite();
            RectTransform canvasRect = transform as RectTransform;
            float width = canvasRect != null ? canvasRect.rect.width : Screen.width;
            float height = canvasRect != null ? canvasRect.rect.height : Screen.height;

            const int count = 24;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("VictoryParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                go.transform.SetAsLastSibling();

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(Random.Range(8f, 18f), Random.Range(16f, 34f));
                rt.anchoredPosition = new Vector2(Random.Range(-width * 0.42f, width * 0.42f), height * 0.42f + Random.Range(0f, 90f));
                rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                image.raycastTarget = false;
                image.color = i % 3 == 0 ? UiTheme.Accent : (i % 3 == 1 ? UiTheme.AccentLime : UiTheme.Info);

                _particles.Add(go);
                StartCoroutine(FallParticle(go, Random.Range(0.85f, 1.45f), Random.Range(-90f, 90f)));
            }

            yield return new WaitForSecondsRealtime(1.7f);
            ClearParticles();
        }

        private IEnumerator FallParticle(GameObject particle, float duration, float drift)
        {
            if (particle == null) yield break;

            var rt = particle.GetComponent<RectTransform>();
            var image = particle.GetComponent<Image>();
            Vector2 start = rt.anchoredPosition;
            Vector2 end = start + new Vector2(drift, -Mathf.Max(Screen.height * 0.55f, 550f));
            float rotationStart = rt.localEulerAngles.z;
            float rotationEnd = rotationStart + Random.Range(180f, 520f);
            float elapsed = 0f;

            while (elapsed < duration && particle != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t;
                rt.anchoredPosition = Vector2.Lerp(start, end, eased);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(rotationStart, rotationEnd, t));

                if (image != null && t > 0.65f)
                {
                    var c = image.color;
                    c.a = Mathf.Lerp(1f, 0f, (t - 0.65f) / 0.35f);
                    image.color = c;
                }
                yield return null;
            }
        }

        private void ClearParticles()
        {
            foreach (var particle in _particles)
            {
                if (particle != null) Destroy(particle);
            }
            _particles.Clear();
        }

        private void OnDestroy()
        {
            ClearParticles();
        }
    }
}
