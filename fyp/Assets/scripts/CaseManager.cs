using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// ####################################################################
// ## 4.3. CASE MANAGER (案件管理器)
// ####################################################################
public class CaseManager : MonoBehaviour
{
    // ## 修改: 移除 public 引用，改为 private 动态查找 ##
    // public Transform factionButtonsParent; // (旧)
    // public Transform decisionStagesParent; // (旧)

    private Transform factionButtonsParent;
    private Transform decisionStagesParent;

    [Header("当前案件状态")]
    public CaseData currentCase;
    public JudgmentNode currentNode;
    public List<int> playerChoiceIndices; // 记录 0/1 路径

    // 管理器引用
    private EndingManager endingManager;
    private FactionManager factionManager;

    void Awake()
    {
        endingManager = FindObjectOfType<EndingManager>();
        factionManager = FindObjectOfType<FactionManager>();

        if (endingManager == null || factionManager == null)
        {
            Debug.LogError("CaseManager: 未能找到 EndingManager 或 FactionManager!");
        }
    }

    // ## 修改: StartCase 现在接收当前的 GameObject ##
    public void StartCase(CaseData caseData, GameObject currentCaseObject)
    {
        currentCase = caseData;
        currentNode = currentCase.judgmentTreeRoot;
        playerChoiceIndices = new List<int>();

        // 1. 动态查找当前案件的 UI 容器
        // **重要**: 你的 Case1, Case2... 下面的子物体名称必须叫 "options" 和 "DecisionStagesParent"
        if (currentCaseObject != null)
        {
            factionButtonsParent = currentCaseObject.transform.Find("options");
            decisionStagesParent = currentCaseObject.transform.Find("DecisionStagesParent");

            if (factionButtonsParent == null) Debug.LogError($"CaseManager: 在 {currentCaseObject.name} 下找不到名为 'options' 的子物体!");
            if (decisionStagesParent == null) Debug.LogError($"CaseManager: 在 {currentCaseObject.name} 下找不到名为 'DecisionStagesParent' 的子物体!");
        }
        else
        {
            Debug.LogError("CaseManager: StartCase 收到了空的 currentCaseObject!");
            return;
        }

        // 2. 通知 EndingManager 开始记录
        endingManager.StartNewCaseRecord(currentCase.caseID);

        // 3. 设置 Stage 2 (Mask 2) - 链接派系按钮
        if (factionButtonsParent != null)
        {
            // includeInactive = true 以防 options 父物体也是隐藏的
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
        }

        // 4. 设置 Stage 3 (Mask 3) - 激活第一个判案组
        if (decisionStagesParent != null)
        {
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
    }

    // 由 UI_JudgmentButton 调用
    public void SelectChoice(JudgmentChoice choice, int choiceIndex)
    {
        if (GameManager.Instance.CurrentState != GameState.JudgmentPhase) return;

        // 1. 记录路径和民心
        playerChoiceIndices.Add(choiceIndex);
        endingManager.RecordPublicOpinionChange(choice.publicOpinionChange);

        // 2. 停用当前 UI 组
        if (decisionStagesParent != null)
        {
            Transform currentStage = decisionStagesParent.Find(currentNode.stageDescription);
            if (currentStage != null)
            {
                currentStage.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"CaseManager: 找不到要停用的 {currentNode.stageDescription}");
            }
        }

        // 3. 移动到下一个节点
        currentNode = choice.nextNode;

        // 4. 检查是否审判结束
        if (currentNode == null || string.IsNullOrEmpty(currentNode.stageDescription))
        {
            Debug.Log("CaseManager: 下一个节点为 null 或描述为空。结束案件。");
            endingManager.RecordJudgment(playerChoiceIndices);
            GameManager.Instance.EndCase();
        }
        else
        {
            // 激活下一个 UI 组
            if (decisionStagesParent != null)
            {
                Transform nextStage = decisionStagesParent.Find(currentNode.stageDescription);
                if (nextStage != null)
                {
                    nextStage.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogError($"CaseManager: 在 {decisionStagesParent.name} 下找不到名为 {currentNode.stageDescription} 的子物体!");
                }
            }
        }
    }
}