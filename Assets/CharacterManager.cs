using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

// キャラクターごとに変わるアイテム画像
[System.Serializable]
public class CharacterItemData
{
    public Sprite item1; // Favorite1 用
    public Sprite item2; // Favorite2 用
    public Sprite item3; // Favorite3 用
}

// キャラクターごとのアイテムタップ時ボイス
[System.Serializable]
public class CharacterVoiceData
{
    [Header("通常時ボイス")]
    public AudioClip[] itemVoices = new AudioClip[5];

    [Header("同じ物を選んだ時ボイス")]
    public AudioClip repeatVoice;
}

public class CharacterManager : MonoBehaviour
{
    [Header("背景画像")]
    public Image backgroundImage;          // Canvas内のBackground画像
    public Sprite[] characterBackgrounds;  // 各キャラの背景写真

    [Header("キャラクター設定情報")]
    public Sprite[] defaultCharacterPhotos; // 各キャラの待機状態の写真
    public AudioClip[] characterBgms;       // 各キャラ専用BGM

    // キャラクターごとの専用アイテム画像
    [Header("キャラクター専用アイテム画像")]
    public CharacterItemData[] characterSpecificItems;

    // キャラクターごとのアイテムボイス
    [Header("キャラクター×アイテムボイス")]
    public CharacterVoiceData[] characterItemVoices;

    [Header("効果音 (SE)")]
    public AudioClip upSe;   // 好感度上昇SE
    public AudioClip downSe; // 好感度下降SE

    // アイテムのImageを書き換えるための参照（Favorite0〜4）
    [Header("UI参照 - 左パネル (アイテム)")]
    public Image[] favoriteItemImages;

    // キャラクター写真とセリフ
    [Header("UI参照 - 中央パネル")]
    public Image characterPhoto;    // CharacterPhoto
    public GameObject talkBase;     // TalkBase (吹き出し全体)
    public GameObject talkFlame;    // 吹き出し枠
    public Text talkNameText;       // 吹き出し内の名前用テキスト
    public Text talkMessageText;    // 吹き出し内のセリフ用テキスト

    [Header("UI参照 - 右パネル (マナ関連)")]
    public Image[] manaIcons;       // star(0) 〜 star(9)
    public Text manaTimerText;      // ManaTimer

    [Header("UI参照 - 右パネル (好感度)")]
    public Slider mainAffectionSlider;  // AffectionScale
    public Text affectionPopText;       // 好感度上昇/下降テキスト

    [Header("UI参照 - 右パネル (ゆいまーるさん専用)")]
    public GameObject gatchanGaugeBase; // AffectionGatchanBase
    public GameObject allbackGaugeBase; // AffectionAllbackBase
    public GameObject nesanGaugeBase;   // AffectionNesanBase
    public Slider gatchanSlider;
    public Slider allbackSlider;
    public Slider nesanSlider;

    private CharacterType currentCharacter;
    private int lastUsedItemIndex = -1;
    
    // マナ更新用タイマー
    private float timerUpdateInterval = 1.0f;
    private float timer = 0f;
    private bool _needUpdateManaUI = false;

    private Coroutine popTextCoroutine;

    // キャラクターの表示名リスト
    private readonly string[] characterNames = { "がっちゃん", "おーるばっく", "ねえさん", "ゆいまーる", "キャプテン" };

    // キャラクター別×アイテム別の基本好感度上昇値テーブル
    private readonly int[,] itemAffectionValues = new int[,]
    {
        // item0, item1, item2, item3, item4
        { 10, 7, 6, 9, 7 },   // 0: がっちゃんさん
        { 2,  4, 8, 10, 6 },  // 1: おーるばっくさん
        { 2,  8, 9, 10, 6 },  // 2: ねーさん
        { 2,  8, 9, 10, 7 },  // 3: ゆいまーるさん
        { 10, 10, 10, 10, 10 } // 4: キャプテン
    };

    void Start()
    {
        
        currentCharacter = SaveData.SelectedCharacter;  // 選択キャラクターを取得
        InitializeUI();                 // 画面の初期化（写真、ゲージ、ゆいまーるさん専用UIのオンオフなど）
        PlayCharacterBGM();             // キャラクター専用BGMの再生
    }

    void Update()
    {
        // マナの回復チェックとUI更新フラグ立て (1秒毎)
        timer += Time.deltaTime;
        if (timer >= timerUpdateInterval)
        {
            timer = 0f;
            RecoverManaIfNeeded();
            _needUpdateManaUI = true;
        }
    }

    void LateUpdate()
    {
        if (_needUpdateManaUI)
        {
            _needUpdateManaUI = false;
            UpdateManaDisplay();
        }
    }

    void InitializeUI()
    {
        int charIndex = (int)currentCharacter;

        // --- 1. 背景写真の設定 ---
        if (backgroundImage != null && characterBackgrounds != null && charIndex < characterBackgrounds.Length)
        {
            if (characterBackgrounds[charIndex] != null)
                backgroundImage.sprite = characterBackgrounds[charIndex];
        }

        // --- 2. 写真と吹き出しの設定 ---
        if (defaultCharacterPhotos != null && charIndex < defaultCharacterPhotos.Length)
        {
            characterPhoto.sprite = defaultCharacterPhotos[charIndex];
        }
        
        // 通常時は吹き出しを非表示にする
        if (talkBase != null) talkBase.SetActive(false);
        if (talkFlame != null) talkFlame.SetActive(false);
        if (talkNameText != null) talkNameText.text = characterNames[charIndex];

        // ポップアップテキストは最初非表示
        if (affectionPopText != null) affectionPopText.gameObject.SetActive(false);

        // --- 3. 好物アイテム画像の設定
        if (characterSpecificItems != null && charIndex < characterSpecificItems.Length)
        {
            CharacterItemData items = characterSpecificItems[charIndex];
            if (favoriteItemImages != null && favoriteItemImages.Length >= 4)
            {
                if (items.item1 != null) favoriteItemImages[1].sprite = items.item1;
                if (items.item2 != null) favoriteItemImages[2].sprite = items.item2;
                if (items.item3 != null) favoriteItemImages[3].sprite = items.item3;
            }
        }

        // --- 4. 好感度メインゲージの反映 ---
        int currentAffection = SaveData.GetAffection(currentCharacter);
        if (mainAffectionSlider != null) mainAffectionSlider.value = currentAffection;

        // --- 5. ゆいまーるさん専用UIの表示制御 ---
        bool isYuimarru = (currentCharacter == CharacterType.Yuimarru);
        if (gatchanGaugeBase != null) gatchanGaugeBase.SetActive(isYuimarru);
        if (allbackGaugeBase != null) allbackGaugeBase.SetActive(isYuimarru);
        if (nesanGaugeBase != null) nesanGaugeBase.SetActive(isYuimarru);

        // ゆいまーるさん選択時のみ、他3人のゲージに数値を反映
        if (isYuimarru)
        {
            if (gatchanSlider != null) gatchanSlider.value = SaveData.GetAffection(CharacterType.Gatchan);
            if (allbackSlider != null) allbackSlider.value = SaveData.GetAffection(CharacterType.Allback);
            if (nesanSlider != null) nesanSlider.value = SaveData.GetAffection(CharacterType.Nesan);
        }

        // --- 6. マナの初期表示 ---
        UpdateManaDisplay();
    }

    // アイテムボタンタップ時の処理
    public void OnItemClicked(int itemIndex)
    {
        // 1. マナチェック
        if (SaveData.Mana <= 0)
        {
            Debug.Log("マナが不足しています！");
            return;
        }

        // 2. マナ消費
        if (SaveData.Mana == 10)
        {
            SaveData.LastRecoveryTime = DateTime.Now;
        }
        SaveData.Mana--;
        UpdateManaDisplay();

        // 3. アイテム専用ボイスの再生
        bool isRepeat = (itemIndex == lastUsedItemIndex);
        PlayItemVoice(itemIndex, isRepeat);

        // 4. 好感度の変化計算
        int changeValue = CalculateAffectionChange(itemIndex);

        // 5. 保存とゲージの反映
        int currentAffection = SaveData.GetAffection(currentCharacter);
        int newAffection = Mathf.Clamp(currentAffection + changeValue, 0, 100);

        SaveData.SetAffection(currentCharacter, newAffection);
        SaveData.Save();

        if (mainAffectionSlider != null)
        {
            mainAffectionSlider.value = newAffection;
        }

        // 6. ポップアップテキスト表示と効果音（SE）再生
        ShowAffectionPop(changeValue);

        // 直前のアイテムを記録
        lastUsedItemIndex = itemIndex;
    }

    // アイテムごとのボイス再生
    private void PlayItemVoice(int itemIndex, bool isRepeat)
    {
        int charIndex = (int)currentCharacter;
        if (characterItemVoices != null && charIndex < characterItemVoices.Length)
        {
            CharacterVoiceData voiceData = characterItemVoices[charIndex];
            AudioClip clipToPlay = null;

            if (isRepeat)
            {
                // 全アイテム共通の連続用ボイスを参照
                clipToPlay = voiceData.repeatVoice;
            }

            // 連続用ボイスが未設定（null）の場合、または通常選択の場合は各アイテムのボイスを再生
            if (clipToPlay == null)
            {
                if (voiceData.itemVoices != null && itemIndex < voiceData.itemVoices.Length)
                {
                    clipToPlay = voiceData.itemVoices[itemIndex];
                }
            }

            // 音声再生
            if (clipToPlay != null)
            {
                AudioManager.Instance.PlaySE(clipToPlay);
            }
        }
    }

    // 好感度変化量の計算
    private int CalculateAffectionChange(int itemIndex)
    {
        int charIndex = (int)currentCharacter;
        int clampedItemIndex = Mathf.Clamp(itemIndex, 0, 4);

        // 連続タップペナルティ
        if (itemIndex == lastUsedItemIndex)
        {
            return -5;
        }

        // ゆいまーるさん特殊計算
        if (currentCharacter == CharacterType.Yuimarru)
        {
            bool gatchanOk = SaveData.GetAffection(CharacterType.Gatchan) >= 80;
            bool allbackOk = SaveData.GetAffection(CharacterType.Allback) >= 80;
            bool nesanOk   = SaveData.GetAffection(CharacterType.Nesan) >= 80;

            if (!gatchanOk || !allbackOk || !nesanOk)
            {
                return 1;
            }
        }

        // 指定テーブルから数値を参照
        return itemAffectionValues[charIndex, clampedItemIndex];
    }

    // 好感度増減の演出（テキスト＆SE）
    private void ShowAffectionPop(int amount)
    {
        // SE再生
        if (amount > 0 && upSe != null)
        {
            AudioManager.Instance.PlaySE(upSe);
        }
        else if (amount < 0 && downSe != null)
        {
            AudioManager.Instance.PlaySE(downSe);
        }

        // ポップアップ表示
        if (affectionPopText == null) return;

        if (popTextCoroutine != null)
        {
            StopCoroutine(popTextCoroutine);
        }

        if (amount > 0)
        {
            affectionPopText.text = $"+{amount}";
            affectionPopText.color = new Color(0.2f, 0.8f, 0.2f); // 緑色
        }
        else
        {
            affectionPopText.text = $"{amount}";
            affectionPopText.color = new Color(0.9f, 0.2f, 0.2f); // 赤色
        }

        affectionPopText.gameObject.SetActive(true);
        popTextCoroutine = StartCoroutine(HidePopTextAfterDelay(3.0f));
    }

    private IEnumerator HidePopTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (affectionPopText != null)
        {
            affectionPopText.gameObject.SetActive(false);
        }
    }

    // キャラ別BGMの再生
    void PlayCharacterBGM()
    {
        int charIndex = (int)currentCharacter;
        if (characterBgms != null && charIndex < characterBgms.Length && characterBgms[charIndex] != null)
        {
            // AudioManager経由で専用BGMをループ再生
            AudioManager.Instance.PlayBGM(characterBgms[charIndex]);
        }
    }

    // マナ回復・表示
    void UpdateManaDisplay()
    {
        int currentMana = SaveData.Mana;
        if (manaIcons != null)
        {
            for (int i = 0; i < manaIcons.Length; i++)
            {
                if (manaIcons[i] != null) manaIcons[i].enabled = (i < currentMana);
            }
        }
        UpdateManaTimer();
    }

    void RecoverManaIfNeeded()
    {
        if (SaveData.Mana >= 10) return;
        TimeSpan elapsed = DateTime.Now - SaveData.LastRecoveryTime;
        int recoverCount = (int)(elapsed.TotalHours / 2.0);

        if (recoverCount > 0)
        {
            SaveData.Mana = Mathf.Min(10, SaveData.Mana + recoverCount);
            SaveData.LastRecoveryTime = SaveData.LastRecoveryTime.AddHours(recoverCount * 2);
            SaveData.Save();
        }
    }

    void UpdateManaTimer()
    {
        if (manaTimerText == null) return;
        if (SaveData.Mana >= 10)
        {
            manaTimerText.text = "MAX";
            return;
        }
        DateTime nextRecovery = SaveData.LastRecoveryTime.AddHours(2);
        TimeSpan remaining = nextRecovery - DateTime.Now;
        if (remaining.TotalSeconds <= 0)
        {
            RecoverManaIfNeeded();
            remaining = TimeSpan.Zero;
        }
        manaTimerText.text = string.Format("回復まであと {0:00}:{1:00}:{2:00}",
            remaining.Hours, remaining.Minutes, remaining.Seconds);
    }
}