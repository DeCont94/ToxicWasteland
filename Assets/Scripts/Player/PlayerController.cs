using System.Collections.Generic;
using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>Герой: движение, прыжок, удар арматурой, аптечки, урон и смерть.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Движение")]
        public float moveSpeed = 6f;
        public float jumpSpeed = 13f;
        public float coyoteTime = 0.1f;
        public float jumpBuffer = 0.12f;

        [Header("Бой")]
        public float attackCooldown = 0.38f;
        public float attackDuration = 0.18f;
        public int meleeDamage = 1;

        public Rigidbody2D Rb { get; private set; }
        public BoxCollider2D Col { get; private set; }
        public PlayerStats Stats { get; private set; }
        public PurifierDevice Device { get; private set; }
        public int Facing { get; private set; } = 1;
        public bool Grounded { get; private set; }
        public bool IsAlive => !dead;
        public Interactable Focused { get; private set; }

        public Vector3 respawnPoint;

        Transform visual, weapon;
        SpriteRenderer sr;
        float coyote, buffer, attackTimer, attackAnim, invuln, hurtTimer, bandageTimer, animTime, deadTimer;
        bool dead;
        Vector3 squash = Vector3.one;
        Vector3 lastSafe;
        bool wasGrounded;

        public static PlayerController Create(Vector2 pos)
        {
            var go = new GameObject("Player");
            go.transform.position = pos;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 1.5f);
            col.offset = new Vector2(0f, 0.75f);
            col.sharedMaterial = Phys.NoFriction;
            go.AddComponent<PlayerStats>();
            go.AddComponent<PurifierDevice>();
            var pc = go.AddComponent<PlayerController>();
            pc.respawnPoint = pos;
            pc.lastSafe = pos;
            return pc;
        }

        void Awake()
        {
            Instance = this;
            Rb = GetComponent<Rigidbody2D>();
            Col = GetComponent<BoxCollider2D>();
            Stats = GetComponent<PlayerStats>();
            Device = GetComponent<PurifierDevice>();

            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            sr = visual.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Art.HeroIdle;
            sr.sortingOrder = 10;

            weapon = new GameObject("Rebar").transform;
            weapon.SetParent(visual, false);
            weapon.localPosition = new Vector3(0.3f, 0.85f, 0f);
            var wsr = weapon.gameObject.AddComponent<SpriteRenderer>();
            wsr.sprite = Art.Rebar;
            wsr.sortingOrder = 11;
            weapon.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dead)
            {
                deadTimer -= dt;
                if (deadTimer <= 0) Respawn();
                return;
            }

            Grounded = CheckGround();
            if (Grounded) coyote = coyoteTime; else coyote -= dt;
            if (Grounded && !wasGrounded) Effects.Dust(transform.position, 4);
            wasGrounded = Grounded;
            if (Grounded && hurtTimer <= 0) lastSafe = transform.position;

            invuln -= dt; hurtTimer -= dt; attackTimer -= dt; attackAnim -= dt;

            bool canAct = !GameState.InputBlocked && hurtTimer <= 0 && bandageTimer <= 0;
            float x = canAct ? GameInput.MoveX : 0f;
            if (x != 0) Facing = x > 0 ? 1 : -1;

            var v = Rb.linearVelocity;
            if (hurtTimer <= 0) v.x = x * moveSpeed;

            if (canAct && GameInput.JumpPressed) buffer = jumpBuffer; else buffer -= dt;
            if (buffer > 0 && coyote > 0)
            {
                v.y = jumpSpeed;
                buffer = 0; coyote = 0;
                Sfx.Play(Sfx.Jump, 0.35f);
            }
            // короткий прыжок, если отпустить кнопку
            if (v.y > 0 && !Grounded && !GameInput.JumpHeld && hurtTimer <= 0)
                v.y = Mathf.Min(v.y, jumpSpeed * 0.45f);
            Rb.linearVelocity = v;

            if (canAct && GameInput.AttackPressed && attackTimer <= 0) DoAttack();
            if (canAct && GameInput.MedkitPressed) TryUseMedkit();

            if (bandageTimer > 0)
            {
                bandageTimer -= dt;
                if (bandageTimer <= 0)
                {
                    Stats.Heal(Stats.medkitHeal);
                    Hud.ShowToast("+" + Stats.medkitHeal + " HP", 1.5f);
                }
            }

            Device.SetInput(canAct && GameInput.DeviceHeld, canAct && GameInput.SwitchModePressed);

            Focused = canAct ? Interactable.Nearest(transform.position) : null;
            if (Focused != null && canAct && GameInput.InteractPressed) Focused.Interact(this);

            if (transform.position.y < -20f) HazardHit();

            Animate(dt, x);
        }

        bool CheckGround()
        {
            var b = Col.bounds;
            var center = new Vector2(b.center.x, b.min.y - 0.04f);
            return Phys.BoxTouchesSolid(center, new Vector2(b.size.x * 0.9f, 0.08f), Col);
        }

        void DoAttack()
        {
            attackTimer = attackCooldown;
            attackAnim = attackDuration;
            Sfx.Play(Sfx.Swing, 0.5f);
            var center = (Vector2)transform.position + new Vector2(Facing * 0.85f, 0.85f);
            var hits = new HashSet<IDamageable>();
            foreach (var h in Physics2D.OverlapBoxAll(center, new Vector2(1.3f, 1.2f), 0f))
            {
                if (h.attachedRigidbody == Rb) continue;
                var d = h.GetComponentInParent<IDamageable>();
                if (d != null && hits.Add(d)) d.TakeDamage(meleeDamage, transform.position, DamageKind.Melee);
            }
            if (hits.Count > 0)
            {
                Sfx.Play(Sfx.Hit, 0.6f);
                CameraFollow.Shake(0.08f, 0.06f);
            }
        }

        void TryUseMedkit()
        {
            if (Stats.medkits <= 0) { Hud.ShowToast("Нет аптечек", 1.5f); return; }
            if (Stats.hp >= Stats.maxHp) { Hud.ShowToast("Здоровье полное", 1.5f); return; }
            if (!Grounded) return;
            Stats.medkits--;
            bandageTimer = 0.9f;
            Sfx.Play(Sfx.Rustle, 0.9f);
        }

        /// <summary>Урон герою. Всегда 1 HP за удар (по плану), неуязвимость после попадания.</summary>
        public void Hurt(int dmg, Vector2 from)
        {
            if (dead || invuln > 0) return;
            if (bandageTimer > 0) { bandageTimer = 0; Stats.medkits++; } // перевязку прервали — аптечка не потрачена
            Stats.hp -= dmg;
            invuln = 1.0f;
            hurtTimer = 0.35f;
            float dir = Mathf.Sign(transform.position.x - from.x);
            if (dir == 0) dir = -Facing;
            Rb.linearVelocity = new Vector2(dir * 5f, 5f);
            Effects.Blood(transform.position + Vector3.up * 0.8f, 10);
            Sfx.Play(Sfx.Hurt, 0.7f);
            CameraFollow.Shake(0.18f, 0.15f);
            if (Stats.hp <= 0) Die();
        }

        /// <summary>Падение в токсичную жижу или за пределы мира: урон и возврат на безопасную точку.</summary>
        public void HazardHit()
        {
            if (dead) return;
            invuln = 0;
            Hurt(1, transform.position + Vector3.down);
            if (!dead)
            {
                transform.position = lastSafe;
                Rb.linearVelocity = Vector2.zero;
            }
        }

        void Die()
        {
            Stats.hp = 0;
            dead = true;
            deadTimer = 2.5f;
            Rb.linearVelocity = Vector2.zero;
            Rb.simulated = false;
            Effects.Blood(transform.position + Vector3.up * 0.7f, 24);
            visual.gameObject.SetActive(false);
            Device.ForceStop();
            Hud.ShowBanner("ТЫ ПОГИБ", 2.3f);
        }

        void Respawn()
        {
            dead = false;
            transform.position = respawnPoint;
            lastSafe = respawnPoint;
            Rb.simulated = true;
            Rb.linearVelocity = Vector2.zero;
            visual.gameObject.SetActive(true);
            Stats.hp = Stats.maxHp;
            Stats.displayHp = Stats.maxHp;
            invuln = 1.5f;
        }

        void Animate(float dt, float x)
        {
            animTime += dt;
            Sprite s;
            if (hurtTimer > 0) s = Art.HeroHurt;
            else if (bandageTimer > 0) s = Art.HeroBandage;
            else if (attackAnim > 0) s = Art.HeroAttack;
            else if (!Grounded) s = Art.HeroJump;
            else if (Mathf.Abs(x) > 0.01f) s = ((int)(animTime * 9f) % 2 == 0) ? Art.HeroRunA : Art.HeroRunB;
            else s = Art.HeroIdle;
            sr.sprite = s;

            // «сжатие» при уроне, лёгкое дыхание в покое
            Vector3 target = Vector3.one;
            if (hurtTimer > 0) target = new Vector3(1.12f, 0.85f, 1f);
            else if (Grounded && Mathf.Abs(x) < 0.01f) target = new Vector3(1f, 1f + Mathf.Sin(animTime * 2.5f) * 0.015f, 1f);
            squash = Vector3.Lerp(squash, target, dt * 20f);
            visual.localScale = new Vector3(squash.x * Facing, squash.y, 1f);

            // мигание во время неуязвимости
            sr.enabled = invuln <= 0 || hurtTimer > 0 || ((int)(invuln * 15f) % 2 == 0);

            // взмах арматурой
            if (attackAnim > 0)
            {
                weapon.gameObject.SetActive(true);
                float t = 1f - attackAnim / attackDuration;
                weapon.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(70f, -110f, t));
            }
            else weapon.gameObject.SetActive(false);
        }
    }
}
