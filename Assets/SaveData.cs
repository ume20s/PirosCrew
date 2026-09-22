using UnityEngine;
using System;

public static class SaveData
{
    private const string KEY_MANA = "Mana";
    private const string KEY_LAST_RECOVERY = "LastRecoveryTicks";
    private const string KEY_CAPTAIN_UNLOCKED = "CaptainUnlocked";
    private const string KEY_BGM_ENABLED = "BgmEnabled";
    private const string KEY_BGM_VOLUME = "BgmVolume";
    private const string KEY_SE_ENABLED = "SeEnabled";
    private const string KEY_SE_VOLUME = "SeVolume";
    private const string KEY_SELECTED_CHARACTER = "SelectedCharacter";

    public static int GetAffection(CharacterType type)
    {
        return PlayerPrefs.GetInt($"Affection_{(int)type}", 0);
    }

    public static void SetAffection(CharacterType type, int value)
    {
        PlayerPrefs.SetInt($"Affection_{(int)type}", Mathf.Clamp(value, 0, 100));
    }

    public static int GetCollectionCount(CharacterType type)
    {
        return PlayerPrefs.GetInt($"Collection_{(int)type}", 0);
    }

    public static void SetCollectionCount(CharacterType type, int value)
    {
        PlayerPrefs.SetInt($"Collection_{(int)type}", value);
    }

    public static int Mana
    {
        get => PlayerPrefs.GetInt(KEY_MANA, 10);
        set => PlayerPrefs.SetInt(KEY_MANA, Mathf.Clamp(value, 0, 10));
    }

    public static DateTime LastRecoveryTime
    {
        get
        {
            string str = PlayerPrefs.GetString(KEY_LAST_RECOVERY, "");
            if (long.TryParse(str, out long ticks))
                return new DateTime(ticks);
            return DateTime.Now;
        }
        set => PlayerPrefs.SetString(KEY_LAST_RECOVERY, value.Ticks.ToString());
    }

    public static bool IsCaptainUnlocked
    {
        get => PlayerPrefs.GetInt(KEY_CAPTAIN_UNLOCKED, 0) == 1;
        set => PlayerPrefs.SetInt(KEY_CAPTAIN_UNLOCKED, value ? 1 : 0);
    }

    // BGMオン/オフ（true = オン）
    public static bool BgmEnabled
    {
        get => PlayerPrefs.GetInt(KEY_BGM_ENABLED, 1) == 1; // デフォルトオン
        set => PlayerPrefs.SetInt(KEY_BGM_ENABLED, value ? 1 : 0);
    }

    // BGM音量（0.0f 〜 1.0f）
    public static float BgmVolume
    {
        get => PlayerPrefs.GetFloat(KEY_BGM_VOLUME, 0.4f); // デフォルト0.4
        set => PlayerPrefs.SetFloat(KEY_BGM_VOLUME, Mathf.Clamp01(value));
    }

    // 音声/SEオン/オフ（true = オン）
    public static bool SeEnabled
    {
        get => PlayerPrefs.GetInt(KEY_SE_ENABLED, 1) == 1; // デフォルトオン
        set => PlayerPrefs.SetInt(KEY_SE_ENABLED, value ? 1 : 0);
    }

    // 音声/SE音量（0.0f 〜 1.0f）
    public static float SeVolume
    {
        get => PlayerPrefs.GetFloat(KEY_SE_VOLUME, 1.0f); // デフォルト1.0
        set => PlayerPrefs.SetFloat(KEY_SE_VOLUME, Mathf.Clamp01(value));
    }

    // 選択中のキャラクター（メニュー → Characterシーン受け渡し用）
    public static CharacterType SelectedCharacter
    {
        get => (CharacterType)PlayerPrefs.GetInt(KEY_SELECTED_CHARACTER, 0);
        set => PlayerPrefs.SetInt(KEY_SELECTED_CHARACTER, (int)value);
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}