using System.Collections;
using UnityEngine.SceneManagement;

namespace FrogAcross.Tests.PlayMode
{
    /// <summary>
    /// Tests that load real scenes must unload them, or their UI leaks into
    /// later tests (the HUD's corner buttons swallow UiGuardTests' touches).
    /// </summary>
    public static class SceneCleanup
    {
        private static int _n;

        public static IEnumerator UnloadAll()
        {
            // Only the game's own scenes: unloading the test runner's scene
            // kills the framework mid-run (learned the hard way).
            bool any = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var name = SceneManager.GetSceneAt(i).name;
                if (name == "Shell" || name == "Game") any = true;
            }
            if (!any) yield break;

            var empty = SceneManager.CreateScene($"test-empty-{_n++}");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if ((s.name == "Shell" || s.name == "Game") && s.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(s);
            }
        }
    }
}
