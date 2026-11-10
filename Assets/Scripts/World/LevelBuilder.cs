using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>
    /// Собирает первую (демо) локацию «Руины города» прямо из кода.
    /// Земля на уровне y = 0, 1 единица = 1 метр.
    /// </summary>
    public static class LevelBuilder
    {
        public const float MinX = -12f, MaxX = 113f;

        public static PlayerController Build()
        {
            var root = new GameObject("Level").transform;

            // ── Подвал (старт) ──
            Block(root, -12, -4, 50, 4);              // земля 1: x -12..38
            Block(root, -12, 0, 1, 14, false);        // левая стена
            Block(root, -11, 4.2f, 13, 1, false);     // потолок подвала
            Block(root, 2, 4.2f, 0.6f, 0.4f, false);  // обломок
            Checkpoint.Spawn(new Vector2(-9.5f, 0), true);
            DiaryPickup.Spawn(new Vector2(-5f, 0));

            // ── Улица: первые платформы ──
            Platform(root, 8, 2, 3);
            Platform(root, 13, 3.5f, 3);
            Pickup.Spawn(PickupKind.Medkit, new Vector2(14.5f, 3.5f));
            Enemy.Spawn(EnemyType.Crawler, new Vector2(18, 0), 3);
            Pickup.Spawn(PickupKind.Scrap, new Vector2(10, 0), 4);

            // ── Марта ──
            StoryNpc.Spawn("Марта", new Vector2(25, 0), Art.Marta, Story.Marta);

            // ── Яма с жижей ──
            Block(root, 38, -4, 4, 1, false);
            SludgePool.Spawn(38, -3, 4, 1.2f);
            Pickup.Spawn(PickupKind.Scrap, new Vector2(33, 0), 5);

            // ── Земля 2 ──
            Block(root, 42, -4, 40, 4);               // x 42..82
            Enemy.Spawn(EnemyType.Mutant, new Vector2(48, 0), 3);
            Enemy.Spawn(EnemyType.Crawler, new Vector2(53, 0), 2);
            Enemy.Spawn(EnemyType.Mutant, new Vector2(57, 0), 2);
            Platform(root, 50, 2.6f, 2.5f);
            Pickup.Spawn(PickupKind.Scrap, new Vector2(51.2f, 2.6f), 6);

            // ── Лагерь торговца ──
            Checkpoint.Spawn(new Vector2(61, 0));
            Merchant.Spawn(new Vector2(64, 0), new Color32(78, 92, 70, 255), "Старьёвщик", Story.MerchantGreeting);

            // ── Уступ с плевуном ──
            Block(root, 69, 0, 6, 2.4f);
            Enemy.Spawn(EnemyType.Spitter, new Vector2(73, 2.4f), 0);
            Pickup.Spawn(PickupKind.Medkit, new Vector2(67, 0));
            Enemy.Spawn(EnemyType.Mutant, new Vector2(79, 0), 2);

            // ── Земля 3: головоломка ──
            Block(root, 82, -4, 31, 4);               // x 82..113
            Block(root, 112, 0, 1, 14, false);        // правая стена
            var gate = Gate.Spawn(93.5f, 0, 1f, 6f);
            Block(root, 91, 6, 22, 1, false);         // перекрытие над воротами
            ToxicBarrier.Spawn(87, 0, 0.8f, 4.2f);
            Lever.Spawn(new Vector2(89.5f, 0), gate);
            Pickup.Spawn(PickupKind.Scrap, new Vector2(90.5f, 0), 6);

            // ── Конец демо ──
            Pickup.Spawn(PickupKind.Scrap, new Vector2(100, 0), 8);
            EndZone.Spawn(104);

            BuildBackground();

            return PlayerController.Create(new Vector2(-8f, 0.05f));
        }

        static GameObject Block(Transform root, float x, float y, float w, float h, bool crust = true)
        {
            var go = new GameObject("Block");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(x + w / 2f, y + h / 2f, 0);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, h);
            go.AddComponent<Solid>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Rock;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(w, h);
            sr.sortingOrder = 0;
            if (crust)
            {
                var top = new GameObject("Crust");
                top.transform.SetParent(go.transform, false);
                top.transform.localPosition = new Vector3(0, h / 2f - 0.08f, 0);
                var tsr = top.AddComponent<SpriteRenderer>();
                tsr.sprite = Art.Crust;
                tsr.drawMode = SpriteDrawMode.Tiled;
                tsr.size = new Vector2(w, 4f / PixelArt.PPU);
                tsr.sortingOrder = 1;
            }
            return go;
        }

        static void Platform(Transform root, float x, float y, float w)
        {
            const float h = 0.5f;
            var go = new GameObject("Platform");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(x + w / 2f, y - h / 2f, 0);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, h);
            go.AddComponent<Solid>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Girder;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(w, h);
            sr.sortingOrder = 0;
        }

        static void BuildBackground()
        {
            float width = (MaxX - MinX) + 60f;
            float cx = (MinX + MaxX) / 2f;
            // небо — всегда за камерой
            ParallaxLayer.Create("Sky", Art.Sky, new Vector3(0, 0, 0), 1f, 1f, -100, Color.white, null, new Vector3(160f, 1f, 1f));
            // дальние руины
            var far = Art.Ruins("ruins_far", 11, PixelArt.C(40, 46, 40), PixelArt.C(170, 200, 90), 30, 95);
            ParallaxLayer.Create("RuinsFar", far, new Vector3(cx * 0.2f, -1.5f, 0), 0.8f, 0.9f, -90, Color.white, new Vector2(width, far.bounds.size.y));
            // ближние руины
            var near = Art.Ruins("ruins_near", 29, PixelArt.C(26, 28, 27), PixelArt.C(220, 170, 80), 20, 70);
            ParallaxLayer.Create("RuinsNear", near, new Vector3(cx * 0.45f, -2.5f, 0), 0.55f, 0.75f, -80, Color.white, new Vector2(width, near.bounds.size.y));
            // токсичный туман
            ParallaxLayer.Create("Fog", PixelArt.SoftCircle(32), new Vector3(cx * 0.3f, 0.5f, 0), 0.7f, 0.9f, -70,
                new Color(0.45f, 0.6f, 0.2f, 0.18f), null, new Vector3(width, 3f, 1f));
        }
    }
}
