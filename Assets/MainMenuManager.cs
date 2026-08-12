using UnityEngine;
using UnityEngine.UI;
using System;

public class MainMenuManager : MonoBehaviour
{
    [Header("キャラクター写真")]
    public GameObject[] characterPhotos;

    [Header("好感度テキスト")]
    public Text[] affectionTexts;

    [Header("コレクション枚数テキスト")]
    public Text[] collectionTexts;

    [Header("マナゲージ（Imageを10個）")]
    public Image[] manaIcons;

    [Header("マナタイマーテキスト")]
    public Text manaTimerText;

    [Header("キャプテンの写真オブジェクト")]
    public GameObject captainPhoto;
    public GameObject captainTextBack;
    public GameObject captainAffect;
    public GameObject captainCollec;

    void Start()
    {

        // テスト用（確認後削除）
        // SaveData.SetAffection(CharacterType.Gatchan, 0);
        // SaveData.Mana = 10;
        // SaveData.IsCaptainUnlocked = false;
        // SaveData.Save();

        LoadAndApplyData();
        UpdateManaDisplay();
        UpdateCaptainVisibility();
    }

    void LoadAndApplyData()
    {
        for (int i = 0; i < 5; i++)
        {
            CharacterType type = (CharacterType)i;

            if (affectionTexts != null && i < affectionTexts.Length && affectionTexts[i] != null)
                affectionTexts[i].text = $"好感度:{SaveData.GetAffection(type)}%";

            if (collectionTexts != null && i < collectionTexts.Length && collectionTexts[i] != null)
                collectionTexts[i].text = $"Photo:{SaveData.GetCollectionCount(type)}/8";
        }
    }

    void UpdateManaDisplay()
    {
        RecoverManaIfNeeded();

        int currentMana = SaveData.Mana;

        if (manaIcons != null)
        {
            for (int i = 0; i < manaIcons.Length; i++)
            {
                if (manaIcons[i] != null)
                    manaIcons[i].enabled = (i < currentMana);
            }
        }

        UpdateManaTimer();
    }

    void RecoverManaIfNeeded()
    {
        DateTime last = SaveData.LastRecoveryTime;
        TimeSpan elapsed = DateTime.Now - last;
        int recoverCount = (int)(elapsed.TotalHours / 2.0);

        if (recoverCount > 0)
        {
            SaveData.Mana = Mathf.Min(10, SaveData.Mana + recoverCount);
            SaveData.LastRecoveryTime = DateTime.Now;
            SaveData.Save();
        }
    }

    void UpdateManaTimer()
    {
        if (manaTimerText == null) return;

        if (SaveData.Mana >= 10)
        {
            manaTimerText.text = "回復まであと 0:00:00";
            return;
        }

        DateTime nextRecovery = SaveData.LastRecoveryTime.AddHours(2);
        TimeSpan remaining = nextRecovery - DateTime.Now;

        if (remaining.TotalSeconds < 0)
            remaining = TimeSpan.Zero;

        manaTimerText.text = string.Format("1マイク回復まであと {0:00}:{1:00}:{2:00}",
            remaining.Hours, remaining.Minutes, remaining.Seconds);
    }

    void UpdateCaptainVisibility()
    {
        if (captainPhoto != null)
        {
            captainPhoto.SetActive(SaveData.IsCaptainUnlocked);
            captainTextBack.SetActive(SaveData.IsCaptainUnlocked);
            captainAffect.SetActive(SaveData.IsCaptainUnlocked);
            captainCollec.SetActive(SaveData.IsCaptainUnlocked);
        }
    }

    // ボタンから呼び出す用
    public void OnCharacterSelected(int index)
    {
        if (index < 0 || index > 4) return;

        CharacterType type = (CharacterType)index;

        if (type == CharacterType.Captain && !SaveData.IsCaptainUnlocked)
        {
            Debug.Log("キャプテンはまだ解放されていません");
            return;
        }

        Debug.Log($"{type} が選択されました");
        // ここにシーン遷移処理を後で追加
    }
}
