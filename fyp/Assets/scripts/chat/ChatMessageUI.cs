using UnityEngine;
using UnityEngine.UI;

// 放在你的 "消息预制件" (ChatMessagePrefab) 的根物体上
public class ChatMessageUI : MonoBehaviour
{
    // 拖入预制件内部的 Text 组件
    public Text messageText;

    public void SetText(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }
}