using UnityEngine;
using UnityEngine.UI;

// ####################################################################
// ## 1. 派系故事线按钮 (用于 'blue', 'yellow', 'red')
// ####################################################################
[RequireComponent(typeof(Button))]
public class UI_FactionButton : MonoBehaviour
{
    private FactionStoryline storyline;
    private FactionManager factionManager;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    // 由 CaseManager 在 StartCase 时调用
    public void Setup(FactionStoryline storylineToLoad, FactionManager manager)
    {
        this.storyline = storylineToLoad;
        this.factionManager = manager;

        // (可选) 你可以在这里根据 storyline.faction 更新按钮的外观
        // e.g., if (storyline.faction == FactionType.Truth) { ... }
    }

    private void OnClick()
    {
        if (storyline != null && factionManager != null)
        {
            factionManager.PurchaseStoryline(storyline);
            // 购买后禁用按钮
            button.interactable = false;
        }
    }
}