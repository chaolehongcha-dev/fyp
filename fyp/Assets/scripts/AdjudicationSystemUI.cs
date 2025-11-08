using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 案件裁决系统的UI交互管理。依赖 CaseManager 和 EnergySystem。
/// </summary>
public class AdjudicationSystemUI : MonoBehaviour
{
    private CaseData currentCase;

    [Header("阶段面板")]
    public GameObject panelA_PreTrial; // 庭前阶段面板（蒙版A）
    public GameObject panelB_Trial;    // 开庭阶段面板（蒙版B）

    [Header("故事线按钮管理")]
    [Tooltip("按顺序拖入派系按钮（0, 1, 2）")]
    public Button[] allStorylineButtons = new Button[3];
    public int storylineEnergyCost = 1;

    [Header("裁决流程")]
    public Transform decisionStagesParent; // 所有裁决阶段的父物体
    private int currentStage = 0;

    /// <summary>
    /// 由 CaseManager 调用，用于切换案件时刷新UI状态。
    /// </summary>
    public void LoadCaseData(CaseData data)
    {
        currentCase = data;

        // 1. 设置 UI 阶段可见性
        panelA_PreTrial.SetActive(!data.hasEnteredTrial);
        panelB_Trial.SetActive(data.hasEnteredTrial);

        // 2. 禁用已开启的故事线按钮
        bool trialStarted = data.hasEnteredTrial;
        allStorylineButtons[0].interactable = !(data.isStoryline1Triggered || trialStarted);
        allStorylineButtons[1].interactable = !(data.isStoryline2Triggered || trialStarted);
        allStorylineButtons[2].interactable = !(data.isStoryline3Triggered || trialStarted);

        // 3. 重置裁决阶段 UI
        currentStage = 0;
        for (int i = 0; i < decisionStagesParent.childCount; i++)
        {
            decisionStagesParent.GetChild(i).gameObject.SetActive(false);
        }

        // 4. 如果已进入开庭阶段，显示裁决阶段（简化为显示第一个）
        if (trialStarted && decisionStagesParent.childCount > 0)
        {
            decisionStagesParent.GetChild(0).gameObject.SetActive(true);
        }
    }

    // -----------------------------------------------------------
    // 按钮一：开启故事线 (FactionIndex: 0, 1, 2)
    // -----------------------------------------------------------
    // 修正：方法只接受 int 参数，提高了 Inspector 兼容性。
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
        if (decisionStagesParent.childCount > 0)
        {
            decisionStagesParent.GetChild(0).gameObject.SetActive(true);
        }
    }

    // -----------------------------------------------------------
    // 按钮三：裁决选择 (进入下一阶段)
    // -----------------------------------------------------------
    public void OnDecisionClicked(string choiceResult)
    {
        if (currentCase == null) return;

        // 1. 记录玩家的选择
        CaseManager.Instance.RecordVerdictChoice(choiceResult);

        // 2. 切换到下一阶段
        currentStage++;

        if (currentStage < decisionStagesParent.childCount)
        {
            // 隐藏上一个，显示下一个
            decisionStagesParent.GetChild(currentStage - 1).gameObject.SetActive(false);
            decisionStagesParent.GetChild(currentStage).gameObject.SetActive(true);
        }
        else
        {
            // 3. 所有阶段完成，进行最终结算
            decisionStagesParent.GetChild(currentStage - 1).gameObject.SetActive(false);
            CaseManager.Instance.FinalizeVerdict();
        }
    }
}