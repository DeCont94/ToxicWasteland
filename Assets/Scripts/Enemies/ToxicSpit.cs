using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>Плевок «плевуна»: летит по дуге, бьёт героя на 1 HP.</summary>
    public class ToxicSpit : MonoBehaviour
    {
        const float G = 12f;
        Vector2 vel;
        float life = 4f;

        public static void Spawn(Vector2 origin, Vector2 target)
        {
            var go = new GameObject("ToxicSpit");
            go.transform.position = origin;
            var s = go.AddComponent<ToxicSpit>();
            float t = Mathf.Clamp(Mathf.Abs(target.x - origin.x) / 6f, 0.5f, 1.2f);
            s.vel = new Vector2((target.x - origin.x) / t, (target.y - origin.y) / t + 0.5f * G * t);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.SpitBall;
            sr.sortingOrder = 18;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            vel.y -= G * dt;
            transform.position += (Vector3)(vel * dt);
            life -= dt;
            Vector2 pos = transform.position;
            if (Random.value < 0.3f) Effects.Spawn(PixelArt.Pixel, pos, Vector2.zero, 0.3f, new Color(0.5f, 0.9f, 0.2f, 0.8f), 0, 1.5f, 17);

            var p = PlayerController.Instance;
            if (p != null && p.IsAlive && Phys.Contains2D(p.Col.bounds, pos))
            {
                p.Hurt(1, pos);
                Splat();
                return;
            }
            if (life <= 0 || Phys.PointInSolid(pos)) Splat();
        }

        void Splat()
        {
            Effects.ToxicSplash(transform.position);
            Sfx.Play(Sfx.Splat, 0.4f);
            Destroy(gameObject);
        }
    }
}
