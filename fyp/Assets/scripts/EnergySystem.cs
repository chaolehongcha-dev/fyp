using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全局能量管理系统。负责存储、消耗和刷新能量UI。
/// </summary>
public class EnergySystem : MonoBehaviour
{
    // 将其设置为静态实例，方便其他脚本全局访问
    public static EnergySystem Instance { get; private set; }

    [Header("能量设置")]
    [Range(0, 3)]
    [Tooltip("当前能量值，最高3，最低0")]
    private int _currentEnergy = 3;
    public int CurrentEnergy
    {
        get => _currentEnergy;
        private set
        {
            // 确保能量值在0到3之间
            _currentEnergy = Mathf.Clamp(value, 0, 3);
            UpdateEnergyUI(); // 能量值改变时，立即更新UI
        }
    }

    [Header("UI 资源")]
    [Tooltip("用于显示能量的 Image 组件")]
    public Image energyDisplayImage;

    [Tooltip("对应能量值0, 1, 2, 3的图片素材，数组大小必须是4")]
    public Sprite[] energySprites = new Sprite[4];

    private void Awake()
    {
        // 实现单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 可选：如果需要在场景切换后保留能量状态
        }
    }

    private void Start()
    {
        // 初始化能量值和UI
        _currentEnergy = 3;
        UpdateEnergyUI();
    }

    /// <summary>
    /// 消耗指定数量的能量。
    /// </summary>
    /// <param name="cost">消耗的能量值。</param>
    /// <returns>如果能量足够并成功消耗，返回True。</returns>
    public bool TryConsumeEnergy(int cost)
    {
        if (_currentEnergy >= cost)
        {
            CurrentEnergy -= cost;
            return true;
        }
        Debug.LogWarning("能量不足！无法执行操作。");
        return false;
    }

    /// <summary>
    /// 根据当前的能量值更新显示的图片。
    /// </summary>
    private void UpdateEnergyUI()
    {
        if (energyDisplayImage != null && energySprites.Length == 4)
        {
            // 数组索引 0, 1, 2, 3 对应能量值 0, 1, 2, 3
            energyDisplayImage.sprite = energySprites[CurrentEnergy];
        }
        else
        {
            Debug.LogError("能量UI或Sprite数组未设置！请检查Inspector。");
        }
    }

    // 可以在这里添加恢复能量的方法，例如 public void RestoreEnergy(int amount)
}