using System;
using System.Collections.Generic;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Services;
using FrogAcross.Sim;
using UnityEngine;
using UnityEngine.Audio;

namespace FrogAcross.Audio
{
    public enum GameSound
    {
        Hop, DeathSplat, DeathSink, DeathSlide, RiderCrash, Stun, BayFill,
        Medal, LevelComplete, TrainWarning, TurtleWarning, UiTap, UiNavigate,
    }

    public enum MusicSlot { Menu, Gameplay }

    /// <summary>
    /// #65: event → sound routing. The sim raises events and this listens —
    /// no audio code in gameplay logic. Clips resolve by convention from
    /// Resources/Audio: "<key>" (owner file, #66) wins over "placeholder-<key>".
    /// Every Play is recorded in PlayedCounts so tests probe routing without
    /// an audio device (batchmode runs have no audio engine).
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        public const string RigResource = "Audio/AudioRig";

        private static AudioDirector _instance;

        public static AudioDirector Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var prefab = Resources.Load<GameObject>(RigResource);
                var go = prefab != null ? Instantiate(prefab) : new GameObject("audio-rig-fallback");
                _instance = go.GetComponent<AudioDirector>() != null
                    ? go.GetComponent<AudioDirector>()
                    : go.AddComponent<AudioDirector>();
                DontDestroyOnLoad(go);
                _instance.Init();
                return _instance;
            }
        }

        // wired by the rig prefab (built in-editor); fallback object leaves them null
        public AudioMixer mixer;
        public AudioSource musicSource;
        public AudioSource effectsSource;
        public AudioSource uiSource;

        /// <summary>Test probe: how often each hook fired this session.</summary>
        public readonly Dictionary<GameSound, int> PlayedCounts = new();

        /// <summary>Test probe: last dB written per exposed param — batchmode has
        /// no audio engine, so tests assert the binding here, not mixer state.</summary>
        public readonly Dictionary<string, float> AppliedDb = new();
        public MusicSlot? CurrentMusic { get; private set; }

        private readonly Dictionary<GameSound, AudioClip> _clips = new();
        private GameSim _sim;
        private LevelDefinition _level;
        private readonly HashSet<int> _warnedRows = new();
        private bool _turtleWarned;

        private void Init()
        {
            SoundSettings.Changed += ApplySettings;
            ApplySettings();
        }

        private void OnDestroy()
        {
            SoundSettings.Changed -= ApplySettings;
            if (_instance == this) _instance = null;
        }

        /// <summary>0dB when audible, −80dB when muted — routed through real buses,
        /// so master genuinely gates the category buses downstream.</summary>
        public static float TargetDb(bool audible) => audible ? 0f : -80f;

        public void ApplySettings()
        {
            AppliedDb["MasterVol"] = TargetDb(SoundSettings.Master);
            AppliedDb["MusicVol"] = TargetDb(SoundSettings.Music);
            AppliedDb["EffectsVol"] = TargetDb(SoundSettings.Effects);
            AppliedDb["UiVol"] = TargetDb(SoundSettings.Ui);
            if (mixer == null) return; // fallback rig (tests without the prefab)
            foreach (var kv in AppliedDb) mixer.SetFloat(kv.Key, kv.Value);
        }

        /// <summary>Listen to one sim run. Rebind per level start.</summary>
        public void Bind(GameSim sim, LevelDefinition level)
        {
            _sim = sim;
            _level = level;
            _warnedRows.Clear();
            _turtleWarned = false;
            sim.OnHop += _ => Play(GameSound.Hop);
            sim.OnBayFilled += _ => Play(GameSound.BayFill);
            sim.OnCompleted += () => Play(GameSound.LevelComplete);
            sim.OnStunned += () => Play(GameSound.Stun);
            sim.OnRiderCrashed += (_, _, _) => Play(GameSound.RiderCrash);
            sim.OnDeath += cause => Play(cause switch
            {
                DeathCause.Water => GameSound.DeathSink,
                DeathCause.EdgeDrift => GameSound.DeathSlide,
                _ => GameSound.DeathSplat,
            });
        }

        private void LateUpdate()
        {
            if (_sim == null || _sim.State.Completed) return;

            // train warnings: rising edge per row
            for (int r = 0; r < _level.Rows.Count; r++)
            {
                bool warn = _sim.WarningActive(r);
                if (warn && _warnedRows.Add(r)) Play(GameSound.TrainWarning);
                if (!warn) _warnedRows.Remove(r);
            }

            // turtle submerge warning while riding a submergible row
            int ticksLeft = TurtleWarnTicksLeft(_level, _sim.State.PlayerRow, _sim.State.Tick, _sim.State.Riding);
            bool turtleWarn = ticksLeft is > 0 and <= 60;
            if (turtleWarn && !_turtleWarned) Play(GameSound.TurtleWarning);
            _turtleWarned = turtleWarn;
        }

        /// <summary>
        /// Pure trigger rule (unit-tested): while riding a row whose cycling
        /// rideables run out of active ticks soon, return the smallest
        /// remaining active span; 0 = no warning.
        /// </summary>
        public static int TurtleWarnTicksLeft(LevelDefinition level, int row, long tick, bool riding)
        {
            if (!riding || row < 0 || row >= level.Rows.Count) return 0;
            int soonest = 0;
            foreach (var train in level.Rows[row].Trains)
            {
                var def = train.Def;
                // any cycling rideable turns unsafe when inactive: a submerged
                // turtle drops the rider in water, an open gator mouth kills
                if (def.role != ObjectRole.Rideable || def.cycleActiveTicks <= 0) continue;
                long cycle = def.cycleActiveTicks + def.cycleInactiveTicks;
                long pos = (tick + train.PhaseTicks) % cycle;
                if (pos >= def.cycleActiveTicks) continue; // already submerged
                int left = (int)(def.cycleActiveTicks - pos);
                if (soonest == 0 || left < soonest) soonest = left;
            }
            return soonest;
        }

        public void Play(GameSound sound)
        {
            PlayedCounts[sound] = PlayedCounts.GetValueOrDefault(sound) + 1;
            var clip = ClipFor(sound);
            var source = sound is GameSound.UiTap or GameSound.UiNavigate ? uiSource : effectsSource;
            if (clip != null && source != null) source.PlayOneShot(clip);
        }

        /// <summary>
        /// Music plays only from a real, licensed track — never a placeholder.
        /// A one-shot placeholder blip is a stand-in; a looping placeholder is
        /// a 220Hz sine pad on repeat forever, which is what shipped and what
        /// the owner heard: "a weird sound like a hum right when the app loads
        /// and it doesn't shut off" (2026-08-31). Silence is the honest
        /// stand-in until the tracks land (#66); no code change needed then —
        /// dropping music-menu.ogg into Resources/Audio starts it playing.
        /// </summary>
        public void PlayMusic(MusicSlot slot)
        {
            CurrentMusic = slot;
            if (musicSource == null) return;
            var clip = Resources.Load<AudioClip>(
                slot == MusicSlot.Menu ? "Audio/music-menu" : "Audio/music-gameplay");
            if (clip == null)
            {
                musicSource.Stop();
                musicSource.clip = null;
                return;
            }
            if (musicSource.clip == clip) return;
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }

        private AudioClip ClipFor(GameSound sound)
        {
            if (_clips.TryGetValue(sound, out var cached)) return cached;
            var clip = LoadClip(KeyFor(sound));
            _clips[sound] = clip;
            return clip;
        }

        /// <summary>Owner file ("Audio/<key>") wins; placeholder fills the gap until #66.</summary>
        private static AudioClip LoadClip(string key)
        {
            var owned = Resources.Load<AudioClip>($"Audio/{key}");
            if (owned != null) return owned;
            return Resources.Load<AudioClip>($"Audio/placeholder-{key}");
        }

        public static string KeyFor(GameSound sound) => sound switch
        {
            GameSound.Hop => "hop",
            GameSound.DeathSplat => "death-splat",
            GameSound.DeathSink => "death-sink",
            GameSound.DeathSlide => "death-slide",
            GameSound.RiderCrash => "rider-crash",
            GameSound.Stun => "stun",
            GameSound.BayFill => "bay-fill",
            GameSound.Medal => "medal",
            GameSound.LevelComplete => "level-complete",
            GameSound.TrainWarning => "train-warning",
            GameSound.TurtleWarning => "turtle-warning",
            GameSound.UiTap => "ui-tap",
            GameSound.UiNavigate => "ui-navigate",
            _ => throw new ArgumentOutOfRangeException(nameof(sound)),
        };
    }
}
