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
    public TextAsset caseDataJsonFile;

    [Header("案件设置")]
    public int totalCases = 3;
    private int _currentCaseIndex = 0;

    [Tooltip("存储所有案件的运行时数据")]
    public List<CaseData> allCasesData = new List<CaseData>();

    [Header("UI 引用")]
    public AdjudicationSystemUI adjudicationUI;
    public ChatSystemUI chatUI;

    public CaseData CurrentCaseData => allCasesData[_currentCaseIndex];

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
        }
    }

    private void Start()
    {
        LoadCaseDataFromJSON();

        if (allCasesData.Count > 0)
        {
            StartNewCase(0);
        }
        else
        {
            Debug.LogError("未加载任何案件数据，游戏无法开始。");
        }
    }

    /// <summary>
    /// 从 JSON 加载案件列表
    /// </summary>
    private void LoadCaseDataFromJSON()
    {
        if (caseDataJsonFile == null)
        {
            Debug.LogError("【加载失败】请将 CaseData.json 文件拖入 Case Manager 的 Inspector 字段中。");
            return;
        }

        try
        {
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

    /// <summary>
    /// 开始一个新的案件，并刷新 UI
    /// </summary>
    public void StartNewCase(int index)
    {
        if (index >= allCasesData.Count) return;

        _currentCaseIndex = index;

        if (adjudicationUI != null)
        {
            adjudicationUI.LoadCaseData(CurrentCaseData);

            if (CurrentCaseData.decisionStages.Count > 0)
                CurrentStageID = CurrentCaseData.decisionStages[0].stageID;
            else
                CurrentStageID = "END_CASE";
        }

        Debug.Log($"【案件加载】案件 {CurrentCaseData.caseID} 已加载: {CurrentCaseData.caseTitle}");
    }

    /// <summary>
    /// 触发故事线，并通知聊天系统发送派系信息
    /// </summary>
    public void TriggerStoryline(int factionIndex, ChatGroup group = null)
    {
        if (chatUI != null)
        {
            // 如果没有传入 ChatGroup，则生成默认消息
            if (group == null)
            {
                group = new ChatGroup
                {
                    groupName = "派系消息",
                    messages = new List<ChatMessage>
                    {
                        new ChatMessage { speakerName = "部长", content = $"派系 {factionIndex} 的默认消息" }
                    }
                };
            }

            chatUI.ReceiveNewMessage(factionIndex, group);
        }

        Debug.Log($"【故事线触发】派系 {factionIndex} 已触发。");
    }

    /// <summary>
    /// 记录玩家在当前案件的判决选择
    /// </summary>
    public void RecordVerdictChoice(string choice)
    {
        CurrentCaseData.playerVerdictChoices.Add(choice);
    }

    /// <summary>
    /// 判定故事线是否完成，并进入下一个案件或结算结局
    /// </summary>
    public void FinalizeVerdict()
    {
        Debug.Log($"【案件结算】案件 {_currentCaseIndex + 1} 结算完成。");

        if (_currentCaseIndex < totalCases - 1)
        {
            StartNewCase(_currentCaseIndex + 1);
        }
        else
        {
            Debug.Log("【最终结局】所有案件完成，生成最终结局！");
        }
    }
}
