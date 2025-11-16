using UnityEngine;
using System.Collections.Generic;

// ####################################################################
// ## 4.4. FACTION MANAGER (派系管理器)
// ####################################################################
public class FactionManager : MonoBehaviour
{
    public List<FactionStoryline> activeStorylines; // 本案购买的故事线

    // 管理器引用
    private ResourceManager resourceManager;
    private EndingManager endingManager;
    private CaseManager caseManager;
    private ChatSystem chatSystem; // 聊天系统

    void Awake()
    {
        resourceManager = FindObjectOfType<ResourceManager>();
        endingManager = FindObjectOfType<EndingManager>();
        caseManager = FindObjectOfType<CaseManager>();
        chatSystem = FindObjectOfType<ChatSystem>();

        if (chatSystem == null)
        {
            Debug.LogError("FactionManager: 找不到 ChatSystem!");
        }

        ClearActiveStorylines();
    }

    public void ClearActiveStorylines()
    {
        activeStorylines = new List<FactionStoryline>();
    }

    // 由 UI_FactionButton 调用
    public void PurchaseStoryline(FactionStoryline storyline)
    {
        if (GameManager.Instance.CurrentState != GameState.StorylinePhase) return;

        if (resourceManager.SpendEnergy(1))
        {
            activeStorylines.Add(storyline);

            // -------------------------------------------------
            // ## 通知聊天系统 ##
            // -------------------------------------------------
            Debug.Log($"购买了 {storyline.faction} 的故事线。");
            if (chatSystem != null)
            {
                chatSystem.ShowFactionMessages(storyline.chatMessages);
            }
            // -------------------------------------------------

            endingManager.RecordStorylinePurchase(storyline.faction);
        }
        else
        {
            Debug.Log("能量不足，无法购买故事线。");
        }
    }

    // 由 GameManager 在 CaseWrapUp 阶段调用
    public void EvaluatePlayerJudgment()
    {
        List<int> playerPath = caseManager.playerChoiceIndices;
        Debug.Log($"开始评价... 玩家路径: {string.Join(",", playerPath)}");

        foreach (FactionStoryline storyline in activeStorylines)
        {
            FactionType faction = storyline.faction;
            List<int> requiredPath = storyline.requirement.requiredChoiceIndices;

            // 核心比对逻辑 (前缀匹配)
            bool didComply = true;
            if (playerPath.Count < requiredPath.Count)
            {
                didComply = false;
            }
            else
            {
                for (int i = 0; i < requiredPath.Count; i++)
                {
                    if (playerPath[i] != requiredPath[i])
                    {
                        didComply = false;
                        break;
                    }
                }
            }

            // -------------------------------------------------
            // ## 通知聊天系统 ##
            // -------------------------------------------------
            Debug.Log(didComply ? $"你满足了 {faction} 的要求。" : $"你违背了 {faction} 的意愿。");

            if (chatSystem != null)
            {
                if (didComply)
                {
                    chatSystem.ShowEvaluationMessages(storyline.evaluationSuccessMessages);
                }
                else
                {
                    chatSystem.ShowEvaluationMessages(storyline.evaluationFailureMessages);
                }
            }
            // -------------------------------------------------

            endingManager.RecordFactionInfluence(faction, didComply);
        }
    }
}