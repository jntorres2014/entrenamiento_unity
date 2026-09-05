using System.Collections.Generic;
using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Convierte el JPG histórico del logo en una textura RGBA transparente en
    /// runtime. Solo elimina el fondo claro conectado a los bordes, por lo que
    /// conserva blancos internos que formen parte del dibujo.
    /// </summary>
    public static class TransparentBrandLogo
    {
        private static Texture2D _texture;

        public static Texture2D Texture
        {
            get
            {
                if (_texture == null) Build();
                return _texture;
            }
        }

        private static void Build()
        {
            Texture2D source = BrandLogo.Texture;
            if (source == null)
            {
                _texture = Texture2D.whiteTexture;
                return;
            }

            int width = source.width;
            int height = source.height;
            Color32[] pixels = source.GetPixels32();
            bool[] visited = new bool[pixels.Length];
            var queue = new Queue<int>();

            for (int x = 0; x < width; x++)
            {
                TrySeed(x, 0, width, height, pixels, visited, queue);
                TrySeed(x, height - 1, width, height, pixels, visited, queue);
            }
            for (int y = 0; y < height; y++)
            {
                TrySeed(0, y, width, height, pixels, visited, queue);
                TrySeed(width - 1, y, width, height, pixels, visited, queue);
            }

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width;
                int y = index / width;
                pixels[index].a = 0;

                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    int ni = ny * width + nx;
                    if (visited[ni] || !IsBackgroundLike(pixels[ni])) continue;
                    visited[ni] = true;
                    queue.Enqueue(ni);
                }
            }

            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "TrainingBrandLogoTransparent",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _texture.SetPixels32(pixels);
            _texture.Apply(false, false);
        }

        private static void TrySeed(int x, int y, int width, int height, Color32[] pixels, bool[] visited, Queue<int> queue)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            int index = y * width + x;
            if (visited[index] || !IsBackgroundLike(pixels[index])) return;
            visited[index] = true;
            queue.Enqueue(index);
        }

        private static bool IsBackgroundLike(Color32 c)
        {
            int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            int min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return min >= 190 && max - min <= 26;
        }
    }
}
