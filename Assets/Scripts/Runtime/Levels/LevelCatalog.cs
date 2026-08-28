using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FrogAcross.Levels
{
    /// <summary>
    /// The playable level chain. Canonical content is level-001..level-100
    /// (M5's generated set). Until those files exist, the dev slices form the
    /// chain — swapping in real content is a pure file operation (invariant 2).
    /// </summary>
    public static class LevelCatalog
    {
        private static readonly string[] DevChain =
        {
            "dev-road", "dev-river", "dev-swamp", "dev-tracks", "dev-bike", "dev-walkway", "dev-full",
        };

        private static List<string> _ids;

        public static IReadOnlyList<string> Ids
        {
            get
            {
                if (_ids != null) return _ids;
                var canonical = Resources.LoadAll<TextAsset>(LevelLoader.ResourceFolder)
                    .Select(t => t.name)
                    .Where(n => n.StartsWith("level-"))
                    .OrderBy(n => n)
                    .ToList();
                _ids = canonical.Count > 0 ? canonical : DevChain.ToList();
                return _ids;
            }
        }

        public static int Count => Ids.Count;

        /// <summary>1-based level number → id; null when out of range.</summary>
        public static string IdFor(int levelNumber) =>
            levelNumber >= 1 && levelNumber <= Ids.Count ? Ids[levelNumber - 1] : null;

        public static int NumberFor(string id)
        {
            int i = Ids.ToList().IndexOf(id);
            return i < 0 ? -1 : i + 1;
        }

        public static void Invalidate() => _ids = null;
    }
}
