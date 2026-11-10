using UnityEngine;

namespace ToxicWasteland
{
    public enum PickupKind { Medkit, Scrap }

    /// <summary>Подбираемые предметы: аптечки (просто лежат, без анимации) и лом (валюта торговца).</summary>
    public class Pickup : MonoBehaviour
    {
        public PickupKind kind;
        public int amount = 1;
        Vector2 vel;
        bool settled = true;
        float delay, toastCd;

        public static Pickup Spawn(PickupKind kind, Vector2 pos, int amount = 1)
        {
            var go = new GameObject("Pickup_" + kind);
            go.transform.position = pos;
            var p = go.AddComponent<Pickup>();
            p.kind = kind;
            p.amount = amount;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = kind == PickupKind.Medkit ? Art.Medkit : Art.Scrap;
            sr.sortingOrder = 5;
            return p;
        }

        /// <summary>Лом, разлетающийся из поверженного врага.</summary>
        public static void SpawnScrap(Vector2 pos, int total)
        {
            int pieces = Mathf.Clamp(total / 2, 1, 4);
            int left = total;
            for (int i = 0; i < pieces; i++)
            {
                int a = i == pieces - 1 ? left : Mathf.Max(1, total / pieces);
                left -= a;
                var p = Spawn(PickupKind.Scrap, pos, a);
                p.settled = false;
                p.delay = 0.4f;
                p.vel = new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(3f, 6f));
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            delay -= dt; toastCd -= dt;
            Vector2 pos = transform.position;
            if (!settled)
            {
                vel.y -= 20f * dt;
                if (vel.y < 0 && Phys.RaycastSolid(pos, Vector2.down, -vel.y * dt + 0.05f, out var hit))
                {
                    pos = hit.point;
                    settled = true;
                }
                else
                {
                    if (Phys.PointInSolid(pos + new Vector2(vel.x * dt, 0))) vel.x = 0;
                    pos += vel * dt;
                }
                transform.position = pos;
            }

            var pl = PlayerController.Instance;
            if (pl == null || !pl.IsAlive) return;
            Vector2 pp = (Vector2)pl.transform.position + Vector2.up * 0.6f;

            if (kind == PickupKind.Scrap && delay <= 0)
            {
                float d = Vector2.Distance(pp, pos);
                if (d < 2.2f) transform.position = Vector2.MoveTowards(pos, pp, dt * 9f);
                if (d < 0.45f)
                {
                    pl.Stats.scrap += amount;
                    Sfx.Play(Sfx.Pickup, 0.25f);
                    Destroy(gameObject);
                }
            }
            else if (kind == PickupKind.Medkit)
            {
                if (Mathf.Abs(pl.transform.position.x - pos.x) < 0.6f && Mathf.Abs(pl.transform.position.y - pos.y) < 1.2f)
                {
                    if (pl.Stats.medkits < PlayerStats.MaxMedkits)
                    {
                        pl.Stats.medkits++;
                        Sfx.Play(Sfx.Pickup, 0.5f);
                        Hud.ShowToast("Аптечка (" + pl.Stats.medkits + "/" + PlayerStats.MaxMedkits + ")  [H] — использовать", 2.5f);
                        Destroy(gameObject);
                    }
                    else if (toastCd <= 0)
                    {
                        toastCd = 3f;
                        Hud.ShowToast("Больше 3 аптечек не унести", 2f);
                    }
                }
            }
        }
    }
}
