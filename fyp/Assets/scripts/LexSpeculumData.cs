using UnityEngine;
using System.Collections.Generic;

// ####################################################################
// ## 1. ENUMS (枚举)
// ####################################################################

// ## 修复: FactionType 现在包含所有4个派系 ##
public enum FactionType { Truth, Order, Love, Peace } // 真理部, 秩序部, 友爱部, 和平部
public enum GameState { CaseBriefing, StorylinePhase, JudgmentPhase, CaseWrapUp, GameEnd }
// ChatSpeaker 枚举在 ChatSystem.cs 中定义

// ####################################################################
// ## 2. SCRIPTABLE OBJECT DEFINITIONS (ScriptableObject 定义)
// ####################################################################

// 3.1. 案件数据
[CreateAssetMenu(fileName = "Case_01", menuName = "Lex Speculum/Case Data")]
public class CaseData : ScriptableObject
{
    [Header("案件基础信息")]
    public string caseID; // e.g., "Case_01_Theft"

    [Header("初始简报/教程消息")]
    public List<ChatMessage> briefingMessages; // 用于 Stage 1

    [Header("判案流程 (Mask 3)")]
    public JudgmentNode judgmentTreeRoot; // 判案分支树的根节点

    [Header("故事线 (Mask 2)")]
    public List<FactionStoryline> factionStorylines; // 本案可供选择的派系故事线
}

// 3.4. 派系故事线
[CreateAssetMenu(fileName = "Case01_Truth", menuName = "Lex Speculum/Faction Storyline")]
public class FactionStoryline : ScriptableObject
{
    // ## 修改: 现在 Order, Love, Peace 是可购买的 ##
    public FactionType faction; // 派系 (应该是 Order, Love 或 Peace)

    [Header("购买后在聊天窗口显示")]
    public List<ChatMessage> chatMessages;

    [Header("派系要求 (0=左, 1=右)")]
    public FactionRequirement requirement;

    [Header("派系评价 (判案后)")]
    public List<ChatMessage> evaluationSuccessMessages;
    public List<ChatMessage> evaluationFailureMessages;
}

// ####################################################################
// ## 3. SERIALIZABLE CLASSES (可序列化的类)
// ####################################################################

// 3.2. 判案节点 (用于 CaseData)
[System.Serializable]
public class JudgmentNode
{
    public string stageDescription;
    public List<JudgmentChoice> choices;
}

// 3.3. 判案选项 (用于 JudgmentNode)
[System.Serializable]
public class JudgmentChoice
{
    public string choiceID;
    public string choiceText;
    public JudgmentNode nextNode;
    public int publicOpinionChange; // 民心 (旧)

    // ## 新增: 每个选项对四大派系的即时权力影响 ##
    [Header("派系权力变化")]
    public int truthInfluenceChange;
    public int orderInfluenceChange;
    public int loveInfluenceChange;
    public int peaceInfluenceChange;
}

// 3.5. 派系要求 (用于 FactionStoryline)
[System.Serializable]
public class FactionRequirement
{
    public List<int> requiredChoiceIndices;
}

// 聊天消息 (用于 FactionStoryline)
[System.Serializable]
public class ChatMessage
{
    public ChatSpeaker sender; // (在 ChatSystem.cs 中定义)

    [TextArea(3, 5)]
    public string messageContent;
}


// ####################################################################
// ## 4. ENDING DATA STRUCTURES (结局数据结构)
// ## (修复: 这些是 EndingManager.cs 需要的类)
// ####################################################################

[System.Serializable]
public class GameEndingData
{
    public int totalPublicOpinion;
    public List<FactionInfluenceEntry> factionInfluences = new List<FactionInfluenceEntry>();
    public List<CaseRecord> caseRecords = new List<CaseRecord>();
}

[System.Serializable]
public class FactionInfluenceEntry
{
    // ## 修改: 更新注释 ##
    public string faction; // e.g., "Truth", "Order", "Love", "Peace"
    public int influenceScore; // e.g., 2, -1, 0
}

[System.Serializable]
public class CaseRecord
{
    public string caseID;
    public List<string> purchasedStorylines = new List<string>(); // e.g., ["Truth", "Love"]
    public List<int> finalJudgmentPath = new List<int>();      // e.g., [1, 0, 1]
}