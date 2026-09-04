using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Tracking de jugador con cámara fija, sin ARCore. Aprende el fondo vacío y
    /// estima la posición de los pies a partir de diferencia de imagen.
    /// Devuelve coordenadas normalizadas de pantalla (0..1).
    /// </summary>
    public sealed class CameraPlayerTracker : MonoBehaviour
    {
        private const int MotionThreshold = 34;
        private const int MinimumMotionSamples = 55;

        private WebCamTexture _webCam;
        private RawImage _preview;
        private RectTransform _previewRect;
        private Color32[] _pixels;
        private byte[] _backgroundGray;
        private int _cameraWidth;
        private int _cameraHeight;
        private int _sampleStep = 5;
        private int _lastVideoAngle = -1;
        private bool _lastVideoMirror;

        public bool IsReady => _webCam != null && _webCam.isPlaying && _webCam.width > 32;
        public bool HasBackground => _backgroundGray != null && _backgroundGray.Length > 0;

        public IEnumerator StartCamera(RawImage preview, RectTransform previewRect, Action<bool, string> completed)
        {
            _preview = preview;
            _previewRect = previewRect;

            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            }

            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                completed?.Invoke(false, "Necesito permiso para usar la cámara. Habilitalo en Ajustes del teléfono.");
                yield break;
            }

            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                completed?.Invoke(false, "No encontré una cámara disponible en este dispositivo.");
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

            StopCamera();
            _webCam = new WebCamTexture(deviceName, 640, 480, 30);
            if (_preview != null) _preview.texture = _webCam;
            _webCam.Play();

            float timeoutAt = Time.realtimeSinceStartup + 8f;
            while ((_webCam.width <= 32 || !_webCam.didUpdateThisFrame) && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            if (_webCam.width <= 32 || _webCam.height <= 32)
            {
                StopCamera();
                completed?.Invoke(false, "La cámara no llegó a iniciar. Cerrá otras apps que puedan estar usándola.");
                yield break;
            }

            _cameraWidth = _webCam.width;
            _cameraHeight = _webCam.height;
            _pixels = new Color32[_cameraWidth * _cameraHeight];
            _sampleStep = Mathf.Clamp(_cameraWidth / 110, 4, 10);
            UpdatePreviewGeometry(true);
            completed?.Invoke(true, null);
        }

        public void UpdatePreviewGeometry(bool force = false)
        {
            if (_webCam == null || _previewRect == null || !_webCam.isPlaying) return;

            int angle = NormalizeAngle(_webCam.videoRotationAngle);
            bool mirror = _webCam.videoVerticallyMirrored;
            if (!force && angle == _lastVideoAngle && mirror == _lastVideoMirror) return;

            _lastVideoAngle = angle;
            _lastVideoMirror = mirror;
            _previewRect.localEulerAngles = new Vector3(0f, 0f, -angle);
            float scale = GetCameraCoverScale(angle);
            _previewRect.localScale = new Vector3(scale, scale, 1f);

            if (_preview != null)
            {
                _preview.uvRect = mirror
                    ? new Rect(0f, 1f, 1f, -1f)
                    : new Rect(0f, 0f, 1f, 1f);
            }
        }

        public bool CaptureBackground()
        {
            if (!IsReady) return false;
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

        public bool TryDetectFeet(out Vector2 feetScreenNormalized, out bool tooClose)
        {
            feetScreenNormalized = default;
            tooClose = false;

            if (!HasBackground || !IsReady || !_webCam.didUpdateThisFrame) return false;
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

        public void StopCamera()
        {
            if (_webCam != null)
            {
                if (_webCam.isPlaying) _webCam.Stop();
                Destroy(_webCam);
                _webCam = null;
            }

            if (_preview != null) _preview.texture = null;
            _backgroundGray = null;
            _pixels = null;
        }

        private static byte Gray(Color32 c)
        {
            return (byte)((c.r * 77 + c.g * 150 + c.b * 29) >> 8);
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

        private static float GetCameraCoverScale(int angle)
        {
            if (angle != 90 && angle != 270) return 1f;
            float ratio = Screen.width > 0 && Screen.height > 0
                ? (float)Screen.width / Screen.height
                : 1f;
            if (ratio <= 0f) return 1f;
            return Mathf.Max(ratio, 1f / ratio);
        }

        private void OnDestroy()
        {
            StopCamera();
        }
    }
}
