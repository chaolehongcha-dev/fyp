using System;
using System.Collections.Generic;

/// <summary>
/// 表示单个案件的数据结构，对应 JSON 文件中的每个案件对象。
/// </summary>
[Serializable]
public class CaseData
{
    public int caseID;
    public string caseTitle;
    public bool isStoryline1Triggered;
    public bool isStoryline2Triggered;
    public bool isStoryline3Triggered;
    public bool hasEnteredTrial;
    public List<string> playerVerdictChoices;
    public List<DecisionStage> decisionStages;
}

/// <summary>
/// 表示案件中的一个阶段（如 判决阶段、量刑阶段）。
/// </summary>
[Serializable]
public class DecisionStage
{
    public string stageID;
    public string stageTitle;
    public List<ChoiceRule> choices;
}

/// <summary>
/// 表示在一个阶段中的一个可选项及其后续跳转。
/// </summary>
[Serializable]
public class ChoiceRule
{
    public string choiceText;    // 显示在按钮上的文字
    public string choiceResult;  // 逻辑结果（例如 "Guilty"）
    public string nextStageID;   // 下一阶段ID（或 "END_CASE"）
}
