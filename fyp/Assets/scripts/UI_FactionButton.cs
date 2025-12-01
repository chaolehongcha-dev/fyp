using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ####################################################################
// ## 1. 派系故事线按钮
// ## (V6.2 - 清理版: 移除了调试用的变色逻辑)
// ####################################################################
[RequireComponent(typeof(Button))]
public class UI_FactionButton : MonoBehaviour
{
    private FactionStoryline storyline;
    private FactionManager factionManager;
    private Button button;
    private Text buttonText; // 如果你需要控制文字颜色，可以保留这个

    private bool isPurchased = false;
    private bool isSetup = false;

    void Awake()
    {
        EnsureComponents();
        // 初始设为不可交互
        if (button) button.interactable = false;
    }

    void Start()
    {
        StartCoroutine(TryRequestSetup());
    }

    void OnEnable()
    {
        if (!isSetup) StartCoroutine(TryRequestSetup());
        else UpdateInteractableState();
    }

    // ## 核心逻辑: 主动寻找 CaseManager ##
    IEnumerator TryRequestSetup()
    {
        int retryCount = 0;
        // 尝试 10 次，每次间隔 0.5秒
        while (!isSetup && retryCount < 10)
        {
            CaseManager cm = FindObjectOfType<CaseManager>();
            if (cm != null && cm.currentCase != null)
            {
                cm.RequestButtonSetup(this);
            }

            if (isSetup) yield break;

            retryCount++;
            yield return new WaitForSeconds(0.5f);
        }

        if (!isSetup)
        {
            Debug.LogError($"UI_FactionButton {gameObject.name}: 连接失败 (超时)");
            // 这里不再变色，保持 interactable = false 即可
        }
    }

    private void EnsureComponents()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button) button.onClick.AddListener(OnClick);
        }

        // 移除了 btnImage 获取，因为不再需要手动变色
        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<Text>();
        }
    }

    public void Setup(FactionStoryline storylineToLoad, FactionManager manager)
    {
        EnsureComponents();
        this.storyline = storylineToLoad;
        this.factionManager = manager;
        this.isPurchased = false;
        this.isSetup = true;

        UpdateInteractableState();
    }

    void Update()
    {
        UpdateInteractableState();
    }

    private void UpdateInteractableState()
    {
        if (button == null) return;

        // 1. 检查数据连接
        if (storyline == null)
        {
            button.interactable = false;
            return;
        }

        // 2. 检查已购买
        if (isPurchased)
        {
            button.interactable = false;
            return;
        }

        // 3. 检查资源管理器和能量
        if (ResourceManager.Instance != null)
        {
            bool hasEnergy = ResourceManager.Instance.currentEnergy >= 1;
            button.interactable = hasEnergy;
            // 按钮变灰或变亮现在完全由 Button 组件的 "Disabled Color" 控制
        }
        else
        {
            button.interactable = false;
        }
    }

    private void OnClick()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.StorylinePhase)
            return;

        if (storyline != null && factionManager != null)
        {
            bool success = factionManager.PurchaseStoryline(storyline);
            if (success)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClickSound();
                isPurchased = true;
                UpdateInteractableState();
            }
        }
    }
}