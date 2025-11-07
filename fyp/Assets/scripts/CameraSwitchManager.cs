using UnityEngine;
using Cinemachine;
using System.Collections;

public class CameraSwitchManager : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    public CinemachineVirtualCameraBase freeLookCam;
    public CinemachineVirtualCamera[] screenCams;

    [Header("Scene Objects")]
    public GameObject[] screens;

    [Header("UI")]
    public GameObject crosshairUI;

    [Header("Cursor Settings")]
    public Texture2D cursorDefault;
    public Texture2D cursorHover;
    public Vector2 cursorHotspot = Vector2.zero;

    [Header("Priority Settings")]
    public int freeLookPriority = 15;
    public int screenFocusPriority = 20;
    public int defaultPriority = 10;

    [Header("Fade Settings")]
    public float crosshairFadeDuration = 0.25f;

    [Header("Camera Blend Delay")]
    [Tooltip("切换到固定视角后等待多少秒再显示光标")]
    public float showCursorDelay = 0.6f;  // 可按Cinemachine的Blend时间调整

    private bool _isFocusing = false;
    private CinemachineVirtualCamera _activeScreenCam;
    private PlayerLook playerLookScript;

    void Start()
    {
        playerLookScript = FindObjectOfType<PlayerLook>();
        ResetAllScreenPriorities();
        SwitchToFreeLook();

        if (crosshairUI != null)
            SetCrosshairVisible(true, true);

        // 启动时隐藏光标
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (_isFocusing)
                SwitchToFreeLook();
            else
                TryFocusScreen();
        }
    }

    private void ResetAllScreenPriorities()
    {
        foreach (var cam in screenCams)
            if (cam != null)
                cam.Priority = defaultPriority;
    }

    private void TryFocusScreen()
    {
        if (freeLookCam == null) return;

        Ray ray = new Ray(freeLookCam.transform.position, freeLookCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            for (int i = 0; i < screens.Length; i++)
            {
                if (hit.collider.gameObject == screens[i])
                {
                    SwitchToScreenFocus(screenCams[i]);
                    return;
                }
            }
        }
    }

    private void SwitchToScreenFocus(CinemachineVirtualCamera targetCam)
    {
        if (freeLookCam != null)
            freeLookCam.Priority = defaultPriority;

        targetCam.Priority = screenFocusPriority;
        _isFocusing = true;
        _activeScreenCam = targetCam;

        if (playerLookScript != null)
            playerLookScript.isFreeLookEnabled = false;

        SetCrosshairVisible(false);

        // 延迟显示光标（等待Cinemachine过渡完成）
        StartCoroutine(ShowCursorAfterDelay());
    }

    private IEnumerator ShowCursorAfterDelay()
    {
        yield return new WaitForSeconds(showCursorDelay);
        EnableCustomCursor(true);
    }

    private void SwitchToFreeLook()
    {
        if (freeLookCam != null)
            freeLookCam.Priority = freeLookPriority;

        if (_activeScreenCam != null)
        {
            _activeScreenCam.Priority = defaultPriority;
            _activeScreenCam = null;
        }

        _isFocusing = false;

        if (playerLookScript != null)
            playerLookScript.isFreeLookEnabled = true;

        SetCrosshairVisible(true);
        EnableCustomCursor(false); // 立即隐藏光标
    }

    // ------------- 鼠标光标逻辑 ----------------
    private void EnableCustomCursor(bool enable)
    {
        if (enable)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (cursorDefault != null)
                Cursor.SetCursor(cursorDefault, cursorHotspot, CursorMode.Auto);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ------------- 准星显示逻辑 ----------------
    private void SetCrosshairVisible(bool show, bool instant = false)
    {
        if (crosshairUI == null) return;

        CanvasGroup cg = crosshairUI.GetComponent<CanvasGroup>();
        if (cg == null) cg = crosshairUI.AddComponent<CanvasGroup>();

        if (instant)
        {
            cg.alpha = show ? 1f : 0f;
            crosshairUI.SetActive(show);
            return;
        }

        StartCoroutine(FadeCrosshair(cg, show));
    }

    private IEnumerator FadeCrosshair(CanvasGroup cg, bool show)
    {
        crosshairUI.SetActive(true);
        float target = show ? 1f : 0f;
        float start = cg.alpha;
        float time = 0f;

        while (time < crosshairFadeDuration)
        {
            cg.alpha = Mathf.Lerp(start, target, time / crosshairFadeDuration);
            time += Time.deltaTime;
            yield return null;
        }

        cg.alpha = target;
        if (!show)
            crosshairUI.SetActive(false);
    }

    // ---------- 提供给按钮悬停事件的接口 ----------
    public void OnCursorHoverEnter()
    {
        if (cursorHover != null)
            Cursor.SetCursor(cursorHover, cursorHotspot, CursorMode.Auto);
    }

    public void OnCursorHoverExit()
    {
        if (cursorDefault != null)
            Cursor.SetCursor(cursorDefault, cursorHotspot, CursorMode.Auto);
    }
}
