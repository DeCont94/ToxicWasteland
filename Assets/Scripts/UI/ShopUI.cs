using System.Collections.Generic;
using UnityEngine;

namespace ToxicWasteland
{
    public class ShopItem
    {
        public string name, desc;
        public System.Func<int> price;
        public System.Func<bool> available;
        public System.Action buy;
    }

    /// <summary>Окно торговца улучшениями. W/S — выбор, Enter/E — купить, Esc — уйти.</summary>
    public class ShopUI : MonoBehaviour
    {
        static ShopUI inst;
        List<ShopItem> items;
        string title, greeting;
        int selected, openFrame;
        float slide;

        public static bool IsOpen => inst != null && inst.items != null;

        void Awake() { inst = this; }

        public static void Open(string title, string greeting, List<ShopItem> items)
        {
            if (inst == null) return;
            inst.title = title;
            inst.greeting = greeting;
            inst.items = items;
            inst.selected = 0;
            inst.slide = 0;
            inst.openFrame = Time.frameCount;
            GameState.UIOpen = true;
            Sfx.Play(Sfx.PageFlip, 0.7f);
        }

        void Close()
        {
            items = null;
            GameState.UIOpen = false;
            GameState.LastUIFrame = Time.frameCount;
            Sfx.Play(Sfx.PageFlip, 0.5f);
        }

        void Update()
        {
            if (items == null) return;
            slide = Mathf.MoveTowards(slide, 1f, Time.deltaTime / 0.25f);
            if (Time.frameCount == openFrame) return;
            int n = items.Count + 1; // + «Уйти»
            if (GameInput.UpPressed) { selected = (selected + n - 1) % n; Sfx.Play(Sfx.Click, 0.25f); }
            if (GameInput.DownPressed) { selected = (selected + 1) % n; Sfx.Play(Sfx.Click, 0.25f); }
            if (GameInput.CancelPressed) { Close(); return; }
            if (GameInput.ConfirmPressed)
            {
                if (selected == items.Count) { Close(); return; }
                var it = items[selected];
                var p = PlayerController.Instance;
                int price = it.price();
                if (p != null && it.available() && p.Stats.scrap >= price)
                {
                    p.Stats.scrap -= price;
                    it.buy();
                    Sfx.Play(Sfx.Buy, 0.6f);
                    Hud.ShowToast("Куплено: " + it.name, 2f);
                }
                else
                {
                    Sfx.Play(Sfx.Deny, 0.5f);
                    Hud.ShowToast(!it.available() ? "Больше нельзя" : "Не хватает лома", 1.5f);
                }
            }
        }

        void OnGUI()
        {
            if (items == null) return;
            GUI.depth = 0;
            float s = UIDraw.Scale;
            var p = PlayerController.Instance;
            float w = Mathf.Min(Screen.width - 20 * s, 320 * s);
            float h = 60 * s + (items.Count + 1) * 14 * s + 30 * s;
            float x = (Screen.width - w) / 2f;
            float targetY = 30 * s;
            float e = 1f - Mathf.Pow(1f - slide, 3f);
            float y = Mathf.Lerp(Screen.height + 4 * s, targetY, e);

            UIDraw.Panel(new Rect(x, y, w, h));
            UIDraw.Text(new Rect(x + 8 * s, y + 5 * s, w - 16 * s, 12 * s), title, 9 * s, UIDraw.Accent, TextAnchor.UpperLeft, false);
            UIDraw.Text(new Rect(x + 8 * s, y + 5 * s, w - 16 * s, 12 * s), "Лом: " + (p != null ? p.Stats.scrap : 0), 8 * s,
                new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperRight, false);
            UIDraw.Text(new Rect(x + 8 * s, y + 19 * s, w - 16 * s, 24 * s), greeting, 8 * s, Color.white);

            float ly = y + 46 * s;
            for (int i = 0; i <= items.Count; i++)
            {
                bool sel = i == selected;
                string label;
                bool ok = true;
                if (i < items.Count)
                {
                    var it = items[i];
                    ok = it.available();
                    label = it.name + (ok ? "  —  " + it.price() + " лома" : "  (нет)");
                }
                else label = "Уйти";
                Color c = sel ? Color.white : (ok ? UIDraw.Dim : new Color(0.35f, 0.35f, 0.35f));
                UIDraw.Text(new Rect(x + 8 * s, ly, w - 16 * s, 12 * s), (sel ? "> " : "   ") + label, 8 * s, c, TextAnchor.UpperLeft, false);
                ly += 14 * s;
            }
            if (selected < items.Count)
                UIDraw.Text(new Rect(x + 8 * s, ly + 4 * s, w - 16 * s, 24 * s), items[selected].desc, 7 * s, new Color(0.75f, 0.75f, 0.7f));
        }
    }
}
