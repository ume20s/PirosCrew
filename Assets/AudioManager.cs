using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioSource bgmSource;

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

        // AudioSourceが未設定なら自分のものを使う
        if (bgmSource == null)
            bgmSource = GetComponent<AudioSource>();

        ApplyBgmSettings();
    }

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
}