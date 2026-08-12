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

    [Header("キャプテンのオブジェクトもろもろ")]
    public GameObject captainPhoto;
    public GameObject captainTextBack;
    public GameObject captainAffect;
    public GameObject captainCollec;

    // マナタイマー制御用
    private float timerUpdateInterval = 1.0f; // 1秒ごとに更新
    private float timer = 0f;

    // Start is called before the first frame update
    void Start()
    {

        // テスト用（確認後削除）
        // SaveData.SetAffection(CharacterType.Gatchan, 0);
        // SaveData.Mana = 6;
        // SaveData.LastRecoveryTime = DateTime.Now.AddHours(-1.99);
        // SaveData.IsCaptainUnlocked = false;
        // SaveData.Save();

        LoadAndApplyData();
        UpdateManaDisplay();
        UpdateCaptainVisibility();
    }

    // Update is called once per frame
    void Update()
    {
        // 1秒ごとにタイマーとマナ回復をチェック
        timer += Time.deltaTime;
        if (timer >= timerUpdateInterval)
        {
            timer = 0f;
            UpdateManaDisplay();      // 回復チェック + 表示更新
        }
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

        // マナアイコンの表示更新
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
        if (SaveData.Mana >= 10) return; // すでにMAXなら何もしない

        DateTime last = SaveData.LastRecoveryTime;
        TimeSpan elapsed = DateTime.Now - last;

        // 2時間（7200秒）経過した分だけ回復
        int recoverCount = (int)(elapsed.TotalHours / 2.0);

        if (recoverCount > 0)
        {
            int newMana = Mathf.Min(10, SaveData.Mana + recoverCount);
            SaveData.Mana = newMana;

            // 回復した時間だけLastRecoveryTimeを進める（正確にするため）
            SaveData.LastRecoveryTime = last.AddHours(recoverCount * 2);

            SaveData.Save();
        }
    }

    void UpdateManaTimer()
    {
        if (manaTimerText == null) return;

        if (SaveData.Mana >= 10)
        {
            manaTimerText.text = "回復まであと 0:00:00";
            manaTimerText.text = "MAX";
            return;
        }

        // 次の1個が回復するまでの時間を計算
        DateTime nextRecovery = SaveData.LastRecoveryTime.AddHours(2);
        TimeSpan remaining = nextRecovery - DateTime.Now;

        if (remaining.TotalSeconds <= 0)
        {
            // 念のため再チェック
            RecoverManaIfNeeded();
            remaining = TimeSpan.Zero;
        }
        manaTimerText.text = string.Format("回復まであと {0:00}:{1:00}:{2:00}",
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
