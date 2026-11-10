using System.Collections.Generic;
using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>
    /// Генерация пиксельных спрайтов прямо из кода (временный арт).
    /// Когда появятся настоящие спрайты, их можно будет подставить в Art.cs.
    /// </summary>
    public static class PixelArt
    {
        public const int PPU = 12; // 12 пикселей = 1 метр

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<Color, Texture2D> guiTex = new Dictionary<Color, Texture2D>();

        public static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public static Color32 C(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);

        static Texture2D NewTex(int w, int h, FilterMode filter = FilterMode.Point)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = filter;
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        public static Sprite FromAscii(string key, string[] rows, Dictionary<char, Color32> pal, Vector2 pivot)
        {
            if (cache.TryGetValue(key, out var s)) return s;
            int h = rows.Length, w = 0;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                string row = rows[h - 1 - y];
                for (int x = 0; x < w; x++)
                {
                    char c = x < row.Length ? row[x] : '.';
                    px[y * w + x] = pal.TryGetValue(c, out var col) ? col : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, PPU, 0, SpriteMeshType.FullRect);
            s.name = key;
            cache[key] = s;
            return s;
        }

        public static Sprite FromPixels(string key, int w, int h, System.Func<int, int, Color32> f, Vector2 pivot,
            float ppu = PPU, FilterMode filter = FilterMode.Point)
        {
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = NewTex(w, h, filter);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = f(x, y);
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
            s.name = key;
            cache[key] = s;
            return s;
        }

        /// <summary>Белый пиксель (1/12 метра), для частиц.</summary>
        public static Sprite Pixel => FromPixels("pixel", 1, 1, (x, y) => C(255, 255, 255), Center);

        /// <summary>Белый квадрат 1×1 метр с опорой слева по центру (для лучей).</summary>
        public static Sprite BeamUnit => FromPixels("beam_unit", 1, 1, (x, y) => C(255, 255, 255), new Vector2(0f, 0.5f), 1f);

        /// <summary>Белый квадрат 1×1 метр с опорой в центре.</summary>
        public static Sprite Unit => FromPixels("unit", 1, 1, (x, y) => C(255, 255, 255), Center, 1f);

        public static Sprite Circle(int d)
        {
            float r = d / 2f;
            return FromPixels("circle_" + d, d, d, (x, y) =>
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                return dx * dx + dy * dy <= r * r ? C(255, 255, 255) : C(0, 0, 0, 0);
            }, Center);
        }

        public static Sprite Ring(int d)
        {
            float r = d / 2f;
            return FromPixels("ring_" + d, d, d, (x, y) =>
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                return dist <= r && dist >= r - 1.2f ? C(255, 255, 255) : C(0, 0, 0, 0);
            }, Center);
        }

        /// <summary>Мягкое пятно (свечение), сглаженное.</summary>
        public static Sprite SoftCircle(int d)
        {
            float r = d / 2f;
            return FromPixels("soft_" + d, d, d, (x, y) =>
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float t = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / r);
                return C(255, 255, 255, (int)(t * t * 255));
            }, Center, PPU, FilterMode.Bilinear);
        }

        /// <summary>Однопиксельная текстура цвета для OnGUI.</summary>
        public static Texture2D Tex(Color c)
        {
            if (guiTex.TryGetValue(c, out var t) && t != null) return t;
            t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, c);
            t.Apply();
            guiTex[c] = t;
            return t;
        }

        public static Color32 Shade(Color32 c, float k) =>
            new Color32((byte)Mathf.Clamp(c.r * k, 0, 255), (byte)Mathf.Clamp(c.g * k, 0, 255), (byte)Mathf.Clamp(c.b * k, 0, 255), c.a);

        /// <summary>Детерминированный шум 0..1 для генерации текстур.</summary>
        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }
    }
}
