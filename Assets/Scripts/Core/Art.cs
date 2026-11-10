using System.Collections.Generic;
using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>
    /// Весь временный пиксельный арт игры. Персонажи нарисованы «текстом»:
    /// каждый символ — пиксель, цвет берётся из палитры. Все смотрят вправо.
    /// </summary>
    public static class Art
    {
        static Color32 C(int r, int g, int b, int a = 255) => PixelArt.C(r, g, b, a);
        static Vector2 BC => PixelArt.BottomCenter;

        static string[] Join(params string[][] parts)
        {
            var list = new List<string>();
            foreach (var p in parts) list.AddRange(p);
            return list.ToArray();
        }

        static string[] Replace(string[] rows, char from, char to)
        {
            var r = new string[rows.Length];
            for (int i = 0; i < rows.Length; i++) r[i] = rows[i].Replace(from, to);
            return r;
        }

        // ───────────────────────── Герой ─────────────────────────
        static readonly Dictionary<char, Color32> HeroPal = new Dictionary<char, Color32>
        {
            { 'h', C(58, 42, 32) },    // волосы
            { 's', C(198, 152, 120) }, // кожа
            { 'S', C(150, 108, 84) },  // кожа в тени
            { 'g', C(140, 220, 70) },  // очки-фильтры (токсичный акцент)
            { 'w', C(128, 40, 36) },   // шарф
            { 'c', C(96, 82, 62) },    // плащ
            { 'C', C(64, 54, 44) },    // плащ в тени
            { 'd', C(90, 175, 255) },  // устройство на поясе
            { 'p', C(54, 58, 70) },    // штаны
            { 'b', C(32, 28, 26) },    // ботинки
            { 'W', C(232, 226, 210) }, // бинт
        };

        static readonly string[] HeroTop =
        {
            ".....hhhh.....",
            "....hhhhhh....",
            "....hhssss....",
            "....hsgggg....",
            "....hsssss....",
            ".....SSss.....",
            "....wwwwww....",
            "...ccwwcccc...",
            "..cCcccccdcc..",
            "..cCcccccdcc..",
            "..sCcccccccs..",
            "..s.cccccc.s..",
            "....cccccc....",
        };

        static readonly string[] HeroTopAttack =
        {
            ".....hhhh.....",
            "....hhhhhh....",
            "....hhssss....",
            "....hsgggg....",
            "....hsssss....",
            ".....SSss.....",
            "....wwwwww....",
            "...ccwwccccss.",
            "..cCcccccdcss.",
            "..cCcccccdc...",
            "..sCcccccc....",
            "..s.cccccc....",
            "....cccccc....",
        };

        static readonly string[] HeroTopBandage =
        {
            ".....hhhh.....",
            "....hhhhhh....",
            "....hhssss....",
            "....hsgggg....",
            "....hsssss....",
            ".....SSss.....",
            "....wwwwww....",
            "...ccwwcccc...",
            "..cWWccsscc...",
            "..cWWcsssdc...",
            "..sCcccccc....",
            "....cccccc....",
            "....cccccc....",
        };

        static readonly string[] LegsIdle =
        {
            "....pppppp....",
            "....pp..pp....",
            "....pp..pp....",
            "....pp..pp....",
            "....pp..pp....",
            "...bbb..bbb...",
            "...bbb..bbb...",
        };

        static readonly string[] LegsRunA =
        {
            "....pppppp....",
            "...ppp..pp....",
            "..ppp....pp...",
            "..pp.....pp...",
            ".bpp......pp..",
            ".bb.......bbb.",
            "..........bbb.",
        };

        static readonly string[] LegsRunB =
        {
            "....pppppp....",
            "....pp.pp.....",
            ".....ppp......",
            ".....pp.......",
            "....ppp.......",
            "....bbbb......",
            "....bbbb......",
        };

        static readonly string[] LegsJump =
        {
            "....pppppp....",
            "...pp...pp....",
            "..pp.....pp...",
            "..bb.....pp...",
            "..bb.....bb...",
            "..........bb..",
            "..............",
        };

        static readonly string[] HeroHurtRows =
        {
            "..............",
            "..............",
            "......hhhh....",
            ".....hhhhhh...",
            ".....hssssh...",
            ".....sgggg....",
            "....wwsss.....",
            "...ccwwccc....",
            "..cCccccccc...",
            "..cCcsssscc...",
            "..cCcsssscc...",
            "...ccccccc....",
            "....cccccc....",
            "....pppppp....",
            "....pp..pp....",
            "...pp....pp...",
            "...pp....pp...",
            "...pp....pp...",
            "..bbb....bbb..",
            "..bbb....bbb..",
        };

        public static Sprite HeroIdle => PixelArt.FromAscii("hero_idle", Join(HeroTop, LegsIdle), HeroPal, BC);
        public static Sprite HeroRunA => PixelArt.FromAscii("hero_runA", Join(HeroTop, LegsRunA), HeroPal, BC);
        public static Sprite HeroRunB => PixelArt.FromAscii("hero_runB", Join(HeroTop, LegsRunB), HeroPal, BC);
        public static Sprite HeroJump => PixelArt.FromAscii("hero_jump", Join(HeroTop, LegsJump), HeroPal, BC);
        public static Sprite HeroAttack => PixelArt.FromAscii("hero_attack", Join(HeroTopAttack, LegsIdle), HeroPal, BC);
        public static Sprite HeroBandage => PixelArt.FromAscii("hero_bandage", Join(HeroTopBandage, LegsIdle), HeroPal, BC);
        public static Sprite HeroHurt => PixelArt.FromAscii("hero_hurt", HeroHurtRows, HeroPal, BC);

        /// <summary>Арматура — стартовое оружие ближнего боя. Опора у рукояти.</summary>
        public static Sprite Rebar => PixelArt.FromAscii("rebar", new[]
        {
            ".r",
            "rR",
            "Rr",
            "rR",
            "Rr",
            "rR",
            "Rr",
            "rR",
            "Rr",
            "rR",
            "Rr",
            "rR",
            "gg",
            "gg",
        }, new Dictionary<char, Color32>
        {
            { 'r', C(128, 84, 60) }, { 'R', C(92, 60, 46) }, { 'g', C(60, 60, 64) },
        }, new Vector2(0.5f, 0.07f));

        // ───────────────────────── Враги ─────────────────────────
        static readonly Dictionary<char, Color32> MutantPal = new Dictionary<char, Color32>
        {
            { 'm', C(92, 116, 72) }, { 'M', C(62, 80, 52) }, { 'e', C(225, 235, 90) },
            { 'r', C(74, 62, 56) }, { 'R', C(52, 44, 40) },
        };

        static readonly string[] MutantTop =
        {
            ".....mmmm.....",
            "....mmmmmm....",
            "....mmmmem....",
            "....mmmmmm....",
            ".....MmmM.....",
            "...rrmmmmrr...",
            "..rrrrrrrrrr..",
            ".mrRrrrrrrRrm.",
            ".m.RrrrrrrR.m.",
            ".m..rrrrrr..m.",
            "mm..rrrrrr..mm",
            "....RRRRRR....",
        };

        public static Sprite[] MutantFrames => new[]
        {
            PixelArt.FromAscii("mutant_a", Join(MutantTop, new[]
            {
                "....mm..mm....",
                "....mm..mm....",
                "...mmm..mmm...",
            }), MutantPal, BC),
            PixelArt.FromAscii("mutant_b", Join(MutantTop, new[]
            {
                "...mm....mm...",
                "...mm....mm...",
                "..mmm....mmm..",
            }), MutantPal, BC),
        };

        static readonly Dictionary<char, Color32> CrawlerPal = new Dictionary<char, Color32>
        {
            { 'k', C(112, 92, 80) }, { 'K', C(80, 62, 56) }, { 'e', C(235, 64, 40) },
        };

        public static Sprite[] CrawlerFrames => new[]
        {
            PixelArt.FromAscii("crawler_a", new[]
            {
                "....kkkkkk....",
                "..kkKkkkkkkke.",
                ".kkKKkkkkkkkkk",
                "kkkkkkkkkkkkk.",
                ".k.k.k...k.k..",
                "k.k.k...k.k...",
            }, CrawlerPal, BC),
            PixelArt.FromAscii("crawler_b", new[]
            {
                "....kkkkkk....",
                "..kkKkkkkkkke.",
                ".kkKKkkkkkkkkk",
                "kkkkkkkkkkkkk.",
                "k.k.k...k.k...",
                ".k.k.k...k.k..",
            }, CrawlerPal, BC),
        };

        static readonly Dictionary<char, Color32> SpitterPal = new Dictionary<char, Color32>
        {
            { 'v', C(120, 150, 62) }, { 'V', C(80, 110, 42) }, { 'g', C(176, 245, 64) }, { 'e', C(240, 230, 120) },
        };

        static readonly string[] SpitterRows =
        {
            "....vvvv....",
            "...vvvvvv...",
            "...vvvveve..",
            "...vvvvvv...",
            "..vvvvvggv..",
            "..vVvvvvVv..",
            ".vvVvggvVvv.",
            ".vvVggggVvv.",
            ".vvVvggvVvv.",
            ".vvVvvvvVvv.",
            "..vVvvvvVv..",
            "...vvvvvv...",
            "...VV..VV...",
            "...vv..vv...",
            "..vvv..vvv..",
        };

        public static Sprite[] SpitterFrames => new[]
        {
            PixelArt.FromAscii("spitter_a", SpitterRows, SpitterPal, BC),
            PixelArt.FromAscii("spitter_b", Replace(SpitterRows, 'g', 'e'), SpitterPal, BC),
        };

        public static Sprite SpitBall => PixelArt.FromAscii("spit", new[]
        {
            ".gg.",
            "gGGg",
            "gGGg",
            ".gg.",
        }, new Dictionary<char, Color32> { { 'g', C(120, 200, 40) }, { 'G', C(190, 255, 90) } }, PixelArt.Center);

        // ───────────────────────── NPC ─────────────────────────
        public static Sprite Marta => PixelArt.FromAscii("npc_marta", new[]
        {
            "....hhhh....",
            "...hhhhhh...",
            "...hhssss...",
            "...hhsSss...",
            "....ssss....",
            "...aaaaaa...",
            "..aaaaaaaa..",
            "..aAaaaaasa.",
            "..aAaaaaa.k.",
            "..aAaaaaa.k.",
            "..aAaaaaa.k.",
            "..aaaaaaa.k.",
            "..aaaaaaa.k.",
            "...aaaaa..k.",
            "...aaaaa..k.",
            "...bb.bb..k.",
        }, new Dictionary<char, Color32>
        {
            { 'h', C(168, 166, 156) }, { 's', C(190, 150, 124) }, { 'S', C(140, 104, 86) },
            { 'a', C(92, 62, 82) }, { 'A', C(62, 42, 56) }, { 'k', C(110, 80, 50) }, { 'b', C(36, 30, 28) },
        }, BC);

        static readonly string[] MerchantRows =
        {
            ".....uuuu.....",
            "....uuuuuu....",
            "...uuuuuuuu...",
            "...uuuuffff...",
            "...uuuufoof...",
            "...uuuuffff...",
            "...BBuuuuuu...",
            "..BBBuuuuuuu..",
            "..BBBuUuuuuu..",
            "..BBBuUuuusu..",
            "..BBBuUuuuuu..",
            "..BBBuUuuuuu..",
            "...BBuUuuuuu..",
            "....uuuuuuu...",
            "....uuuuuuu...",
            "....pp...pp...",
            "....pp...pp...",
            "...bbb..bbb...",
        };

        /// <summary>Торговец. Цвет капюшона меняется от локации к локации.</summary>
        public static Sprite[] MerchantFrames(Color32 hood)
        {
            string k = hood.r + "_" + hood.g + "_" + hood.b;
            var pal = new Dictionary<char, Color32>
            {
                { 'u', hood }, { 'U', PixelArt.Shade(hood, 0.65f) }, { 'f', C(38, 32, 30) },
                { 'o', C(240, 160, 60) }, { 'O', C(255, 210, 120) }, { 's', C(190, 150, 124) },
                { 'B', C(104, 82, 52) }, { 'p', C(52, 50, 54) }, { 'b', C(30, 26, 24) },
            };
            return new[]
            {
                PixelArt.FromAscii("merchant_a_" + k, MerchantRows, pal, BC),
                PixelArt.FromAscii("merchant_b_" + k, Replace(MerchantRows, 'o', 'O'), pal, BC),
            };
        }

        // ───────────────────────── Предметы ─────────────────────────
        public static Sprite Medkit => PixelArt.FromAscii("medkit", new[]
        {
            "..bbbb..",
            "bBBBBBBb",
            "BBBrrBBB",
            "BBrrrrBB",
            "BBBrrBBB",
            "bBBBBBBb",
        }, new Dictionary<char, Color32>
        {
            { 'B', C(222, 204, 164) }, { 'b', C(176, 156, 118) }, { 'r', C(196, 32, 32) },
        }, BC);

        public static Sprite Scrap => PixelArt.FromAscii("scrap", new[]
        {
            ".mMm.",
            "mMmMM",
            "MmmMm",
        }, new Dictionary<char, Color32> { { 'm', C(150, 150, 156) }, { 'M', C(100, 100, 108) } }, BC);

        public static Sprite Diary => PixelArt.FromAscii("diary", new[]
        {
            ".LLLLLL...mmmmm.",
            ".LllllL..mmdddmm",
            ".LlwwlL..mmdDdmm",
            ".LllllL..mmdddmm",
            ".LLLLLL...mmmmm.",
        }, new Dictionary<char, Color32>
        {
            { 'L', C(96, 56, 36) }, { 'l', C(132, 84, 52) }, { 'w', C(210, 200, 170) },
            { 'm', C(70, 74, 82) }, { 'd', C(80, 160, 240) }, { 'D', C(200, 235, 255) },
        }, BC);

        public static Sprite LeverBase => PixelArt.FromAscii("lever_base", new[]
        {
            ".mmmm.",
            "mMMMMm",
            "mMMMMm",
        }, new Dictionary<char, Color32> { { 'm', C(70, 70, 76) }, { 'M', C(100, 100, 108) } }, BC);

        public static Sprite LeverHandle => PixelArt.FromAscii("lever_handle", new[]
        {
            "rr",
            "rr",
            "mm",
            "mm",
            "mm",
            "mm",
            "mm",
            "mm",
        }, new Dictionary<char, Color32> { { 'r', C(200, 40, 30) }, { 'm', C(120, 120, 128) } }, new Vector2(0.5f, 0f));

        public static Sprite LampPost => PixelArt.FromAscii("lamp", new[]
        {
            ".mmmmmm.",
            "mLLLLLLm",
            "mLLLLLLm",
            ".mmmmmm.",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "..mmmm..",
            ".mmmmmm.",
        }, new Dictionary<char, Color32> { { 'm', C(52, 52, 58) }, { 'L', C(255, 200, 110) } }, BC);

        public static Sprite LampPostOff => PixelArt.FromAscii("lamp_off", new[]
        {
            ".mmmmmm.",
            "mLLLLLLm",
            "mLLLLLLm",
            ".mmmmmm.",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "...mm...",
            "..mmmm..",
            ".mmmmmm.",
        }, new Dictionary<char, Color32> { { 'm', C(52, 52, 58) }, { 'L', C(70, 64, 54) } }, BC);

        public static Sprite Puddle => PixelArt.FromAscii("puddle", new[]
        {
            "...gggggggggggg...",
            ".gggGGgggggGGgggg.",
            "ggGGGgggGGGGggGGgg",
            ".gggggggggggggggg.",
        }, new Dictionary<char, Color32> { { 'g', C(100, 180, 36, 225) }, { 'G', C(160, 240, 70, 235) } }, BC);

        // ───────────────────────── Тайлы уровня ─────────────────────────
        public static Sprite Rock => PixelArt.FromPixels("tile_rock", 12, 12, (x, y) =>
        {
            float n = PixelArt.Hash(x, y, 7);
            var baseC = C(56, 50, 48);
            if ((x + y * 3) % 11 == 0 && n > 0.4f) return C(38, 34, 34); // трещины
            if (n > 0.85f) return C(72, 66, 60);
            if (n < 0.12f) return C(44, 40, 40);
            return baseC;
        }, PixelArt.Center);

        public static Sprite Crust => PixelArt.FromPixels("tile_crust", 12, 4, (x, y) =>
        {
            float n = PixelArt.Hash(x, y, 13);
            if (y == 3) return n > 0.55f ? C(118, 140, 52) : C(0, 0, 0, 0);
            if (y == 2) return n > 0.3f ? C(96, 116, 44) : C(78, 92, 40);
            return n > 0.6f ? C(76, 82, 44) : C(64, 62, 46);
        }, PixelArt.Center);

        public static Sprite Girder => PixelArt.FromAscii("tile_girder", new[]
        {
            "RRRRRRRRRRRR",
            "RrrrrrrrrrrR",
            "RrxrrrrrrxrR",
            "RrrrrrrrrrrR",
            "RRRRRRRRRRRR",
            ".RR......RR.",
        }, new Dictionary<char, Color32>
        {
            { 'R', C(84, 54, 42) }, { 'r', C(118, 76, 54) }, { 'x', C(160, 120, 90) },
        }, PixelArt.Center);

        public static Sprite GateTile => PixelArt.FromPixels("tile_gate", 12, 12, (x, y) =>
        {
            if (x == 0 || x == 11) return C(48, 40, 36);
            if ((x == 2 || x == 9) && (y == 2 || y == 9)) return C(150, 130, 110);
            float n = PixelArt.Hash(x, y, 21);
            return n > 0.8f ? C(120, 70, 46) : C(92, 62, 48);
        }, PixelArt.Center);

        public static Sprite BarrierTile => PixelArt.FromPixels("tile_barrier", 12, 12, (x, y) =>
        {
            float n = PixelArt.Hash(x, y, 33);
            int a = 120 + (int)(n * 90);
            return n > 0.7f ? C(170, 250, 80, a) : C(90, 170, 40, a - 30);
        }, PixelArt.Center);

        public static Sprite SludgeTile => PixelArt.FromPixels("tile_sludge", 12, 12, (x, y) =>
        {
            float n = PixelArt.Hash(x, y, 41);
            if (y == 11) return n > 0.5f ? C(170, 240, 70) : C(120, 200, 50);
            return n > 0.8f ? C(100, 170, 40) : C(64, 120, 30);
        }, PixelArt.Center);

        // ───────────────────────── Фон ─────────────────────────
        public static Sprite Sky => PixelArt.FromPixels("sky", 1, 64, (x, y) =>
        {
            float t = y / 63f;
            var low = new Color(0.33f, 0.37f, 0.19f);
            var mid = new Color(0.16f, 0.18f, 0.12f);
            var top = new Color(0.05f, 0.06f, 0.07f);
            Color c = t < 0.4f ? Color.Lerp(low, mid, t / 0.4f) : Color.Lerp(mid, top, (t - 0.4f) / 0.6f);
            return c;
        }, PixelArt.Center, 64f / 14f, FilterMode.Bilinear);

        /// <summary>Силуэты разрушенного города для параллакса.</summary>
        public static Sprite Ruins(string key, int seed, Color32 body, Color32 window, int minH, int maxH)
        {
            const int W = 384, H = 110;
            var heights = new int[W];
            var rng = new System.Random(seed);
            int xPos = 0;
            while (xPos < W)
            {
                int bw = rng.Next(10, 32);
                int bh = rng.Next(minH, maxH);
                bool broken = rng.NextDouble() < 0.5;
                for (int i = 0; i < bw && xPos + i < W; i++)
                {
                    int hh = bh;
                    if (broken) hh -= (int)(Mathf.Abs(Mathf.Sin((i + seed) * 0.7f)) * 10) + (i > bw / 2 ? (i - bw / 2) : 0);
                    heights[xPos + i] = Mathf.Max(4, hh);
                }
                xPos += bw + rng.Next(0, 6);
            }
            return PixelArt.FromPixels(key, W, H, (x, y) =>
            {
                if (y >= heights[x]) return C(0, 0, 0, 0);
                bool win = x % 5 >= 2 && x % 5 <= 3 && y % 7 >= 3 && y % 7 <= 4 && y < heights[x] - 3;
                if (win)
                {
                    float n = PixelArt.Hash(x / 5, y / 7, seed);
                    if (n > 0.93f) return window;
                    if (n > 0.5f) return PixelArt.Shade(body, 0.7f);
                }
                return body;
            }, new Vector2(0.5f, 0f));
        }
    }
}
