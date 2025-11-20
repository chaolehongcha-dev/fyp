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

    // ## 新增: 记录是否已购买 ##
    private bool isPurchased = false;

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
        this.isPurchased = false; // 重置状态

        // 立即刷新一次状态
        UpdateInteractableState();
    }

    void Update()
    {
        // 每帧检查能量状态，决定按钮是否可点
        UpdateInteractableState();
    }

    private void UpdateInteractableState()
    {
        if (button == null) return;

        if (isPurchased)
        {
            // 如果已经购买，永久禁用
            button.interactable = false;
        }
        else
        {
            // 如果未购买，检查能量是否足够 (需要 >= 1)
            // ResourceManager.Instance 应该在 [MANAGERS] 上
            if (ResourceManager.Instance != null)
            {
                button.interactable = ResourceManager.Instance.currentEnergy >= 1;
            }
        }
    }

    private void OnClick()
    {
        if (storyline != null && factionManager != null)
        {
            // 尝试购买
            bool success = factionManager.PurchaseStoryline(storyline);

            if (success)
            {
                // 购买成功，标记为已购买
                isPurchased = true;
                button.interactable = false;
            }
        }
    }
}