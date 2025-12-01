using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// ####################################################################
// ## 2. RESOURCE MANAGER (资源管理器)
// ## (V6.1 - 完整功能回归版)
// ####################################################################
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("能量状态")]
    public int currentEnergy = 3; // 初始能量
    public int maxEnergy = 3;     // 最大能量

    [Header("UI 引用")]
    public Image energyImage; // 拖入你的 'Energy' Image
    public List<Sprite> energySprites; // 拖入4个 Sprite (3/3, 2/3, 1/3, 0/3)

    void Awake()
    {
        // 单例模式保护
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Build 修复逻辑：防止打包后数值丢失变成 0
        if (currentEnergy <= 0)
        {
            currentEnergy = 3; // 强制重置为 3 (或 maxEnergy)
            Debug.LogWarning("ResourceManager: 检测到能量异常，已强制重置为 3 (防 Build 错误)");
        }
    }

    void Start()
    {
        UpdateEnergyUI();
    }

    public void AddEnergy(int amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        Debug.Log($"获得能量: {amount}, 当前: {currentEnergy}/{maxEnergy}");
        UpdateEnergyUI();
    }

    public bool SpendEnergy(int amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            Debug.Log($"消耗能量: {amount}, 剩余: {currentEnergy}/{maxEnergy}");
            UpdateEnergyUI();
            return true;
        }
        else
        {
            Debug.Log("能量不足!");
            return false;
        }
    }

    private void UpdateEnergyUI()
    {
        // 防空检查
        if (energyImage == null)
        {
            // 如果你在 Inspector 里忘了拖图片，这里就不执行，避免报错
            return;
        }

        if (energySprites == null || energySprites.Count == 0)
        {
            return;
        }

        // 逻辑：
        // 满能量 (3) -> 索引 0
        // 2 点能量 -> 索引 1
        // 1 点能量 -> 索引 2
        // 0 点能量 -> 索引 3
        int spriteIndex = maxEnergy - currentEnergy;

        // 确保索引不越界
        if (spriteIndex >= 0 && spriteIndex < energySprites.Count)
        {
            energyImage.sprite = energySprites[spriteIndex];
        }
        else
        {
            // 如果算出来的索引不对 (比如能量变成了 -1)，强制显示最后一张 (空能量)
            if (spriteIndex >= energySprites.Count)
                energyImage.sprite = energySprites[energySprites.Count - 1];

            // 如果能量超标了 (比如 4)，显示第一张 (满能量)
            if (spriteIndex < 0)
                energyImage.sprite = energySprites[0];
        }
    }
}