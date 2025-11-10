using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 用于 FirstOrDefault

/// <summary>
/// 辅助容器类，用于封装 JSON 文件中的所有案件列表。
/// 必须与 JSON 顶层结构 {"Cases": [...]} 匹配。
/// </summary>
[System.Serializable]
public class CaseDataContainer
{
    // 字段名 "Cases" 必须与 JSON 文件中的顶层键名完全匹配
    public List<CaseData> Cases;
}

/// <summary>
/// 管理所有案件的流程、数据和最终结局的生成。
/// 采用 TextAsset 拖拽方式加载数据。
/// </summary>
public class CaseManager : MonoBehaviour
{
    public static CaseManager Instance { get; private set; }

    [Header("JSON 数据源")]
    [Tooltip("将 CaseData.json 文件作为 TextAsset 拖入此处")]
    public TextAsset caseDataJsonFile; // <-- 拖拽 JSON 文件到这里

    [Header("案件设置")]
    public int totalCases = 3;
    private int _currentCaseIndex = 0; // 当前案件在列表中的索引 (0, 1, 2)

    [Tooltip("存储所有案件的运行时数据")]
    public List<CaseData> allCasesData = new List<CaseData>();

    [Header("UI 引用")]
    public AdjudicationSystemUI adjudicationUI;
    public ChatSystemUI chatUI;

    public CaseData CurrentCaseData => allCasesData[_currentCaseIndex];

    // 追踪当前的 Stage ID，用于数据驱动的分支跳转
    public string CurrentStageID { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        // -------------------------------------------------------------------
        // 核心修改：从拖拽的 TextAsset 加载数据
        // -------------------------------------------------------------------
        if (caseDataJsonFile != null)
        {
            try
            {
                // 使用辅助容器类 CaseDataContainer 来解析 List<CaseData>
                CaseDataContainer container = JsonUtility.FromJson<CaseDataContainer>(caseDataJsonFile.text);

                if (container != null && container.Cases != null)
                {
                    allCasesData = container.Cases;
                    totalCases = allCasesData.Count;
                    Debug.Log($"【JSON 加载成功】从 Inspector 加载了 {totalCases} 个案件。");
                }
                else
                {
                    Debug.LogError("【JSON 加载失败】JSON 格式错误或 CaseDataContainer 解析失败，请检查文件内容是否匹配 C# 结构。");
                    allCasesData = new List<CaseData>();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"【JSON 运行时错误】反序列化失败: {e.Message}");
                allCasesData = new List<CaseData>();
            }
        }
        else
        {
            Debug.LogError("【加载失败】请将 CaseData.json 文件拖入 Case Manager 的 Inspector 字段中。");
            allCasesData = new List<CaseData>();
        }
        // -------------------------------------------------------------------

        if (allCasesData.Count > 0)
        {
            StartNewCase(0); // 从第一个案件开始
        }
        else
        {
            Debug.LogError("未加载任何案件数据，游戏无法开始。");
        }
    }

    /// <summary>
    /// 开始一个新的案件，并刷新UI。
    /// </summary>
    public void StartNewCase(int index)
    {
        if (index >= allCasesData.Count) return;

        _currentCaseIndex = index;

        // 1. 设置 UI
        if (adjudicationUI != null)
        {
            adjudicationUI.LoadCaseData(CurrentCaseData);

            // 2. 设置当前 Stage ID (从案件数据中获取第一个 Stage ID)
            if (CurrentCaseData.decisionStages.Count > 0)
            {
                CurrentStageID = CurrentCaseData.decisionStages[0].stageID;
            }
            else
            {
                // 如果没有 Stage，案件流程可能存在问题
                CurrentStageID = "END_CASE";
            }
        }

        Debug.Log($"【案件加载】案件 {CurrentCaseData.caseID} 已加载: {CurrentCaseData.caseTitle}");
    }

    /// <summary>
    /// 触发故事线，并通知聊天系统发送派系信息。
    /// </summary>
    public void TriggerStoryline(int factionIndex)
    {
        if (chatUI != null)
        {
            chatUI.SendNewMessage(factionIndex);
        }
        Debug.Log($"【故事线触发】派系 {factionIndex} 已触发。");
    }

    /// <summary>
    /// 记录玩家在当前案件的判决选择。
    /// </summary>
    public void RecordVerdictChoice(string choice)
    {
        CurrentCaseData.playerVerdictChoices.Add(choice);
    }

    /// <summary>
    /// 判定故事线是否完成，并进入下一个案件或结算结局。
    /// </summary>
    public void FinalizeVerdict()
    {
        // ... (此处省略复杂的判定和剧情生成逻辑)

        Debug.Log($"【案件结算】案件 {_currentCaseIndex + 1} 结算完成。");

        if (_currentCaseIndex < totalCases - 1)
        {
            StartNewCase(_currentCaseIndex + 1);
        }
        else
        {
            Debug.Log("【最终结局】所有案件完成，生成最终结局！");
            // GenerateFinalEnding();
        }
    }
}