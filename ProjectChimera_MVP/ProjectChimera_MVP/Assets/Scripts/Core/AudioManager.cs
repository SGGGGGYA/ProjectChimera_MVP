using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("音量")]
    [Range(0f, 1f)] public float bgmVolume = 0.4f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    public Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    public Dictionary<string, AudioClip> bgmClips = new Dictionary<string, AudioClip>();

    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        LoadAllClips();
    }

    void LoadAllClips()
    {
        AudioClip[] allClips = Resources.LoadAll<AudioClip>("");
        foreach (var clip in allClips)
        {
            string name = clip.name.ToLower();
            if (name.StartsWith("bgm_"))
                bgmClips[name] = clip;
            else
                sfxClips[name] = clip;
        }
        Debug.Log($"[AudioManager] 已加载 {sfxClips.Count} 个音效, {bgmClips.Count} 个BGM");
    }

    public void PlayBGM(string clipName, float volume = -1f)
    {
        if (bgmSource == null) return;

        float vol = volume >= 0f ? volume : bgmVolume;
        string key = clipName.ToLower();

        if (bgmClips.TryGetValue(key, out AudioClip clip))
        {
            bgmSource.clip = clip;
            bgmSource.volume = vol;
            bgmSource.Play();
        }
        else
        {
            Debug.Log($"[Audio] BGM 播放: {key} (资源未找到，静默跳过)");
        }
    }

    public void PlaySFX(string clipName, float volume = -1f)
    {
        if (sfxSource == null) return;

        float vol = volume >= 0f ? volume : sfxVolume;
        string key = clipName.ToLower();

        if (sfxClips.TryGetValue(key, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip, vol);
        }
        else
        {
            Debug.Log($"[Audio] SFX 播放: {key} (资源未找到，静默跳过)");
        }
    }

    public void SetBGMVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void PauseBGM()
    {
        if (bgmSource != null)
            bgmSource.Pause();
    }

    public void ResumeBGM()
    {
        if (bgmSource != null)
            bgmSource.UnPause();
    }
}
