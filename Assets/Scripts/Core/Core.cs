using UnityEngine;

namespace ToxicWasteland
{
    public enum DamageKind { Melee, Clean, Toxic }

    /// <summary>Всё, что можно ударить арматурой или обжечь лучом.</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector2 from, DamageKind kind);
    }

    /// <summary>Всё, что убирается режимом «Очистка» (лужи, токсичные завесы).</summary>
    public interface ICleanable
    {
        void Clean(float amount);
    }

    /// <summary>Метка твёрдой поверхности уровня (земля, стены, платформы, ворота).</summary>
    public class Solid : MonoBehaviour { }

    public static class GameState
    {
        /// <summary>Открыт диалог или магазин.</summary>
        public static bool UIOpen;
        /// <summary>Идёт вступительная заставка.</summary>
        public static bool Intro;
        /// <summary>Кадр, в котором закрылось последнее окно (чтобы нажатие не "протекло" в игру).</summary>
        public static int LastUIFrame = -1;

        public static bool InputBlocked => UIOpen || Intro || Time.frameCount <= LastUIFrame;
    }

    public static class Phys
    {
        static PhysicsMaterial2D noFriction;

        public static PhysicsMaterial2D NoFriction
        {
            get
            {
                if (noFriction == null)
                {
                    noFriction = new PhysicsMaterial2D("NoFriction");
                    noFriction.friction = 0f;
                    noFriction.bounciness = 0f;
                }
                return noFriction;
            }
        }

        public static bool IsSolid(Collider2D c) => c != null && !c.isTrigger && c.GetComponent<Solid>() != null;

        public static bool RaycastSolid(Vector2 origin, Vector2 dir, float dist, out RaycastHit2D hit)
        {
            foreach (var h in Physics2D.RaycastAll(origin, dir, dist))
            {
                if (IsSolid(h.collider)) { hit = h; return true; }
            }
            hit = default;
            return false;
        }

        public static bool PointInSolid(Vector2 p)
        {
            foreach (var c in Physics2D.OverlapPointAll(p)) if (IsSolid(c)) return true;
            return false;
        }

        public static bool BoxTouchesSolid(Vector2 center, Vector2 size, Collider2D ignore)
        {
            foreach (var c in Physics2D.OverlapBoxAll(center, size, 0f))
                if (c != ignore && IsSolid(c)) return true;
            return false;
        }

        public static bool Overlap2D(Bounds a, Bounds b) =>
            a.min.x < b.max.x && a.max.x > b.min.x && a.min.y < b.max.y && a.max.y > b.min.y;

        public static bool Contains2D(Bounds b, Vector2 p) =>
            p.x >= b.min.x && p.x <= b.max.x && p.y >= b.min.y && p.y <= b.max.y;
    }
}
