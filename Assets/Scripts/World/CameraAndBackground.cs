using System.Collections.Generic;
using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>Камера следует за героем с небольшим упреждением и тряской.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float levelMinX = -11f, levelMaxX = 113f, minY = 1.5f;
        static float shakeT, shakeAmp;
        Camera cam;
        Vector3 basePos, vel;
        float look;

        public static void Shake(float time, float amp)
        {
            shakeT = Mathf.Max(shakeT, time);
            shakeAmp = Mathf.Max(shakeAmp, amp);
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            basePos = transform.position;
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            basePos = Desired(0f);
            transform.position = basePos;
        }

        Vector3 Desired(float lookAhead)
        {
            var p = target.position;
            float halfW = cam.orthographicSize * cam.aspect;
            float x = Mathf.Clamp(p.x + lookAhead, levelMinX + halfW, levelMaxX - halfW);
            float y = Mathf.Max(p.y + 1.2f, minY);
            return new Vector3(x, y, -10f);
        }

        void LateUpdate()
        {
            if (target == null) return;
            var pc = PlayerController.Instance;
            look = Mathf.MoveTowards(look, pc != null ? pc.Facing * 1.5f : 0f, Time.deltaTime * 3f);
            basePos = Vector3.SmoothDamp(basePos, Desired(look), ref vel, 0.15f);
            Vector3 offset = Vector3.zero;
            if (shakeT > 0)
            {
                shakeT -= Time.deltaTime;
                offset = (Vector3)(Random.insideUnitCircle * shakeAmp);
                if (shakeT <= 0) shakeAmp = 0;
            }
            transform.position = basePos + offset;
            ParallaxLayer.RefreshAll(basePos);
        }
    }

    /// <summary>Слой фона, смещающийся медленнее камеры.</summary>
    public class ParallaxLayer : MonoBehaviour
    {
        static readonly List<ParallaxLayer> layers = new List<ParallaxLayer>();
        public float factorX, factorY;
        Vector3 start;

        public static ParallaxLayer Create(string name, Sprite sprite, Vector3 start, float factorX, float factorY,
            int order, Color color, Vector2? tiledSize = null, Vector3? scale = null)
        {
            var go = new GameObject(name);
            go.transform.position = start;
            if (scale.HasValue) go.transform.localScale = scale.Value;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = color;
            if (tiledSize.HasValue)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = tiledSize.Value;
            }
            var l = go.AddComponent<ParallaxLayer>();
            l.start = start;
            l.factorX = factorX;
            l.factorY = factorY;
            return l;
        }

        void OnEnable() { layers.Add(this); }
        void OnDisable() { layers.Remove(this); }

        public static void RefreshAll(Vector3 cam)
        {
            foreach (var l in layers)
                l.transform.position = new Vector3(l.start.x + cam.x * l.factorX, l.start.y + cam.y * l.factorY, l.start.z);
        }
    }

    /// <summary>Шторм: летящий мусор и пыль, вспышки молний и гром.</summary>
    public class Storm : MonoBehaviour
    {
        public static float Flash;
        struct Streak { public Transform tr; public Vector2 vel; }
        readonly List<Streak> streaks = new List<Streak>();
        Camera cam;
        float nextLightning = 7f, thunderAt = -1f;

        void Start()
        {
            cam = GetComponent<Camera>();
            for (int i = 0; i < 45; i++)
            {
                var go = new GameObject("WindStreak");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PixelArt.Unit;
                sr.sortingOrder = 30;
                sr.color = new Color(0.75f, 0.8f, 0.6f, Random.Range(0.06f, 0.18f));
                go.transform.localScale = new Vector3(Random.Range(0.4f, 1.1f), 0.04f, 1);
                go.transform.rotation = Quaternion.Euler(0, 0, -12f);
                var s = new Streak { tr = go.transform, vel = new Vector2(Random.Range(10f, 18f), Random.Range(-3.5f, -2f)) };
                streaks.Add(s);
                Place(s, true);
            }
        }

        void Place(Streak s, bool anywhere)
        {
            float h = cam.orthographicSize, w = h * cam.aspect;
            var c = cam.transform.position;
            float x = anywhere ? Random.Range(c.x - w, c.x + w) : c.x - w - Random.Range(0.5f, 3f);
            float y = Random.Range(c.y - h, c.y + h + 2f);
            s.tr.position = new Vector3(x, y, 0);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float h = cam.orthographicSize, w = h * cam.aspect;
            var c = cam.transform.position;
            foreach (var s in streaks)
            {
                s.tr.position += (Vector3)(s.vel * dt);
                var p = s.tr.position;
                if (p.x > c.x + w + 1f || p.y < c.y - h - 1f || p.x < c.x - w - 4f) Place(s, false);
            }

            Flash = Mathf.MoveTowards(Flash, 0, dt * 2.5f);
            nextLightning -= dt;
            if (nextLightning <= 0)
            {
                nextLightning = Random.Range(9f, 18f);
                Flash = Random.Range(0.5f, 0.8f);
                thunderAt = Time.time + Random.Range(0.3f, 0.9f);
            }
            if (thunderAt > 0 && Time.time >= thunderAt)
            {
                thunderAt = -1;
                Sfx.Play(Sfx.Thunder, 0.6f);
                CameraFollow.Shake(0.4f, 0.04f);
            }
        }
    }
}
