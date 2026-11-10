using System.Collections.Generic;
using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>Тон ответа героя: у него всегда 3 варианта.</summary>
    public enum Tone { Neutral, Positive, Negative }

    public class DialogueChoice
    {
        public string text;
        public Tone tone;
        public int next;              // индекс следующей реплики, -1 — конец
        public System.Action action;  // что произойдёт (например, выдать предмет)

        public DialogueChoice(Tone tone, string text, int next, System.Action action = null)
        {
            this.tone = tone; this.text = text; this.next = next; this.action = action;
        }
    }

    public class DialogueLine
    {
        public const int NextLine = -2;
        public string speaker;
        public string text;
        public DialogueChoice[] choices;
        public int next = NextLine;   // -2 — следующая по списку, -1 — конец

        public DialogueLine(string speaker, string text, int next = NextLine)
        {
            this.speaker = speaker; this.text = text; this.next = next;
        }
    }

    public class Dialogue
    {
        public readonly List<DialogueLine> lines = new List<DialogueLine>();
        public System.Action onEnd;

        public Dialogue Say(string speaker, string text, int next = DialogueLine.NextLine)
        {
            lines.Add(new DialogueLine(speaker, text, next));
            return this;
        }

        /// <summary>Реплика с тремя вариантами ответа героя: нейтральный, положительный, отрицательный.</summary>
        public Dialogue Ask(string speaker, string text, DialogueChoice neutral, DialogueChoice positive, DialogueChoice negative)
        {
            lines.Add(new DialogueLine(speaker, text) { choices = new[] { neutral, positive, negative } });
            return this;
        }
    }

    /// <summary>
    /// Окно диалога: выезжает снизу, стоит по центру между собеседниками и не закрывает их.
    /// Тёмный фон, белый текст, печать по букве, звук перелистывания страниц.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        static DialogueUI inst;
        const float CharsPerSecond = 32f;

        Dialogue dlg;
        int index, selected, openFrame;
        float chars, slide, tickT;
        Transform a, b;

        public static bool IsOpen => inst != null && inst.dlg != null;

        void Awake() { inst = this; }

        public static void Open(Dialogue d, Transform speakerA, Transform speakerB)
        {
            if (inst == null || d == null || d.lines.Count == 0) return;
            inst.dlg = d;
            inst.a = speakerA;
            inst.b = speakerB;
            inst.slide = 0;
            inst.openFrame = Time.frameCount;
            GameState.UIOpen = true;
            Sfx.Play(Sfx.PageFlip, 0.8f);
            inst.Goto(0);
        }

        void Goto(int i)
        {
            if (i < 0 || i >= dlg.lines.Count) { Close(); return; }
            index = i;
            chars = 0;
            selected = 0;
        }

        void Close()
        {
            var d = dlg;
            dlg = null;
            GameState.UIOpen = false;
            GameState.LastUIFrame = Time.frameCount;
            d.onEnd?.Invoke();
        }

        void Update()
        {
            if (dlg == null) return;
            float dt = Time.deltaTime;
            slide = Mathf.MoveTowards(slide, 1f, dt / 0.3f);
            if (slide < 1f) return; // текст начинает печататься, когда рамка выехала

            var line = dlg.lines[index];
            int before = (int)chars;
            chars = Mathf.Min(line.text.Length, chars + dt * CharsPerSecond);
            if ((int)chars != before) { tickT -= 1; if (tickT <= 0) { tickT = 3; Sfx.Play(Sfx.Click, 0.04f); } }
            if (Time.frameCount == openFrame) return;

            bool typed = chars >= line.text.Length;
            if (!typed)
            {
                if (GameInput.ConfirmPressed) chars = line.text.Length;
                return;
            }

            if (line.choices != null && line.choices.Length > 0)
            {
                int n = line.choices.Length;
                if (GameInput.UpPressed) { selected = (selected + n - 1) % n; Sfx.Play(Sfx.Click, 0.25f); }
                if (GameInput.DownPressed) { selected = (selected + 1) % n; Sfx.Play(Sfx.Click, 0.25f); }
                if (GameInput.ConfirmPressed)
                {
                    var c = line.choices[selected];
                    Sfx.Play(Sfx.PageFlip, 0.7f);
                    c.action?.Invoke();
                    Goto(c.next);
                }
            }
            else if (GameInput.ConfirmPressed)
            {
                Sfx.Play(Sfx.PageFlip, 0.5f);
                Goto(line.next == DialogueLine.NextLine ? index + 1 : line.next);
            }
        }

        void OnGUI()
        {
            if (dlg == null) return;
            GUI.depth = 0;
            float s = UIDraw.Scale;
            var cam = Camera.main;
            if (cam == null) return;

            // позиция: по центру между собеседниками, над их головами
            Vector3 sa = cam.WorldToScreenPoint(a != null ? a.position + Vector3.up * 1.9f : cam.transform.position);
            Vector3 sb = cam.WorldToScreenPoint(b != null ? b.position + Vector3.up * 1.9f : cam.transform.position);
            float midX = (sa.x + sb.x) / 2f;
            float headTop = Screen.height - Mathf.Max(sa.y, sb.y);
            float w = Mathf.Min(Screen.width - 20 * s, 300 * s);
            float h = 104 * s;
            float targetY = Mathf.Max(8 * s, headTop - h - 10 * s);
            float x = Mathf.Clamp(midX - w / 2f, 8 * s, Screen.width - w - 8 * s);
            float e = 1f - Mathf.Pow(1f - slide, 3f);
            float y = Mathf.Lerp(Screen.height + 4 * s, targetY, e);

            UIDraw.Panel(new Rect(x, y, w, h));
            var line = dlg.lines[index];
            UIDraw.Text(new Rect(x + 8 * s, y + 5 * s, w - 16 * s, 12 * s), line.speaker, 8 * s, UIDraw.Accent, TextAnchor.UpperLeft, false);
            string shown = line.text.Substring(0, Mathf.Min(line.text.Length, (int)chars));
            UIDraw.Text(new Rect(x + 8 * s, y + 18 * s, w - 16 * s, 44 * s), shown, 8 * s, Color.white);

            bool typed = chars >= line.text.Length;
            if (!typed) return;
            if (line.choices != null && line.choices.Length > 0)
            {
                for (int i = 0; i < line.choices.Length; i++)
                {
                    bool sel = i == selected;
                    var r = new Rect(x + 8 * s, y + 62 * s + i * 13 * s, w - 16 * s, 12 * s);
                    UIDraw.Text(r, (sel ? "> " : "   ") + line.choices[i].text, 8 * s, sel ? Color.white : UIDraw.Dim, TextAnchor.UpperLeft, false);
                }
            }
            else if (Mathf.Repeat(Time.time * 2f, 1f) > 0.4f)
            {
                UIDraw.Text(new Rect(x + w - 24 * s, y + h - 14 * s, 16 * s, 10 * s), "▼", 8 * s, UIDraw.Dim, TextAnchor.MiddleRight, false);
            }
        }
    }
}
