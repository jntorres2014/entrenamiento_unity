using UnityEngine;
using UnityEngine.SceneManagement;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Integra las barras del sistema Android con el fondo Deportivo Pro.
    /// </summary>
    public sealed class AndroidSystemUiPro : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<AndroidSystemUiPro>() == null)
                {
                    canvas.gameObject.AddComponent<AndroidSystemUiPro>();
                    break;
                }
            }
        }

        private void Start() => Apply();
        private void OnApplicationFocus(bool focus) { if (focus) Apply(); }

        private static void Apply()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                using (var decor = window.Call<AndroidJavaObject>("getDecorView"))
                {
                    int dark = unchecked((int)0xFF070D0B);
                    window.Call("setStatusBarColor", dark);
                    window.Call("setNavigationBarColor", dark);

                    const int LightStatusBar = 0x00002000;
                    const int LightNavigationBar = 0x00000010;
                    int flags = decor.Call<int>("getSystemUiVisibility");
                    flags &= ~LightStatusBar;
                    flags &= ~LightNavigationBar;
                    decor.Call("setSystemUiVisibility", flags);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[UI] No se pudieron aplicar barras Android: " + e.Message);
            }
#endif
        }
    }
}
