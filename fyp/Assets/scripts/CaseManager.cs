using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// ####################################################################
// ## 4.3. CASE MANAGER (案件管理器)
// ####################################################################
public class CaseManager : MonoBehaviour
{
    [Header("UI 引用")]
    public Transform factionButtonsParent; // (Stage 2) 拖入 'options' GameObject
    public Transform decisionStagesParent; // **(Stage 3) 拖入 'DecisionStagesParent' GameObject**

    [Header("当前案件状态")]
    public CaseData currentCase;
    public JudgmentNode currentNode;
    public List<int> playerChoiceIndices; // 记录 0/1 路径

    // 管理器引用
    private EndingManager endingManager;
    private FactionManager factionManager; // 需要用来设置按钮

    void Awake()
    {
        // 自动查找其他管理器
        endingManager = FindObjectOfType<EndingManager>();
        factionManager = FindObjectOfType<FactionManager>();

        if (endingManager == null || factionManager == null)
        {
            Debug.LogError("CaseManager: 未能找到 EndingManager 或 FactionManager!");
        }
    }

    public void StartCase(CaseData caseData)
    {
        currentCase = caseData;
        currentNode = currentCase.judgmentTreeRoot;
        playerChoiceIndices = new List<int>(); // 确保每次都初始化

        // 1. 通知 EndingManager 开始记录
        endingManager.StartNewCaseRecord(currentCase.caseID);

        // 2. 设置 Stage 1 (Mask 1)
        // (无 - 'caseSummary' 逻辑已移除)

        // 3. 设置 Stage 2 (Mask 2) - 链接派系按钮
        UI_FactionButton[] factionButtons = factionButtonsParent.GetComponentsInChildren<UI_FactionButton>(true);
        for (int i = 0; i < factionButtons.Length; i++)
        {
            if (i < currentCase.factionStorylines.Count)
            {
                factionButtons[i].Setup(currentCase.factionStorylines[i], factionManager);
                factionButtons[i].gameObject.SetActive(true);
                factionButtons[i].GetComponent<Button>().interactable = true;
            }
            else
            {
                factionButtons[i].gameObject.SetActive(false);
            }
        }

        // 4. 设置 Stage 3 (Mask 3) - 激活第一个判案组
        if (decisionStagesParent == null)
        {
            Debug.LogError("CaseManager: decisionStagesParent 未链接!");
            return;
        }

        // 停用所有决策组
        foreach (Transform child in decisionStagesParent)
        {
            child.gameObject.SetActive(false);
        }

        // 激活第一个
        Transform rootStage = decisionStagesParent.Find(currentNode.stageDescription);
        if (rootStage != null)
        {
            rootStage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError($"CaseManager: 在 {decisionStagesParent.name} 下找不到名为 {currentNode.stageDescription} 的子物体!");
        }
    }

    // 由 UI_JudgmentButton 调用
    public void SelectChoice(JudgmentChoice choice, int choiceIndex)
    {
        if (GameManager.Instance.CurrentState != GameState.JudgmentPhase) return;

        // 1. 记录路径和民心 (这是正确的)
        playerChoiceIndices.Add(choiceIndex);
        endingManager.RecordPublicOpinionChange(choice.publicOpinionChange);

        // 2. 停用当前 UI 组
        Transform currentStage = decisionStagesParent.Find(currentNode.stageDescription);
        if (currentStage != null)
        {
            currentStage.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"CaseManager: 找不到要停用的 {currentNode.stageDescription}");
        }

        // 3. 移动到下一个节点
        currentNode = choice.nextNode;

        // 4. 检查是否审判结束
        // ####################################################################
        // ## 修改 (根据你的要求): ##
        // ## 检查下一个节点是否为 null，或者下一个节点的 stageDescription 是否为 null 或空 ##
        // ####################################################################
        if (currentNode == null || string.IsNullOrEmpty(currentNode.stageDescription))
        {
            // 审判结束
            Debug.Log("CaseManager: 下一个节点为 null 或描述为空。结束案件。");
            endingManager.RecordJudgment(playerChoiceIndices); // 记录最终路径
            GameManager.Instance.EndCase(); // 通知 GameManager
        }
        else
        {
            // 激活下一个 UI 组
            Transform nextStage = decisionStagesParent.Find(currentNode.stageDescription);
            if (nextStage != null)
            {
                nextStage.gameObject.SetActive(true);
            }
            else
            {
                // 如果 stageDescription 不是 null 但依然找不到 (例如 "panjue" 拼写错误)，
                // 这里仍然会报错，这是正确的。
                Debug.LogError($"CaseManager: 在 {decisionStagesParent.name} 下找不到名为 {currentNode.stageDescription} 的子物体!");
            }
        }
    }
}