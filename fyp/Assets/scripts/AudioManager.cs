using UnityEngine;

// 放在 [MANAGERS] 物体上
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources (请添加两个AudioSource组件并拖入)")]
    public AudioSource bgmSource; // 用于播放背景音乐
    public AudioSource sfxSource; // 用于播放音效

    [Header("Audio Clips (请拖入音频文件)")]
    public AudioClip backgroundMusic; // 游戏主BGM
    public AudioClip buttonClickSound; // 通用按钮点击声

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 注意: [MANAGERS] 物体已经在 GameManager 中被设为 DontDestroyOnLoad 了
            // 所以这里不需要再写 DontDestroyOnLoad
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayBGM();
    }

    public void PlayBGM()
    {
        if (bgmSource != null && backgroundMusic != null)
        {
            // 如果已经在播放这首曲子，就不重置
            if (bgmSource.clip == backgroundMusic && bgmSource.isPlaying) return;

            bgmSource.clip = backgroundMusic;
            bgmSource.loop = true; // 循环播放
            bgmSource.Play();
        }
    }

    // 供按钮调用的公共方法
    public void PlayClickSound()
    {
        if (sfxSource != null && buttonClickSound != null)
        {
            // PlayOneShot 适合短促的音效，不会打断正在播放的其他音效
            sfxSource.PlayOneShot(buttonClickSound);
        }
    }
}