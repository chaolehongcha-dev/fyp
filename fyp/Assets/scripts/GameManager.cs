using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ####################################################################
// ## 4.1. GAME MANAGER (游戏总管理器)
// ####################################################################
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("案件列表")]
    public List<CaseData> allCases;
    public int currentCaseIndex = 0;

    [Header("场景Mask引用")]
    public GameObject stage1_Briefing; // 拖入你的 'Stage1'
    public GameObject stage2_Storyline; // 拖入你的 'Stage2'
    public GameObject stage3_Judgment; // 拖入你的 'Stage3'

    [Header("管理器引用")]
    public CaseManager caseManager;
    public FactionManager factionManager;
    public EndingManager endingManager;
    private ChatSystem chatSystem; // 聊天系统

    private GameState currentState;

    // 公开属性，允许其他脚本读取当前状态
    public GameState CurrentState => currentState;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 管理器应该跨场景
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 确保所有管理器都已链接
        if (caseManager == null || factionManager == null || endingManager == null)
        {
            Debug.LogError("GAME MANAGER: 管理器未完全链接!");
        }

        // 链接 ChatSystem
        chatSystem = FindObjectOfType<ChatSystem>();
        if (chatSystem == null)
        {
            Debug.LogError("GameManager: 找不到 ChatSystem!");
        }

        // 游戏开始
        LoadCase(currentCaseIndex);
    }

    public void LoadCase(int index)
    {
        currentCaseIndex = index;

        // 如果所有案件都结束了
        if (currentCaseIndex >= allCases.Count)
        {
            EndGame();
            return;
        }

        CaseData caseToLoad = allCases[currentCaseIndex];

        // 1. 通知管理器准备
        caseManager.StartCase(caseToLoad);
        factionManager.ClearActiveStorylines();

        // 2. 设置状态为 'CaseBriefing' (案子前)
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

    // 由 ChatSystem (聊天系统) 调用
    public void EnterStorylinePhase()
    {
        if (currentState != GameState.CaseBriefing) return;

        currentState = GameState.StorylinePhase;

        // 激活 Mask 2
        stage1_Briefing.SetActive(false);
        stage2_Storyline.SetActive(true);
        stage3_Judgment.SetActive(false);
    }

    // 由 Stage2 的 'kaiting' 按钮调用
    public void EnterJudgmentPhase()
    {
        if (currentState != GameState.StorylinePhase) return;

        currentState = GameState.JudgmentPhase;

        // 激活 Mask 3
        stage1_Briefing.SetActive(false);
        stage2_Storyline.SetActive(false);
        stage3_Judgment.SetActive(true);
    }

    // 由 CaseManager 在判案结束后调用
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

        Debug.Log("--- 最终结局 JSON 数据 ---");
        Debug.Log(finalJson);
        Debug.Log("-----------------------------");
    }
}