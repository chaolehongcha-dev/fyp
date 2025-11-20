using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 用于 List 筛选

// 枚举，用于聊天系统内部识别
public enum ChatSpeaker
{
    // ## 修复: 将 None 移到最后，恢复原有顺序，解决数据错位问题 ##
    Truth = 0, // 真理部部长 (Btn_Truth) - 恢复为 0
    Order = 1, // 秩序部 (Btn_Blue)   - 恢复为 1
    Love = 2,  // 友爱部 (Btn_Yellow) - 恢复为 2
    Peace = 3, // 和平部 (Btn_Red)    - 恢复为 3
    None = 4   // 默认空状态          - 新增为 4
}

public class ChatSystem : MonoBehaviour
{
    public static ChatSystem Instance { get; private set; }

    [Header("UI 引用 (Right Panel)")]
    public Text topNameText;
    public Transform contentParent; // 拖入 Scroll View -> Viewport -> Content
    public GameObject chatMessagePrefab;
    public ScrollRect scrollRect;

    [Header("UI 引用 (Left Panel Hints)")]
    public GameObject hintTruth;
    public GameObject hintBlue;
    public GameObject hintYellow;
    public GameObject hintRed;

    // 内部类: 用于存储运行时的消息
    private class RuntimeChatMessage
    {
        public string text;
        public bool isPermanent; // 是否永久保留

        public RuntimeChatMessage(string t, bool p)
        {
            text = t;
            isPermanent = p;
        }
    }

    private Dictionary<ChatSpeaker, List<RuntimeChatMessage>> messageHistory;
    private ChatSpeaker currentSpeaker;

    private GameManager gameManager;
    private bool isFirstBriefingRead = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 初始化
        messageHistory = new Dictionary<ChatSpeaker, List<RuntimeChatMessage>>();
        foreach (ChatSpeaker speaker in System.Enum.GetValues(typeof(ChatSpeaker)))
        {
            messageHistory[speaker] = new List<RuntimeChatMessage>();
        }
    }

    void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager == null) Debug.LogError("ChatSystem: 找不到 GameManager!");

        // 隐藏所有提示
        if (hintTruth) hintTruth.SetActive(false);
        if (hintBlue) hintBlue.SetActive(false);
        if (hintYellow) hintYellow.SetActive(false);
        if (hintRed) hintRed.SetActive(false);

        // ## 修改: 游戏开始时，默认不显示任何频道 ##
        SwitchToChat(ChatSpeaker.None);
    }

    void Update()
    {
        // ## 新增: 按 F 键重置聊天页面 (切换到 None) ##
        if (Input.GetKeyDown(KeyCode.F))
        {
            SwitchToChat(ChatSpeaker.None);
        }
    }

    // 核心功能：切换聊天频道
    public void SwitchToChat(ChatSpeaker speaker)
    {
        currentSpeaker = speaker;

        // ## 处理 None 状态 (空页面) ##
        if (speaker == ChatSpeaker.None)
        {
            if (topNameText) topNameText.text = "系统待机";
            // 清空显示区域
            foreach (Transform child in contentParent) Destroy(child.gameObject);
            return;
        }

        // 1. 更新顶部名称
        if (topNameText) topNameText.text = GetNameFromSpeaker(speaker);

        // 2. 隐藏对应的 "新消息" 提示
        GameObject hint = GetHintForSpeaker(speaker);
        if (hint != null) hint.SetActive(false);

        // 3. 刷新聊天窗口
        RefreshChatDisplay();

        // 触发器检查 (仅针对 Truth 频道)
        if (speaker == ChatSpeaker.Truth && !isFirstBriefingRead &&
            gameManager.CurrentState == GameState.CaseBriefing)
        {
            isFirstBriefingRead = true;
            Debug.Log("ChatSystem: 案情简报已读，通知 GameManager 进入 Stage 2...");
            gameManager.EnterStorylinePhase();
        }
    }

    // 核心功能：刷新聊天窗口
    private void RefreshChatDisplay()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (messageHistory.ContainsKey(currentSpeaker))
        {
            List<RuntimeChatMessage> history = messageHistory[currentSpeaker];
            foreach (RuntimeChatMessage msg in history)
            {
                InstantiateMessagePrefab(msg.text);
            }
        }

        ScrollToBottom();
    }

    // 核心功能：添加新消息
    private void AddMessage(ChatSpeaker speaker, string message, bool isPermanent)
    {
        if (speaker == ChatSpeaker.None) return;

        if (messageHistory.ContainsKey(speaker))
        {
            messageHistory[speaker].Add(new RuntimeChatMessage(message, isPermanent));
        }

        // 如果当前正看着这个频道，直接显示
        if (speaker == currentSpeaker)
        {
            InstantiateMessagePrefab(message);
            ScrollToBottom();
        }
        else
        {
            // 显示提示
            GameObject hint = GetHintForSpeaker(speaker);
            if (hint != null) hint.SetActive(true);
        }
    }

    // 清理临时消息
    public void ClearTransientMessages()
    {
        foreach (ChatSpeaker speaker in messageHistory.Keys)
        {
            // 删除所有 !isPermanent 的消息
            messageHistory[speaker].RemoveAll(msg => !msg.isPermanent);
        }

        // 如果当前在某个频道，刷新一下显示
        if (currentSpeaker != ChatSpeaker.None)
        {
            RefreshChatDisplay();
        }
    }

    // --- 公开 API ---

    // 1. 显示简报 (临时)
    public void ShowBriefing(List<ChatMessage> messages)
    {
        isFirstBriefingRead = false;
        foreach (var msg in messages)
        {
            AddMessage(msg.sender, msg.messageContent, false);
        }
    }

    // 2. 显示派系观点 (临时)
    public void ShowFactionMessages(List<ChatMessage> messages)
    {
        foreach (var msg in messages)
        {
            AddMessage(msg.sender, msg.messageContent, false);
        }
    }

    // 3. 显示评价消息
    public void ShowEvaluationMessages(FactionType faction, List<ChatMessage> messages)
    {
        ChatSpeaker speaker = ConvertFactionToSpeaker(faction);
        foreach (var msg in messages)
        {
            // 这里改为 false (临时)，会在下个案子结束时清理
            AddMessage(speaker, msg.messageContent, false);
        }
    }

    // --- 辅助工具 ---

    private void InstantiateMessagePrefab(string message)
    {
        if (chatMessagePrefab == null) return;
        GameObject messageObj = Instantiate(chatMessagePrefab, contentParent);
        ChatMessageUI ui = messageObj.GetComponent<ChatMessageUI>();
        if (ui != null) ui.SetText(message);
    }

    private void ScrollToBottom()
    {
        StartCoroutine(ForceScrollDown());
    }

    IEnumerator ForceScrollDown()
    {
        yield return null;
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }

    private GameObject GetHintForSpeaker(ChatSpeaker speaker)
    {
        switch (speaker)
        {
            case ChatSpeaker.Truth: return hintTruth;
            case ChatSpeaker.Order: return hintBlue;
            case ChatSpeaker.Love: return hintYellow;
            case ChatSpeaker.Peace: return hintRed;
            default: return null;
        }
    }

    private string GetNameFromSpeaker(ChatSpeaker speaker)
    {
        switch (speaker)
        {
            case ChatSpeaker.Truth: return "真理部部长";
            case ChatSpeaker.Order: return "秩序部 (精英)";
            case ChatSpeaker.Love: return "友爱部 (民众)";
            case ChatSpeaker.Peace: return "和平部 (军队)";
            case ChatSpeaker.None: return "系统待机";
            default: return "???";
        }
    }

    private ChatSpeaker ConvertFactionToSpeaker(FactionType faction)
    {
        switch (faction)
        {
            case FactionType.Truth: return ChatSpeaker.Truth;
            case FactionType.Order: return ChatSpeaker.Order;
            case FactionType.Love: return ChatSpeaker.Love;
            case FactionType.Peace: return ChatSpeaker.Peace;
            default: return ChatSpeaker.Truth;
        }
    }
}