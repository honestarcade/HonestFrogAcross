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
            var empty = SceneManager.CreateScene($"test-empty-{_n++}");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s != empty && s.isLoaded) yield return SceneManager.UnloadSceneAsync(s);
            }
        }
    }
}
