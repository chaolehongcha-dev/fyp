using UnityEngine;
using System.Collections.Generic;

// ####################################################################
// ## 4.4. FACTION MANAGER (派系管理器)
// ####################################################################
public class FactionManager : MonoBehaviour
{
    public List<FactionStoryline> activeStorylines;

    private ResourceManager resourceManager;
    private EndingManager endingManager;
    private CaseManager caseManager;
    private ChatSystem chatSystem;

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

    // ## 修改: 返回 bool 类型，指示购买是否成功 ##
    public bool PurchaseStoryline(FactionStoryline storyline)
    {
        if (GameManager.Instance.CurrentState != GameState.StorylinePhase) return false;

        // 检查是否已经购买过 (防止重复购买)
        if (activeStorylines.Contains(storyline))
        {
            return true; // 已经买过了，视为成功但不扣费
        }

        if (resourceManager.SpendEnergy(1))
        {
            activeStorylines.Add(storyline);

            // 通知聊天系统
            Debug.Log($"购买了 {storyline.faction} 的故事线。");
            if (chatSystem != null)
            {
                chatSystem.ShowFactionMessages(storyline.chatMessages);
            }

            endingManager.RecordStorylinePurchase(storyline.faction);
            return true; // 购买成功
        }
        else
        {
            Debug.Log("能量不足，无法购买故事线。");
            return false; // 购买失败
        }
    }

    public void EvaluatePlayerJudgment()
    {
        List<int> playerPath = caseManager.playerChoiceIndices;
        Debug.Log($"开始评价... 玩家路径: {string.Join(",", playerPath)}");

        foreach (FactionStoryline storyline in activeStorylines)
        {
            FactionType faction = storyline.faction;
            List<int> requiredPath = storyline.requirement.requiredChoiceIndices;

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

            Debug.Log(didComply ? $"你满足了 {faction} 的要求。" : $"你违背了 {faction} 的意愿。");

            if (chatSystem != null)
            {
                if (didComply)
                {
                    chatSystem.ShowEvaluationMessages(faction, storyline.evaluationSuccessMessages);
                }
                else
                {
                    chatSystem.ShowEvaluationMessages(faction, storyline.evaluationFailureMessages);
                }
            }

            endingManager.RecordFactionInfluence(faction, didComply);
        }
    }
}