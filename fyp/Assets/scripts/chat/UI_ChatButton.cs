using UnityEngine;
using UnityEngine.UI;

// 放在 Btn_Truth, Btn_Blue, Btn_Yellow, Btn_Red 按钮上
public class UI_ChatButton : MonoBehaviour
{
    // 在 Inspector 中为每个按钮设置它对应的频道
    public ChatSpeaker speakerType;

    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        // ## 修复: 移除了 "Block 6:" 语法错误 ##
        ChatSystem.Instance.SwitchToChat(speakerType);
    }
}