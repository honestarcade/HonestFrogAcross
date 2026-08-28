using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace FrogAcross.Services
{
    [Serializable]
    public class LevelRecord
    {
        public string levelId;
        public float bestSeconds = -1f;
        public int medal; // 0 none, 1 bronze, 2 silver, 3 gold
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int highestUnlocked = 1; // level numbers are 1-based
        public List<LevelRecord> records = new();
    }

    /// <summary>
    /// #60: on-device-only progression (invariant 1). Versioned atomic JSON at
    /// persistentDataPath; corruption recovers to a fresh state with a logged
    /// warning, never a crash. The clock itself lives in the sim (game-time —
    /// OS suspension adds nothing because ticks simply don't happen).
    /// </summary>
    public static class Progression
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, "frogacross-save.json");

        private static SaveData _data;

        public static SaveData Data => _data ??= LoadInternal();

        public static int HighestUnlocked => Data.highestUnlocked;

        public static bool IsUnlocked(int levelNumber) => levelNumber <= Data.highestUnlocked;

        public static LevelRecord RecordFor(string levelId) =>
            Data.records.FirstOrDefault(r => r.levelId == levelId);

        /// <summary>Returns (medal, isNewBest). Persists immediately.</summary>
        public static (int medal, bool newBest) ReportCompletion(
            string levelId, int levelNumber, float seconds, float gold, float silver, float bronze)
        {
            int medal = seconds <= gold ? 3 : seconds <= silver ? 2 : seconds <= bronze ? 1 : 0;
            var rec = RecordFor(levelId);
            if (rec == null)
            {
                rec = new LevelRecord { levelId = levelId };
                Data.records.Add(rec);
            }
            bool newBest = rec.bestSeconds < 0f || seconds < rec.bestSeconds;
            if (newBest) rec.bestSeconds = seconds;
            rec.medal = Math.Max(rec.medal, medal);
            Data.highestUnlocked = Math.Max(Data.highestUnlocked, levelNumber + 1);
            Save();
            return (medal, newBest);
        }

        public static void ResetAll()
        {
            _data = new SaveData();
            Save();
        }

        public static void Save()
        {
            try
            {
                string tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Data));
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(tmp, SavePath); // atomic-enough on one filesystem
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Progression] save failed: {e.Message}");
            }
        }

        /// <summary>Test hook: drop the cache so the next access reloads from disk.</summary>
        public static void ReloadFromDisk() => _data = null;

        private static SaveData LoadInternal()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                    if (loaded != null && loaded.version >= 1 && loaded.highestUnlocked >= 1)
                        return loaded;
                    Debug.LogWarning("[Progression] save failed validation — starting fresh");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Progression] corrupt save ({e.Message}) — starting fresh");
            }
            return new SaveData();
        }
    }

    /// <summary>#59's persisted sound toggles; #65 binds mixer buses to this.</summary>
    public static class SoundSettings
    {
        public static event Action Changed;

        public static bool Master
        {
            get => PlayerPrefs.GetInt("sound.master", 1) == 1;
            set { PlayerPrefs.SetInt("sound.master", value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static bool Music
        {
            get => PlayerPrefs.GetInt("sound.music", 1) == 1;
            set { PlayerPrefs.SetInt("sound.music", value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static bool Effects
        {
            get => PlayerPrefs.GetInt("sound.effects", 1) == 1;
            set { PlayerPrefs.SetInt("sound.effects", value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static bool Ui
        {
            get => PlayerPrefs.GetInt("sound.ui", 1) == 1;
            set { PlayerPrefs.SetInt("sound.ui", value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static bool EffectiveMusic => Master && Music;
        public static bool EffectiveEffects => Master && Effects;
        public static bool EffectiveUi => Master && Ui;

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey("sound.master");
            PlayerPrefs.DeleteKey("sound.music");
            PlayerPrefs.DeleteKey("sound.effects");
            PlayerPrefs.DeleteKey("sound.ui");
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// #59/#60: the single wipe path — everything the game persists goes
    /// through here so "Reset all data" can't silently miss a store.
    /// </summary>
    public static class DataWipe
    {
        public static void WipeAll()
        {
            Progression.ResetAll();
            SoundSettings.ResetAll();
            CharacterSelection.Reset();
            PlayerPrefs.DeleteKey(FrogAcross.Input.ControlSchemeSetting.PrefKey);
            PlayerPrefs.Save();
        }
    }

    /// <summary>#57's persisted character choice.</summary>
    public static class CharacterSelection
    {
        public const string PrefKey = "character.id";

        public static string SelectedId
        {
            get => PlayerPrefs.GetString(PrefKey, "");
            set { PlayerPrefs.SetString(PrefKey, value); PlayerPrefs.Save(); }
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
        }
    }
}
