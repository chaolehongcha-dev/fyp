using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChatMessage
{
    public string speakerName;   // 小名字，每组对话显示一次
    public string content;       // 消息内容
}

[System.Serializable]
public class ChatGroup
{
    public string groupName;          // 本组对话顶部名字
    public List<ChatMessage> messages = new List<ChatMessage>();
}

[System.Serializable]
public class FactionChatData
{
    public int factionIndex;                 // 0=真理部,1=友爱部,2=友爱部,3=和平部
    public List<ChatGroup> chatGroups = new List<ChatGroup>();
}
