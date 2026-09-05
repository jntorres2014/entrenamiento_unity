using System.Collections.Generic;
using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Arte procedural liviano para acercar la UI al prototipo Deportivo Pro
    /// sin depender de paquetes ni sprites externos.
    /// </summary>
    public static class ProVisualAssets
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static Texture2D HeroAthlete => Get("hero", BuildHero);
        public static Texture2D Reaction => Get("reaction", () => BuildTarget(new Color32(0x57, 0xF2, 0x63, 0xFF)));
        public static Texture2D AllSame => Get("allsame", BuildAllSame);
        public static Texture2D Colors => Get("colors", BuildColors);
        public static Texture2D Decision => Get("decision", BuildDecision);
        public static Texture2D Finta => Get("finta", BuildFinta);
        public static Texture2D Football => Get("football", BuildFootball);

        private static Texture2D Get(string key, System.Func<Texture2D> factory)
        {
            if (Cache.TryGetValue(key, out var texture) && texture != null) return texture;
            texture = factory();
            Cache[key] = texture;
            return texture;
        }

        private static Texture2D NewTexture(int size, string name)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var clear = new Color32[size * size];
            texture.SetPixels32(clear);
            return texture;
        }

        private static Texture2D BuildHero()
        {
            const int s = 512;
            var t = NewTexture(s, "ProHeroAthlete");
            var p = t.GetPixels32();

            // Fondo de humo / luz verde a la derecha.
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - 350f) / 260f;
                    float dy = (y - 255f) / 300f;
                    float glow = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    if (glow <= 0f) continue;
                    int i = y * s + x;
                    p[i] = new Color(0.09f, 0.30f, 0.10f, glow * 0.55f);
                }
            }

            // Resplandor del atleta.
            DrawRunner(p, s, new Color32(0x42, 0xFF, 0x42, 0x70), 34);
            DrawRunner(p, s, new Color32(0x06, 0x0A, 0x09, 0xFF), 22);
            DrawDisc(p, s, 351, 91, 34, new Color32(0x08, 0x0C, 0x0A, 0xFF));
            DrawDisc(p, s, 352, 92, 39, new Color32(0x4A, 0xFF, 0x46, 0x42), true);

            // Pelota y brillo inferior.
            DrawDisc(p, s, 350, 416, 48, new Color32(0x08, 0x0D, 0x0A, 0xFF));
            DrawRing(p, s, 350, 416, 48, 5, new Color32(0x61, 0xEF, 0x58, 0xE0));
            DrawLine(p, s, 318, 416, 382, 416, 4, new Color32(0x61, 0xEF, 0x58, 0xB0));
            DrawLine(p, s, 350, 382, 350, 450, 4, new Color32(0x61, 0xEF, 0x58, 0xA0));

            t.SetPixels32(p);
            t.Apply(false, false);
            return t;
        }

        private static void DrawRunner(Color32[] p, int s, Color32 c, int width)
        {
            DrawLine(p, s, 338, 128, 284, 226, width, c);
            DrawLine(p, s, 310, 167, 210, 204, Mathf.Max(8, width - 6), c);
            DrawLine(p, s, 306, 165, 432, 203, Mathf.Max(8, width - 6), c);
            DrawLine(p, s, 284, 222, 205, 352, width, c);
            DrawLine(p, s, 286, 226, 364, 346, width, c);
            DrawLine(p, s, 204, 351, 145, 399, Mathf.Max(8, width - 8), c);
            DrawLine(p, s, 363, 346, 420, 373, Mathf.Max(8, width - 8), c);
        }

        private static Texture2D BuildTarget(Color32 c)
        {
            const int s = 128;
            var t = NewTexture(s, "ProIconTarget");
            var p = t.GetPixels32();
            DrawRing(p, s, 64, 64, 42, 8, c);
            DrawRing(p, s, 64, 64, 25, 8, c);
            DrawDisc(p, s, 64, 64, 7, c);
            t.SetPixels32(p); t.Apply(false, false); return t;
        }

        private static Texture2D BuildAllSame()
        {
            const int s = 128; var t = NewTexture(s, "ProIconAllSame"); var p = t.GetPixels32();
            var c = new Color32(0x4C, 0x8D, 0xFF, 0xFF);
            DrawRing(p, s, 64, 37, 20, 6, c);
            DrawRing(p, s, 40, 75, 20, 6, c);
            DrawRing(p, s, 88, 75, 20, 6, c);
            DrawLine(p, s, 52, 53, 42, 60, 5, c); DrawLine(p, s, 76, 53, 86, 60, 5, c);
            t.SetPixels32(p); t.Apply(false, false); return t;
        }

        private static Texture2D BuildColors()
        {
            const int s = 128; var t = NewTexture(s, "ProIconColors"); var p = t.GetPixels32();
            DrawDisc(p, s, 32, 64, 19, new Color32(0xFF, 0x55, 0x55, 0xFF));
            DrawDisc(p, s, 64, 64, 19, new Color32(0xFF, 0xC9, 0x3F, 0xFF));
            DrawDisc(p, s, 96, 64, 19, new Color32(0xB9, 0x67, 0xFF, 0xFF));
            t.SetPixels32(p); t.Apply(false, false); return t;
        }

        private static Texture2D BuildDecision()
        {
            const int s = 128; var t = NewTexture(s, "ProIconDecision"); var p = t.GetPixels32();
            var c = new Color32(0xF3, 0xD3, 0x44, 0xFF);
            DrawArrow(p, s, 64, 96, 64, 24, c);
            DrawArrow(p, s, 64, 82, 25, 47, c);
            DrawArrow(p, s, 64, 82, 103, 47, c);
            t.SetPixels32(p); t.Apply(false, false); return t;
        }

        private static Texture2D BuildFinta()
        {
            const int s = 128; var t = NewTexture(s, "ProIconFinta"); var p = t.GetPixels32();
            var c = new Color32(0xB9, 0x67, 0xFF, 0xFF);
            DrawRing(p, s, 49, 55, 24, 7, c); DrawRing(p, s, 79, 55, 24, 7, c);
            DrawRing(p, s, 49, 78, 20, 7, c); DrawRing(p, s, 79, 78, 20, 7, c);
            DrawLine(p, s, 64, 34, 64, 99, 6, c);
            DrawLine(p, s, 35, 52, 25, 43, 5, c); DrawLine(p, s, 93, 52, 103, 43, 5, c);
            t.SetPixels32(p); t.Apply(false, false); return t;
        }

        private static Texture2D BuildFootball()
        {
            const int s = 128; var t = NewTexture(s, "ProIconFootball"); var p = t.GetPixels32();
            var c = new Color32(0x38, 0xD6, 0xA1, 0xFF);
            DrawRing(p, s, 64, 64, 45, 7, c); DrawDisc(p, s, 64, 64, 11, c);
            for (int i = 0; i < 5; i++)
            {
                float a = Mathf.Deg2Rad * (i * 72f - 90f);
                int x = Mathf.RoundToInt(64 + Mathf.Cos(a) * 33);
                int y = Mathf.RoundToInt(64 + Mathf.Sin(a) * 33);
                DrawLine(p, s, 64, 64, x, y, 5, c);
                DrawDisc(p, s, x, y, 7, c);
            }
            t.SetPixels32(p); t.Apply(false, false); return t;
        }

        private static void DrawArrow(Color32[] p, int s, int x0, int y0, int x1, int y1, Color32 c)
        {
            DrawLine(p, s, x0, y0, x1, y1, 7, c);
            Vector2 d = new Vector2(x1 - x0, y1 - y0).normalized;
            Vector2 n = new Vector2(-d.y, d.x);
            Vector2 tip = new Vector2(x1, y1);
            Vector2 basePoint = tip - d * 18f;
            DrawLine(p, s, x1, y1, Mathf.RoundToInt(basePoint.x + n.x * 10f), Mathf.RoundToInt(basePoint.y + n.y * 10f), 6, c);
            DrawLine(p, s, x1, y1, Mathf.RoundToInt(basePoint.x - n.x * 10f), Mathf.RoundToInt(basePoint.y - n.y * 10f), 6, c);
        }

        private static void DrawLine(Color32[] p, int s, int x0, int y0, int x1, int y1, int width, Color32 c)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps <= 0) { DrawDisc(p, s, x0, y0, width / 2, c); return; }
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                DrawDisc(p, s, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), Mathf.Max(1, width / 2), c);
            }
        }

        private static void DrawRing(Color32[] p, int s, int cx, int cy, int radius, int thickness, Color32 c)
        {
            int outer2 = radius * radius;
            int inner = Mathf.Max(0, radius - thickness); int inner2 = inner * inner;
            for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= s || y < 0 || y >= s) continue;
                int dx = x - cx, dy = y - cy, d2 = dx * dx + dy * dy;
                if (d2 <= outer2 && d2 >= inner2) Blend(p, y * s + x, c);
            }
        }

        private static void DrawDisc(Color32[] p, int s, int cx, int cy, int radius, Color32 c, bool ringOnly = false)
        {
            if (ringOnly) { DrawRing(p, s, cx, cy, radius, Mathf.Max(2, radius / 5), c); return; }
            int r2 = radius * radius;
            for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= s || y < 0 || y >= s) continue;
                int dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= r2) Blend(p, y * s + x, c);
            }
        }

        private static void Blend(Color32[] p, int index, Color32 src)
        {
            float a = src.a / 255f;
            if (a >= 0.999f) { p[index] = src; return; }
            Color32 dst = p[index];
            p[index] = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(src.r * a + dst.r * (1f - a)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(src.g * a + dst.g * (1f - a)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(src.b * a + dst.b * (1f - a)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt((a + dst.a / 255f * (1f - a)) * 255f), 0, 255));
        }
    }
}
