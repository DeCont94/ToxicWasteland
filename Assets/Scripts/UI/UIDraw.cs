using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>
    /// Вспомогательная отрисовка интерфейса. Шрифт единый для всей игры:
    /// положи пиксельный TTF с кириллицей в Assets/Resources/Fonts/PixelFont.ttf — он подхватится сам.
    /// </summary>
    public static class UIDraw
    {
        public static readonly Color PanelBg = new Color(0.05f, 0.05f, 0.06f, 0.93f);
        public static readonly Color PanelBorder = new Color(0.55f, 0.55f, 0.5f, 1f);
        public static readonly Color Accent = new Color(0.62f, 0.86f, 0.32f);
        public static readonly Color Dim = new Color(0.5f, 0.5f, 0.5f);

        static Font font;
        static bool fontLoaded;
        static GUIStyle style;

        public static float Scale => Screen.height / 360f;

        static Font PixelFont
        {
            get
            {
                if (!fontLoaded) { fontLoaded = true; font = Resources.Load<Font>("Fonts/PixelFont"); }
                return font;
            }
        }

        public static void Rect(Rect r, Color c) => GUI.DrawTexture(r, PixelArt.Tex(c));

        public static void Panel(Rect r)
        {
            float s = Mathf.Max(1f, Mathf.Round(Scale));
            Rect(new Rect(r.x - s, r.y - s, r.width + 2 * s, r.height + 2 * s), PanelBorder);
            Rect(r, PanelBg);
        }

        public static void Text(Rect r, string text, float size, Color color, TextAnchor anchor = TextAnchor.UpperLeft,
            bool wrap = true, bool shadow = true)
        {
            if (style == null) style = new GUIStyle();
            style.font = PixelFont;
            style.fontSize = Mathf.Max(8, Mathf.RoundToInt(size));
            style.wordWrap = wrap;
            style.alignment = anchor;
            style.richText = false;
            if (shadow)
            {
                style.normal.textColor = new Color(0, 0, 0, color.a * 0.8f);
                float o = Mathf.Max(1f, Scale * 0.6f);
                GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), text, style);
            }
            style.normal.textColor = color;
            GUI.Label(r, text, style);
        }
    }
}
