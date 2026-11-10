using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>
    /// Точка входа. Срабатывает автоматически при запуске любой сцены:
    /// настраивает камеру и собирает уровень из кода. Ничего вручную в сцене делать не нужно.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<GameBootstrap>() != null) return;
            new GameObject("Game").AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            Application.targetFrameRate = 120;
            ResetStatics();
            Sfx.Init();

            var cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera");
                cgo.tag = "MainCamera";
                cam = cgo.AddComponent<Camera>();
                cgo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.07f);
            cam.transform.position = new Vector3(0, 0, -10);
            cam.transform.rotation = Quaternion.identity;

            // убрать 3D-свет из шаблонной сцены — для спрайтов он не нужен
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) l.gameObject.SetActive(false);

            gameObject.AddComponent<Hud>();
            gameObject.AddComponent<DialogueUI>();
            gameObject.AddComponent<ShopUI>();

            var player = LevelBuilder.Build();

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.levelMinX = LevelBuilder.MinX;
            follow.levelMaxX = LevelBuilder.MaxX;
            follow.SnapToTarget();
            if (cam.GetComponent<Storm>() == null) cam.gameObject.AddComponent<Storm>();

            gameObject.AddComponent<Intro>();
        }

        static void ResetStatics()
        {
            GameState.UIOpen = false;
            GameState.Intro = false;
            GameState.LastUIFrame = -1;
            Enemy.All.Clear();
            ToxicPuddle.All.Clear();
            Interactable.All.Clear();
        }
    }
}
