using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI参照")]
    public GameObject settingsPanel;
    public Toggle bgmToggle;
    public Slider volumeSlider;
    public Button closeButton;
    public Button openSettingsButton;

    void Start()
    {
        // 初期値反映
        bgmToggle.isOn = SaveData.BgmEnabled;
        volumeSlider.value = SaveData.BgmVolume;

        // イベント登録
        bgmToggle.onValueChanged.AddListener(OnBgmToggleChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        closeButton.onClick.AddListener(CloseSettings);
        openSettingsButton.onClick.AddListener(OpenSettings);

        settingsPanel.SetActive(false);
    }

    void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    void CloseSettings()
    {
        settingsPanel.SetActive(false);
        SaveData.Save();
    }

    void OnBgmToggleChanged(bool isOn)
    {
        SaveData.BgmEnabled = isOn;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ApplyBgmSettings();
    }

    void OnVolumeChanged(float value)
    {
        SaveData.BgmVolume = value;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ApplyBgmSettings();
    }
}
