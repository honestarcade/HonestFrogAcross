using System.Linq;
using FrogAcross.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace FrogAcross.Editor.Audio
{
    /// <summary>
    /// #65: the runtime audio rig — AudioDirector + one AudioSource per bus,
    /// wired to the committed Mixer.mixer groups, saved as a Resources prefab
    /// so the director can instantiate itself in any scene.
    /// </summary>
    public static class AudioRigBuilder
    {
        public const string PrefabPath = "Assets/Resources/Audio/AudioRig.prefab";

        [MenuItem("FrogAcross/Audio/Build audio rig prefab")]
        public static void Build()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerBuilder.MixerPath);
            if (mixer == null)
            {
                Debug.LogError("[AudioRigBuilder] build the mixer first (FrogAcross/Audio/Build mixer asset)");
                return;
            }

            var root = new GameObject("audio-rig");
            try
            {
                root.AddComponent<AudioListener>(); // code-built cameras carry none; exactly one lives here
                var director = root.AddComponent<AudioDirector>();
                director.mixer = mixer;
                director.musicSource = Source(root, "music", mixer, "Music");
                director.effectsSource = Source(root, "effects", mixer, "Effects");
                director.uiSource = Source(root, "ui", mixer, "UI");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[AudioRigBuilder] saved {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static AudioSource Source(GameObject root, string name, AudioMixer mixer, string group)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var source = go.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = mixer.FindMatchingGroups(group).First(g => g.name == group);
            source.playOnAwake = false;
            return source;
        }
    }
}
