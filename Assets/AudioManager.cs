using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource seSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // AudioSourceの自動セット（未設定の場合）
        AudioSource[] sources = GetComponents<AudioSource>();
        if (bgmSource == null && sources.Length > 0) bgmSource = sources[0];
        if (seSource == null)
        {
            if (sources.Length > 1)
            {
                seSource = sources[1];
            }
            else
            {
                // AudioSourceが1つしかなければ自動で追加
                seSource = gameObject.AddComponent<AudioSource>();
            }
        }

        ApplyBgmSettings();
        ApplySeSettings();
    }

    // BGM用の設定反映
    public void ApplyBgmSettings()
    {
        if (bgmSource == null) return;

        bgmSource.volume = SaveData.BgmEnabled ? SaveData.BgmVolume : 0f;

        if (SaveData.BgmEnabled)
        {
            if (!bgmSource.isPlaying && bgmSource.clip != null)
                bgmSource.Play();
        }
        else
        {
            if (bgmSource.isPlaying)
                bgmSource.Pause();
        }
    }

    // SE/ボイス用の設定反映
    public void ApplySeSettings()
    {
        if (seSource == null) return;
        seSource.volume = SaveData.SeEnabled ? SaveData.SeVolume : 0f;
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null || clip == null) return;

        if (bgmSource.clip != clip)
        {
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }

        ApplyBgmSettings();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    // SE・ボイスを1回再生する関数
    public void PlaySE(AudioClip clip)
    {
        if (seSource == null || clip == null) return;
        ApplySeSettings();
        if (SaveData.SeEnabled)
        {
            seSource.PlayOneShot(clip, SaveData.SeVolume);
        }
    }
}