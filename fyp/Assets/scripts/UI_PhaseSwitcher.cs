using UnityEngine;
using UnityEngine.UI;

// ####################################################################
// ## 3. 阶段切换按钮 (现在只用于 'kaiting' 按钮)
// ####################################################################
[RequireComponent(typeof(Button))]
public class UI_PhaseSwitcher : MonoBehaviour
{
    // 在 Inspector 中设置
    public SwitcherType type;

    public enum SwitcherType { ToJudgment }

    private Button button;
    private GameManager gameManager;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError("UI_PhaseSwitcher: 找不到 GameManager!");
        }
    }

    private void OnClick()
    {
        // ## 新增: 播放音效 ##
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClickSound();

        if (type == SwitcherType.ToJudgment)
        {
            gameManager.EnterJudgmentPhase();
        }
    }
}