using System.Collections.Generic;
using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>
    /// HUD: красная полоска здоровья из ячеек (слева сверху), под ней синяя полоска перезарядки устройства,
    /// аптечки, лом, подсказки и сообщения.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        static Hud inst;

        class Toast { public string text; public float t, dur; }

        readonly List<Toast> toasts = new List<Toast>();
        string banner;
        float bannerT, bannerDur;
        float controlsT = 45f;

        void Awake() { inst = this; }

        public static void ShowBanner(string text, float dur)
        {
            if (inst == null) return;
            inst.banner = text;
            inst.bannerT = inst.bannerDur = dur;
        }

        public static void ShowToast(string text, float dur = 3f)
        {
            if (inst == null) return;
            inst.toasts.Add(new Toast { text = text, t = dur, dur = dur });
            if (inst.toasts.Count > 4) inst.toasts.RemoveAt(0);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bannerT -= dt;
            if (!GameState.Intro) controlsT -= dt;
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                toasts[i].t -= dt;
                if (toasts[i].t <= 0) toasts.RemoveAt(i);
            }
        }

        void OnGUI()
        {
            GUI.depth = 10;
            float s = UIDraw.Scale;

            if (Storm.Flash > 0) UIDraw.Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.9f, 0.95f, 0.85f, Storm.Flash * 0.45f));
            if (GameState.Intro) return;

            var p = PlayerController.Instance;
            if (p != null)
            {
                DrawHealth(p.Stats, s);
                if (p.Device.unlocked) DrawEnergy(p, s);
                DrawMedkits(p.Stats, s);
                UIDraw.Text(new Rect(Screen.width - 130 * s, 8 * s, 122 * s, 14 * s), "Лом: " + p.Stats.scrap, 9 * s,
                    new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperRight, false);

                if (p.Focused != null && !GameState.UIOpen && Camera.main != null)
                {
                    var sp = Camera.main.WorldToScreenPoint(p.Focused.PromptPos);
                    var r = new Rect(sp.x - 50 * s, Screen.height - sp.y - 7 * s, 100 * s, 12 * s);
                    UIDraw.Text(r, "[E] " + p.Focused.prompt, 8 * s, Color.white, TextAnchor.MiddleCenter, false);
                }
            }

            // сообщения
            float y = Screen.height - 46 * s;
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var t = toasts[i];
                float a = Mathf.Clamp01(t.t / 0.4f) * Mathf.Clamp01((t.dur - t.t) / 0.15f);
                UIDraw.Text(new Rect(0, y, Screen.width, 14 * s), t.text, 9 * s, new Color(1, 1, 1, a), TextAnchor.MiddleCenter, false);
                y -= 13 * s;
            }

            if (bannerT > 0 && !string.IsNullOrEmpty(banner))
            {
                float a = Mathf.Clamp01(bannerT / 0.6f) * Mathf.Clamp01((bannerDur - bannerT) / 0.4f);
                UIDraw.Rect(new Rect(0, Screen.height * 0.4f, Screen.width, 34 * s), new Color(0, 0, 0, 0.6f * a));
                UIDraw.Text(new Rect(0, Screen.height * 0.4f, Screen.width, 34 * s), banner, 18 * s,
                    new Color(0.85f, 0.15f, 0.12f, a), TextAnchor.MiddleCenter, false);
            }

            if (controlsT > 0)
            {
                float a = Mathf.Clamp01(controlsT / 3f) * 0.75f;
                UIDraw.Text(new Rect(8 * s, Screen.height - 16 * s, Screen.width - 16 * s, 12 * s),
                    "A/D — ходьба   Пробел — прыжок   ЛКМ — удар   ПКМ — устройство   Q — режим   H — аптечка   E — действие",
                    7 * s, new Color(0.8f, 0.8f, 0.75f, a), TextAnchor.MiddleLeft, false);
            }
        }

        static void DrawHealth(PlayerStats st, float s)
        {
            float x0 = 8 * s, y0 = 8 * s, cw = 12 * s, ch = 9 * s, gap = 2 * s;
            UIDraw.Rect(new Rect(x0 - 2 * s, y0 - 2 * s, st.maxHp * (cw + gap) + 2 * s, ch + 4 * s), new Color(0, 0, 0, 0.55f));
            for (int i = 0; i < st.maxHp; i++)
            {
                var r = new Rect(x0 + i * (cw + gap), y0, cw, ch);
                UIDraw.Rect(r, new Color(0.22f, 0.04f, 0.04f));
                float fill = Mathf.Clamp01(st.displayHp - i);
                if (fill > 0)
                {
                    UIDraw.Rect(new Rect(r.x, r.y, r.width * fill, r.height), new Color(0.78f, 0.1f, 0.1f));
                    UIDraw.Rect(new Rect(r.x, r.y, r.width * fill, 2 * s), new Color(0.95f, 0.35f, 0.3f));
                }
            }
        }

        static void DrawEnergy(PlayerController p, float s)
        {
            var d = p.Device;
            float x0 = 8 * s, y0 = 21 * s, h = 4 * s;
            float w = 70 * s * (p.Stats.deviceCapacity / 100f);
            UIDraw.Rect(new Rect(x0 - 2 * s, y0 - 1 * s, w + 4 * s, h + 2 * s), new Color(0, 0, 0, 0.55f));
            UIDraw.Rect(new Rect(x0, y0, w, h), new Color(0.05f, 0.1f, 0.22f));
            Color fill = d.overheated ? new Color(0.25f, 0.3f, 0.5f) : new Color(0.25f, 0.55f, 1f);
            if (d.overheated && Mathf.Repeat(Time.time * 4f, 1f) > 0.5f) fill = new Color(0.4f, 0.45f, 0.7f);
            UIDraw.Rect(new Rect(x0, y0, w * d.Energy01, h), fill);
            string mode = d.mode == DeviceMode.Clean ? "ОЧИСТКА" : "ЗАГРЯЗНЕНИЕ";
            Color mc = d.mode == DeviceMode.Clean ? new Color(0.6f, 0.85f, 1f) : new Color(0.55f, 0.95f, 0.25f);
            UIDraw.Text(new Rect(x0, y0 + h + 2 * s, 120 * s, 10 * s), mode, 7 * s, mc, TextAnchor.UpperLeft, false);
        }

        static void DrawMedkits(PlayerStats st, float s)
        {
            float x0 = 8 * s, y0 = 38 * s;
            for (int i = 0; i < PlayerStats.MaxMedkits; i++)
            {
                var r = new Rect(x0 + i * 13 * s, y0, 11 * s, 8 * s);
                bool has = i < st.medkits;
                UIDraw.Rect(r, has ? new Color(0.87f, 0.8f, 0.64f) : new Color(0.2f, 0.2f, 0.2f, 0.6f));
                if (has)
                {
                    var red = new Color(0.77f, 0.12f, 0.12f);
                    UIDraw.Rect(new Rect(r.x + 4.5f * s, r.y + 1.5f * s, 2 * s, 5 * s), red);
                    UIDraw.Rect(new Rect(r.x + 3 * s, r.y + 3 * s, 5 * s, 2 * s), red);
                }
            }
        }
    }
}
