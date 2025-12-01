using UnityEngine;
using System.Collections.Generic;

// ####################################################################
// ## 1. ENUMS (枚举)
// ####################################################################

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
    // 这里引用的现在是独立的 FactionStoryline 类 (在 FactionStoryline.cs 中)
    public List<FactionStoryline> factionStorylines;
}

// (原先在这里的 FactionStoryline 类已经被移除，移到了独立文件中)

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
    public int publicOpinionChange;
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
    public string faction;
    public int influenceScore;
}

[System.Serializable]
public class CaseRecord
{
    public string caseID;
    public List<string> purchasedStorylines = new List<string>();
    public List<int> finalJudgmentPath = new List<int>();
}