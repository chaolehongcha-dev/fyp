using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class EndingSceneController : MonoBehaviour
{
    [Header("UI 组件")]
    public Text storyText;       // 显示叙事文字
    public RawImage finalImage;  // 显示生成的图片

    [Header("加载界面")]
    public GameObject loadingPanel; // 包含 "Generating..." 文本的面板
    public Text loadingText;        // "正在生成..." 文本组件

    [Header("结局信息 UI (最后才显示)")]
    public GameObject finalInfoPanel; // 包含 Ending ID 和 Replay 按钮的面板
    public Text endingIdText;         // "Ending 15/105"
    public Button replayButton;       // 重玩按钮

    public float fadeDuration = 1.0f;

    private List<string> textsToShow;
    private Texture2D imageToShow;
    private int clickCount = 0;
    private bool isFading = false;
    private bool isDataReady = false;

    void Start()
    {
        // 1. 初始化 UI 状态 (确保一开始是对的)

        // A. Loading 界面：一开始就显示
        if (loadingPanel != null) loadingPanel.SetActive(true);

        // B. 信息面板 (Replay/ID)：一开始隐藏，最后才出来
        if (finalInfoPanel != null) finalInfoPanel.SetActive(false);

        // C. 图片：初始全黑/透明
        if (finalImage != null)
        {
            finalImage.color = Color.black;
            SetAlpha(finalImage, 0f);
        }

        // D. 故事文本：初始清空
        if (storyText != null)
        {
            storyText.text = "";
            SetAlpha(storyText, 0f);
        }

        // E. 绑定按钮事件
        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners(); // 防止重复绑定
            replayButton.onClick.AddListener(OnReplayClicked);
        }

        // 2. 开始获取数据和生成
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.FinalJsonData))
        {
            string jsonData = GameManager.FinalJsonData;

            // 计算并设置结局 ID (虽然现在不显示，但先填好内容)
            if (ImageGenerationService.Instance != null)
            {
                int id = ImageGenerationService.Instance.CalculateEndingID(jsonData);
                if (endingIdText != null) endingIdText.text = $"ENDING {id}/105";

                // 请求生成
                if (loadingText) loadingText.text = "Ending Generating...";
                ImageGenerationService.Instance.GenerateEndingImage(jsonData, OnGenerationComplete);
            }
            else
            {
                Debug.LogError("Error: ImageGenerationService 实例丢失");
            }
        }
        else
        {
            Debug.LogError("EndingScene: 缺少结局数据！(直接运行了此场景？)");
            if (loadingText) loadingText.text = "Error: No Data.";
            // 测试模式：3秒后模拟完成
            StartCoroutine(SimulateFakeGeneration());
        }
    }

    // 模拟测试 (仅用于调试)
    IEnumerator SimulateFakeGeneration()
    {
        yield return new WaitForSeconds(2.0f);
        OnGenerationComplete(null, new List<string> { "Test Narrative 1", "Test Narrative 2", "Test Narrative 3" });
    }

    // 回调：当图片和文本准备好时
    private void OnGenerationComplete(Texture2D texture, List<string> narratives)
    {
        imageToShow = texture;
        textsToShow = narratives;
        isDataReady = true;

        // ## 关键时刻 1: 开始放文本时，Loading 消失 ##
        if (loadingPanel != null) loadingPanel.SetActive(false);

        // 开始显示第一段话
        if (textsToShow != null && textsToShow.Count > 0)
        {
            StartCoroutine(FadeTextIn(textsToShow[0]));
            clickCount = 1;
        }
    }

    void Update()
    {
        // 只有数据准备好了，且不在淡入淡出中，才响应点击
        if (isDataReady && Input.GetMouseButtonDown(0) && !isFading)
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        // 逻辑：Text 1 -> Text 2 -> Text 3 -> 图片 + UI
        if (clickCount < textsToShow.Count)
        {
            // 切换下一段文字
            StartCoroutine(SwitchText(textsToShow[clickCount]));
            clickCount++;
        }
        else if (clickCount == textsToShow.Count)
        {
            // 文字放完了，显示最终图片和 UI
            StartCoroutine(ShowFinalImageAndUI());
            clickCount++;
        }
    }

    // --- 协程 ---

    IEnumerator SwitchText(string newContent)
    {
        isFading = true;
        // 淡出旧字
        yield return StartCoroutine(FadeAlpha(storyText, 1f, 0f));
        storyText.text = newContent;
        // 淡入新字
        yield return StartCoroutine(FadeAlpha(storyText, 0f, 1f));
        isFading = false;
    }

    IEnumerator FadeTextIn(string content)
    {
        isFading = true;
        storyText.text = content;
        // 淡入第一段
        yield return StartCoroutine(FadeAlpha(storyText, 0f, 1f));
        isFading = false;
    }

    IEnumerator ShowFinalImageAndUI()
    {
        isFading = true;
        // 1. 淡出最后一段文字
        yield return StartCoroutine(FadeAlpha(storyText, 1f, 0f));

        // 2. 淡入最终图片
        if (finalImage != null)
        {
            if (imageToShow != null)
            {
                finalImage.texture = imageToShow;
                finalImage.color = Color.white;
            }
            // 图片淡入
            yield return StartCoroutine(FadeAlpha(finalImage, 0f, 1f));
        }

        // ## 关键时刻 2: 图片出来后，显示 Ending ID 和 Replay 按钮 ##
        if (finalInfoPanel != null)
        {
            finalInfoPanel.SetActive(true);
        }

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
        Debug.Log("重玩游戏...");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }
        else
        {
            SceneManager.LoadScene("scene1");
        }
    }
}