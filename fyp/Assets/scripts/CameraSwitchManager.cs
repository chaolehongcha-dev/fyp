using UnityEngine;
using Cinemachine;

public class CameraSwitchManager : MonoBehaviour
{
    [Header("摄像机引用")]
    [Tooltip("玩家自由视角摄像机 (FreeLook)")]
    public CinemachineVirtualCameraBase freeLookCam;

    [Tooltip("三个屏幕聚焦的虚拟摄像机")]
    public CinemachineVirtualCamera[] screenCams = new CinemachineVirtualCamera[3];

    [Header("设置")]
    [Tooltip("自由视角时的优先级 (最高)")]
    public int freeLookPriority = 15;

    [Tooltip("固定视角时的优先级 (更高)")]
    public int screenFocusPriority = 20;

    [Tooltip("非活动摄像机的默认优先级 (最低)")]
    public int defaultPriority = 10;

    [Tooltip("场景中所有需要检测的屏幕物体")]
    public GameObject[] screens;

    [Header("其他组件")]
    [Tooltip("控制玩家视角的脚本，用于禁用自由转动")]
    public PlayerLook playerLookScript;

    // 内部状态追踪
    private bool _isFocusing = false;
    private CinemachineVirtualCamera _activeScreenCam = null;

    void Start()
    {
        // 确保一开始是自由视角
        SwitchToFreeLook();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (_isFocusing)
            {
                // 状态 2: 当前处于聚焦状态，按下 F 键 -> 退回到自由视角
                SwitchToFreeLook();
            }
            else
            {
                // 状态 1: 当前处于自由视角，按下 F 键 -> 尝试聚焦屏幕
                TryFocusScreen();
            }
        }
    }

    private void TryFocusScreen()
    {
        // 射线检测参数
        Ray ray = new Ray(freeLookCam.transform.position, freeLookCam.transform.forward);
        RaycastHit hit;
        float rayDistance = 100f; // 检测距离可根据场景调整

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            // 检查射线击中的物体是否是我们的屏幕之一
            int screenIndex = -1;
            for (int i = 0; i < screens.Length; i++)
            {
                if (hit.collider.gameObject == screens[i])
                {
                    screenIndex = i;
                    break;
                }
            }

            if (screenIndex != -1 && screenIndex < screenCams.Length)
            {
                // 射线击中了一个屏幕，切换到对应的固定视角
                SwitchToScreenFocus(screenCams[screenIndex]);
            }
            // 如果没有击中屏幕，保持自由视角，不做任何操作
        }
    }

    private void SwitchToScreenFocus(CinemachineVirtualCamera targetCam)
    {
        // 1. 切换摄像机优先级
        freeLookCam.Priority = defaultPriority; // 自由视角优先级降低
        targetCam.Priority = screenFocusPriority; // 目标固定视角优先级最高

        // 2. 更新状态
        _isFocusing = true;
        _activeScreenCam = targetCam;

        // 3. 禁用玩家的自由转动脚本
        if (playerLookScript != null)
        {
            playerLookScript.isFreeLookEnabled = false;
            // 重新锁定/隐藏光标（确保聚焦时不受鼠标影响）
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void SwitchToFreeLook()
    {
        // 1. 切换摄像机优先级
        freeLookCam.Priority = freeLookPriority; // 自由视角优先级最高

        // 如果有活动的固定视角摄像机，将其优先级降回去
        if (_activeScreenCam != null)
        {
            _activeScreenCam.Priority = defaultPriority;
            _activeScreenCam = null;
        }

        // 2. 更新状态
        _isFocusing = false;

        // 3. 启用玩家的自由转动脚本
        if (playerLookScript != null)
        {
            playerLookScript.isFreeLookEnabled = true;
            // 重新锁定/隐藏光标
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}