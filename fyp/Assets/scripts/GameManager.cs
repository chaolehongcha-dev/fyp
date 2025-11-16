using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; // 引入 UI 命名空间

// ####################################################################
// ## 4.1. GAME MANAGER (游戏总管理器)
// ## (已修复所有语法错误)
// ####################################################################
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("案件列表")]
    public List<CaseData> allCases;
    public int currentCaseIndex = 0;

    [Header("场景Mask引用")]
    public GameObject stage1_Briefing;
    public GameObject stage2_Storyline;
    public GameObject stage3_Judgment;

    [Header("管理器引用")]
    public CaseManager caseManager;
    public FactionManager factionManager;
    public EndingManager endingManager;
    private ChatSystem chatSystem;
    private ImageGenerationService imageGenService; // API 服务

    [Header("结局画面 UI")]
    public GameObject endingScreenPanel; // 拖入你的结局画面 Panel (默认隐藏)
    public RawImage endingImageDisplay;  // 拖入用于显示图像的 RawImage
    public Text loadingText;             // 拖入 "生成中..." 的 Text 提示

    private GameState currentState;
    public GameState CurrentState => currentState;

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
        }
    }

    void Start()
    {
        if (caseManager == null || factionManager == null || endingManager == null)
        {
            Debug.LogError("GAME MANAGER: 管理器未完全链接!");
        }

        chatSystem = FindObjectOfType<ChatSystem>();
        if (chatSystem == null)
        {
            Debug.LogError("GameManager: 找不到 ChatSystem!");
        }

        imageGenService = FindObjectOfType<ImageGenerationService>();
        if (imageGenService == null)
        {
            Debug.LogError("GameManager: 找不到 ImageGenerationService! (你是否已将其添加到 [MANAGERS] 物体上?)");
        }

        if (endingScreenPanel != null)
        {
            endingScreenPanel.SetActive(false);
        }

        LoadCase(currentCaseIndex);
    }

    public void LoadCase(int index)
    {
        // ## 修复: LoadCase 的所有逻辑现在都在这个大括号内 ##
        currentCaseIndex = index;

        if (currentCaseIndex >= allCases.Count)
        {
            EndGame();
            return;
        }

        CaseData caseToLoad = allCases[currentCaseIndex];

        // 1. 通知管理器准备
        caseManager.StartCase(caseToLoad);
        factionManager.ClearActiveStorylines();

        // 2. 设置状态
        currentState = GameState.CaseBriefing;

        // 3. 激活 Mask 1
        stage1_Briefing.SetActive(true);
        stage2_Storyline.SetActive(false);
        stage3_Judgment.SetActive(false);

        // 5. 通知聊天系统
        StopAllCoroutines();

        if (chatSystem != null)
        {
            // 发送新的消息列表
            chatSystem.ShowBriefing(caseToLoad.briefingMessages);
        }
        else
        {
            Debug.LogError("GameManager: ChatSystem 未链接，无法显示案情简报!");
        }
    }

    public void EnterStorylinePhase()
    {
        if (currentState != GameState.CaseBriefing) return;

        currentState = GameState.StorylinePhase;

        // 激活 Mask 2
        stage1_Briefing.SetActive(false);
        stage2_Storyline.SetActive(true);
        stage3_Judgment.SetActive(false);
    }

    public void EnterJudgmentPhase()
    {
        if (currentState != GameState.StorylinePhase) return;

        currentState = GameState.JudgmentPhase;

        // 激活 Mask 3
        stage1_Briefing.SetActive(false);
        stage2_Storyline.SetActive(false);
        stage3_Judgment.SetActive(true);
    }

    public void EndCase()
    {
        if (currentState != GameState.JudgmentPhase) return;

        currentState = GameState.CaseWrapUp;

        // 1. 触发派系评价 (核心)
        factionManager.EvaluatePlayerJudgment();

        Debug.Log("本案结束. 准备加载下一个案件...");

        // 3. 加载下一个案件
        LoadCase(currentCaseIndex + 1);
    }

    void EndGame()
    {
        currentState = GameState.GameEnd;
        Debug.Log("所有案件已审理完毕！");

        // 生成最终的 JSON 数据
        string finalJson = endingManager.GenerateFinalDataForAPI();
        Debug.Log("--- 最终结局 JSON 数据 (发送给 API) ---");
        Debug.Log(finalJson);
        Debug.Log("-----------------------------");

        // 隐藏所有游戏画面 (Masks)
        stage1_Briefing.SetActive(false);
        stage2_Storyline.SetActive(false);
        stage3_Judgment.SetActive(false);

        // 显示结局画面 (显示 "加载中...")
        if (endingScreenPanel != null)
        {
            endingScreenPanel.SetActive(true);
            loadingText.text = "正在根据你的判决生成最终结局...";
            endingImageDisplay.gameObject.SetActive(false); // 隐藏图像框
        }

        // 调用 API 服务
        if (imageGenService != null)
        {
            imageGenService.GenerateEndingImage(finalJson, OnImageReceived);
        }
        else
        {
            Debug.LogError("EndGame: ImageGenerationService 未找到!");
            loadingText.text = "错误: 图像生成服务未启动。";
        }
    }

    private void OnImageReceived(Texture2D generatedImage)
    {
        Debug.Log("GameManager: 成功接收到结局图像!");

        if (endingScreenPanel != null)
        {
            // 隐藏 "加载中" 文本
            loadingText.gameObject.SetActive(false);

            // 将 Texture2D 应用到 RawImage 上并显示
            endingImageDisplay.texture = generatedImage;
            endingImageDisplay.gameObject.SetActive(true);
        }
    }
}