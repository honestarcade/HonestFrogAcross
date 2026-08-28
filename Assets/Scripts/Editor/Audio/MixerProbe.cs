using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace FrogAcross.Editor.Audio
{
    public static class MixerProbe
    {
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static void Probe()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerBuilder.MixerPath);
            var t = mixer.GetType();
            Debug.Log("[Probe] runtime type: " + t.FullName);
            var prop = t.GetProperty("exposedParameters", Any);
            if (prop != null)
            {
                var array = (Array)prop.GetValue(mixer);
                Debug.Log("[Probe] exposedParameters count: " + array.Length);
                foreach (var p in array)
                {
                    var nameF = p.GetType().GetField("name", Any);
                    var guidF = p.GetType().GetField("guid", Any);
                    Debug.Log($"[Probe]   name='{nameF?.GetValue(p)}' guid={guidF?.GetValue(p)}");
                }
            }
            Debug.Log("[Probe] SetFloat MasterVol: " + mixer.SetFloat("MasterVol", -6f));
            Debug.Log("[Probe] SetFloat MusicVol: " + mixer.SetFloat("MusicVol", -6f));
        }
    }
}
