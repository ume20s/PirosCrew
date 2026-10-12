using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

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

    [Header("キャラクター選択ボイス")]
    public AudioClip[] selectVoiceClips;

    [Header("コンプリート演出・バラの花")]
    public GameObject[] completeRoseObjects;      // バラの花のオブジェクト
    public ParticleSystem menuCongratulateEffect; // 紙吹雪パーティクル
    public AudioClip menuRewardSe;                // コンプリート時のファンファーレSE

    [Header("演出・入力ブロック用")]
    public GameObject inputBlockPanel;       // 演出中のタップ防止用パネル
    
    [Header("キャプテン登場演出用")]
    public AudioClip captainAppearBgm;       // ツァラトゥストラ
    public AudioClip captainAppearVoice;     // 「待たせたな！」ボイス

    // マナタイマー制御用
    private float timerUpdateInterval = 1.0f; // 1秒ごとに更新
    private float timer = 0f;
    private bool _needUpdateManaUI = false;

    // シーン遷移中の重複タップ防止フラグ
    private bool isTransitioning = false;

    // Start is called before the first frame update
    void Start()
    {
        SaveData.Mana = 9;
        SaveData.Save();

        // まず好感度低下処理を実行
        SaveData.ApplyAffectionDecay();

        // キャプテンの解放チェック
        SaveData.CheckAndUnlockCaptain();

        // もろもろの初期処理
        LoadAndApplyData();
        UpdateManaDisplay();
        UpdateCaptainVisibility();
        UpdateCompleteRoses();

        // キャプテン登場演出が未読かどうかを判定
        bool isAllCompleted = true;
        for (int i = 0; i < 4; i++)
        {
            if (SaveData.GetCollectionCount((CharacterType)i) < 8)
            {
                isAllCompleted = false;
                break;
            }
        }

        if (isAllCompleted && !SaveData.IsCaptainAppearShown)
        {
            // 初回の4人コンプリート＆キャプテン初登場エフェクトを開始
            StartCoroutine(PlayMasterpieceAppearSequence());
        }
        else
        {
            // 通常起動時
            CheckAndPlayMenuCompletionEffect();
        }
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

            // 好感度低下のチェック
            if (SaveData.ApplyAffectionDecay())
            {
                LoadAndApplyData();
            }
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

    // 残り時間タイマーテキストの表示更新（余り時間計算方式）
    void UpdateManaTimer()
    {
        if (manaTimerText == null) return;

        if (SaveData.Mana >= 10)
        {
            manaTimerText.text = "MAX";
            return;
        }

        // 最後に基準となった時間からの経過時間を計算
        TimeSpan elapsed = DateTime.Now - SaveData.LastRecoveryTime;
        double totalSeconds = elapsed.TotalSeconds;

        // 2時間（7200秒）を1ターンの間隔とする
        double intervalSeconds = 7200.0;

        // 次の1個が回復するまでの残り秒数（7200秒から経過秒数の余りを引く）
        double remainingSeconds = intervalSeconds - (totalSeconds % intervalSeconds);
        if (remainingSeconds < 0) remainingSeconds = 0;

        TimeSpan remaining = TimeSpan.FromSeconds(remainingSeconds);

        manaTimerText.text = string.Format("回復まであと {0:00}:{1:00}:{2:00}",
            remaining.Hours, remaining.Minutes, remaining.Seconds);
    }

    // キャプテン登場エフェクトコルーチン
    private IEnumerator PlayCaptainAppearEffect()
    {
        // 画面遷移直後の落ち着きを待つため少し待機
        yield return new WaitForSeconds(0.5f);

        // キャプテンのオブジェクトを一旦消して演出準備
        if (captainPhoto != null) captainPhoto.transform.localScale = Vector3.zero;

        // TODO: ここでファンファーレSEや登場ボイスを鳴らす
        // if (AudioManager.Instance != null && captainAppearVoice != null)
        //     AudioManager.Instance.PlaySE(captainAppearVoice);

        // 次第に大きく表示されるアニメーション
        float timer = 0f;
        float duration = 0.5f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Sin((timer / duration) * (Mathf.PI / 2)); // イージング
            if (captainPhoto != null)
                captainPhoto.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        if (captainPhoto != null) captainPhoto.transform.localScale = Vector3.one;

        Debug.Log("【演出】キャプテンが華々しく登場しました！");
    }

    // キャプテンがいたら表示（ただし初登場演出を見るまでは非表示を死守）
    void UpdateCaptainVisibility()
    {
        if (captainPhoto != null)
        {
            // ★修正：解放されていて、かつ初登場演出が済んでいる場合のみ表示する
            bool shouldShow = SaveData.IsCaptainUnlocked && SaveData.IsCaptainAppearShown;

            captainPhoto.SetActive(shouldShow);
            captainTextBack.SetActive(shouldShow);
            captainAffect.SetActive(shouldShow);
            captainCollec.SetActive(shouldShow);
        }
    }

    // キャラがタップされた
    public void OnCharacterSelected(int index)
    {
        if (isTransitioning) return; // 既に遷移中なら連打対策で無視
        if (index < 0 || index > 4) return;

        CharacterType type = (CharacterType)index;

        if (type == CharacterType.Captain && !SaveData.IsCaptainUnlocked)
        {
            Debug.Log("キャプテンはまだ解放されていません");
            return;
        }

        // コルーチンを開始してボイス再生〜シーン遷移を実行
        StartCoroutine(PlayVoiceAndLoadScene(type, index));
    }

    private IEnumerator PlayVoiceAndLoadScene(CharacterType type, int index)
    {
        isTransitioning = true;

        // 選択したキャラを保存
        SaveData.SelectedCharacter = type;
        SaveData.Save();

        // シーン遷移前にBGMを停止して静かにする
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }

        // ボイスの取得と再生
        AudioClip voiceClip = null;
        if (selectVoiceClips != null && index < selectVoiceClips.Length)
        {
            voiceClip = selectVoiceClips[index];
        }

        if (voiceClip != null && SaveData.SeEnabled && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(voiceClip);
            // 音声の長さ+0.2秒だけ待機（キャラのセリフをしっかり聞かせる）
            yield return new WaitForSeconds(voiceClip.length+0.2f);
        }
        else
        {
            // 音声がない場合や音声OFF時は少しだけ余白を入れて遷移
            yield return new WaitForSeconds(0.5f);
        }

        // シーン遷移
        SceneManager.LoadScene("Character");
    }

    // 8枚コンプリートしているキャラのバラの花を表示
    void UpdateCompleteRoses()
    {
        for (int i = 0; i < 5; i++)
        {
            CharacterType type = (CharacterType)i;
            bool isCompleted = (SaveData.GetCollectionCount(type) >= 8);

            if (completeRoseObjects != null && i < completeRoseObjects.Length && completeRoseObjects[i] != null)
            {
                completeRoseObjects[i].SetActive(isCompleted);
            }
        }
    }

    // メインメニューに戻ってきたとき、未読のコンプリートがあれば演出を再生
    void CheckAndPlayMenuCompletionEffect()
    {
        bool hasPlayedEffect = false;

        for (int i = 0; i < 4; i++)
        {
            CharacterType type = (CharacterType)i;
            
            // 8枚コンプリートしていて、まだメインメニューの演出を見ていない場合
            if (SaveData.GetCollectionCount(type) >= 8 && !SaveData.IsCompletionEffectShown(type))
            {
                // 演出を既読にする
                SaveData.SetCompletionEffectShown(type, true);

                // キャラクターを指定してコルーチンを呼び出す
                StartCoroutine(PlayMenuCompletionEffectRoutine(i));
                hasPlayedEffect = true;
                break; 
            }
        }

        // もしコンプリート演出が再生されない場合は、ここでBGMを初期化・再生する
        if (!hasPlayedEffect)
        {
            InitializeBgm();
        }
    }

    // メインメニューでのコンプリート凱旋演出コルーチン
    private IEnumerator PlayMenuCompletionEffectRoutine(int charIndex)
    {
        // 画面が落ち着くのを少し待つ
        yield return new WaitForSeconds(0.4f);

        // パーティクルの位置を、該当キャラクターの写真の位置に合わせる
        if (menuCongratulateEffect != null && characterPhotos != null && charIndex < characterPhotos.Length)
        {
            if (characterPhotos[charIndex] != null)
            {
                // キャラクターの写真のワールド座標をパーティクルにコピー
                menuCongratulateEffect.transform.position = characterPhotos[charIndex].transform.position;
            }
        }

        // エフェクト前にBGMを停止して静かにする
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }

        // 1. ファンファーレSEの再生
        if (menuRewardSe != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(menuRewardSe);
        }

        // 2. 紙吹雪パーティクルの再生
        if (menuCongratulateEffect != null)
        {
            menuCongratulateEffect.Play();
        }

        // 演出の余韻を数秒待つ
        yield return new WaitForSeconds(7.0f);

        // パーティクル停止
        if (menuCongratulateEffect != null)
        {
            menuCongratulateEffect.Stop();
        }

        // BGM再生
        InitializeBgm();
    }

    // 4人目コンプリートからキャプテン登場までの完全版ドラマチック演出
    private IEnumerator PlayMasterpieceAppearSequence()
    {
        // ★追加：4人全員のコンプリート演出は「既読」にしておき、通常のコンプリート演出が誤爆するのを防ぐ
        for (int i = 0; i < 4; i++)
        {
            SaveData.SetCompletionEffectShown((CharacterType)i, true);
        }

        // 2) メインメニューに遷移した瞬間、全てのタップ等を完全ブロック
        if (inputBlockPanel != null) inputBlockPanel.SetActive(true);
        isTransitioning = true; // 操作受付フラグもガード

        // BGMはOFFのまま静寂を保つ
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }

        // 画面が落ち着くのを少し待つ
        yield return new WaitForSeconds(0.5f);

        // 3) 4人目の通常のご褒美写真8枚コンプリートエフェクト（ファンファーレ＆紙吹雪）
        int lastCharIndex = 3; // 4人目のインデックス
        if (menuCongratulateEffect != null && characterPhotos != null && lastCharIndex < characterPhotos.Length)
        {
            if (characterPhotos[lastCharIndex] != null)
            {
                menuCongratulateEffect.transform.position = characterPhotos[lastCharIndex].transform.position;
            }
            menuCongratulateEffect.gameObject.SetActive(true);
            menuCongratulateEffect.Play();
        }

        if (menuRewardSe != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(menuRewardSe);
        }

        // 紙吹雪とファンファーレの余韻を待つ
        yield return new WaitForSeconds(3.5f);

        if (menuCongratulateEffect != null)
        {
            menuCongratulateEffect.Stop();
        }

        // 3) ひと拍子、間を置く
        yield return new WaitForSeconds(1.0f);

        // キャプテンの写真オブジェクト等の初期状態（サイズ0、表示状態へ）
        if (captainPhoto != null)
        {
            captainPhoto.SetActive(true);
            captainPhoto.transform.localScale = Vector3.zero;
        }
        if (captainTextBack != null) captainTextBack.SetActive(false);
        if (captainAffect != null) captainAffect.SetActive(false);
        if (captainCollec != null) captainCollec.SetActive(false);

        // 4) ツァラトウストラ再生開始
        if (captainAppearBgm != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(captainAppearBgm); 
        }

        // 5) ツァラトウストラ再生開始から「7秒程度」あとからキャプテン写真がニューっと大きくなる
        yield return new WaitForSeconds(7.0f);

        float expandTimer = 0f;
        float expandDuration = 2.5f; 
        while (expandTimer < expandDuration)
        {
            expandTimer += Time.deltaTime;
            float t = Mathf.Clamp01(expandTimer / expandDuration);
            float scale = Mathf.SmoothStep(0f, 1f, t); 

            if (captainPhoto != null)
            {
                captainPhoto.transform.localScale = Vector3.one * scale;
            }
            yield return null;
        }
        if (captainPhoto != null) captainPhoto.transform.localScale = Vector3.one;

        // 6) ツァラトウストラ再生終了までの残り時間を調整
        yield return new WaitForSeconds(7.5f);

        // 7) キャプテンの「待たせたな」のセリフ再生
        float voiceLength = 2.0f;
        if (captainAppearVoice != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(captainAppearVoice);
            voiceLength = captainAppearVoice.length;
        }
        yield return new WaitForSeconds(voiceLength + 0.5f);

        // 8) 演出既読フラグを保存し、キャプテンのUI常時表示を有効化
        SaveData.IsCaptainAppearShown = true; 
        UpdateCaptainVisibility(); // これでキャプテンが通常表示されるようになります

        isTransitioning = false;
        if (inputBlockPanel != null) inputBlockPanel.SetActive(false);

        // メインメニューの通常BGMを開始
        InitializeBgm();

        Debug.Log("【演出完了】キャプテン降臨の全ドラマが完了しました！");
    }
}
