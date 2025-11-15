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

    // ## 修改: Start() -> Awake() ##
    void Awake()
    {
        resourceManager = FindObjectOfType<ResourceManager>();
        endingManager = FindObjectOfType<EndingManager>();
        caseManager = FindObjectOfType<CaseManager>();

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

        // 1. 尝试花费能量
        if (resourceManager.SpendEnergy(1)) // 能量花费固定为 1
        {
            // 2. 购买成功
            activeStorylines.Add(storyline);

            // 3. (通知聊天系统显示 storyline.chatMessages)
            Debug.Log($"购买了 {storyline.faction} 的故事线。");
            // 在这里通知聊天系统
            // ChatSystem.Instance.ShowMessages(storyline.chatMessages);

            // 4. 通知 EndingManager 记录
            endingManager.RecordStorylinePurchase(storyline.faction);
        }
        else
        {
            // 能量不足
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
                didComply = false; // 玩家的步骤不够
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

            // (发送评价到聊天系统)
            string evalMessage = didComply ? $"你满足了 {faction} 的要求。" : $"你违背了 {faction} 的意愿。";
            Debug.Log(evalMessage);

            // TODO: (在这里通知你的聊天系统UI显示 *评价* 消息)
            // ChatSystem.Instance.ShowEvaluationMessage(faction, didComply);

            // 记录结果
            endingManager.RecordFactionInfluence(faction, didComply);
        }
    }
}