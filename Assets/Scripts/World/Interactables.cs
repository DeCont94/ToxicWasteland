using System.Collections.Generic;
using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>Объект, с которым можно взаимодействовать клавишей E.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public float range = 1.4f;
        public string prompt = "Говорить";
        public float promptHeight = 2.1f;

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        public abstract void Interact(PlayerController player);

        public Vector3 PromptPos => transform.position + Vector3.up * promptHeight;

        public static Interactable Nearest(Vector2 p)
        {
            Interactable best = null;
            float bestD = float.MaxValue;
            foreach (var i in All)
            {
                float dx = Mathf.Abs(i.transform.position.x - p.x);
                float dy = Mathf.Abs(i.transform.position.y - p.y);
                if (dx > i.range || dy > 2f) continue;
                if (dx < bestD) { bestD = dx; best = i; }
            }
            return best;
        }
    }

    /// <summary>
    /// Сюжетный NPC: в основном стоит, но слегка «дышит» и переминается в пределах пары шагов.
    /// </summary>
    public class StoryNpc : Interactable
    {
        public string npcName;
        public System.Func<Dialogue> dialogue;
        Transform visual;
        SpriteRenderer sr;
        float t, homeX, targetX, wait = 2f;
        int facing = -1;

        public static StoryNpc Spawn(string name, Vector2 pos, Sprite sprite, System.Func<Dialogue> dialogue)
        {
            var go = new GameObject("NPC_" + name);
            go.transform.position = pos;
            var n = go.AddComponent<StoryNpc>();
            n.npcName = name;
            n.dialogue = dialogue;
            n.homeX = n.targetX = pos.x;
            n.visual = new GameObject("Visual").transform;
            n.visual.SetParent(go.transform, false);
            n.sr = n.visual.gameObject.AddComponent<SpriteRenderer>();
            n.sr.sprite = sprite;
            n.sr.sortingOrder = 8;
            n.promptHeight = sprite.bounds.size.y + 0.4f;
            return n;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            var p = PlayerController.Instance;
            bool talking = DialogueUI.IsOpen;
            bool near = p != null && Mathf.Abs(p.transform.position.x - transform.position.x) < 4f;

            if (!talking && !near)
            {
                wait -= dt;
                if (wait <= 0)
                {
                    wait = Random.Range(3f, 6f);
                    targetX = homeX + Random.Range(-0.8f, 0.8f);
                }
                float x = Mathf.MoveTowards(transform.position.x, targetX, dt * 0.6f);
                if (Mathf.Abs(x - transform.position.x) > 0.0001f) facing = x > transform.position.x ? 1 : -1;
                transform.position = new Vector3(x, transform.position.y, 0);
            }
            else if (p != null)
            {
                facing = p.transform.position.x > transform.position.x ? 1 : -1;
            }

            visual.localScale = new Vector3(facing, 1f + Mathf.Sin(t * 2f) * 0.02f, 1f);
        }

        public override void Interact(PlayerController player)
        {
            if (dialogue != null) DialogueUI.Open(dialogue(), transform, player.transform);
        }
    }

    /// <summary>Торговец улучшениями. Встречается в каждой локации, внешность зависит от локации.</summary>
    public class Merchant : Interactable
    {
        public string merchantName = "Старьёвщик";
        public string greeting;
        Transform visual;
        SpriteRenderer sr;
        Sprite[] frames;
        float t;

        public static Merchant Spawn(Vector2 pos, Color32 hood, string name, string greeting)
        {
            var go = new GameObject("Merchant");
            go.transform.position = pos;
            var m = go.AddComponent<Merchant>();
            m.merchantName = name;
            m.greeting = greeting;
            m.prompt = "Торговать";
            m.frames = Art.MerchantFrames(hood);
            m.visual = new GameObject("Visual").transform;
            m.visual.SetParent(go.transform, false);
            m.sr = m.visual.gameObject.AddComponent<SpriteRenderer>();
            m.sr.sprite = m.frames[0];
            m.sr.sortingOrder = 8;
            m.promptHeight = 2.0f;
            return m;
        }

        void Update()
        {
            t += Time.deltaTime;
            // мигающие очки и лёгкое покачивание
            sr.sprite = frames[(Mathf.Sin(t * 1.3f) > 0.85f) ? 1 : 0];
            var p = PlayerController.Instance;
            int facing = p != null && p.transform.position.x > transform.position.x ? 1 : -1;
            visual.localScale = new Vector3(facing, 1f + Mathf.Sin(t * 1.7f) * 0.015f, 1f);
        }

        public override void Interact(PlayerController player)
        {
            ShopUI.Open(merchantName, greeting, BuildItems(player.Stats, player.Device));
        }

        static List<ShopItem> BuildItems(PlayerStats s, PurifierDevice dev)
        {
            return new List<ShopItem>
            {
                new ShopItem
                {
                    name = "Аптечка",
                    desc = "Бинты и антисептик. Лечит " + s.medkitHeal + " HP. Носить можно до 3 шт.",
                    price = () => 8,
                    available = () => s.medkits < PlayerStats.MaxMedkits,
                    buy = () => s.medkits++,
                },
                new ShopItem
                {
                    name = "Стерильные бинты",
                    desc = "Аптечки лечат 2 HP вместо 1.",
                    price = () => 30,
                    available = () => !s.bandageUpgrade,
                    buy = () => { s.bandageUpgrade = true; s.medkitHeal = 2; },
                },
                new ShopItem
                {
                    name = "Закалка",
                    desc = "+1 к максимальному здоровью (максимум 10).",
                    price = () => 20 + 10 * s.hpUpgrades,
                    available = () => s.maxHp < PlayerStats.HardMaxHp,
                    buy = () => { s.hpUpgrades++; s.maxHp++; s.hp++; },
                },
                new ShopItem
                {
                    name = "Батарея из старых аккумуляторов",
                    desc = "Ёмкость устройства +25%.",
                    price = () => 25 + 10 * s.batteryUpgrades,
                    available = () => s.batteryUpgrades < 3,
                    buy = () => { s.batteryUpgrades++; s.deviceCapacity += 25f; dev.Refill(); },
                },
            };
        }
    }

    /// <summary>Дневник деда и устройство в подвале — начало истории.</summary>
    public class DiaryPickup : Interactable
    {
        public static DiaryPickup Spawn(Vector2 pos)
        {
            var go = new GameObject("Diary");
            go.transform.position = pos;
            var d = go.AddComponent<DiaryPickup>();
            d.prompt = "Осмотреть";
            d.promptHeight = 1.0f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Diary;
            sr.sortingOrder = 5;
            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0.3f, 0.2f, 0);
            var gsr = glow.AddComponent<SpriteRenderer>();
            gsr.sprite = PixelArt.SoftCircle(20);
            gsr.color = new Color(0.5f, 0.8f, 1f, 0.35f);
            gsr.sortingOrder = 4;
            return d;
        }

        public override void Interact(PlayerController player)
        {
            var d = Story.GrandfatherDiary();
            d.onEnd = () =>
            {
                player.Device.unlocked = true;
                player.Device.Refill();
                Sfx.Play(Sfx.Cleanse, 0.6f);
                Hud.ShowToast("Получено: устройство деда", 4f);
                Hud.ShowToast("[ПКМ] или [K] — использовать,  [Q] или [СКМ] — сменить режим", 7f);
                Destroy(gameObject);
            };
            DialogueUI.Open(d, transform, player.transform);
        }
    }
}
