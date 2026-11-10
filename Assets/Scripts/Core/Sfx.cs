using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>
    /// Временные звуки, синтезированные в коде. Позже заменяются на настоящие клипы.
    /// </summary>
    public static class Sfx
    {
        const int SR = 44100;

        public static AudioClip Jump, Swing, Hit, Hurt, PageFlip, Rustle, Pickup, Click, EnergyHum, ToxicHiss,
            Thunder, Splat, Overheat, Lever, Gate, Buy, Deny, Pop, EnemyDie, Spit, Cleanse;

        static AudioSource src;
        static bool ready;

        public static void Init()
        {
            if (ready) return;
            ready = true;
            var rng = new System.Random(1234);

            Jump = Tone("jump", 0.12f, t => Mathf.Lerp(260, 520, t / 0.12f), 0.25f, square: true);
            Swing = Noise("swing", 0.16f, rng, t => Bump(t, 0.16f), t => 900 + 2500 * Bump(t, 0.16f));
            Hit = Mix(Tone("hit_t", 0.14f, t => Mathf.Lerp(140, 60, t / 0.14f), 0.9f),
                      Noise("hit_n", 0.05f, rng, t => 1 - t / 0.05f, t => 3000), 0.6f);
            Hurt = Mix(Tone("hurt_t", 0.28f, t => Mathf.Lerp(220, 90, t / 0.28f), 0.7f, square: true),
                       Noise("hurt_n", 0.18f, rng, t => 1 - t / 0.18f, t => 1500), 0.5f);
            PageFlip = Noise("pageflip", 0.28f, rng,
                t => Mathf.Max(Bump(t, 0.12f), 0.7f * Bump(t - 0.1f, 0.18f)), t => 5000, highpass: true);
            Rustle = RustleClip(rng);
            Pickup = Concat(Tone("pk1", 0.07f, t => 660, 0.35f), Tone("pk2", 0.12f, t => 990, 0.35f));
            Click = Tone("click", 0.03f, t => 900, 0.3f, square: true);
            EnergyHum = HumClip();
            ToxicHiss = HissClip(rng);
            Thunder = Noise("thunder", 2.6f, rng, t => Mathf.Clamp01(t * 8) * Mathf.Exp(-t * 1.4f) * (0.6f + 0.4f * Mathf.Sin(t * 9)), t => 140);
            Splat = Noise("splat", 0.18f, rng, t => 1 - t / 0.18f, t => 700);
            Overheat = Tone("overheat", 0.45f, t => Mathf.Lerp(620, 180, t / 0.45f), 0.35f, square: true);
            Lever = Mix(Tone("lv1", 0.3f, t => 410, 0.5f), Tone("lv2", 0.3f, t => 640, 0.4f), 0.7f);
            Gate = Noise("gate", 1.4f, rng, t => Mathf.Clamp01(t * 6) * Mathf.Clamp01((1.4f - t) * 3), t => 220);
            Buy = Concat(Tone("b1", 0.06f, t => 1200, 0.3f), Tone("b2", 0.1f, t => 1600, 0.3f));
            Deny = Tone("deny", 0.18f, t => 140, 0.35f, square: true);
            Pop = Tone("pop", 0.05f, t => Mathf.Lerp(900, 380, t / 0.05f), 0.25f);
            EnemyDie = Mix(Tone("ed", 0.4f, t => Mathf.Lerp(180, 50, t / 0.4f), 0.6f, square: true),
                           Noise("edn", 0.3f, rng, t => 1 - t / 0.3f, t => 900), 0.6f);
            Spit = Noise("spit", 0.2f, rng, t => Bump(t, 0.2f), t => 1800);
            Cleanse = Concat(Tone("c1", 0.08f, t => 880, 0.3f), Tone("c2", 0.08f, t => 1320, 0.3f), Tone("c3", 0.2f, t => 1760, 0.25f));

            var go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
        }

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null && src != null) src.PlayOneShot(clip, volume);
        }

        /// <summary>Создаёт зацикленный источник (громкость по умолчанию 0).</summary>
        public static AudioSource Loop(GameObject host, AudioClip clip)
        {
            var s = host.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.volume = 0f;
            s.playOnAwake = false;
            s.Play();
            return s;
        }

        // ── синтез ──
        static float Bump(float t, float dur) => t < 0 || t > dur ? 0 : Mathf.Sin(Mathf.PI * t / dur);

        static AudioClip Make(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, SR, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip Tone(string name, float dur, System.Func<float, float> freq, float vol, bool square = false)
        {
            int n = (int)(dur * SR);
            var d = new float[n];
            float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                phase += freq(t) / SR;
                float s = square ? (Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f) * 0.5f : Mathf.Sin(phase * Mathf.PI * 2);
                float env = Mathf.Clamp01(t * 200f) * (1f - t / dur);
                d[i] = s * env * vol;
            }
            return Make(name, d);
        }

        static AudioClip Noise(string name, float dur, System.Random rng, System.Func<float, float> env,
            System.Func<float, float> cutoff, bool highpass = false)
        {
            int n = (int)(dur * SR);
            var d = new float[n];
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float x = (float)(rng.NextDouble() * 2 - 1);
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff(t) / SR);
                lp += a * (x - lp);
                float s = highpass ? x - lp : lp * 2.5f;
                d[i] = Mathf.Clamp(s * env(t) * 0.6f, -1, 1);
            }
            return Make(name, d);
        }

        static float[] Data(AudioClip c)
        {
            var d = new float[c.samples];
            c.GetData(d, 0);
            return d;
        }

        static AudioClip Mix(AudioClip a, AudioClip b, float vol)
        {
            var da = Data(a); var db = Data(b);
            var d = new float[Mathf.Max(da.Length, db.Length)];
            for (int i = 0; i < d.Length; i++)
                d[i] = Mathf.Clamp(((i < da.Length ? da[i] : 0) + (i < db.Length ? db[i] : 0)) * vol, -1, 1);
            return Make(a.name + "_mix", d);
        }

        static AudioClip Concat(params AudioClip[] clips)
        {
            int total = 0;
            foreach (var c in clips) total += c.samples;
            var d = new float[total];
            int o = 0;
            foreach (var c in clips) { var cd = Data(c); cd.CopyTo(d, o); o += cd.Length; }
            return Make(clips[0].name + "_seq", d);
        }

        /// <summary>Шуршание бинта: серия коротких шорохов.</summary>
        static AudioClip RustleClip(System.Random rng)
        {
            float dur = 0.8f;
            int n = (int)(dur * SR);
            var d = new float[n];
            float lp = 0, env = 0;
            for (int i = 0; i < n; i++)
            {
                if (rng.NextDouble() < 0.0006) env = 0.5f + (float)rng.NextDouble() * 0.5f;
                env *= 0.9993f;
                float x = (float)(rng.NextDouble() * 2 - 1);
                lp += 0.35f * (x - lp);
                d[i] = (x - lp) * env * 0.5f * Mathf.Clamp01((n - i) / (SR * 0.1f));
            }
            return Make("rustle", d);
        }

        /// <summary>Гул энергии (бесшовная петля 1 с).</summary>
        static AudioClip HumClip()
        {
            var d = new float[SR];
            for (int i = 0; i < SR; i++)
            {
                float t = i / (float)SR;
                float s = Mathf.Sin(2 * Mathf.PI * 110 * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 220 * t) * 0.3f +
                          Mathf.Sin(2 * Mathf.PI * 440 * t) * 0.15f + Mathf.Sin(2 * Mathf.PI * 1320 * t) * 0.05f;
                float trem = 0.8f + 0.2f * Mathf.Sin(2 * Mathf.PI * 6 * t);
                d[i] = s * trem * 0.4f;
            }
            return Make("energy_hum", d);
        }

        /// <summary>Токсичное шипение с «бульканьем» (петля 1 с).</summary>
        static AudioClip HissClip(System.Random rng)
        {
            var d = new float[SR];
            float lp = 0;
            for (int i = 0; i < SR; i++)
            {
                float t = i / (float)SR;
                float x = (float)(rng.NextDouble() * 2 - 1);
                lp += 0.12f * (x - lp);
                float bubble = 0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * 8 * t);
                d[i] = lp * 1.8f * bubble * 0.5f;
            }
            return Make("toxic_hiss", d);
        }
    }
}
