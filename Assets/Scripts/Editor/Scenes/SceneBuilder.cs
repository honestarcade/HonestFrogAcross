using FrogAcross.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogAcross.Editor.Scenes
{
    /// <summary>
    /// Idempotently (re)builds the Game scene: camera rig approximating the
    /// design's tilted board camera (fine-tuned against design screenshots in
    /// M3), plus the GameBootstrap driver. Scene composition is code so it is
    /// reviewable and reproducible.
    /// </summary>
    public static class SceneBuilder
    {
        public const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("FrogAcross/Scenes/Rebuild Game Scene")]
        public static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.086f, 0.196f, 0.121f); // #16321F board surround
            // Sprites live on the X/Y plane (art is pre-drawn from the design's
            // tilted top-down perspective); the -8° design roll is a camera Z-roll.
            camGo.transform.SetPositionAndRotation(new Vector3(5f, -4.5f, -10f), Quaternion.Euler(0f, 0f, -8f));

            var boot = new GameObject("Game");
            boot.AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, GameScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GameScenePath, true),
            };
            Debug.Log("[SceneBuilder] Game scene rebuilt and set as the sole build scene.");
        }
    }
}
