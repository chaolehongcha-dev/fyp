using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // 必须导入
using System.Linq; // 必须导入

/// <summary>
/// 案件裁决系统的UI交互管理。已重构为数据驱动的 Stage ID 跳转。
/// </summary>
public class AdjudicationSystemUI : MonoBehaviour
{
    private CaseData currentCase;

    // 存储 Stage ID 和实际 GameObject 的映射，用于快速查找要显示的 Panel
    private Dictionary<string, GameObject> stagePanelsMap = new Dictionary<string, GameObject>();

    // 您的 UI 变量 (保留并使用它们来切换主面板)
    [Header("阶段面板")]
    public GameObject panelA_PreTrial; // 庭前阶段面板
    public GameObject panelB_Trial;    // 开庭阶段面板

    [Header("故事线按钮管理")]
    [Tooltip("按顺序拖入派系按钮（0, 1, 2）")]
    public Button[] allStorylineButtons = new Button[3];
    public int storylineEnergyCost = 1;

    // [Header("裁决流程")]
    public Transform decisionStagesParent; // 所有裁决阶段的父物体
    // private int currentStage = 0; // 已废弃，使用 CaseManager.CurrentStageID

    /// <summary>
    /// 由 CaseManager 调用，用于切换案件时刷新UI状态。
    /// </summary>
    public void LoadCaseData(CaseData data)
    {
        currentCase = data;

        bool trialStarted = data.hasEnteredTrial;

        // 1. 设置 UI 阶段可见性 (使用您的原逻辑)
        panelA_PreTrial.SetActive(!trialStarted);
        panelB_Trial.SetActive(trialStarted);

        // 2. 禁用已开启的故事线按钮
        allStorylineButtons[0].interactable = !(data.isStoryline1Triggered || trialStarted);
        allStorylineButtons[1].interactable = !(data.isStoryline2Triggered || trialStarted);
        allStorylineButtons[2].interactable = !(data.isStoryline3Triggered || trialStarted);

        // 3. 构建 Stage 映射字典并隐藏所有 Stage
        stagePanelsMap.Clear();

        // 警告：Hierarchy 中的 Stage Panel 数量必须与 CaseData.decisionStages 中的定义数量一致且顺序对应
        if (data.decisionStages.Count != decisionStagesParent.childCount)
        {
            Debug.LogError("致命错误：Hierarchy Stage 数量与 CaseData 定义数量不匹配！");
        }

        for (int i = 0; i < decisionStagesParent.childCount; i++)
        {
            GameObject stageObject = decisionStagesParent.GetChild(i).gameObject;
            stageObject.SetActive(false); // 默认隐藏所有 Stage

            if (i < data.decisionStages.Count)
            {
                string stageId = data.decisionStages[i].stageID;
                stagePanelsMap.Add(stageId, stageObject);
            }
        }

        // 4. 如果已进入开庭阶段，显示第一个裁决 Stage
        if (trialStarted && data.decisionStages.Count > 0)
        {
            // CaseManager 应该已经设置了 CurrentStageID
            string startID = CaseManager.Instance.CurrentStageID;

            if (stagePanelsMap.ContainsKey(startID))
            {
                stagePanelsMap[startID].SetActive(true);
            }
        }
    }

    // -----------------------------------------------------------
    // 按钮一：开启故事线 (FactionIndex: 0, 1, 2)
    // -----------------------------------------------------------
    public void OnStartStorylineClicked(int factionIndex)
    {
        if (currentCase == null || EnergySystem.Instance == null || currentCase.hasEnteredTrial) return;

        if (factionIndex < 0 || factionIndex >= allStorylineButtons.Length) return;

        if (EnergySystem.Instance.TryConsumeEnergy(storylineEnergyCost))
        {
            // 1. 更新案件数据状态
            if (factionIndex == 0) currentCase.isStoryline1Triggered = true;
            else if (factionIndex == 1) currentCase.isStoryline2Triggered = true;
            else if (factionIndex == 2) currentCase.isStoryline3Triggered = true;

            // 2. 禁用被点击的按钮
            Button clickedButton = allStorylineButtons[factionIndex];
            if (clickedButton != null)
            {
                clickedButton.interactable = false;
            }
            // 3. 通知 CaseManager 触发聊天系统
            CaseManager.Instance.TriggerStoryline(factionIndex);
        }
    }

    // -----------------------------------------------------------
    // 按钮二：开庭 (阶段切换)
    // -----------------------------------------------------------
    public void OnStartTrialClicked(Button startTrialButton)
    {
        if (currentCase == null) return;

        // 1. 更新案件数据状态
        currentCase.hasEnteredTrial = true;

        // 2. 切换阶段 (蒙版A -> 蒙版B)
        panelA_PreTrial.SetActive(false);
        panelB_Trial.SetActive(true);

        // 3. 禁用开庭按钮
        if (startTrialButton != null)
        {
            startTrialButton.interactable = false;
        }

        // 4. 初始化第一个裁决阶段UI
        if (currentCase.decisionStages.Count > 0)
        {
            string firstID = currentCase.decisionStages[0].stageID;

            // 更新全局 Stage ID
            CaseManager.Instance.CurrentStageID = firstID;

            if (stagePanelsMap.ContainsKey(firstID))
            {
                stagePanelsMap[firstID].SetActive(true);
            }
        }
    }

    // -----------------------------------------------------------
    // 按钮三：裁决选择 (数据驱动核心)
    // -----------------------------------------------------------
    public void OnDecisionClicked(string choiceResult)
    {
        // 必须从 CaseManager 获取当前的 Stage ID
        string currentID = CaseManager.Instance?.CurrentStageID;
        if (currentCase == null || string.IsNullOrEmpty(currentID)) return;

        // 1. 记录玩家的选择
        CaseManager.Instance.RecordVerdictChoice(choiceResult);

        // 2. 找到当前 Stage 的数据结构
        DecisionStage currentStageData = currentCase.decisionStages.FirstOrDefault(s => s.stageID == currentID);
        if (currentStageData == null) return;

        // 3. 找到玩家选择的跳转规则 (例如：选择 "Guilty" 后的规则)
        ChoiceRule rule = currentStageData.choices.FirstOrDefault(c => c.choiceResult == choiceResult);
        if (rule == null)
        {
            Debug.LogError($"未找到裁决结果 '{choiceResult}' 的跳转规则！(Stage ID: {currentID})");
            return;
        }

        string nextID = rule.nextStageID;

        // 4. 隐藏当前的 Stage UI
        if (stagePanelsMap.ContainsKey(currentID))
        {
            stagePanelsMap[currentID].SetActive(false);
        }

        // 5. 根据下一 Stage ID 进行跳转
        if (nextID == "END_CASE") // 终点判断：例如选择了 "NotGuilty"
        {
            Debug.Log("选择 END_CASE，案件结算。");
            CaseManager.Instance.FinalizeVerdict();
        }
        else if (stagePanelsMap.ContainsKey(nextID)) // 分支跳转：例如选择了 "Guilty"，跳转到 "Sentencing"
        {
            // 跳转到新的 Stage
            stagePanelsMap[nextID].SetActive(true);
            CaseManager.Instance.CurrentStageID = nextID; // 更新全局 Stage ID
            Debug.Log($"跳转到 Stage: {nextID}");
        }
        else
        {
            Debug.LogError($"跳转目标 Stage ID '{nextID}' 无效或未找到UI面板。请检查 CaseData 设置。");
        }
    }
}