using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;  // 追加
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

    [Header("BGM")]
    public AudioClip[] menuBgmClips;

    // マナタイマー制御用
    private float timerUpdateInterval = 1.0f; // 1秒ごとに更新
    private float timer = 0f;

    // GUILayoutの描画グループエラー対策
    private bool _needUpdateManaUI = false;

    // Start is called before the first frame update
    void Start()
    {
        // テスト用（確認後削除）
        // SaveData.SetAffection(CharacterType.Gatchan, 0);
        // SaveData.Mana = 6;
        // SaveData.LastRecoveryTime = DateTime.Now.AddHours(-1.99);
        // SaveData.IsCaptainUnlocked = false;
        // SaveData.Save();

        InitializeBgm();
        LoadAndApplyData();
        UpdateManaDisplay();
        UpdateCaptainVisibility();
    }

    // Update is called once per frame
    void Update()
    {
        // 1秒周期でマナ更新＆表示チェッカーを駆動
        timer += Time.deltaTime;
        if (timer >= timerUpdateInterval)
        {
            timer = 0f;
            RecoverManaIfNeeded();      // 回復チェック
            _needUpdateManaUI = true;   // UIを更新してもいいよフラグ
        }
    }

    // Updateの最後で安全にUIを反映する
    private void LateUpdate()
    {
        if (_needUpdateManaUI)
        {
            _needUpdateManaUI = false;
            UpdateManaDisplay(); // 実際のUI反映処理
        }
    }

    // ランダムBGMの再生処理
    void InitializeBgm()
    {
        if (AudioManager.Instance == null || menuBgmClips == null || menuBgmClips.Length == 0) 
            return;

        // 10%の確率で0番目、残り90%で1番目
        int bgmIndex = (UnityEngine.Random.Range(0, 10) == 0) ? 0 : 1;
        if (bgmIndex < menuBgmClips.Length && menuBgmClips[bgmIndex] != null)
        {
            AudioManager.Instance.PlayBGM(menuBgmClips[bgmIndex]);
        }
    }

    // 各キャラクター情報の描画・表示制御
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

    // アイコン・タイマーの表示更新
    void UpdateManaDisplay()
    {
        int currentMana = SaveData.Mana;

        // マナアイコンの表示更新
        if (manaIcons != null)
        {
            for (int i = 0; i < manaIcons.Length; i++)
            {
                if (manaIcons[i] != null)
                {
                    manaIcons[i].enabled = (i < currentMana);
                }
            }
        }
        UpdateManaTimer();
    }

    // 時間経過によるマナ自動回復の計算
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

    // 残り時間タイマーテキストの表示更新
    void UpdateManaTimer()
    {
        if (manaTimerText == null) return;

        if (SaveData.Mana >= 10)
        {
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

    // アキラさんがいたら表示
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

        // 選択キャラを保存してCharacterシーンへ
        SaveData.SelectedCharacter = type;
        SaveData.Save();
        SceneManager.LoadScene("Character");
    }
}
