using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class EndingSceneController : MonoBehaviour
{
    [Header("UI 引用 (请务必拖入)")]
    public Text storyText;
    public RawImage finalImage;
    public float fadeDuration = 1.0f;

    private List<string> textsToShow;
    private Texture2D imageToShow;
    private int clickCount = 0;
    private bool isFading = false;

    void Start()
    {
        // 1. 检查 UI 引用
        if (storyText == null) Debug.LogError("EndingSceneController: 'Story Text' 未绑定！请在 Inspector 中拖入 Text 物体。");
        if (finalImage == null) Debug.LogError("EndingSceneController: 'Final Image' 未绑定！请在 Inspector 中拖入 RawImage 物体。");

        // 2. 从 GameManager 获取数据
        if (GameManager.Instance != null)
        {
            textsToShow = GameManager.FinalTexts;
            imageToShow = GameManager.FinalImage;
            Debug.Log($"EndingScene: 从 GameManager 获取数据成功。文本数: {(textsToShow != null ? textsToShow.Count : 0)}, 图片存在: {imageToShow != null}");
        }
        else
        {
            Debug.LogWarning("EndingScene: GameManager 实例为 null，可能直接运行了场景？使用测试数据。");
        }

        // 安全检查：如果列表为空
        if (textsToShow == null || textsToShow.Count == 0)
        {
            textsToShow = new List<string> { "Error: No narrative data found.", "Please check console logs.", "Click to exit." };
            Debug.LogError("EndingScene: 没有找到叙事文本数据！使用默认错误提示。");
        }

        // 3. 初始化 UI 状态
        if (finalImage != null)
        {
            if (imageToShow != null)
            {
                finalImage.texture = imageToShow;
                finalImage.color = Color.white; // 设为白色以显示贴图颜色
            }
            else
            {
                finalImage.color = Color.black; // 无图片时保持黑色
            }
            SetAlpha(finalImage, 0f); // 初始透明

            // 确保图片铺满屏幕 (假设 1920x1080)
            // 如果你的 Canvas Scaler 设置不同，这里可能需要调整，或者直接设为 Stretch
            finalImage.rectTransform.anchorMin = Vector2.zero;
            finalImage.rectTransform.anchorMax = Vector2.one;
            finalImage.rectTransform.offsetMin = Vector2.zero;
            finalImage.rectTransform.offsetMax = Vector2.zero;
        }

        if (storyText != null)
        {
            // 确保文字颜色可见 (白色)
            storyText.color = new Color(1, 1, 1, 0); // 白色，初始透明
            storyText.text = "";
        }

        // 4. 开始流程
        if (textsToShow.Count > 0)
        {
            Debug.Log("EndingScene: 开始显示第一段文字...");
            StartCoroutine(FadeTextIn(textsToShow[0]));
            clickCount = 1;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isFading)
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        Debug.Log($"EndingScene: 点击 (Count: {clickCount})");

        if (clickCount < textsToShow.Count)
        {
            // 显示下一段文字
            StartCoroutine(SwitchText(textsToShow[clickCount]));
            clickCount++;
        }
        else if (clickCount == textsToShow.Count) // 文字显示完了，显示图片
        {
            if (finalImage != null) StartCoroutine(ShowFinalImage());
            clickCount++;
        }
        else
        {
            Debug.Log("结局流程结束。");
            // 这里可以加返回主菜单的逻辑
        }
    }

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
        if (storyText != null) storyText.text = content;
        yield return StartCoroutine(FadeAlpha(storyText, 0f, 1f));
        isFading = false;
    }

    IEnumerator ShowFinalImage()
    {
        isFading = true;
        // 1. 淡出文字
        yield return StartCoroutine(FadeAlpha(storyText, 1f, 0f));

        // 2. 淡入图片
        if (imageToShow != null)
        {
            yield return StartCoroutine(FadeAlpha(finalImage, 0f, 1f));
        }
        else
        {
            Debug.LogWarning("EndingScene: 没有图片可显示 (imageToShow is null)");
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
}