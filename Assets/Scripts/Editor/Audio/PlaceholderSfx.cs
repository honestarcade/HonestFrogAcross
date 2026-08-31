using System;
using System.IO;
using FrogAcross.Audio;
using UnityEditor;
using UnityEngine;

namespace FrogAcross.Editor.Audio
{
    /// <summary>
    /// #65: clearly-temporary generated blips, one per hook, named
    /// placeholder-* so #66's swap can find every one. Distinct
    /// pitches make the every-hook demo audible without real assets.
    /// </summary>
    public static class PlaceholderSfx
    {
        public const string Folder = "Assets/Resources/Audio";
        private const int Rate = 22050;

        [MenuItem("FrogAcross/Audio/Generate placeholder sounds")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(Folder);
            int i = 0;
            foreach (GameSound sound in Enum.GetValues(typeof(GameSound)))
            {
                // one octave-ish ladder: each hook gets its own pitch
                float freq = 330f * Mathf.Pow(1.12f, i++);
                bool descending = sound is GameSound.DeathSplat or GameSound.DeathSink or GameSound.DeathSlide;
                Write($"placeholder-{AudioDirector.KeyFor(sound)}", 0.18f,
                    t => Blip(t, freq, descending));
            }
            // No placeholder MUSIC. A blip stands in for a one-shot; a looping
            // pad stands in for nothing — it is a continuous hum for as long as
            // the app is open, which is exactly how it shipped (owner,
            // 2026-08-31). The music slots stay silent until #66 delivers real
            // tracks.
            AssetDatabase.Refresh();
            Debug.Log($"[PlaceholderSfx] generated {i} placeholder clips in {Folder}");
        }

        private static float Blip(float t, float freq, bool descending)
        {
            float f = descending ? freq * (1f - 0.4f * t / 0.18f) : freq;
            float envelope = Mathf.Exp(-t * 18f);
            return Mathf.Sin(2f * Mathf.PI * f * t) * envelope * 0.5f;
        }


        private static void Write(string name, float seconds, Func<float, float> sample)
        {
            int count = (int)(Rate * seconds);
            var bytes = new byte[44 + count * 2];
            void U32(int off, uint v) { for (int b = 0; b < 4; b++) bytes[off + b] = (byte)(v >> (8 * b)); }
            void U16(int off, ushort v) { bytes[off] = (byte)v; bytes[off + 1] = (byte)(v >> 8); }
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(bytes, 0);
            U32(4, (uint)(36 + count * 2));
            System.Text.Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(bytes, 8);
            U32(16, 16); U16(20, 1); U16(22, 1);
            U32(24, Rate); U32(28, Rate * 2); U16(32, 2); U16(34, 16);
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(bytes, 36);
            U32(40, (uint)(count * 2));
            for (int s = 0; s < count; s++)
            {
                short v = (short)(Mathf.Clamp(sample(s / (float)Rate), -1f, 1f) * short.MaxValue);
                U16(44 + s * 2, (ushort)v);
            }
            File.WriteAllBytes($"{Folder}/{name}.wav", bytes);
        }
    }
}
