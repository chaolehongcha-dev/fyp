using System.Collections.Generic;

[System.Serializable]
public class CaseData
{
    public int caseID; // 案件编号 (1, 2, 3)
    public string caseTitle; // 案件名称

    // 庭前阶段数据
    public bool isStoryline1Triggered = false; // 派系1是否已开启
    public bool isStoryline2Triggered = false; // 派系2是否已开启
    public bool isStoryline3Triggered = false; // 派系3是否已开启
    public bool hasEnteredTrial = false;       // 是否已进入开庭阶段

    // 裁决结果数据
    // 记录玩家在每个 Stage 的选择
    public List<string> playerVerdictChoices = new List<string>();

    // 最终判定结果
    public bool isStoryline1Completed = false; // 派系1要求是否满足
    public bool isStoryline2Completed = false; // 派系2要求是否满足
    public bool isStoryline3Completed = false; // 派系3要求是否满足
    public string caseEndingPlot;              // 案件最终达成的剧情线 (A/B/None)
}