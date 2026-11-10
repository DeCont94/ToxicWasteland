using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ToxicWasteland.EditorTools
{
    /// <summary>
    /// При первом открытии проекта создаёт сцену Assets/Scenes/Main.unity и добавляет её в сборку.
    /// Плюс пункт меню для сборки .exe под Windows.
    /// </summary>
    [InitializeOnLoad]
    static class ToxicProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        static ToxicProjectSetup()
        {
            EditorApplication.delayCall += Setup;
        }

        static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!File.Exists(ScenePath))
            {
                var active = EditorSceneManager.GetActiveScene();
                if (active.isDirty) return; // не трогаем несохранённую работу
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
                Debug.Log("[Toxic Wasteland] Создана сцена " + ScenePath + ". Нажми Play, чтобы играть.");
            }

            if (EditorBuildSettings.scenes.Length == 0)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            if (string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene(ScenePath);

            PlayerSettings.productName = "Toxic Wasteland";
            PlayerSettings.companyName = "ToxicTeam";
        }

        [MenuItem("Toxic Wasteland/Собрать для Windows (.exe)")]
        static void BuildWindows()
        {
            Setup();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/ToxicWasteland.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[Toxic Wasteland] Сборка: " + report.summary.result + " → " + options.locationPathName);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorUtility.RevealInFinder(options.locationPathName);
        }

        [MenuItem("Toxic Wasteland/Открыть игровую сцену")]
        static void OpenMain()
        {
            Setup();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
