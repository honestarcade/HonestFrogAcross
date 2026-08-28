using UnityEngine;

namespace FrogAcross.Input
{
    public enum ControlScheme { Swipe, TapRegions }

    /// <summary>Persisted scheme selection (Settings UI arrives in M4 #59).</summary>
    public static class ControlSchemeSetting
    {
        public const string PrefKey = "controls.scheme";

        public static ControlScheme Current
        {
            get => PlayerPrefs.GetInt(PrefKey, 0) == 1 ? ControlScheme.TapRegions : ControlScheme.Swipe;
            set
            {
                PlayerPrefs.SetInt(PrefKey, value == ControlScheme.TapRegions ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
