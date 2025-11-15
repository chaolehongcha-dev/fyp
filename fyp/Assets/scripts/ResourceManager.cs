using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// ####################################################################
// ## 4.2. RESOURCE MANAGER (资源管理器)
// ####################################################################
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("能量状态")]
    public int currentEnergy = 3; // 假设满能量为3
    public int maxEnergy = 3;

    [Header("UI 引用")]
    public Image energyImage; // 拖入你的 'Energy' Image
    public List<Sprite> energySprites; // 拖入4个 Sprite (3/3, 2/3, 1/3, 0/3)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateEnergyUI();
    }

    // (你的接口)
    public void AddEnergy(int amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        UpdateEnergyUI();
    }

    public bool SpendEnergy(int amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            UpdateEnergyUI();
            return true;
        }
        else
        {
            Debug.Log("能量不足!");
            return false;
        }
    }

    void UpdateEnergyUI()
    {
        if (energyImage == null || energySprites == null || energySprites.Count == 0)
            return;

        // 能量值为 3, 2, 1, 0
        // 对应 Sprite 索引 0, 1, 2, 3
        int spriteIndex = maxEnergy - currentEnergy;

        if (spriteIndex >= 0 && spriteIndex < energySprites.Count)
        {
            energyImage.sprite = energySprites[spriteIndex];
        }
    }
}