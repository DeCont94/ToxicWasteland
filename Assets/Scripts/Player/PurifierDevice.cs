using System.Collections.Generic;
using UnityEngine;

namespace ToxicWasteland
{
    public enum DeviceMode { Clean, Pollute }

    /// <summary>
    /// Устройство деда. Радиус действия 3 метра в обоих режимах.
    /// Очистка — световой луч, 1 HP/сек врагам, убирает лужи и завесы.
    /// Загрязнение — токсичное облако, создаёт временные лужи (2–3 HP врагам).
    /// </summary>
    public class PurifierDevice : MonoBehaviour
    {
        public const float Radius = 3f;
        public const float CleanDps = 1f;

        public bool unlocked;
        public DeviceMode mode = DeviceMode.Clean;
        public float energy = 100f;
        public bool overheated;

        public float cleanDrain = 22f;
        public float polluteDrain = 30f;
        public float recharge = 18f;

        PlayerController pc;
        PlayerStats stats;
        SpriteRenderer beamOuter, beamCore, glow;
        AudioSource hum, hiss;
        bool active;
        float rechargeDelay, puddleTimer, puffTimer, t;

        public bool Active => active;
        public float Energy01 => stats ? Mathf.Clamp01(energy / stats.deviceCapacity) : 0f;

        void Start()
        {
            pc = GetComponent<PlayerController>();
            stats = GetComponent<PlayerStats>();
            energy = stats.deviceCapacity;

            beamOuter = MakeRenderer("BeamOuter", PixelArt.BeamUnit, 15, new Color(0.65f, 0.9f, 1f, 0.35f));
            beamCore = MakeRenderer("BeamCore", PixelArt.BeamUnit, 16, new Color(0.95f, 1f, 1f, 0.9f));
            glow = MakeRenderer("Glow", PixelArt.SoftCircle(24), 14, new Color(0.6f, 0.9f, 1f, 0.5f));

            hum = Sfx.Loop(gameObject, Sfx.EnergyHum);
            hiss = Sfx.Loop(gameObject, Sfx.ToxicHiss);
        }

        SpriteRenderer MakeRenderer(string name, Sprite s, int order, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = s;
            r.sortingOrder = order;
            r.color = c;
            r.enabled = false;
            return r;
        }

        public void SetInput(bool held, bool switchPressed)
        {
            if (!unlocked) { active = false; return; }
            if (switchPressed)
            {
                mode = mode == DeviceMode.Clean ? DeviceMode.Pollute : DeviceMode.Clean;
                Sfx.Play(Sfx.Click, 0.5f);
                Hud.ShowToast(mode == DeviceMode.Clean ? "Режим: ОЧИСТКА" : "Режим: ЗАГРЯЗНЕНИЕ", 1.2f);
            }
            active = held && !overheated && energy > 0f;
        }

        public void ForceStop() { active = false; }

        public void Refill() { if (stats) energy = stats.deviceCapacity; overheated = false; }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            float cap = stats.deviceCapacity;
            if (!pc.IsAlive || GameState.UIOpen) active = false;

            if (active)
            {
                energy -= (mode == DeviceMode.Clean ? cleanDrain : polluteDrain) * dt;
                rechargeDelay = 0.6f;
                if (energy <= 0f)
                {
                    energy = 0f;
                    overheated = true;
                    active = false;
                    Sfx.Play(Sfx.Overheat, 0.5f);
                    Hud.ShowToast("Устройство перегрето", 1.5f);
                }
            }
            else
            {
                rechargeDelay -= dt;
                if (rechargeDelay <= 0f) energy = Mathf.Min(cap, energy + recharge * dt);
                if (overheated && energy >= cap * 0.35f) overheated = false;
            }

            Vector2 origin = (Vector2)transform.position + new Vector2(pc.Facing * 0.35f, 0.85f);
            UpdateVisuals(origin, dt);

            if (!active) return;
            if (mode == DeviceMode.Clean) DoClean(origin, dt);
            else DoPollute(origin, dt);
        }

        void DoClean(Vector2 origin, float dt)
        {
            // зона луча: 3 м вперёд, захватывает и пол (лужи)
            var center = new Vector2(origin.x + pc.Facing * Radius * 0.5f, origin.y - 0.3f);
            var size = new Vector2(Radius, 2f);
            var damaged = new HashSet<IDamageable>();
            var cleaned = new HashSet<ICleanable>();
            foreach (var h in Physics2D.OverlapBoxAll(center, size, 0f))
            {
                if (h.attachedRigidbody == pc.Rb) continue;
                var d = h.GetComponentInParent<IDamageable>();
                if (d != null && damaged.Add(d)) d.TakeDamage(CleanDps * dt, origin, DamageKind.Clean);
                var c = h.GetComponentInParent<ICleanable>();
                if (c != null && cleaned.Add(c)) c.Clean(dt);
            }
        }

        void DoPollute(Vector2 origin, float dt)
        {
            puffTimer -= dt;
            if (puffTimer <= 0f)
            {
                puffTimer = 0.04f;
                var vel = new Vector2(pc.Facing * Random.Range(4f, 6.5f), Random.Range(-1.2f, 0.6f));
                Effects.ToxicPuff(origin, vel);
            }

            puddleTimer -= dt;
            if (puddleTimer <= 0f)
            {
                puddleTimer = 0.45f;
                float dist = Random.Range(0.9f, Radius);
                var p = new Vector2(transform.position.x + pc.Facing * dist, origin.y);
                if (Phys.RaycastSolid(p, Vector2.down, 4f, out var hit))
                    ToxicPuddle.Spawn(hit.point);
            }
        }

        void UpdateVisuals(Vector2 origin, float dt)
        {
            bool beamOn = active && mode == DeviceMode.Clean;
            beamOuter.enabled = beamCore.enabled = beamOn;
            glow.enabled = active;
            if (active)
            {
                glow.transform.position = origin;
                glow.color = mode == DeviceMode.Clean ? new Color(0.6f, 0.9f, 1f, 0.55f) : new Color(0.5f, 0.9f, 0.2f, 0.5f);
                glow.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 30f) * 0.1f);
            }
            if (beamOn)
            {
                float flick = 1f + Mathf.Sin(t * 47f) * 0.15f + Random.Range(-0.05f, 0.05f);
                beamOuter.transform.position = origin;
                beamOuter.transform.localScale = new Vector3(pc.Facing * Radius, 0.42f * flick, 1f);
                beamCore.transform.position = origin;
                beamCore.transform.localScale = new Vector3(pc.Facing * Radius, 0.12f * flick, 1f);
                if (Random.value < 0.3f)
                    Effects.Sparkle(origin + new Vector2(pc.Facing * Random.Range(0.5f, Radius), Random.Range(-0.15f, 0.15f)), 1, new Color(0.8f, 0.95f, 1f));
            }

            float humTarget = beamOn ? 0.35f : 0f;
            float hissTarget = active && mode == DeviceMode.Pollute ? 0.4f : 0f;
            hum.volume = Mathf.MoveTowards(hum.volume, humTarget, dt * 3f);
            hiss.volume = Mathf.MoveTowards(hiss.volume, hissTarget, dt * 3f);
        }
    }
}
