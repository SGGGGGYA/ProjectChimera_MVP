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

    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<AudioManager>(FindObjectsInactive.Exclude);
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    if (Application.isPlaying) DontDestroyOnLoad(go);
                    Debug.Log("[AudioManager] 运行时自动创建 AudioManager 对象");
                }
                else
                {
                    Debug.Log("[AudioManager] 在场景中找到已有实例");
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        Debug.Log("[AudioManager] Awake 被调用");
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);

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

        EnsureAudioListener();
        LoadAllClips();
    }

    void EnsureAudioListener()
    {
        var listener = FindAnyObjectByType<AudioListener>(FindObjectsInactive.Include);
        if (listener == null)
        {
            gameObject.AddComponent<AudioListener>();
            Debug.Log("[AudioManager] 场景中无 AudioListener，已自动添加");
        }
        else
        {
            Debug.Log($"[AudioManager] 发现 AudioListener 在 {listener.gameObject.name} 上");
        }
    }

    void LoadAllClips()
    {
        AudioClip[] allClips = Resources.LoadAll<AudioClip>("");
        Debug.Log($"[AudioManager] Resources.LoadAll 返回 {allClips.Length} 个音频资源");
        foreach (var clip in allClips)
        {
            if (clip == null) continue;
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
        if (bgmSource == null) { Debug.LogWarning("[AudioManager] bgmSource 为 null"); return; }

        float vol = volume >= 0f ? volume : bgmVolume;
        string key = clipName.ToLower();

        if (bgmClips.TryGetValue(key, out AudioClip clip))
        {
            bgmSource.clip = clip;
            bgmSource.volume = vol;
            bgmSource.Play();
            Debug.Log($"[AudioManager] 播放 BGM: {key}, 音量={vol}");
        }
        else
        {
            Debug.LogWarning($"[AudioManager] BGM 未找到: {key}");
        }
    }

    public void PlaySFX(string clipName, float volume = -1f)
    {
        if (sfxSource == null) { Debug.LogWarning("[AudioManager] sfxSource 为 null"); return; }

        float vol = volume >= 0f ? volume : sfxVolume;
        string key = clipName.ToLower();

        if (sfxClips.TryGetValue(key, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip, vol);
            Debug.Log($"[AudioManager] 播放 SFX: {key}, 音量={vol}");
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX 未找到: {key}");
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
