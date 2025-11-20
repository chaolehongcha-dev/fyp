using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // ## 1. 引入场景管理 ##

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ## 2. 静态变量，用于把数据传给 EndingScene ##
    public static Texture2D FinalImage;
    public static List<string> FinalTexts;

    [Header("案件列表")]
    public List<CaseData> allCases;

    [Header("案件 UI 物体")]
    public List<GameObject> caseGameObjects;

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

    [Header("加载提示 (结局前)")]
    public GameObject loadingPanel; // 简单的加载界面
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

        if (loadingPanel != null) loadingPanel.SetActive(false);

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

        if (currentCaseIndex > 0 && currentCaseIndex - 1 < caseGameObjects.Count)
        {
            if (caseGameObjects[currentCaseIndex - 1] != null)
                caseGameObjects[currentCaseIndex - 1].SetActive(false);
        }

        GameObject currentCaseObj = null;
        if (currentCaseIndex < caseGameObjects.Count)
        {
            currentCaseObj = caseGameObjects[currentCaseIndex];
            if (currentCaseObj != null)
                currentCaseObj.SetActive(true);
        }

        CaseData caseToLoad = allCases[currentCaseIndex];
        caseManager.StartCase(caseToLoad, currentCaseObj);
        factionManager.ClearActiveStorylines();

        currentState = GameState.CaseBriefing;
        stage1_Briefing.SetActive(true);
        stage2_Storyline.SetActive(false);
        stage3_Judgment.SetActive(false);

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

        if (chatSystem != null) chatSystem.ClearTransientMessages();
        factionManager.EvaluatePlayerJudgment();

        if (ResourceManager.Instance != null) ResourceManager.Instance.AddEnergy(1);

        LoadCase(currentCaseIndex + 1);
    }

    void EndGame()
    {
        currentState = GameState.GameEnd;
        Debug.Log("所有案件已审理完毕！");
        string finalJson = endingManager.GenerateFinalDataForAPI();

        // 隐藏所有 Mask 和 Case 物体
        stage1_Briefing.SetActive(false);
        stage2_Storyline.SetActive(false);
        stage3_Judgment.SetActive(false);
        int lastCaseIndex = allCases.Count - 1;
        if (lastCaseIndex < caseGameObjects.Count && caseGameObjects[lastCaseIndex] != null)
            caseGameObjects[lastCaseIndex].SetActive(false);

        // 显示加载提示
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingText) loadingText.text = "正在演算最终结局... (Calculating Final Outcome...)";
        }

        if (imageGenService != null)
            imageGenService.GenerateEndingImage(finalJson, OnImageReceived);
        else
            Debug.LogError("错误: 图像生成服务未启动。");
    }

    // ## 修改: 接收数据并跳转场景 ##
    private void OnImageReceived(Texture2D generatedImage, List<string> narratives)
    {
        Debug.Log("结局生成完毕，准备跳转...");

        // 1. 保存数据到静态变量，以便新场景读取
        FinalImage = generatedImage;
        FinalTexts = narratives;

        // 2. 隐藏加载面板
        if (loadingPanel != null) loadingPanel.SetActive(false);

        // 3. 跳转到 EndingScene
        SceneManager.LoadScene("EndingScene");
    }
}