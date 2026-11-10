using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>Простая спрайтовая частица (кровь, искры, пар).</summary>
    public class Particle : MonoBehaviour
    {
        public Vector2 vel;
        public float gravity;
        public float life, maxLife;
        public float drag;
        public bool shrink;
        public bool grow;
        SpriteRenderer sr;
        Color baseColor;
        Vector3 baseScale;

        void Awake() { sr = GetComponent<SpriteRenderer>(); }

        public void Init(Color c, float lifeTime)
        {
            baseColor = c;
            sr.color = c;
            life = maxLife = lifeTime;
            baseScale = transform.localScale;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0) { Destroy(gameObject); return; }
            vel.y -= gravity * dt;
            vel *= 1f / (1f + drag * dt);
            transform.position += (Vector3)(vel * dt);
            float k = life / maxLife;
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * Mathf.Clamp01(k * 2f));
            if (shrink) transform.localScale = baseScale * Mathf.Max(0.2f, k);
            if (grow) transform.localScale = baseScale * (1f + (1f - k) * 1.5f);
        }
    }

    public static class Effects
    {
        public static Particle Spawn(Sprite sprite, Vector2 pos, Vector2 vel, float life, Color color,
            float gravity = 0f, float scale = 1f, int order = 20)
        {
            var go = new GameObject("fx");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            var p = go.AddComponent<Particle>();
            p.vel = vel;
            p.gravity = gravity;
            p.Init(color, life);
            return p;
        }

        /// <summary>Капли крови при уроне героя.</summary>
        public static void Blood(Vector2 pos, int count, Color? color = null)
        {
            var c = color ?? new Color(0.62f, 0.05f, 0.05f);
            for (int i = 0; i < count; i++)
            {
                var v = new Vector2(Random.Range(-3f, 3f), Random.Range(1.5f, 5f));
                Spawn(PixelArt.Pixel, pos, v, Random.Range(0.4f, 0.9f), c, 18f, Random.Range(1f, 2.2f), 25);
            }
        }

        public static void Sparkle(Vector2 pos, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                var v = Random.insideUnitCircle * 2f + Vector2.up * 0.8f;
                Spawn(PixelArt.Pixel, pos + Random.insideUnitCircle * 0.3f, v, Random.Range(0.3f, 0.7f), color, 0f, 1.5f, 22).drag = 2f;
            }
        }

        public static void ToxicPuff(Vector2 pos, Vector2 vel)
        {
            var p = Spawn(PixelArt.SoftCircle(10), pos, vel, Random.Range(0.35f, 0.6f),
                new Color(0.45f, 0.85f, 0.2f, 0.45f), -0.5f, Random.Range(0.5f, 0.9f), 16);
            p.grow = true;
            p.drag = 2.5f;
        }

        public static void ToxicSplash(Vector2 pos)
        {
            for (int i = 0; i < 8; i++)
            {
                var v = new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(1f, 4f));
                Spawn(PixelArt.Pixel, pos, v, Random.Range(0.3f, 0.6f), new Color(0.5f, 0.9f, 0.2f), 14f, 1.8f, 22);
            }
        }

        public static void Dust(Vector2 pos, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var v = new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(0.2f, 1f));
                Spawn(PixelArt.Pixel, pos, v, Random.Range(0.3f, 0.5f), new Color(0.5f, 0.47f, 0.4f, 0.8f), 2f, 2f, 21).drag = 3f;
            }
        }
    }
}
