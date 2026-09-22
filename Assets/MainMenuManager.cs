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

    [Header("キャラクター選択ボイス（0:がっちゃん, 1:おーるばっく, 2:ねえさん, 3:ゆいまーる, 4:キャプテン）")] // 追加
    public AudioClip[] selectVoiceClips;

    // マナタイマー制御用
    private float timerUpdateInterval = 1.0f; // 1秒ごとに更新
    private float timer = 0f;
    private bool _needUpdateManaUI = false;

    // シーン遷移中の重複タップ防止フラグ
    private bool isTransitioning = false;

    // Start is called before the first frame update
    void Start()
    {
        // テスト用（確認後削除）
        SaveData.SetAffection(CharacterType.Gatchan, 0);
        SaveData.Mana = 6;
        SaveData.LastRecoveryTime = DateTime.Now.AddHours(-1.99);
        SaveData.IsCaptainUnlocked = true;
        SaveData.Save();

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
            // 音声の長さ分だけ待機（キャラのセリフをしっかり聞かせる）
            yield return new WaitForSeconds(voiceClip.length+0.5f);
        }
        else
        {
            // 音声がない場合や音声OFF時は少しだけ余白を入れて遷移
            yield return new WaitForSeconds(0.5f);
        }

        // シーン遷移
        SceneManager.LoadScene("Character");
    }


}
