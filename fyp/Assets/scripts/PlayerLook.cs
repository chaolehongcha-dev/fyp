using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    // 只需要保留这个标志，用于 CameraSwitchManager 告诉此脚本何时启用/禁用视角
    [HideInInspector] public bool isFreeLookEnabled = true;

    void Start()
    {
        // 锁定鼠标光标并隐藏
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 只有当自由视角启用时才处理输入（尽管现在 HandleLookInput() 是空的，
        // 但我们保留这个结构，以便未来需要时可以添加非旋转的输入处理，例如：切换 Cinemachine POV 的组件状态）
        if (isFreeLookEnabled)
        {
            HandleLookInput();
        }
    }

    private void HandleLookInput()
    {
        // **** 原先的旋转逻辑已全部删除 ****
        // 现在，旋转功能将完全由 Cinemachine Virtual Camera 上的 POV 模块处理。

        // 我们可以选择在这里显式启用/禁用 Cinemachine POV 组件，
        // 但更常见和更简洁的做法是只通过 Cinemachine Brain 的优先级切换来控制 VCam 的激活状态。

        // 因此，此方法暂时留空，或者只保留光标状态管理（如果需要）。
    }
}