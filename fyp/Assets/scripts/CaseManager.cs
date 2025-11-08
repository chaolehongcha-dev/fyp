using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理所有案件的流程、数据和最终结局的生成。
/// </summary>
public class CaseManager : MonoBehaviour
{
    public static CaseManager Instance { get; private set; }

    [Header("案件设置")]
    public int totalCases = 3;
    private int _currentCaseIndex = 0; // 当前案件在列表中的索引 (0, 1, 2)

    [Tooltip("预设的案件初始数据，大小必须是 totalCases")]
    public List<CaseData> allCasesData = new List<CaseData>();

    [Header("UI 引用")]
    public AdjudicationSystemUI adjudicationUI;
    public ChatSystemUI chatUI; // 假设有一个 ChatSystemUI.cs 脚本

    public CaseData CurrentCaseData => allCasesData[_currentCaseIndex];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 可选
        }
    }

    private void Start()
    {
        // 确保数据已加载（在实际项目中，此处应调用 CSV DataLoader）
        if (allCasesData.Count == 0)
        {
            // 示例：手动创建3个空案件数据
            allCasesData.Add(new CaseData { caseID = 1, caseTitle = "救生舱盗窃案" });
            allCasesData.Add(new CaseData { caseID = 2, caseTitle = "能源分配渎职案" });
            allCasesData.Add(new CaseData { caseID = 3, caseTitle = "数字幽灵伪造案" });
        }

        StartNewCase(0); // 从第一个案件开始
    }

    /// <summary>
    /// 开始一个新的案件，并刷新UI。
    /// </summary>
    public void StartNewCase(int index)
    {
        if (index >= allCasesData.Count) return;

        _currentCaseIndex = index;

        if (adjudicationUI != null)
        {
            adjudicationUI.LoadCaseData(CurrentCaseData);
        }

        Debug.Log($"【案件加载】案件 {CurrentCaseData.caseID} 已加载: {CurrentCaseData.caseTitle}");
    }

    /// <summary>
    /// 触发故事线，并通知聊天系统发送派系信息。
    /// </summary>
    public void TriggerStoryline(int factionIndex)
    {
        // 实际：查找并加载对应的聊天信息
        if (chatUI != null)
        {
            // 假设 chatUI 有一个 SendFactionMessage 方法
            // chatUI.SendFactionMessage(factionIndex, CurrentCaseData.caseID);
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
        // 1. 核心判定逻辑 (省略，但需在此处实现)
        // 例如：检查 CurrentCaseData.playerVerdictChoices 是否满足所有已触发的故事线要求。

        // 2. 结算并生成剧情线
        // CurrentCaseData.caseEndingPlot = "Plot A"; 

        Debug.Log($"【案件结算】案件 {_currentCaseIndex + 1} 结算完成。");

        // 3. 进入下一案件或结算总结局
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