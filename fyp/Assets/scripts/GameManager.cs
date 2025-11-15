using UnityEngine;
using System.Collections; // <-- 重要: 为协程添加
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

    private GameState currentState;

    // 允许其他脚本读取当前状态
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
            return;
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

        // 4. (CaseManager 会负责填充 Stage1 的文本)

        // -----------------------------------------------------------
        // ## 5. 修改: 模拟聊天系统回调 ##
        // -----------------------------------------------------------
        // 停止任何正在运行的旧模拟
        StopAllCoroutines();
        // 启动新的模拟协程
        StartCoroutine(SimulateChatCallback());
        // -----------------------------------------------------------
    }

    // ## 新增: 模拟协程 ##
    private IEnumerator SimulateChatCallback()
    {
        // 模拟玩家在聊天系统阅读案情简报
        // 等待 3 秒钟
        yield return new WaitForSeconds(3.0f);

        // 3 秒后，自动调用进入 Stage2
        // (一旦你有了真正的聊天系统，就由聊天系统在玩家点击"我读完了"时调用此方法)
        Debug.Log("模拟聊天回调：自动进入 Stage 2...");
        EnterStorylinePhase();
    }

    // 由 Stage1 的 "下一步" 按钮调用
    // ## 注意: 此方法现在由 SimulateChatCallback (聊天系统) 调用 ##
    public void EnterStorylinePhase()
    {
        if (currentState != GameState.CaseBriefing) return;

        currentState = GameState.StorylinePhase;

        // 激活 Mask 2
        stage1_Briefing.SetActive(false);
        stage2_Storyline.SetActive(true);
        stage3_Judgment.SetActive(false);

        // (CaseManager 会负责设置 Stage2 的按钮)
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

        // (CaseManager 会负责激活第一个判案UI组)
    }

    // 由 CaseManager 在判案结束后调用
    public void EndCase()
    {
        if (currentState != GameState.JudgmentPhase) return;

        currentState = GameState.CaseWrapUp;

        // 1. 触发派系评价 (核心)
        factionManager.EvaluatePlayerJudgment();

        // 2. (可选) 显示一个 "本案结束" 的UI
        Debug.Log("本案结束. 准备加载下一个案件...");

        // 3. 加载下一个案件 (这里可以加一个延迟或等待玩家点击)
        // 为简单起见，我们立即加载
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

        // (在这里，你可以将 finalJson 发送到你的 AI API)
    }
}