using UnityEngine;
using UnityEngine.UI;
using System;

// キャラクターごとに変わるアイテム（1〜3）の画像をまとめるためのデータ構造
[System.Serializable]
public class CharacterItemData
{
    public Sprite item1; // Favorite1 用
    public Sprite item2; // Favorite2 用
    public Sprite item3; // Favorite3 用
}

public class CharacterManager : MonoBehaviour
{
    [Header("背景画像")]
    public Image backgroundImage;          // Canvas内のBackground画像
    public Sprite[] characterBackgrounds;  // 各キャラの背景写真

    [Header("キャラクター設定情報")]
    public Sprite[] defaultCharacterPhotos; // 各キャラの待機状態の写真
    public AudioClip[] characterBgms;       // 各キャラ専用BGM

    // キャラクターごとの専用アイテム画像（要素数5）
    [Header("キャラクター専用アイテム画像")]
    public CharacterItemData[] characterSpecificItems;

    // アイテムのImageを書き換えるための参照（Favorite0〜4）
    [Header("UI参照 - 左パネル (アイテム)")]
    public Image[] favoriteItemImages;

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
    public Slider mainAffectionSlider; // AffectionScale

    [Header("UI参照 - 右パネル (ゆいまーるさん専用)")]
    public GameObject gatchanGaugeBase; // AffectionGatchanBase
    public GameObject allbackGaugeBase; // AffectionAllbackBase
    public GameObject nesanGaugeBase;   // AffectionNesanBase
    public Slider gatchanSlider;
    public Slider allbackSlider;
    public Slider nesanSlider;

    private CharacterType currentCharacter;
    
    // マナ更新用タイマー
    private float timerUpdateInterval = 1.0f;
    private float timer = 0f;
    private bool _needUpdateManaUI = false;

    // キャラクターの表示名リスト
    private readonly string[] characterNames = { "がっちゃん", "おーるばっく", "ねえさん", "ゆいまーる", "キャプテン" };

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

        // --- 1. 写真と吹き出しの設定 ---
        if (defaultCharacterPhotos != null && charIndex < defaultCharacterPhotos.Length)
        {
            characterPhoto.sprite = defaultCharacterPhotos[charIndex];
        }
        
        // 通常時は吹き出しを非表示にする
        if (talkBase != null) talkBase.SetActive(false);
        if (talkFlame != null) talkFlame.SetActive(false);
        if (talkNameText != null) talkNameText.text = characterNames[charIndex];

        // --- 2. 好物アイテム画像の設定（1〜3をキャラ専用に差し替え）
        if (characterSpecificItems != null && charIndex < characterSpecificItems.Length)
        {
            CharacterItemData items = characterSpecificItems[charIndex];
            
            // favoriteItemImagesに5つのImageが設定されているか確認
            if (favoriteItemImages != null && favoriteItemImages.Length >= 4)
            {
                if (items.item1 != null) favoriteItemImages[1].sprite = items.item1;
                if (items.item2 != null) favoriteItemImages[2].sprite = items.item2;
                if (items.item3 != null) favoriteItemImages[3].sprite = items.item3;
            }
        }

        // --- 3. 好感度メインゲージの反映 ---
        int currentAffection = SaveData.GetAffection(currentCharacter);
        if (mainAffectionSlider != null) mainAffectionSlider.value = currentAffection;

        // --- 4. ゆいまーるさん専用UIの表示制御 ---
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

        // --- 5. マナの初期表示 ---
        UpdateManaDisplay();
    }

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