using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class EndingSceneController : MonoBehaviour
{
    [Header("UI 组件")]
    public Text storyText;        // 显示叙事文字
    public RawImage finalImage;   // 显示生成的图片 (请在 Inspector 挂好默认固定结局图)

    [Header("加载界面")]
    public GameObject loadingPanel;
    public Text loadingText;

    [Header("结局信息 UI (最后才显示)")]
    public GameObject finalInfoPanel;
    public Text endingIdText;
    public Button replayButton;

    public float fadeDuration = 1.0f;

    // 内部变量
    private List<string> textsToShow;
    private Texture2D aiGeneratedTexture; // 存储 AI 生成的图
    private Texture fixedTexture;         // 存储 Inspector 里原本挂的固定图
    private int clickCount = 0;
    private bool isFading = false;
    private bool isDataReady = false;

    void Start()
    {
        // 1. 初始化 UI
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (finalInfoPanel != null) finalInfoPanel.SetActive(false);

        // 备份 Inspector 里挂的固定图
        if (finalImage != null)
        {
            fixedTexture = finalImage.texture;
            finalImage.color = Color.black; // 初始全黑
            SetAlpha(finalImage, 0f);
        }

        if (storyText != null)
        {
            storyText.text = "";
            SetAlpha(storyText, 0f);
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(OnReplayClicked);
        }

        // 2. 开始请求数据
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.FinalJsonData))
        {
            string jsonData = GameManager.FinalJsonData;

            if (ImageGenerationService.Instance != null)
            {
                int id = ImageGenerationService.Instance.CalculateEndingID(jsonData);
                if (endingIdText != null) endingIdText.text = $"ENDING {id}/105";

                if (loadingText) loadingText.text = "Computing Destiny...";
                // 请求 AI 生成 (包括文字和图片)
                ImageGenerationService.Instance.GenerateEndingImage(jsonData, OnGenerationComplete);
            }
        }
        else
        {
            StartCoroutine(SimulateFakeGeneration());
        }
    }

    IEnumerator SimulateFakeGeneration()
    {
        yield return new WaitForSeconds(1.0f);
        OnGenerationComplete(null, new List<string> { "Test Narrative 1", "Test Narrative 2" });
    }

    // 回调：数据准备完毕
    private void OnGenerationComplete(Texture2D texture, List<string> narratives)
    {
        if (this == null || gameObject == null) return;

        aiGeneratedTexture = texture;

        // ----------------------------------------------------
        // ✅ 核心修改：手动添加第 4 段过渡文本
        // ----------------------------------------------------
        textsToShow = new List<string>();
        if (narratives != null) textsToShow.AddRange(narratives); // 先加入 AI 生成的前3段

        // 在最后追加这一句
        textsToShow.Add("下面将首先展示固定结局，然后展示ai生成结局");
        // ----------------------------------------------------

        isDataReady = true;

        if (loadingPanel != null) loadingPanel.SetActive(false);

        // 流程开始：先显示第一段文字 (图片此时还是黑的)
        if (textsToShow.Count > 0)
        {
            StartCoroutine(FadeTextIn(textsToShow[0]));
            clickCount = 1;
        }
        else
        {
            StartCoroutine(ShowEndingVisualsSequence());
        }
    }

    void Update()
    {
        if (isDataReady && Input.GetMouseButtonDown(0) && !isFading)
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        // 1. 如果还有下一段文字（包括最后那句提示），切换到下一段
        if (clickCount < textsToShow.Count)
        {
            StartCoroutine(SwitchText(textsToShow[clickCount]));
            clickCount++;
        }
        // 2. 此时屏幕上正显示着最后一段文字 ("下面将首先展示...")
        //    再次点击后，才进入图片展示流程
        else if (clickCount == textsToShow.Count)
        {
            StartCoroutine(ShowEndingVisualsSequence());
            clickCount++; // 防止重复触发
        }
    }

    // --- 核心流程：文字结束 -> 固定图 -> AI图 ---
    IEnumerator ShowEndingVisualsSequence()
    {
        isFading = true;

        // 1. 淡出最后一段文字 (即那句提示语)
        yield return StartCoroutine(FadeAlpha(storyText, 1f, 0f));

        // 2. 显示固定图片 (Base Ending)
        if (finalImage != null && fixedTexture != null)
        {
            finalImage.texture = fixedTexture; // 确保是固定图
            finalImage.color = new Color(1, 1, 1, 0); // 准备淡入

            // 淡入固定图
            yield return StartCoroutine(FadeAlpha(finalImage, 0f, 1f));
        }

        // 3. 停留展示固定图 (2秒)
        yield return new WaitForSeconds(2.0f);

        // 4. 如果有 AI 图片，淡入 AI 图片 (AI Ending)
        if (aiGeneratedTexture != null && finalImage != null)
        {
            // 淡出固定图 -> 换图 -> 淡入 AI 图
            yield return StartCoroutine(FadeAlpha(finalImage, 1f, 0f));
            finalImage.texture = aiGeneratedTexture;
            yield return StartCoroutine(FadeAlpha(finalImage, 0f, 1f));
        }

        // 5. 最后显示 UI 面板
        if (finalInfoPanel != null)
        {
            finalInfoPanel.SetActive(true);
        }

        isFading = false;
    }

    // --- 辅助协程 ---

    IEnumerator SwitchText(string newContent)
    {
        isFading = true;
        yield return StartCoroutine(FadeAlpha(storyText, 1f, 0f));
        storyText.text = newContent;
        yield return StartCoroutine(FadeAlpha(storyText, 0f, 1f));
        isFading = false;
    }

    IEnumerator FadeTextIn(string content)
    {
        isFading = true;
        storyText.text = content;
        yield return StartCoroutine(FadeAlpha(storyText, 0f, 1f));
        isFading = false;
    }

    IEnumerator FadeAlpha(Graphic graphic, float start, float end)
    {
        if (graphic == null) yield break;
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(start, end, time / fadeDuration);
            SetAlpha(graphic, alpha);
            yield return null;
        }
        SetAlpha(graphic, end);
    }

    private void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic != null)
        {
            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }

    private void OnReplayClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }
}