using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Coordina la zona inferior de Home C independientemente del orden en que
    /// se creen Camera Training y Solo Training, y aclara los caminos SOLO/PODS.
    /// </summary>
    public sealed class SoloHomeLayoutFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<SoloHomeLayoutFix>() == null)
                {
                    canvas.gameObject.AddComponent<SoloHomeLayoutFix>();
                    break;
                }
            }
        }

        private void Awake()
        {
            StartCoroutine(ApplyWhenReady());
        }

        private IEnumerator ApplyWhenReady()
        {
            GameObject solo = null;
            GameObject camera = null;

            for (int i = 0; i < 120; i++)
            {
                solo = FindDeep("SoloTrainingButton");
                camera = FindDeep("CameraTrainingButton");
                if (solo != null && camera != null) break;
                yield return null;
            }

            if (solo == null) yield break;

            var statusCard = FindDeep("ARStatusCard");
            if (statusCard != null) statusCard.SetActive(false);

            var soloRect = solo.GetComponent<RectTransform>();
            if (soloRect != null) SetRect(soloRect, 0.072f, 0.154f, 0.928f, 0.245f);
            var soloImage = solo.GetComponent<Image>();
            if (soloImage != null) soloImage.color = UiTheme.Accent;
            var soloText = solo.GetComponentInChildren<TMP_Text>(true);
            if (soloText != null)
            {
                soloText.text = "SOLO  ·  1 TELÉFONO   →\n<size=63%><color=#A8B2C1>6 EJERCICIOS  •  CÁMARA  •  SIN ARCORE</color></size>";
                soloText.alignment = TextAlignmentOptions.MidlineLeft;
                soloText.fontSizeMax = 23f;
                soloText.fontSizeMin = 13f;
            }
            solo.transform.SetAsLastSibling();

            if (camera != null)
            {
                var cameraRect = camera.GetComponent<RectTransform>();
                if (cameraRect != null) SetRect(cameraRect, 0.072f, 0.070f, 0.928f, 0.142f);
                var cameraImage = camera.GetComponent<Image>();
                if (cameraImage != null) cameraImage.color = UiTheme.Surface;
                var cameraText = camera.GetComponentInChildren<TMP_Text>(true);
                if (cameraText != null)
                {
                    cameraText.text = "CAMERA TRAINING  ·  CALIBRACIÓN LIBRE";
                    cameraText.alignment = TextAlignmentOptions.Center;
                    cameraText.fontSizeMax = 18f;
                    cameraText.fontSizeMin = 12f;
                }
                camera.transform.SetAsLastSibling();
                solo.transform.SetAsLastSibling();
            }

            RelabelRoleButton("HostRoleButton",
                "CON PODS\n<size=67%><color=#A8B2C1>Crear y dirigir una sesión</color></size>");
            RelabelRoleButton("StationRoleButton",
                "ESTACIÓN\n<size=67%><color=#A8B2C1>Usar este teléfono como pod</color></size>");
        }

        private void RelabelRoleButton(string objectName, string value)
        {
            var go = FindDeep(objectName);
            if (go == null) return;
            var label = go.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            label.text = value;
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.fontSizeMax = 24f;
            label.fontSizeMin = 14f;
        }

        private GameObject FindDeep(string objectName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
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
            rect.localScale = Vector3.one;
        }
    }
}
