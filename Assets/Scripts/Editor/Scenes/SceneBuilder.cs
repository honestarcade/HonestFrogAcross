using FrogAcross.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogAcross.Editor.Scenes
{
    /// <summary>
    /// Idempotently (re)builds the Game scene: a straight orthographic camera
    /// (GameBootstrap frames it to each level at runtime) plus the
    /// GameBootstrap driver. Scene composition is code so it is reviewable
    /// and reproducible.
    /// </summary>
    public static class SceneBuilder
    {
        public const string GameScenePath = "Assets/Scenes/Game.unity";
        public const string ShellScenePath = "Assets/Scenes/Shell.unity";

        [MenuItem("FrogAcross/Scenes/Rebuild Shell Scene")]
        public static void BuildShellScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.157f, 0.373f);
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            new GameObject("Shell").AddComponent<FrogAcross.UI.AppShell>();
            EditorSceneManager.SaveScene(scene, ShellScenePath);
            Debug.Log("[SceneBuilder] Shell scene rebuilt.");
        }

        [MenuItem("FrogAcross/Scenes/Rebuild Game Scene")]
        public static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 6.2f; // placeholder — GameBootstrap fits per level (#87)
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.086f, 0.196f, 0.121f); // #16321F board surround
            // No roll: owner ruling at verify (#87) — the board renders straight
            // and GameBootstrap frames it to the bound level at runtime.
            camGo.transform.SetPositionAndRotation(new Vector3(5f, -4.5f, -10f), Quaternion.identity);

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            var boot = new GameObject("Game");
            boot.AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, GameScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ShellScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
            Debug.Log("[SceneBuilder] Game scene rebuilt; build scenes = Shell, Game.");
        }
    }
}
