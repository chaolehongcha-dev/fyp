using UnityEngine;
using System.Collections.Generic;

// ####################################################################
// ## 4.3. CASE MANAGER (案件管理器)
// ## (V6.0 - 稳定版)
// ####################################################################
public class CaseManager : MonoBehaviour
{
    private Transform decisionStagesParent;

    [Header("当前案件状态")]
    public CaseData currentCase;
    public JudgmentNode currentNode;
    public List<int> playerChoiceIndices;

    private EndingManager endingManager;
    private FactionManager factionManager;

    void Awake()
    {
        endingManager = FindObjectOfType<EndingManager>();
        factionManager = FindObjectOfType<FactionManager>();
    }

    public void StartCase(CaseData caseData, GameObject currentCaseObject)
    {
        currentCase = caseData;
        currentNode = currentCase.judgmentTreeRoot;
        playerChoiceIndices = new List<int>();

        if (currentCaseObject != null)
        {
            // 确保 Options 被激活，这样按钮脚本才能运行 Start() 来找我
            Transform optionsTrans = FindChildRecursively(currentCaseObject.transform, "options");
            if (optionsTrans != null) optionsTrans.gameObject.SetActive(true);

            decisionStagesParent = FindChildRecursively(currentCaseObject.transform, "DecisionStagesParent");
        }
        else
        {
            Debug.LogError("CaseManager: currentCaseObject 为空!");
            return;
        }

        if (endingManager) endingManager.StartNewCaseRecord(currentCase.caseID);

        // 设置判案界面
        if (decisionStagesParent != null)
        {
            decisionStagesParent.gameObject.SetActive(true);
            foreach (Transform child in decisionStagesParent) child.gameObject.SetActive(false);

            Transform rootStage = FindChildRecursively(decisionStagesParent, currentNode.stageDescription);
            if (rootStage != null) rootStage.gameObject.SetActive(true);
        }
    }

    // ## 核心: 响应按钮的注册请求 ##
    public void RequestButtonSetup(UI_FactionButton button)
    {
        if (currentCase == null) return;

        // 根据按钮在 Hierarchy 中的顺序来分配对应的 Storyline
        int index = button.transform.GetSiblingIndex();

        Debug.Log($"CaseManager: 为按钮 [{button.name}] 分配数据, 索引: {index}");

        if (index < currentCase.factionStorylines.Count)
        {
            button.Setup(currentCase.factionStorylines[index], factionManager);
            button.gameObject.SetActive(true);
        }
        else
        {
            // 如果按钮数量多于数据数量，隐藏多余的按钮
            button.gameObject.SetActive(false);
        }
    }

    private Transform FindChildRecursively(Transform parent, string nameToFind)
    {
        foreach (Transform child in parent)
        {
            if (child.name.IndexOf(nameToFind, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return child;
            Transform result = FindChildRecursively(child, nameToFind);
            if (result != null) return result;
        }
        return null;
    }

    public void SelectChoice(JudgmentChoice choice, int choiceIndex)
    {
        if (GameManager.Instance.CurrentState != GameState.JudgmentPhase) return;

        playerChoiceIndices.Add(choiceIndex);
        if (endingManager) endingManager.RecordPublicOpinionChange(choice.publicOpinionChange);

        if (decisionStagesParent != null)
        {
            Transform currentStage = FindChildRecursively(decisionStagesParent, currentNode.stageDescription);
            if (currentStage != null) currentStage.gameObject.SetActive(false);
        }

        currentNode = choice.nextNode;

        if (currentNode == null || string.IsNullOrEmpty(currentNode.stageDescription))
        {
            if (endingManager) endingManager.RecordJudgment(playerChoiceIndices);
            GameManager.Instance.EndCase();
        }
        else
        {
            if (decisionStagesParent != null)
            {
                Transform nextStage = FindChildRecursively(decisionStagesParent, currentNode.stageDescription);
                if (nextStage != null) nextStage.gameObject.SetActive(true);
            }
        }
    }
}