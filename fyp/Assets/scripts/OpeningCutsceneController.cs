using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class OpeningCutsceneController : MonoBehaviour
{
    [Header("场景跳转")]
    public string nextSceneName = "Scene1"; // 结束后跳转的场景名

    [Header("UI 组件引用")]
    public Image displayImage;       // 显示插图
    public Text subtitleText;        // 显示字幕
    public Image flashPanel;         // 用于闪白/闪黑的纯色Panel (全屏，RaycastTarget关掉)
    public Transform cameraTransform; // 主摄像机 (用于震动)

    [Header("音频源")]
    public AudioSource bgmSource;    // 背景音乐/环境音
    public AudioSource voiceSource;  // 配音 (不会被SFX打断)
    public AudioSource sfxSource;    // 音效 (法槌声等)

    [Header("特效参数")]
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 20f; // 震动幅度
    public float textShakeMagnitude = 5f; // 字幕震动幅度

    [System.Serializable]
    public class CutsceneFrame
    {
        [Header("基础内容")]
        public Sprite image;
        [TextArea(2, 5)]
        public string subtitle;

        [Header("音频配置")]
        public AudioClip voiceOver; // 该页面的配音
        public bool stopPreviousVoice = true; // 是否打断上一句配音 (法槌连贯时选false)
        public AudioClip sfx;       // 该页面播放的音效

        [Header("自动化控制")]
        public bool autoAdvance = false; // 是否自动切换到下一张
        public float duration = 0f;      // 自动切换的等待时间 (秒)

        [Header("打击感特效 (法槌专用)")]
        public bool triggerImpact = false; // 是否触发重击特效
        public Color flashColor = Color.white; // 闪光颜色 (白色或黑色)
    }

    [Header("分镜列表")]
    public List<CutsceneFrame> frames;

    private int currentIndex = 0;
    private bool isTransitioning = false;
    private Vector3 originalCamPos;
    private Vector3 originalTextPos;

    void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        originalCamPos = cameraTransform.localPosition;

        if (subtitleText != null) originalTextPos = subtitleText.rectTransform.localPosition;

        if (flashPanel != null)
        {
            flashPanel.color = new Color(1, 1, 1, 0);
            flashPanel.gameObject.SetActive(true);
        }

        ShowFrame(0);
    }

    void Update()
    {
        // 如果正在播放特效或当前帧是自动播放的，则禁止点击
        if (isTransitioning) return;
        if (currentIndex < frames.Count && frames[currentIndex].autoAdvance) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            NextFrame();
        }
    }

    void NextFrame()
    {
        if (currentIndex < frames.Count - 1)
        {
            currentIndex++;
            StartCoroutine(TransitionToFrame(currentIndex));
        }
        else
        {
            FinishCutscene();
        }
    }

    void FinishCutscene()
    {
        Debug.Log("CG结束，进入游戏");
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator TransitionToFrame(int index)
    {
        isTransitioning = true;
        CutsceneFrame frame = frames[index];

        // 1. 画面切换 (这里用简单的硬切，符合像素风格，如果需要淡入淡出可以加)
        if (displayImage != null && frame.image != null)
            displayImage.sprite = frame.image;

        // 2. 字幕更新
        if (subtitleText != null)
            subtitleText.text = frame.subtitle;

        // 3. 音频处理
        if (frame.voiceOver != null)
        {
            if (frame.stopPreviousVoice) voiceSource.Stop();
            voiceSource.PlayOneShot(frame.voiceOver);
        }

        if (frame.sfx != null)
        {
            sfxSource.PlayOneShot(frame.sfx);
        }

        // 4. 处理打击特效 (重头戏)
        if (frame.triggerImpact)
        {
            yield return StartCoroutine(PlayImpactEffect(frame.flashColor));
        }

        // 5. 自动播放逻辑
        if (frame.autoAdvance)
        {
            isTransitioning = false; // 允许计时器运行
            yield return new WaitForSeconds(frame.duration);
            NextFrame();
        }
        else
        {
            // 防止快速点击导致逻辑错乱，稍微加一点点冷却
            yield return new WaitForSeconds(0.2f);
            isTransitioning = false;
        }
    }

    // 直接显示第一帧 (不带过渡)
    void ShowFrame(int index)
    {
        if (index >= frames.Count) return;
        CutsceneFrame frame = frames[index];

        if (displayImage != null) displayImage.sprite = frame.image;
        if (subtitleText != null) subtitleText.text = frame.subtitle;

        if (frame.voiceOver != null) voiceSource.PlayOneShot(frame.voiceOver);
        if (frame.sfx != null) sfxSource.PlayOneShot(frame.sfx);

        // 第一帧通常不触发Impact，如果有需求可以加
        if (frame.autoAdvance) StartCoroutine(AutoAdvanceFirstFrame(frame.duration));
    }

    IEnumerator AutoAdvanceFirstFrame(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextFrame();
    }

    // ---------------------------------------------------------
    // 核心打击感特效协程
    // ---------------------------------------------------------
    IEnumerator PlayImpactEffect(Color flashColor)
    {
        // 1. 屏幕闪光 (瞬间变亮，然后快速衰减)
        if (flashPanel != null)
        {
            flashPanel.color = flashColor; // 设置闪光颜色
            float alpha = 0.8f; // 闪光强度
            flashPanel.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);

            // 快速淡出
            float flashDuration = 0.2f;
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(alpha, 0f, elapsed / flashDuration);
                flashPanel.color = new Color(flashColor.r, flashColor.g, flashColor.b, a);

                // 同时也进行震动处理 (每一帧都震)
                ShakeUpdate(1.0f - (elapsed / flashDuration)); // 震动随闪光衰减

                yield return null;
            }
            flashPanel.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0);
        }

        // 恢复位置
        cameraTransform.localPosition = originalCamPos;
        if (subtitleText != null) subtitleText.rectTransform.localPosition = originalTextPos;
    }

    void ShakeUpdate(float strengthMultiplier)
    {
        // 屏幕震动
        float x = Random.Range(-1f, 1f) * shakeMagnitude * strengthMultiplier;
        float y = Random.Range(-1f, 1f) * shakeMagnitude * strengthMultiplier;
        cameraTransform.localPosition = originalCamPos + new Vector3(x, y, 0);

        // 字幕震动 (增加混乱感)
        if (subtitleText != null)
        {
            float tx = Random.Range(-1f, 1f) * textShakeMagnitude * strengthMultiplier;
            float ty = Random.Range(-1f, 1f) * textShakeMagnitude * strengthMultiplier;
            subtitleText.rectTransform.localPosition = originalTextPos + new Vector3(tx, ty, 0);
        }
    }
}