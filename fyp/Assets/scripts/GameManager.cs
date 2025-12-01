using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static string FinalJsonData;

    [Header("案件列表 (CaseData)")]
    public List<CaseData> allCases;

    [Header("案件 UI 物体 (建议手动拖拽 Case1-5 到这里)")]
    public List<GameObject> caseGameObjects;

    public int currentCaseIndex = 0;

    public GameObject stage1_Briefing;
    public GameObject stage2_Storyline;
    public GameObject stage3_Judgment;

    public CaseManager caseManager;
    public FactionManager factionManager;
    public EndingManager endingManager;
    private ChatSystem chatSystem;

    private GameState currentState;
    public GameState CurrentState => currentState;

    private bool isInitialized = false;

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

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 🛑 修复 1: 如果是结局场景，绝对不要初始化游戏逻辑！
        // 防止 GameManager 在结局场景里瞎找 Case 物体，导致报错和循环重载
        if (scene.name == "EndingScene") return;

        if (FindObjectOfType<CaseManager>() != null)
        {
            isInitialized = false;
            StartCoroutine(InitGameSequence());
        }
    }

    void Start()
    {
        // 同样在 Start 里也防一手 (虽然通常 OnSceneLoaded 会先触发)
        if (SceneManager.GetActiveScene().name == "EndingScene") return;

        if (!isInitialized && FindObjectOfType<CaseManager>() != null)
        {
            StartCoroutine(InitGameSequence());
        }
    }

    IEnumerator InitGameSequence()
    {
        isInitialized = true;
        yield return new WaitForSeconds(0.5f);

        caseManager = FindObjectOfType<CaseManager>();
        factionManager = FindObjectOfType<FactionManager>();
        endingManager = FindObjectOfType<EndingManager>();

        if (caseManager == null) yield break;

        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null) audioManager.PlayBGM();

        RefreshSceneReferences();

        // 🛑 修复 2: 如果找不到 Case 物体，立即终止！
        // 绝对不要继续往下执行 LoadCase，否则会触发 "EndGame" -> "重载场景" 的死循环
        if (caseGameObjects.Count == 0)
        {
            Debug.LogError("[GameManager] 严重错误：Case 列表为空！停止初始化。");
            yield break;
        }

        foreach (var caseObj in caseGameObjects)
        {
            if (caseObj != null) caseObj.SetActive(false);
        }

        float timer = 0f;
        while (chatSystem == null && timer < 3.0f)
        {
            chatSystem = FindObjectOfType<ChatSystem>();
            if (chatSystem == null) { yield return null; timer += Time.deltaTime; }
        }

        LoadCase(currentCaseIndex);
    }

    void RefreshSceneReferences()
    {
        if (caseGameObjects != null && caseGameObjects.Count > 0)
        {
            caseGameObjects.RemoveAll(item => item == null);
            if (caseGameObjects.Count > 0)
            {
                Debug.Log($"[GameManager] 使用手动赋值的 {caseGameObjects.Count} 个案件物体。");
                RefreshStageReferencesOnly();
                return;
            }
        }

        Debug.Log("[GameManager] 自动查找场景物体中...");
        caseGameObjects = new List<GameObject>();
        List<GameObject> allRootObjs = new List<GameObject>();
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid()) allRootObjs.AddRange(scene.GetRootGameObjects());

        for (int i = 1; i <= 5; i++)
        {
            string[] possibleNames = new string[] {
                "Case" + i, "Case " + i, "Case_" + i,
                "Case0" + i, "Case 0" + i, "Case_0" + i
            };

            GameObject found = null;
            foreach (var targetName in possibleNames)
            {
                foreach (var root in allRootObjs)
                {
                    if (root.name.IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        found = root;
                        break;
                    }
                    Transform t = root.transform.FindRecursive(targetName);
                    if (t != null)
                    {
                        found = t.gameObject;
                        break;
                    }
                }
                if (found != null) break;
            }

            if (found != null) caseGameObjects.Add(found);
        }

        RefreshStageReferencesOnly();
    }

    void RefreshStageReferencesOnly()
    {
        Transform canvas = GameObject.Find("Screen1_Canvas")?.transform;
        if (canvas == null) canvas = FindObjectOfType<Canvas>()?.transform;

        if (canvas != null)
        {
            if (stage1_Briefing == null) stage1_Briefing = canvas.FindRecursive("Stage1")?.gameObject;
            if (stage2_Storyline == null) stage2_Storyline = canvas.FindRecursive("Stage2")?.gameObject;
            if (stage3_Judgment == null) stage3_Judgment = canvas.FindRecursive("Stage3")?.gameObject;
        }
    }

    public void ResetGame()
    {
        Destroy(gameObject);
        SceneManager.LoadScene(0);
    }

    public void LoadCase(int index)
    {
        currentCaseIndex = index;

        // 1. 检查索引是否越界
        if (currentCaseIndex >= allCases.Count)
        {
            Debug.Log("[GameManager] 所有案件已结束，进入结局。");
            EndGame();
            return;
        }

        // 2. 隐藏上一个案件物体
        if (currentCaseIndex > 0 && currentCaseIndex - 1 < caseGameObjects.Count)
        {
            if (caseGameObjects[currentCaseIndex - 1] != null)
                caseGameObjects[currentCaseIndex - 1].SetActive(false);
        }

        // 3. 准备当前案件物体 (但先别急着激活！)
        GameObject currentCaseObj = null;
        if (currentCaseIndex < caseGameObjects.Count)
        {
            currentCaseObj = caseGameObjects[currentCaseIndex];
            // ❌ 删除原来的激活代码: if (currentCaseObj != null) currentCaseObj.SetActive(true);
        }

        if (currentCaseObj == null)
        {
            Debug.LogError($"[GameManager] 无法加载案件 {index + 1}: 对应的场景物体未找到！");
            return;
        }

        // 4. 加载数据 (这是重点：必须先让 CaseManager 拿到新数据)
        CaseData caseToLoad = allCases[currentCaseIndex];

        Debug.Log($"========== [开始加载案件 {currentCaseIndex + 1}] ==========");
        Debug.Log($"加载的数据: {caseToLoad.name}");
        Debug.Log($"场景物体: {currentCaseObj.name}");
        Debug.Log("===============================================");

        // 先更新 CaseManager 的数据 (此时 currentCase 变成 Case_02)
        caseManager.StartCase(caseToLoad, currentCaseObj);
        factionManager.ClearActiveStorylines();

        // 5. 数据更新完毕后，再激活物体！
        // 这样按钮醒来时，看到的就是最新的 Case_02 数据了
        if (currentCaseObj != null)
        {
            currentCaseObj.SetActive(true);
        }

        currentState = GameState.CaseBriefing;
        if (stage1_Briefing) stage1_Briefing.SetActive(true);
        if (stage2_Storyline) stage2_Storyline.SetActive(false);
        if (stage3_Judgment) stage3_Judgment.SetActive(false);

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
        if (stage1_Briefing) stage1_Briefing.SetActive(false);
        if (stage2_Storyline) stage2_Storyline.SetActive(true);
        if (stage3_Judgment) stage3_Judgment.SetActive(false);
    }

    public void EnterJudgmentPhase()
    {
        if (currentState != GameState.StorylinePhase) return;
        currentState = GameState.JudgmentPhase;
        if (stage1_Briefing) stage1_Briefing.SetActive(false);
        if (stage2_Storyline) stage2_Storyline.SetActive(false);
        if (stage3_Judgment) stage3_Judgment.SetActive(true);
    }

    public void EndCase()
    {
        if (currentState != GameState.JudgmentPhase) return;
        currentState = GameState.CaseWrapUp;
        if (chatSystem != null) chatSystem.ClearTransientMessages();
        factionManager.EvaluatePlayerJudgment();
        if (ResourceManager.Instance != null) ResourceManager.Instance.AddEnergy(1);

        Debug.Log($"[GameManager] 案件结束，准备加载下一个案件 (Index: {currentCaseIndex + 1})");
        LoadCase(currentCaseIndex + 1);
    }

    void EndGame()
    {
        currentState = GameState.GameEnd;
        FinalJsonData = endingManager.GenerateFinalDataForAPI();
        SceneManager.LoadScene("EndingScene");
    }
}

public static class TransformExtensions
{
    public static Transform FindRecursive(this Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0) return child;
            Transform result = child.FindRecursive(name);
            if (result != null) return result;
        }
        return null;
    }
}