using UnityEngine;
using System.Collections.Generic;

// ####################################################################
// ## 独立文件: 派系故事线定义
// ## (文件名必须是 FactionStoryline.cs)
// ####################################################################
[CreateAssetMenu(fileName = "NewFactionStoryline", menuName = "Lex Speculum/Faction Storyline")]
public class FactionStoryline : ScriptableObject
{
    [Header("所属派系")]
    public FactionType faction; // 引用 CaseData.cs 中的枚举

    [Header("购买后在聊天窗口显示")]
    public List<ChatMessage> chatMessages;

    [Header("派系要求 (0=左, 1=右)")]
    public FactionRequirement requirement;

    [Header("派系评价 (判案后)")]
    public List<ChatMessage> evaluationSuccessMessages;
    public List<ChatMessage> evaluationFailureMessages;
}