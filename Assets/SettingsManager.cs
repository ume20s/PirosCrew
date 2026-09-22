using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI参照 - パネル・ボタン")]
    public GameObject settingsPanel;
    public Button closeButton;
    public Button openSettingsButton;

    [Header("UI参照 - BGM")]
    public Toggle bgmToggle;
    public Slider volumeSlider;

    [Header("UI参照 - Voice/SE")]
    public Toggle seToggle;
    public Slider seVolumeSlider;

    [Header("シーン遷移用")]
    public Button backToMenuButton;

    void Start()
    {
        // 初期値反映
        if (bgmToggle != null) bgmToggle.isOn = SaveData.BgmEnabled;
        if (volumeSlider != null) volumeSlider.value = SaveData.BgmVolume;
        if (seToggle != null) seToggle.isOn = SaveData.SeEnabled;
        if (seVolumeSlider != null) seVolumeSlider.value = SaveData.SeVolume;

        // イベント登録
        if (bgmToggle != null) bgmToggle.onValueChanged.AddListener(OnBgmToggleChanged);
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (seToggle != null) seToggle.onValueChanged.AddListener(OnSeToggleChanged);
        if (seVolumeSlider != null) seVolumeSlider.onValueChanged.AddListener(OnSeVolumeChanged);

        if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);
        if (openSettingsButton != null) openSettingsButton.onClick.AddListener(OpenSettings);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(BackToMainMenu);

        // 最初はセッティングパネルは隠しておく
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // セッティングパネル開く
    void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    // セッティングパネル閉じる
    void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        SaveData.Save();
    }

    // BGM ON/OFF切り替え
    void OnBgmToggleChanged(bool isOn)
    {
        SaveData.BgmEnabled = isOn;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ApplyBgmSettings();
    }

    // BGMボリューム変更
    void OnVolumeChanged(float value)
    {
        SaveData.BgmVolume = value;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ApplyBgmSettings();
    }

    // 音声ON/OFF切り替え
    void OnSeToggleChanged(bool isOn)
    {
        SaveData.SeEnabled = isOn;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ApplySeSettings();
    }

    // 音声ボリューム変更
    void OnSeVolumeChanged(float value)
    {
        SaveData.SeVolume = value;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ApplySeSettings();
    }

    // メインメニューへ戻る
    void BackToMainMenu()
    {
        SaveData.Save(); // 念のため保存
        SceneManager.LoadScene("MainMenu");
    }
}
