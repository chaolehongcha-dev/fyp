using UnityEngine;
using Cinemachine;
using System.Collections.Generic;

/// <summary>
/// 管理 Cinemachine 虚拟摄像机之间的平滑切换。
/// 通过 F 键输入和射线检测，将自由视角切换到固定屏幕视角。
/// </summary>
public class CameraSwitchManager : MonoBehaviour
{
    [Header("摄像机引用")]
    [Tooltip("玩家自由视角摄像机 (应为 VCam + POV)")]
    public CinemachineVirtualCameraBase freeLookCam;

    [Tooltip("所有屏幕聚焦的虚拟摄像机（需在 Inspector 中按顺序连接）")]
    public CinemachineVirtualCamera[] screenCams;

    [Header("设置")]
    [Tooltip("自由视角时的优先级 (应最高)")]
    public int freeLookPriority = 15;

    [Tooltip("固定视角时的优先级 (需高于自由视角)")]
    public int screenFocusPriority = 20;

    [Tooltip("非活动摄像机的默认优先级 (最低)")]
    public int defaultPriority = 10;

    [Tooltip("场景中所有需要检测的屏幕物体（需在 Inspector 中按 screenCams 顺序连接）")]
    public GameObject[] screens;

    [Header("其他组件")]
    [Tooltip("控制玩家视角的脚本，用于禁用自由转动")]
    public PlayerLook playerLookScript;

    // 内部状态追踪
    private bool _isFocusing = false;
    private CinemachineVirtualCamera _activeScreenCam = null;

    void Start()
    {
        // ...

        // 启动时，强制重置所有固定视角的 VCam 优先级
        ResetAllScreenPriorities();

        // 确保一开始是自由视角
        SwitchToFreeLook();

        // ----------- 新增调试日志 -----------
        if (freeLookCam == null)
        {
            Debug.LogError("致命错误：freeLookCam 引用丢失！无法启动自由视角。");
        }
        else
        {
            Debug.Log("【CM 启动调试】CM_FreeLook_POV 优先级赋值结果: " + freeLookCam.Priority);
        }
        // ------------------------------------
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (_isFocusing)
            {
                // 状态 2: 当前处于聚焦状态 -> 退回到自由视角
                SwitchToFreeLook();
            }
            else
            {
                // 状态 1: 当前处于自由视角 -> 尝试聚焦屏幕
                TryFocusScreen();
            }
        }
    }

    /// <summary>
    /// 强制将所有固定视角 VCam 的优先级设置为最低 (defaultPriority)。
    /// </summary>
    private void ResetAllScreenPriorities()
    {
        foreach (var cam in screenCams)
        {
            if (cam != null)
            {
                cam.Priority = defaultPriority; // 强制设置为 10
            }
        }
    }

    private void TryFocusScreen()
    {
        if (freeLookCam == null) return;

        // 射线检测从当前摄像机位置（即 Player Look Target）发出
        Ray ray = new Ray(freeLookCam.transform.position, freeLookCam.transform.forward);
        RaycastHit hit;
        float rayDistance = 100f;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            // 检查射线击中的物体是否是我们列表中的屏幕之一
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
        }
    }

    private void SwitchToScreenFocus(CinemachineVirtualCamera targetCam)
    {
        // 1. 切换摄像机优先级
        if (freeLookCam != null)
        {
            freeLookCam.Priority = defaultPriority; // 自由视角优先级降低 (10)
        }

        targetCam.Priority = screenFocusPriority; // 目标固定视角优先级最高 (20)

        // 2. 更新状态
        _isFocusing = true;
        _activeScreenCam = targetCam;

        // 3. 禁用玩家的自由转动脚本
        if (playerLookScript != null)
        {
            playerLookScript.isFreeLookEnabled = false;
        }
    }

    private void SwitchToFreeLook()
    {
        // 1. 切换摄像机优先级
        if (freeLookCam != null)
        {
            freeLookCam.Priority = freeLookPriority; // 自由视角优先级最高 (15)
        }

        // 如果有活动的固定视角摄像机，将其优先级降回去
        if (_activeScreenCam != null)
        {
            _activeScreenCam.Priority = defaultPriority; // 降到最低 (10)
            _activeScreenCam = null;
        }

        // 2. 更新状态
        _isFocusing = false;

        // 3. 启用玩家的自由转动脚本
        if (playerLookScript != null)
        {
            playerLookScript.isFreeLookEnabled = true;
        }
    }
}