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
        else
        {
            Debug.LogWarning("GameManager: Case GameObjects 列表中缺少对应索引的物体!");
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
            // ## 修改: 这里的清理逻辑已经移动到了 EndCase ##
            // 这里只负责显示新简报
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

        // ## 修改: 1. 先清理旧消息 (删掉本案的购买记录和简报) ##
        if (chatSystem != null)
        {
            chatSystem.ClearTransientMessages();
        }

        // ## 修改: 2. 再生成评价 (这些评价会作为“新”的临时消息保留到下一个案子) ##
        factionManager.EvaluatePlayerJudgment();

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddEnergy(1);
            Debug.Log("案件结束，能量 +1");
        }

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