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

// キャラクターごとのアイテムタップ時画像データ
[System.Serializable]
public class CharacterPhotoData
{
    [Header("通常時画像 (アイテム0〜4)")]
    public Sprite[] itemPhotos = new Sprite[5];

    [Header("同じ物を選んだ時画像")]
    public Sprite repeatPhoto;
}

// キャラクターごとのご褒美スチルデータ
[System.Serializable]
public class CharacterRewardPhotoData
{
    [Header("ご褒美スチル")]
    public Sprite[] rewardPhotos = new Sprite[8];
}

public class CharacterManager : MonoBehaviour
{

    [Header("セリフデータ (CSV)")]
    public TextAsset voiceLinesCsv;         // VoiceLines.csv

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

    // キャラクターごとのアイテム対応写真
    [Header("キャラクター×アイテム写真")]
    public CharacterPhotoData[] characterItemPhotos;

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

    [Header("ご褒美・コンプリート演出UI")]
    public GameObject rewardModalPanel;       // ご褒美全画面パネル
    public Image rewardPhotoImage;            // 獲得した写真を表示するImage
    public Text rewardTitleText;              // 「写真GET！」などのテキスト
    public Button rewardCloseButton;          // 閉じるボタン
    public ParticleSystem congratulateEffect; // 紙吹雪/光などのパーティクル (任意)
    public AudioClip rewardSe;                // ファンファーレ等の効果音 (任意)

    [Header("キャラクター×ご褒美スチル写真")]
    public CharacterRewardPhotoData[] characterRewardPhotos;

    // 現在の選択キャラクターと直近の選択アイテム
    private CharacterType currentCharacter;
    private int lastUsedItemIndex = -1;
    
    // マナ更新用タイマー
    private float timerUpdateInterval = 1.0f;
    private float timer = 0f;
    private bool _needUpdateManaUI = false;

    // テキスト表示用コルーチン
    private Coroutine popTextCoroutine;     // 好感度上下ポップアップ
    private Coroutine talkCoroutine;        // セリフ表示制御

    // CSVから読み込んだセリフを格納する2次元配列 [キャラ番号(0~4), アイテム(0~4:通常, 5:連続)]
    private string[,] loadedVoiceLines = new string[5, 6];

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
    private Vector2 popTextDefaultPos; // 好感度ポップアップ初期位置保存用

    void Start()
    {
        
        currentCharacter = SaveData.SelectedCharacter;  // 選択キャラクターを取得

        // ポップアップテキストの初期位置を保存
        if (affectionPopText != null)
        {
            popTextDefaultPos = affectionPopText.rectTransform.anchoredPosition;
        }

        // 好感度低下のチェック
        SaveData.ApplyAffectionDecay();

        LoadVoiceLinesFromCsv();        // CSVセリフデータの読み込み
        InitializeUI();                 // 画面の初期化（写真、ゲージ、ゆいまーるさん専用UIのオンオフなど）
        PlayCharacterBGM();             // キャラクター専用BGMの再生
    }

    // CSVファイルを配列に格納する処理
    private void LoadVoiceLinesFromCsv()
    {
        if (voiceLinesCsv == null)
        {
            Debug.LogWarning("セリフCSVファイルがセットされていません。");
            return;
        }

        // 改行コードで1行ずつ分割
        string[] lines = voiceLinesCsv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // 1行目はヘッダーなので i = 1 からスタート
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(','); // カンマで分割

            if (values.Length >= 7)
            {
                if (int.TryParse(values[0].Trim(), out int charIdx) && charIdx >= 0 && charIdx < 5)
                {
                    // アイテム0〜4のセリフ
                    for (int j = 0; j < 5; j++)
                    {
                        loadedVoiceLines[charIdx, j] = values[j + 1].Trim();
                    }
                    // 連続選択時のセリフ (インデックス5に格納)
                    loadedVoiceLines[charIdx, 5] = values[6].Trim();
                }
            }
        }
    }

    void Update()
    {
        // マナの回復チェックとUI更新フラグ立て (1秒毎)
        timer += Time.deltaTime;
        if (timer >= timerUpdateInterval)
        {
            timer = 0f;
            RecoverManaIfNeeded();

            // 好感度低下チェック（時間経過で下がった場合、スライダー表示を自動更新）
            if (SaveData.ApplyAffectionDecay())
            {
                RefreshAffectionSliders();
            }
            _needUpdateManaUI = true;
        }

        // セリフ表示中に画面をタップしたら吹き出し非表示
        if (talkBase != null && talkBase.activeSelf && Input.GetMouseButtonDown(0))
        {
            ResetToDefaultState();
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

    // 吹き出し非表示＋キャラクター画像を待機写真に戻す処理
    private void ResetToDefaultState()
    {
        if (talkCoroutine != null)
        {
            StopCoroutine(talkCoroutine);
            talkCoroutine = null;
        }

        if (talkBase != null) talkBase.SetActive(false);
        if (talkFlame != null) talkFlame.SetActive(false);

        // キャラの待機写真に戻す
        int charIndex = (int)currentCharacter;
        if (characterPhoto != null && defaultCharacterPhotos != null && charIndex < defaultCharacterPhotos.Length)
        {
            if (defaultCharacterPhotos[charIndex] != null)
            {
                characterPhoto.sprite = defaultCharacterPhotos[charIndex];
            }
        }
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
        PlayItemVoiceAndText(itemIndex, isRepeat);

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

    // ボイス・テキスト再生と同時にキャラクター画像も切り替える
    private void PlayItemVoiceAndText(int itemIndex, bool isRepeat)
    {
        int charIndex = (int)currentCharacter;
        AudioClip clipToPlay = null;
        string textToDisplay = "";
        Sprite photoToDisplay = null;

        // 1. ボイスAudioClipの取得
        if (characterItemVoices != null && charIndex < characterItemVoices.Length)
        {
            CharacterVoiceData voiceData = characterItemVoices[charIndex];
            if (isRepeat)
            {
                clipToPlay = voiceData.repeatVoice;
            }

            if (clipToPlay == null && voiceData.itemVoices != null && itemIndex < voiceData.itemVoices.Length)
            {
                clipToPlay = voiceData.itemVoices[itemIndex];
            }
        }

        // 2. キャラクター写真Spriteの取得（★追加）
        if (characterItemPhotos != null && charIndex < characterItemPhotos.Length)
        {
            CharacterPhotoData photoData = characterItemPhotos[charIndex];
            if (isRepeat)
            {
                photoToDisplay = photoData.repeatPhoto;
            }

            if (photoToDisplay == null && photoData.itemPhotos != null && itemIndex < photoData.itemPhotos.Length)
            {
                photoToDisplay = photoData.itemPhotos[itemIndex];
            }
        }

        // 写真が見つかった場合は切り替え、未設定の場合はデフォルト待機写真
        if (characterPhoto != null)
        {
            if (photoToDisplay != null)
            {
                characterPhoto.sprite = photoToDisplay;
            }
            else if (defaultCharacterPhotos != null && charIndex < defaultCharacterPhotos.Length)
            {
                characterPhoto.sprite = defaultCharacterPhotos[charIndex];
            }
        }

        // 3. CSVテキストの取得
        if (isRepeat)
        {
            textToDisplay = loadedVoiceLines[charIndex, 5];
        }
        else
        {
            textToDisplay = loadedVoiceLines[charIndex, itemIndex];
        }

        // 4. 音声再生（上書き再生）
        if (clipToPlay != null)
        {
            AudioManager.Instance.PlaySE(clipToPlay, stopPrevious: true);
        }

        // 5. セリフ表示の呼び出し
        if (talkCoroutine != null)
        {
            StopCoroutine(talkCoroutine);
        }
        talkCoroutine = StartCoroutine(ShowTalkRoutine(textToDisplay, clipToPlay != null ? clipToPlay.length : 2.0f));
    }

    // セリフ表示コルーチン（ボイスの長さに合わせて自動非表示）
    private IEnumerator ShowTalkRoutine(string message, float displayTime)
    {
        if (talkNameText != null) talkNameText.text = characterNames[(int)currentCharacter];
        if (talkMessageText != null) talkMessageText.text = message;

        if (talkBase != null) talkBase.SetActive(true);
        if (talkFlame != null) talkFlame.SetActive(true);

        // 音声の長さに合わせて表示を維持（最低1秒は表示）
        float duration = Mathf.Max(1.0f, displayTime+0.5f);
        yield return new WaitForSeconds(duration);

        // 表示終了後、吹き出しを隠すのと同時にキャラ画像を待機状態へ戻す
        ResetToDefaultState();
    }

    // 好感度スライダー表示の再更新メソッド
    private void RefreshAffectionSliders()
    {
        int currentAffection = SaveData.GetAffection(currentCharacter);
        if (mainAffectionSlider != null) mainAffectionSlider.value = currentAffection;

        // ゆいまーるさんの場合は他3人のスライダーも更新
        if (currentCharacter == CharacterType.Yuimarru)
        {
            if (gatchanSlider != null) gatchanSlider.value = SaveData.GetAffection(CharacterType.Gatchan);
            if (allbackSlider != null) allbackSlider.value = SaveData.GetAffection(CharacterType.Allback);
            if (nesanSlider != null) nesanSlider.value = SaveData.GetAffection(CharacterType.Nesan);
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

    // 好感度増減のポップアップ
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

        if (affectionPopText == null) return;

        // 既に再生中のアニメーションがあれば停止して初期位置に戻す
        if (popTextCoroutine != null)
        {
            StopCoroutine(popTextCoroutine);
            affectionPopText.rectTransform.anchoredPosition = popTextDefaultPos;
        }

        // アニメーションコルーチンの開始
        popTextCoroutine = StartCoroutine(AnimateAffectionPop(amount));
    }

    // ポップアップの移動＆フェードアウトを行うコルーチン
    private IEnumerator AnimateAffectionPop(int amount)
    {
        affectionPopText.gameObject.SetActive(true);

        RectTransform rect = affectionPopText.rectTransform;
        rect.anchoredPosition = popTextDefaultPos; // 初期位置にリセット

        // 上昇（緑）/ 下降（赤）の色設定
        Color baseColor = (amount > 0) ? new Color(0.0f, 0.5f, 0.0f, 1f) : new Color(0.9f, 0.1f, 0.1f, 1f);
        affectionPopText.text = (amount > 0) ? $"+{amount}" : $"{amount}";

        float duration = 0.8f;                   // アニメーションの時間（秒）
        float moveY = (amount > 0) ? 50f : -50f; // 移動距離（上昇は+50、下降は-50）

        Vector2 startPos = popTextDefaultPos;
        Vector2 endPos = popTextDefaultPos + new Vector2(0, moveY);

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // 1. 位置の移動（イージングっぽく上/下へ移動）
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            // 2. アルファ値（透明度）を 1.0 から 0.0 へフェードアウト
            Color c = baseColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            affectionPopText.color = c;

            yield return null; // 1フレーム待機
        }

        // アニメーション終了処理
        affectionPopText.gameObject.SetActive(false);
        rect.anchoredPosition = popTextDefaultPos; // 次回用に初期位置へ復帰
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