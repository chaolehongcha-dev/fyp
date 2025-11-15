using UnityEngine;
using UnityEngine.UI;

// ####################################################################
// ## 2. 判案按钮 (用于 'judge1' 到 'judge6')
// ####################################################################
public class UI_JudgmentButton : MonoBehaviour
{
    // 在 Inspector 中设置
    [Header("这个按钮对应哪个 Case Data?")]
    public CaseData activeCase; // 拖入 Case1 的 ScriptableObject

    [Header("这个按钮是哪一步的哪个选项?")]
    public string targetNodeID; // **重要: 节点ID (来自 JudgmentNode.stageDescription)**
    public int choiceIndex; // **重要: 0 = 左, 1 = 右**

    private Button button;
    private CaseManager caseManager;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
        caseManager = FindObjectOfType<CaseManager>();

        if (caseManager == null)
        {
            Debug.LogError("UI_JudgmentButton: 找不到 CaseManager!");
        }
    }

    private void OnClick()
    {
        if (caseManager.currentCase != activeCase)
        {
            Debug.LogWarning("按钮的 ActiveCase 与 CaseManager 不匹配!");
            return;
        }

        // 确保我们在正确的节点
        if (caseManager.currentNode.stageDescription != targetNodeID)
        {
            Debug.LogError($"点击错误! CaseManager 在 {caseManager.currentNode.stageDescription}, 但按钮是 {targetNodeID}");
            return;
        }

        // 找到对应的 JudgmentChoice 数据
        JudgmentChoice choiceData = caseManager.currentNode.choices[choiceIndex];

        // 触发 CaseManager
        caseManager.SelectChoice(choiceData, choiceIndex);
    }
}