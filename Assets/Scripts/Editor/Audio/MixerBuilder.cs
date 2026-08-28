using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace FrogAcross.Editor.Audio
{
    /// <summary>
    /// #65: builds Assets/Audio/Mixer.mixer — Master → Music/Effects/UI with
    /// exposed volume params (MasterVol/MusicVol/EffectsVol/UiVol). Unity has
    /// no public AudioMixer creation API, so this drives the internal
    /// AudioMixerController once; the committed asset is the deliverable and
    /// CI never rebuilds it.
    /// </summary>
    public static class MixerBuilder
    {
        public const string MixerPath = "Assets/Audio/Mixer.mixer";
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        [MenuItem("FrogAcross/Audio/Build mixer asset")]
        public static void Build()
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var controllerT = asm.GetType("UnityEditor.Audio.AudioMixerController");
            var groupT = asm.GetType("UnityEditor.Audio.AudioMixerGroupController");

            Directory.CreateDirectory(Path.GetDirectoryName(MixerPath)!);
            AssetDatabase.DeleteAsset(MixerPath);
            var controller = controllerT.GetMethod("CreateMixerControllerAtPath", Any)!
                .Invoke(null, new object[] { MixerPath });
            var master = controllerT.GetProperty("masterGroup", Any)!.GetValue(controller);

            var createGroup = controllerT.GetMethod("CreateNewGroup", Any)!;
            var addChild = controllerT.GetMethod("AddChildToParent", Any)!;
            var addToView = controllerT.GetMethod("AddGroupToCurrentView", Any);

            Debug.Log("[MixerBuilder] created controller, master=" + master);
            Expose(asm, controllerT, groupT, controller, master, "MasterVol");
            Debug.Log("[MixerBuilder] exposed MasterVol");
            foreach (var (groupName, param) in new[] { ("Music", "MusicVol"), ("Effects", "EffectsVol"), ("UI", "UiVol") })
            {
                var g = createGroup.Invoke(controller, new object[] { groupName, false });
                Debug.Log($"[MixerBuilder] created group {groupName}");
                addChild.Invoke(controller, new[] { g, master });
                Debug.Log($"[MixerBuilder] parented {groupName}");
                try { addToView?.Invoke(controller, new[] { g }); }
                catch (Exception e) { Debug.Log($"[MixerBuilder] view add skipped ({e.InnerException?.GetType().Name}) — cosmetic only"); }
                Expose(asm, controllerT, groupT, controller, g, param);
                Debug.Log($"[MixerBuilder] exposed {param}");
            }

            EditorUtility.SetDirty((UnityEngine.Object)controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MixerPath);

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            string groups = string.Join(", ", mixer.FindMatchingGroups("").Select(g => g.name));
            bool ok = mixer.SetFloat("MusicVol", -12f) && mixer.GetFloat("MusicVol", out float v) && Mathf.Approximately(v, -12f);
            mixer.SetFloat("MusicVol", 0f);
            Debug.Log($"[MixerBuilder] built {MixerPath}: groups [{groups}], exposed-param roundtrip {(ok ? "OK" : "FAILED")}");
        }

        private static void Expose(Assembly asm, Type controllerT, Type groupT, object controller, object group, string exposedName)
        {
            var guid = groupT.GetMethod("GetGUIDForVolume", Any)!.Invoke(group, null);
            var pathT = asm.GetType("UnityEditor.Audio.AudioGroupParameterPath");
            var path = Activator.CreateInstance(pathT, group, guid);
            controllerT.GetMethod("AddExposedParameter", Any)!.Invoke(controller, new[] { path });

            // rename the exposed parameter to a stable name
            var prop = controllerT.GetProperty("exposedParameters", Any)!;
            var array = (Array)prop.GetValue(controller);
            for (int i = 0; i < array.Length; i++)
            {
                object p = array.GetValue(i);
                var guidField = p.GetType().GetField("guid", Any)!;
                if (guidField.GetValue(p).Equals(guid))
                {
                    p.GetType().GetField("name", Any)!.SetValue(p, exposedName);
                    array.SetValue(p, i);
                }
            }
            prop.SetValue(controller, array);
        }
    }
}
