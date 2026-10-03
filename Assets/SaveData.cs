using UnityEngine;
using System;

public static class SaveData
{
    private const string KEY_MANA = "Mana";
    private const string KEY_LAST_RECOVERY = "LastRecoveryTicks";
    private const string KEY_LAST_AFFECTION_DECAY = "LastAffectionDecayTicks";
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

    // マナ最終回復時間
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

    // 好感度最終低下計算時間
    public static DateTime LastAffectionDecayTime
    {
        get
        {
            string str = PlayerPrefs.GetString(KEY_LAST_AFFECTION_DECAY, "");
            if (long.TryParse(str, out long ticks))
                return new DateTime(ticks);
            return DateTime.Now;
        }
        set => PlayerPrefs.SetString(KEY_LAST_AFFECTION_DECAY, value.Ticks.ToString());
    }

    // 時間経過による好感度低下処理
    public static bool ApplyAffectionDecay()
    {
        DateTime last = LastAffectionDecayTime;
        TimeSpan elapsed = DateTime.Now - last;

        // 2時間（7200秒）経過した回数を計算
        int decayCount = (int)(elapsed.TotalHours / 2.0);

        if (decayCount > 0)
        {
            for (int i = 0; i < 5; i++)
            {
                CharacterType type = (CharacterType)i;

                // 8枚コンプリート済みのキャラは好感度が下がらない
                if (GetCollectionCount(type) >= 8) continue;

                int current = GetAffection(type);
                int newAffection = Mathf.Max(0, current - decayCount); // 0未満にはならない
                SetAffection(type, newAffection);
            }

            // 経過した時間分だけ記録日時を進める
            LastAffectionDecayTime = last.AddHours(decayCount * 2);
            Save();
            return true; // 変化があった場合trueを返す
        }
        return false;
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
        get => PlayerPrefs.GetFloat(KEY_BGM_VOLUME, 0.8f); // デフォルト0.8
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

    // 指定した写真（0〜7）が解放されているか
    public static bool IsPhotoUnlocked(CharacterType type, int photoIndex)
    {
        return PlayerPrefs.GetInt($"PhotoUnlocked_{(int)type}_{photoIndex}", 0) == 1;
    }

    // 写真を解放し、CollectionCountを自動同期する
    public static void UnlockPhoto(CharacterType type, int photoIndex)
    {
        PlayerPrefs.SetInt($"PhotoUnlocked_{(int)type}_{photoIndex}", 1);

        // 解放済み枚数をカウントして更新
        int count = 0;
        for (int i = 0; i < 8; i++)
        {
            if (IsPhotoUnlocked(type, i)) count++;
        }
        SetCollectionCount(type, count); // コレクション枚数を保存[cite: 7]
        Save();
    }

    // まだ解放されていない最初の写真インデックス（0〜7）を取得（全解放時は -1）
    public static int GetNextPhotoToUnlock(CharacterType type)
    {
        for (int i = 0; i < 8; i++)
        {
            if (!IsPhotoUnlocked(type, i)) return i;
        }
        return -1; // 8枚すべて解放済み
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}