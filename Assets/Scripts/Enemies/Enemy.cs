using System.Collections.Generic;
using UnityEngine;

namespace ToxicWasteland
{
    public enum EnemyType
    {
        Mutant,  // медленный, крепкий, преследует
        Crawler, // быстрый, низкий, слабый
        Spitter, // стоит на месте, плюётся токсином
    }

    public class Enemy : MonoBehaviour, IDamageable
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        public EnemyType type;
        public float hp, maxHp;
        public bool Dead { get; private set; }
        public BoxCollider2D Col { get; private set; }

        float patrolSpeed, chaseSpeed, homeX, range, sightRange = 7f;
        int dir = 1;
        int scrap;
        Rigidbody2D rb;
        SpriteRenderer sr;
        Transform visual;
        Sprite[] frames;
        float flash, stun, anim, deathT, hpBarT;
        float shootCd = 1.5f;
        Color hurtColor = Color.white;

        public static Enemy Spawn(EnemyType t, Vector2 pos, float patrolRange = 3f)
        {
            var go = new GameObject("Enemy_" + t);
            go.transform.position = pos;
            var e = go.AddComponent<Enemy>();
            e.type = t;
            e.homeX = pos.x;
            e.range = patrolRange;
            e.Setup();
            return e;
        }

        void Setup()
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            Col = gameObject.AddComponent<BoxCollider2D>();
            Col.sharedMaterial = Phys.NoFriction;

            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            sr = visual.gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 9;

            switch (type)
            {
                case EnemyType.Mutant:
                    maxHp = 4; patrolSpeed = 1.4f; chaseSpeed = 2.6f; scrap = 6;
                    frames = Art.MutantFrames; Col.size = new Vector2(0.8f, 1.3f);
                    break;
                case EnemyType.Crawler:
                    maxHp = 2; patrolSpeed = 2.4f; chaseSpeed = 4.2f; scrap = 3;
                    frames = Art.CrawlerFrames; Col.size = new Vector2(1.0f, 0.45f);
                    break;
                case EnemyType.Spitter:
                    maxHp = 3; scrap = 5; sightRange = 9f;
                    frames = Art.SpitterFrames; Col.size = new Vector2(0.8f, 1.2f);
                    break;
            }
            Col.offset = new Vector2(0f, Col.size.y / 2f);
            hp = maxHp;
            sr.sprite = frames[0];
            All.Add(this);
        }

        void Start()
        {
            var p = PlayerController.Instance;
            if (p != null) Physics2D.IgnoreCollision(Col, p.Col); // враги не толкают героя физически
            foreach (var other in All)
                if (other != this && other.Col != null) Physics2D.IgnoreCollision(Col, other.Col);
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Dead)
            {
                deathT += dt;
                sr.color = new Color(0.6f, 0.6f, 0.6f, 1f - deathT / 0.6f);
                visual.localPosition = new Vector3(0, -deathT * 0.4f, 0);
                if (deathT >= 0.6f) Destroy(gameObject);
                return;
            }

            anim += dt; flash -= dt; stun -= dt; hpBarT -= dt;
            var p = PlayerController.Instance;
            var v = rb.linearVelocity;
            Vector2 pos = transform.position;
            bool sees = p != null && p.IsAlive &&
                        Mathf.Abs(p.transform.position.x - pos.x) < sightRange &&
                        Mathf.Abs(p.transform.position.y - pos.y) < 2.5f;

            if (type == EnemyType.Spitter)
            {
                v.x = 0;
                if (sees)
                {
                    dir = p.transform.position.x > pos.x ? 1 : -1;
                    shootCd -= dt;
                    if (shootCd <= 0 && stun <= 0) { shootCd = 2.2f; Shoot(p); }
                }
            }
            else if (stun <= 0)
            {
                float speed = patrolSpeed;
                if (sees)
                {
                    dir = p.transform.position.x > pos.x ? 1 : -1;
                    speed = Mathf.Abs(p.transform.position.x - pos.x) < 0.2f ? 0 : chaseSpeed;
                }
                else
                {
                    if (pos.x > homeX + range) dir = -1;
                    else if (pos.x < homeX - range) dir = 1;
                }
                if (!GroundAhead() || WallAhead())
                {
                    if (sees) speed = 0; else dir = -dir;
                }
                v.x = dir * speed;
            }
            rb.linearVelocity = v;

            // контактный урон
            if (p != null && p.IsAlive && Phys.Overlap2D(Col.bounds, p.Col.bounds))
                p.Hurt(1, pos);

            // анимация
            int f = 0;
            if (type == EnemyType.Spitter) f = (int)(anim * 2f) % frames.Length;
            else if (Mathf.Abs(v.x) > 0.1f) f = (int)(anim * (type == EnemyType.Crawler ? 12f : 6f)) % frames.Length;
            sr.sprite = frames[f];
            float sq = flash > 0 ? 0.92f : 1f; // сдержанная реакция на урон
            visual.localScale = new Vector3(dir * (2f - sq), sq, 1f);
            sr.color = flash > 0 ? hurtColor : Color.white;
        }

        bool GroundAhead()
        {
            var o = new Vector2(transform.position.x + dir * (Col.size.x / 2f + 0.1f), transform.position.y + 0.2f);
            return Phys.RaycastSolid(o, Vector2.down, 0.7f, out _);
        }

        bool WallAhead()
        {
            var o = new Vector2(transform.position.x, transform.position.y + Col.size.y / 2f);
            return Phys.RaycastSolid(o, Vector2.right * dir, Col.size.x / 2f + 0.15f, out _);
        }

        void Shoot(PlayerController p)
        {
            var o = (Vector2)transform.position + new Vector2(dir * 0.4f, 1.0f);
            var target = (Vector2)p.transform.position + Vector2.up * 0.7f;
            ToxicSpit.Spawn(o, target);
            Sfx.Play(Sfx.Spit, 0.5f);
        }

        public void TakeDamage(float amount, Vector2 from, DamageKind kind)
        {
            if (Dead) return;
            hp -= amount;
            hpBarT = 2f;
            if (kind == DamageKind.Clean)
            {
                flash = 0.05f;
                hurtColor = new Color(0.75f, 0.95f, 1f);
                if (Random.value < 0.08f) Effects.Sparkle((Vector2)transform.position + Vector2.up * Col.size.y * 0.6f, 2, new Color(0.8f, 1f, 1f));
            }
            else
            {
                flash = 0.15f;
                stun = 0.25f;
                hurtColor = kind == DamageKind.Toxic ? new Color(0.7f, 1f, 0.4f) : new Color(1f, 0.6f, 0.55f);
                if (kind == DamageKind.Melee && type != EnemyType.Spitter)
                    rb.linearVelocity = new Vector2(Mathf.Sign(transform.position.x - from.x) * 3.5f, 2.5f);
                Effects.Blood((Vector2)transform.position + Vector2.up * Col.size.y * 0.6f, 4, new Color(0.3f, 0.4f, 0.15f));
            }
            if (hp <= 0) Die();
        }

        void Die()
        {
            Dead = true;
            Col.enabled = false;
            rb.simulated = false;
            All.Remove(this);
            Pickup.SpawnScrap((Vector2)transform.position + Vector2.up * 0.5f, scrap);
            Sfx.Play(Sfx.EnemyDie, 0.5f);
        }

        /// <summary>Небольшая полоска здоровья над врагом, видна пару секунд после удара.</summary>
        void OnGUI()
        {
            if (Dead || hpBarT <= 0 || Camera.main == null) return;
            var sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * (Col.size.y + 0.35f));
            if (sp.z < 0) return;
            float s = Screen.height / 360f;
            float w = 18 * s, h = 2 * s;
            var r = new Rect(sp.x - w / 2, Screen.height - sp.y, w, h);
            GUI.DrawTexture(new Rect(r.x - s, r.y - s, r.width + 2 * s, r.height + 2 * s), PixelArt.Tex(new Color(0, 0, 0, 0.7f)));
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(hp / maxHp), r.height), PixelArt.Tex(new Color(0.75f, 0.2f, 0.15f)));
        }
    }
}
