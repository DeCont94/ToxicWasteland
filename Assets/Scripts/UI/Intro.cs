using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>Вступление: чёрный экран, название и несколько строк о мире. Enter — пропустить строку.</summary>
    public class Intro : MonoBehaviour
    {
        const float LineTime = 3.6f;
        float t, fadeOut = -1f;
        int index;

        void Awake() { GameState.Intro = true; }

        void Update()
        {
            if (fadeOut >= 0)
            {
                fadeOut += Time.deltaTime;
                if (fadeOut >= 1.5f) Finish();
                return;
            }
            t += Time.deltaTime;
            bool skip = GameInput.ConfirmPressed;
            if (GameInput.CancelPressed) { fadeOut = 0; return; }
            if (t >= LineTime || skip)
            {
                t = 0;
                index++;
                if (index >= Story.IntroLines.Length) fadeOut = 0;
            }
        }

        void Finish()
        {
            GameState.Intro = false;
            GameState.LastUIFrame = Time.frameCount;
            Hud.ShowToast("Подвал разрушенного дома. Снаружи воет шторм.", 4f);
            Destroy(this);
        }

        void OnGUI()
        {
            GUI.depth = -10;
            float s = UIDraw.Scale;
            float bgA = fadeOut >= 0 ? 1f - fadeOut / 1.5f : 1f;
            UIDraw.Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, bgA));
            if (fadeOut >= 0) return;

            float a = Mathf.Clamp01(t / 0.6f) * Mathf.Clamp01((LineTime - t) / 0.6f);
            if (index == 0)
            {
                UIDraw.Text(new Rect(0, Screen.height * 0.35f, Screen.width, 30 * s), "TOXIC WASTELAND", 22 * s,
                    new Color(0.6f, 0.9f, 0.25f, a), TextAnchor.MiddleCenter, false);
            }
            if (index < Story.IntroLines.Length)
            {
                UIDraw.Text(new Rect(40 * s, Screen.height * 0.5f, Screen.width - 80 * s, 40 * s), Story.IntroLines[index], 10 * s,
                    new Color(0.85f, 0.85f, 0.8f, a), TextAnchor.UpperCenter);
            }
            UIDraw.Text(new Rect(0, Screen.height - 20 * s, Screen.width - 10 * s, 14 * s), "Enter — дальше,  Esc — пропустить", 7 * s,
                new Color(0.5f, 0.5f, 0.5f), TextAnchor.MiddleRight, false);
        }
    }
}
