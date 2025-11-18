using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("案件列表")]
    public List<CaseData> allCases;

    [Header("案件 UI 物体 (Case GameObjects)")]
    public List<GameObject> caseGameObjects; // 请把 Case1, Case2, Case3... 拖进去

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
    private ImageGenerationService imageGenService;

    [Header("结局画面 UI")]
    public GameObject endingScreenPanel;
    public RawImage endingImageDisplay;
    public Text loadingText;

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
            Debug.LogError("GAME MANAGER: 管理器未完全链接!");
        chatSystem = FindObjectOfType<ChatSystem>();
        imageGenService = FindObjectOfType<ImageGenerationService>();

        if (endingScreenPanel != null) endingScreenPanel.SetActive(false);

        // 初始隐藏所有案件物体
        foreach (var caseObj in caseGameObjects)
        {
            if (caseObj != null) caseObj.SetActive(false);
        }

        LoadCase(currentCaseIndex);
    }

    public void LoadCase(int index)
    {
        currentCaseIndex = index;

        if (currentCaseIndex >= allCases.Count)
        {
            EndGame();
            return;
        }

        // 切换案件 UI 物体的显隐
        // 1. 隐藏上一个案件 (如果有)
        if (currentCaseIndex > 0 && currentCaseIndex - 1 < caseGameObjects.Count)
        {
            if (caseGameObjects[currentCaseIndex - 1] != null)
                caseGameObjects[currentCaseIndex - 1].SetActive(false);
        }

        // 2. 激活当前案件
        GameObject currentCaseObj = null;
        if (currentCaseIndex < caseGameObjects.Count)
        {
            currentCaseObj = caseGameObjects[currentCaseIndex];
            if (currentCaseObj != null)
                currentCaseObj.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameManager: Case GameObjects 列表中缺少对应索引的物体!");
        }

        CaseData caseToLoad = allCases[currentCaseIndex];

        // 1. 通知管理器准备 (## 修改: 传入当前案件的 GameObject ##)
        caseManager.StartCase(caseToLoad, currentCaseObj);

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
            chatSystem.ShowBriefing(caseToLoad.briefingMessages);
        }
    }

    public void EnterStorylinePhase()
    {
        if (currentState != GameState.CaseBriefing) return;
        currentState = GameState.StorylinePhase;
        stage1_Briefing.SetActive(false);
        stage2_Storyline.SetActive(true);
        stage3_Judgment.SetActive(false);
    }

    public void EnterJudgmentPhase()
    {
        if (currentState != GameState.StorylinePhase) return;
        currentState = GameState.JudgmentPhase;
        stage1_Briefing.SetActive(false);
        stage2_Storyline.SetActive(false);
        stage3_Judgment.SetActive(true);
    }

    public void EndCase()
    {
        if (currentState != GameState.JudgmentPhase) return;
        currentState = GameState.CaseWrapUp;
        factionManager.EvaluatePlayerJudgment();
        Debug.Log("本案结束. 准备加载下一个案件...");
        LoadCase(currentCaseIndex + 1);
    }

    void EndGame()
    {
        currentState = GameState.GameEnd;
        Debug.Log("所有案件已审理完毕！");
        string finalJson = endingManager.GenerateFinalDataForAPI();

        stage1_Briefing.SetActive(false);
        stage2_Storyline.SetActive(false);
        stage3_Judgment.SetActive(false);

        // 隐藏最后一个案件的 UI 物体
        int lastCaseIndex = allCases.Count - 1;
        if (lastCaseIndex < caseGameObjects.Count && caseGameObjects[lastCaseIndex] != null)
        {
            caseGameObjects[lastCaseIndex].SetActive(false);
        }

        if (endingScreenPanel != null)
        {
            endingScreenPanel.SetActive(true);
            loadingText.text = "正在根据你的判决生成最终结局...";
            endingImageDisplay.gameObject.SetActive(false);
        }

        if (imageGenService != null)
            imageGenService.GenerateEndingImage(finalJson, OnImageReceived);
        else
            loadingText.text = "错误: 图像生成服务未启动。";
    }

    private void OnImageReceived(Texture2D generatedImage)
    {
        if (endingScreenPanel != null)
        {
            loadingText.gameObject.SetActive(false);
            endingImageDisplay.texture = generatedImage;
            endingImageDisplay.gameObject.SetActive(true);
        }
    }
}