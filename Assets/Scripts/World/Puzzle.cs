using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>Тяжёлые ворота. Открываются рычагом.</summary>
    public class Gate : MonoBehaviour
    {
        float height;
        bool opening;
        float openT;
        Vector3 closedPos;

        public static Gate Spawn(float x, float y, float width, float height)
        {
            var go = new GameObject("Gate");
            go.transform.position = new Vector3(x, y + height / 2f, 0);
            var g = go.AddComponent<Gate>();
            g.height = height;
            g.closedPos = go.transform.position;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);
            go.AddComponent<Solid>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.GateTile;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, height);
            sr.sortingOrder = -1; // уезжает за перекрытие
            return g;
        }

        public void Open()
        {
            if (opening) return;
            opening = true;
            Sfx.Play(Sfx.Gate, 0.8f);
        }

        void Update()
        {
            if (!opening || openT >= 1f) return;
            openT = Mathf.Min(1f, openT + Time.deltaTime / 1.6f);
            transform.position = closedPos + Vector3.up * height * Mathf.SmoothStep(0, 1, openT);
            CameraFollow.Shake(0.05f, 0.04f);
            if (Random.value < 0.4f) Effects.Dust(closedPos + Vector3.down * height / 2f + Vector3.right * Random.Range(-0.5f, 0.5f), 1);
        }
    }

    /// <summary>Рычаг. Срабатывает от удара арматурой.</summary>
    public class Lever : MonoBehaviour, IDamageable
    {
        public Gate gate;
        Transform handle;
        bool pulled;

        public static Lever Spawn(Vector2 pos, Gate gate)
        {
            var go = new GameObject("Lever");
            go.transform.position = pos;
            var l = go.AddComponent<Lever>();
            l.gate = gate;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.LeverBase;
            sr.sortingOrder = 4;
            l.handle = new GameObject("Handle").transform;
            l.handle.SetParent(go.transform, false);
            l.handle.localPosition = new Vector3(0, 0.2f, 0);
            l.handle.localRotation = Quaternion.Euler(0, 0, 35f);
            var hsr = l.handle.gameObject.AddComponent<SpriteRenderer>();
            hsr.sprite = Art.LeverHandle;
            hsr.sortingOrder = 3;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 1f);
            col.offset = new Vector2(0, 0.5f);
            return l;
        }

        public void TakeDamage(float amount, Vector2 from, DamageKind kind)
        {
            if (pulled || kind != DamageKind.Melee) return;
            pulled = true;
            handle.localRotation = Quaternion.Euler(0, 0, -35f);
            Sfx.Play(Sfx.Lever, 0.7f);
            Effects.Sparkle((Vector2)transform.position + Vector2.up * 0.6f, 6, new Color(1f, 0.8f, 0.4f));
            if (gate != null) gate.Open();
            Hud.ShowToast("Где-то впереди загрохотали ворота", 3f);
        }
    }

    /// <summary>
    /// Токсичная завеса: не пропускает героя и обжигает при касании.
    /// Растворяется под лучом «Очистки»; если бросить на полпути — медленно восстанавливается.
    /// </summary>
    public class ToxicBarrier : MonoBehaviour, ICleanable
    {
        public float toxicity = 2.5f;
        float max;
        float lastClean = -10f;
        bool gone;
        SpriteRenderer sr;
        BoxCollider2D col;
        float t;

        public static ToxicBarrier Spawn(float x, float y, float width, float height)
        {
            var go = new GameObject("ToxicBarrier");
            go.transform.position = new Vector3(x, y + height / 2f, 0);
            var b = go.AddComponent<ToxicBarrier>();
            b.max = b.toxicity;
            b.col = go.AddComponent<BoxCollider2D>();
            b.col.size = new Vector2(width, height);
            b.sr = go.AddComponent<SpriteRenderer>();
            b.sr.sprite = Art.BarrierTile;
            b.sr.drawMode = SpriteDrawMode.Tiled;
            b.sr.size = new Vector2(width, height);
            b.sr.sortingOrder = 12;
            return b;
        }

        public void Clean(float amount)
        {
            if (gone) return;
            toxicity -= amount;
            lastClean = Time.time;
            if (Random.value < 0.4f)
                Effects.Sparkle(transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-1.5f, 1.5f), 0), 1, new Color(0.8f, 1f, 1f));
            if (toxicity <= 0f)
            {
                gone = true;
                col.enabled = false;
                Sfx.Play(Sfx.Cleanse, 0.7f);
                Hud.ShowToast("Завеса рассеялась", 2f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            if (gone)
            {
                var c = sr.color; c.a = Mathf.MoveTowards(c.a, 0, dt * 2f); sr.color = c;
                if (c.a <= 0) Destroy(gameObject);
                return;
            }
            if (Time.time - lastClean > 1f) toxicity = Mathf.Min(max, toxicity + dt * 0.5f);
            float k = toxicity / max;
            sr.color = new Color(1, 1, 1, (0.35f + 0.65f * k) * (0.85f + 0.15f * Mathf.Sin(t * 6f)));
            if (Random.value < 0.15f) Bubble.Spawn(transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-1.8f, 1.8f), 0));

            var p = PlayerController.Instance;
            if (p != null && p.IsAlive)
            {
                var b = col.bounds;
                b.Expand(new Vector3(0.1f, 0f, 0f));
                if (Phys.Overlap2D(b, p.Col.bounds)) p.Hurt(1, transform.position);
            }
        }
    }

    /// <summary>Токсичная жижа на дне ямы.</summary>
    public class SludgePool : MonoBehaviour
    {
        Bounds area;

        public static SludgePool Spawn(float x, float y, float width, float height)
        {
            var go = new GameObject("Sludge");
            go.transform.position = new Vector3(x + width / 2f, y + height / 2f, 0);
            var s = go.AddComponent<SludgePool>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.SludgeTile;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, height);
            sr.sortingOrder = 13;
            s.area = new Bounds(go.transform.position, new Vector3(width, height, 1));
            return s;
        }

        void Update()
        {
            if (Random.value < 0.2f)
                Bubble.Spawn(new Vector2(Random.Range(area.min.x, area.max.x), area.max.y - 0.05f));
            var p = PlayerController.Instance;
            if (p != null && p.IsAlive && Phys.Overlap2D(area, p.Col.bounds)) p.HazardHit();
        }
    }

    /// <summary>Фонарь-чекпоинт: здесь герой возрождается.</summary>
    public class Checkpoint : MonoBehaviour
    {
        bool lit;
        SpriteRenderer sr, glow;
        float t;

        public static Checkpoint Spawn(Vector2 pos, bool startLit = false)
        {
            var go = new GameObject("Checkpoint");
            go.transform.position = pos;
            var c = go.AddComponent<Checkpoint>();
            c.sr = go.AddComponent<SpriteRenderer>();
            c.sr.sprite = Art.LampPostOff;
            c.sr.sortingOrder = 3;
            var g = new GameObject("Glow");
            g.transform.SetParent(go.transform, false);
            g.transform.localPosition = new Vector3(0, 1.55f, 0);
            c.glow = g.AddComponent<SpriteRenderer>();
            c.glow.sprite = PixelArt.SoftCircle(40);
            c.glow.color = new Color(1f, 0.75f, 0.4f, 0.4f);
            c.glow.sortingOrder = 2;
            c.glow.enabled = false;
            if (startLit) c.Light(false);
            return c;
        }

        void Light(bool announce)
        {
            lit = true;
            sr.sprite = Art.LampPost;
            glow.enabled = true;
            var p = PlayerController.Instance;
            if (p != null) p.respawnPoint = transform.position + Vector3.right * 0.8f;
            if (announce)
            {
                Sfx.Play(Sfx.Cleanse, 0.5f);
                Hud.ShowToast("Фонарь зажжён — здесь ты очнёшься после гибели", 3f);
                if (p != null) { p.Stats.hp = p.Stats.maxHp; p.Device.Refill(); }
            }
        }

        void Update()
        {
            t += Time.deltaTime;
            if (lit) { glow.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.05f); return; }
            var p = PlayerController.Instance;
            if (p != null && p.IsAlive && Vector2.Distance(p.transform.position, transform.position) < 1.5f) Light(true);
        }
    }

    /// <summary>Конец демо-главы.</summary>
    public class EndZone : MonoBehaviour
    {
        bool done;

        public static void Spawn(float x)
        {
            var go = new GameObject("EndZone");
            go.transform.position = new Vector3(x, 0, 0);
            go.AddComponent<EndZone>();
        }

        void Update()
        {
            if (done) return;
            var p = PlayerController.Instance;
            if (p != null && p.transform.position.x > transform.position.x)
            {
                done = true;
                Hud.ShowBanner("ГЛАВА I ПРОЙДЕНА", 6f);
                Hud.ShowToast("Это конец демо. Дальше — завод «Химпром-7»...", 8f);
            }
        }
    }
}
