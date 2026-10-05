using System;
using UnityEngine;

namespace RollerSplat
{
    /// <summary>PlayerPrefs key namespace; tests swap the prefix so real saves are never touched.</summary>
    public static class SaveKeys
    {
        public static string Prefix = "rollersplat.";
    }

    /// <summary>Which level the player continues from.</summary>
    public static class GameProgress
    {
        static string LevelKey => SaveKeys.Prefix + "level";

        public static int CurrentLevel
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(LevelKey, 0));
            set
            {
                PlayerPrefs.SetInt(LevelKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(LevelKey);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Player options. The game has no audio yet: audio sources should read these volumes and listen to
    /// <see cref="Changed"/> once sound is added.
    /// </summary>
    public static class GameSettings
    {
        public const float DefaultVolume = 0.8f;

        public static event Action Changed;

        static string MusicKey => SaveKeys.Prefix + "music";
        static string SfxKey => SaveKeys.Prefix + "sfx";

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, DefaultVolume);
            set => SetVolume(MusicKey, value);
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, DefaultVolume);
            set => SetVolume(SfxKey, value);
        }

        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(MusicKey);
            PlayerPrefs.DeleteKey(SfxKey);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        static void SetVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
