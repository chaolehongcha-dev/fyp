using UnityEngine;
using System.Collections.Generic;

// ####################################################################
// ## 1. ENUMS (枚举)
// ####################################################################

public enum FactionType { Truth, Love, Peace } // 真理派, 友爱部, 和平部

public enum GameState { CaseBriefing, StorylinePhase, JudgmentPhase, CaseWrapUp, GameEnd }

// ####################################################################
// ## 2. SCRIPTABLE OBJECT DEFINITIONS (ScriptableObject 定义)
// ####################################################################

// 3.1. 案件数据
[CreateAssetMenu(fileName = "Case_01", menuName = "Lex Speculum/Case Data")]
public class CaseData : ScriptableObject
{
    [Header("案件基础信息")]
    public string caseID; // e.g., "Case_01_Theft"
    [TextArea(5, 10)]
    public string caseSummary; // 在 Mask 1 (Stage1) 显示

    [Header("判案流程 (Mask 3)")]
    public JudgmentNode judgmentTreeRoot; // 判案分支树的根节点

    [Header("故事线 (Mask 2)")]
    public List<FactionStoryline> factionStorylines; // 本案可供选择的派系故事线
}

// 3.4. 派系故事线
[CreateAssetMenu(fileName = "Case01_Truth", menuName = "Lex Speculum/Faction Storyline")]
public class FactionStoryline : ScriptableObject
{
    public FactionType faction; // 派系

    [Header("购买后在聊天窗口显示")]
    public List<ChatMessage> chatMessages;

    [Header("派系要求 (0=左, 1=右)")]
    public FactionRequirement requirement;
}

// ####################################################################
// ## 3. SERIALIZABLE CLASSES (可序列化的类)
// ####################################################################

// 3.2. 判案节点 (用于 CaseData)
[System.Serializable]
public class JudgmentNode
{
    public string stageDescription; // 阶段描述 (e.g., "panjue")
                                    // **重要: 必须与 DecisionStagesParent 下的子物体名称一致**

    // ## 已删除 ##
    // [Header("链接到场景中的UI组")]
    // public GameObject stageUIGroup; // <-- 这个字段已被移除，因为它无法工作

    [Header("此阶段的选项 (左=0, 右=1)")]
    public List<JudgmentChoice> choices; // 必须有两个选项
}

// 3.3. 判案选项 (用于 JudgmentNode)
[System.Serializable]
public class JudgmentChoice
{
    public string choiceID; // 调试用 ID
    public string choiceText; // *注意: 这个现在由你场景中的按钮文本决定*

    [Header("点击后跳转的下一个节点")]
    public JudgmentNode nextNode; // 如果为 null，则审判结束

    [Header("结局统计")]
    public int publicOpinionChange; // 影响民心 (e.g., -1, 0, 1)
}

// 3.5. 派系要求 (用于 FactionStoryline)
[System.Serializable]
public class FactionRequirement
{
    // e.g., [0] = 要求第一个选左
    // e.g., [1, 0] = 要求第一个选右, 第二个选左
    public List<int> requiredChoiceIndices;
}

// 聊天消息 (用于 FactionStoryline)
[System.Serializable]
public class ChatMessage
{
    public FactionType sender; // 谁发送的
    [TextArea(3, 5)]
    public string messageContent;
}


// ####################################################################
// ## 4. ENDING DATA STRUCTURES (结局数据结构)
// ####################################################################

// (来自 4.6. 用于最后生成 JSON)

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
    public string faction; // e.g., "Truth", "Love", "Peace"
    public int influenceScore; // e.g., 2, -1, 0
}

[System.Serializable]
public class CaseRecord
{
    public string caseID;
    public List<string> purchasedStorylines = new List<string>(); // e.g., ["Truth", "Love"]
    public List<int> finalJudgmentPath = new List<int>();      // e.g., [1, 0, 1]
}