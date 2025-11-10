using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 管理聊天系统UI，包括左侧派系按钮和右侧聊天显示
/// </summary>
public class ChatSystemUI : MonoBehaviour
{
    [Header("左侧按钮")]
    public Button[] factionButtons;               // 0→真理部,1→秩序部,2→友爱部,3→和平部
    public GameObject[] newMessageIndicators;    // 按钮旁的新消息红点

    [Header("右侧聊天")]
    public Text topNameText;          // 聊天组顶部名字
    public Transform chatContentParent;          // ScrollView Content
    public GameObject chatMessagePrefab;         // 聊天消息预制件（Text或TMP_Text）

    [Header("聊天数据")]
    public List<FactionChatData> allFactionChatData = new List<FactionChatData>();

    private int currentFactionIndex = -1;

    private void Start()
    {
        // 给左边按钮绑定点击事件
        for (int i = 0; i < factionButtons.Length; i++)
        {
            int index = i;
            factionButtons[i].onClick.AddListener(() => OnFactionButtonClicked(index));
        }
    }

    /// <summary>
    /// 点击左侧派系按钮
    /// </summary>
    public void OnFactionButtonClicked(int factionIndex)
    {
        currentFactionIndex = factionIndex;

        // 隐藏红点
        if (newMessageIndicators != null && factionIndex < newMessageIndicators.Length)
        {
            newMessageIndicators[factionIndex].SetActive(false);
        }

        // 切换聊天显示
        ShowFactionChat(factionIndex);
    }

    /// <summary>
    /// 根据派系索引显示聊天
    /// </summary>
    private void ShowFactionChat(int factionIndex)
    {
        if (factionIndex < 0 || factionIndex >= allFactionChatData.Count) return;

        FactionChatData factionData = allFactionChatData[factionIndex];
        if (factionData == null) return;

        // 清空内容
        foreach (Transform t in chatContentParent)
        {
            Destroy(t.gameObject);
        }

        // 按顺序生成每组聊天
        foreach (var group in factionData.chatGroups)
        {
            // 顶部名字
            if (!string.IsNullOrEmpty(group.groupName))
            {
                topNameText.text = group.groupName;
            }

            foreach (var msg in group.messages)
            {
                GameObject msgGO = Instantiate(chatMessagePrefab, chatContentParent);
                TextMeshProUGUI msgText = msgGO.GetComponent<TextMeshProUGUI>();
                if (msgText != null)
                {
                    msgText.text = $"{msg.speakerName}: {msg.content}";
                }
            }
        }
    }

    /// <summary>
    /// 外部触发新消息
    /// </summary>
    public void ReceiveNewMessage(int factionIndex, ChatGroup newGroup)
    {
        if (factionIndex < 0 || factionIndex >= allFactionChatData.Count) return;

        // 添加新组
        allFactionChatData[factionIndex].chatGroups.Add(newGroup);

        // 显示红点
        if (newMessageIndicators != null && factionIndex < newMessageIndicators.Length)
        {
            newMessageIndicators[factionIndex].SetActive(true);
        }

        // 如果当前正在查看该派系聊天，则刷新显示
        if (currentFactionIndex == factionIndex)
        {
            ShowFactionChat(factionIndex);
        }
    }
}
