using UnityEngine;

/// <summary>
/// 负责管理聊天系统的UI和交互。
/// </summary>
public class ChatSystemUI : MonoBehaviour
{

    public void SendNewMessage(int factionIndex)
    {
        Debug.Log($"Chat UI: 正在发送来自派系 {factionIndex} 的私信。");
        // 这里是加载聊天数据并刷新 UI 的地方
    }

}