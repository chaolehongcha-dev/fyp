using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 用于 .Last()

// ####################################################################
// ## 4.5. ENDING MANAGER (结局管理器)
// ####################################################################
public class EndingManager : MonoBehaviour
{
    private int totalPublicOpinion;
    private Dictionary<FactionType, int> factionInfluence;
    private GameEndingData finalData;

    void Awake()
    {
        DontDestroyOnLoad(gameObject); // 必须跨场景保留

        totalPublicOpinion = 0;
        factionInfluence = new Dictionary<FactionType, int>
        {
            { FactionType.Truth, 0 },
            { FactionType.Love, 0 },
            { FactionType.Peace, 0 }
        };
        finalData = new GameEndingData();
        finalData.caseRecords = new List<CaseRecord>(); // 确保列表已初始化
    }

    public void StartNewCaseRecord(string caseID)
    {
        finalData.caseRecords.Add(new CaseRecord { caseID = caseID });
    }

    public void RecordPublicOpinionChange(int change)
    {
        totalPublicOpinion += change;
    }

    public void RecordStorylinePurchase(FactionType faction)
    {
        // 确保 caseRecords 列表不为空
        if (finalData.caseRecords.Count > 0)
        {
            finalData.caseRecords.Last().purchasedStorylines.Add(faction.ToString());
        }
    }

    public void RecordJudgment(List<int> judgmentPath)
    {
        if (finalData.caseRecords.Count > 0)
        {
            finalData.caseRecords.Last().finalJudgmentPath = new List<int>(judgmentPath);
        }
    }

    public void RecordFactionInfluence(FactionType faction, bool didComply)
    {
        int influenceChange = didComply ? 1 : -1;
        factionInfluence[faction] += influenceChange;
    }

    public string GenerateFinalDataForAPI()
    {
        // 1. 汇总最终数据
        finalData.totalPublicOpinion = this.totalPublicOpinion;
        finalData.factionInfluences.Clear();

        foreach (var pair in factionInfluence)
        {
            finalData.factionInfluences.Add(new FactionInfluenceEntry
            {
                faction = pair.Key.ToString(),
                influenceScore = pair.Value
            });
        }

        // 2. 序列化为 JSON
        // **注意: Unity 默认的 JsonUtility 不支持 List 顶层对象，
        // 但我们的 GameEndingData 是一个类, 所以它是支持的。**
        string jsonData = JsonUtility.ToJson(finalData, true); // true = 格式化排版
        return jsonData;
    }
}