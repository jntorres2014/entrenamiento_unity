using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Integra visualmente las barras del sistema Android con la UI oscura.
    /// Screen.safeArea sigue siendo quien protege los controles interactivos.
    /// </summary>
    public static class AndroidSystemBarStyler
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                {
                    int dark = unchecked((int)0xFF0B0F14);
                    window.Call("setStatusBarColor", dark);
                    window.Call("setNavigationBarColor", dark);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[UI] No se pudieron estilizar las barras de Android: " + ex.Message);
            }
#endif
        }
    }
}
