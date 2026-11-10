using System.Collections.Generic;
using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>
    /// Токсичная лужа из режима «Загрязнение». Наносит врагам 2–3 HP,
    /// живёт ограниченное время, убирается режимом «Очистка».
    /// </summary>
    public class ToxicPuddle : MonoBehaviour, ICleanable
    {
        public static readonly List<ToxicPuddle> All = new List<ToxicPuddle>();
        const int MaxPuddles = 6;
        const float HalfWidth = 0.75f;

        float life = 8f;
        float cleanHp = 0.35f;
        float bubbleTimer;
        SpriteRenderer sr;
        readonly Dictionary<Enemy, float> cooldown = new Dictionary<Enemy, float>();

        public static void Spawn(Vector2 pos)
        {
            foreach (var p in All)
            {
                if (Vector2.Distance(p.transform.position, pos) < 0.9f) { p.life = 8f; return; } // обновить существующую
            }
            if (All.Count >= MaxPuddles) Destroy(All[0].gameObject);
            var go = new GameObject("ToxicPuddle");
            go.transform.position = pos;
            go.AddComponent<ToxicPuddle>();
            Effects.ToxicSplash(pos);
        }

        void Awake()
        {
            All.Add(this);
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Puddle;
            sr.sortingOrder = 5;
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(HalfWidth * 2f, 0.4f);
            col.offset = new Vector2(0, 0.2f);
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            sr.color = new Color(1, 1, 1, Mathf.Clamp01(life / 1.5f));

            // лопающиеся пузыри разного размера
            bubbleTimer -= dt;
            if (bubbleTimer <= 0f)
            {
                bubbleTimer = Random.Range(0.08f, 0.25f);
                Bubble.Spawn((Vector2)transform.position + new Vector2(Random.Range(-HalfWidth * 0.8f, HalfWidth * 0.8f), 0.15f));
            }

            // урон врагам, стоящим в луже
            Vector2 pos = transform.position;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dead) continue;
                Vector2 ep = e.transform.position;
                if (Mathf.Abs(ep.x - pos.x) > HalfWidth + e.Col.size.x * 0.5f || Mathf.Abs(ep.y - pos.y) > 0.6f) continue;
                if (cooldown.TryGetValue(e, out var cd) && Time.time < cd) continue;
                cooldown[e] = Time.time + 1.5f;
                e.TakeDamage(Random.Range(2, 4), pos, DamageKind.Toxic);
            }
        }

        public void Clean(float amount)
        {
            cleanHp -= amount;
            if (cleanHp <= 0f)
            {
                Effects.Sparkle((Vector2)transform.position + Vector2.up * 0.2f, 6, new Color(0.8f, 1f, 1f));
                Sfx.Play(Sfx.Pop, 0.4f);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>Пузырь: растёт и лопается.</summary>
    public class Bubble : MonoBehaviour
    {
        float t, life, size;
        SpriteRenderer sr;

        public static void Spawn(Vector2 pos)
        {
            var go = new GameObject("Bubble");
            go.transform.position = pos;
            var b = go.AddComponent<Bubble>();
            b.life = Random.Range(0.4f, 1f);
            b.size = Random.Range(0.5f, 1.6f);
            b.sr = go.AddComponent<SpriteRenderer>();
            b.sr.sprite = PixelArt.Ring(5);
            b.sr.color = new Color(0.7f, 1f, 0.35f, 0.9f);
            b.sr.sortingOrder = 6;
            go.transform.localScale = Vector3.zero;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / life;
            transform.localScale = Vector3.one * size * Mathf.Min(1f, k * 1.3f);
            transform.position += Vector3.up * Time.deltaTime * 0.1f;
            if (k >= 1f)
            {
                if (size > 1.1f && Random.value < 0.3f) Sfx.Play(Sfx.Pop, 0.08f);
                Effects.Spawn(PixelArt.Pixel, transform.position, Vector2.up * 1.5f + Random.insideUnitCircle, 0.2f, sr.color, 6f, 1.2f, 6);
                Destroy(gameObject);
            }
        }
    }
}
