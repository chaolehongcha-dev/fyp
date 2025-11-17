using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// 枚举，用于聊天系统内部识别
public enum ChatSpeaker
{
    Truth, // 真理部部长 (Btn_Truth)
    Order, // 秩序部 (Btn_Blue)
    Love,  // 友爱部 (Btn_Yellow)
    Peace  // 和平部 (Btn_Red)
}

public class ChatSystem : MonoBehaviour
{
    public static ChatSystem Instance { get; private set; }

    [Header("UI 引用 (Right Panel)")]
    public Text topNameText;
    public Transform contentParent; // 拖入 Scroll View -> Viewport -> Content
    public GameObject chatMessagePrefab; // **重要: 你需要创建一个消息预制件**
    public ScrollRect scrollRect; // 拖入 Scroll View

    [Header("UI 引用 (Left Panel Hints)")]
    public GameObject hintTruth;
    public GameObject hintBlue;
    public GameObject hintYellow;
    public GameObject hintRed;

    // ## 修复: 恢复所有丢失的类级别变量 ##
    [Header("聊天数据")]
    private Dictionary<ChatSpeaker, List<string>> messageHistory;
    private ChatSpeaker currentSpeaker;

    // 游戏流程控制
    private GameManager gameManager;
    private bool isFirstBriefingRead = false; // 用于触发 Stage2

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 1. 初始化消息记忆
        messageHistory = new Dictionary<ChatSpeaker, List<string>>();
        foreach (ChatSpeaker speaker in System.Enum.GetValues(typeof(ChatSpeaker)))
        {
            messageHistory[speaker] = new List<string>();
        }
    }

    void Start()
    {
        // 2. 链接 GameManager
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("ChatSystem: 找不到 GameManager!");
        }

        // 3. 隐藏所有提示
        hintTruth.SetActive(false);
        hintBlue.SetActive(false);
        hintYellow.SetActive(false);
        hintRed.SetActive(false);

        // 4. 默认打开 "真理部部长" 频道
        SwitchToChat(ChatSpeaker.Truth);
    }

    // 核心功能：切换聊天频道 (由 UI_ChatButton 调用)
    public void SwitchToChat(ChatSpeaker speaker)
    {
        currentSpeaker = speaker;

        // 1. 更新顶部名称
        topNameText.text = GetNameFromSpeaker(speaker);

        // 2. 隐藏对应的 "新消息" 提示
        GetHintForSpeaker(speaker).SetActive(false);

        // 3. 刷新聊天窗口
        RefreshChatDisplay();

        // -----------------------------------------------------------
        // ## 游戏流程触发器 ##
        // -----------------------------------------------------------
        // 检查: 这是不是玩家第一次点击 "Truth" 频道，并且游戏还停在 Stage1 ?
        if (speaker == ChatSpeaker.Truth && !isFirstBriefingRead &&
            gameManager.CurrentState == GameState.CaseBriefing)
        {
            isFirstBriefingRead = true; // 标记为已读

            // 通知 GameManager 进入 Stage2 (派系选择)
            Debug.Log("ChatSystem: 案情简报已读，通知 GameManager 进入 Stage 2...");
            gameManager.EnterStorylinePhase();
        }
    }

    // 核心功能：刷新聊天窗口 (显示记忆)
    private void RefreshChatDisplay()
    {
        // 1. 清空旧消息
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. 重新加载此频道的所有历史消息
        List<string> history = messageHistory[currentSpeaker];
        foreach (string message in history)
        {
            InstantiateMessagePrefab(message);
        }

        ScrollToBottom();
    }

    // 核心功能：添加新消息 (供外部调用)
    private void AddMessage(ChatSpeaker speaker, string message)
    {
        // 1. 存入记忆
        if (messageHistory.ContainsKey(speaker))
        {
            messageHistory[speaker].Add(message);
        }
        else
        {
            Debug.LogWarning($"ChatSystem: 尝试向不存在的 speaker {speaker} 添加消息");
            return;
        }


        // 2. 检查是否显示
        if (speaker == currentSpeaker)
        {
            // 如果玩家正在看这个频道，直接显示
            InstantiateMessagePrefab(message);
            ScrollToBottom();
        }
        else
        {
            // 否则，显示 "新消息" 提示
            GetHintForSpeaker(speaker).SetActive(true);
        }
    }

    // --- 公开 API (供 GameManager 和 FactionManager 调用) ---

    // 1. (供 GameManager 调用) 显示案情简报
    public void ShowBriefing(List<ChatMessage> messages)
    {
        isFirstBriefingRead = false; // 重置触发器

        foreach (var msg in messages)
        {
            AddMessage(msg.sender, msg.messageContent);
        }
    }

    // 2. (供 FactionManager 调用) 显示派系故事线消息
    public void ShowFactionMessages(List<ChatMessage> messages)
    {
        foreach (var msg in messages)
        {
            AddMessage(msg.sender, msg.messageContent);
        }
    }

    // ## 修复: 此方法现在在正确的位置 ##
    // 3. (供 FactionManager 调用) 显示评价消息
    public void ShowEvaluationMessages(FactionType faction, List<ChatMessage> messages)
    {
        // ## 修改: 评价消息现在需要知道发送者是谁 ##
        ChatSpeaker speaker = ConvertFactionToSpeaker(faction);
        foreach (var msg in messages)
        {
            // 覆盖 sender，确保评价来自正确的派系
            AddMessage(speaker, msg.messageContent);
        }
    }

    // --- 辅助工具 ---

    private void InstantiateMessagePrefab(string message)
    {
        if (chatMessagePrefab == null)
        {
            Debug.LogError("ChatSystem: Chat Message Prefab 未设置!");
            return;
        }
        GameObject messageObj = Instantiate(chatMessagePrefab, contentParent);
        ChatMessageUI ui = messageObj.GetComponent<ChatMessageUI>();
        if (ui != null)
        {
            ui.SetText(message);
        }
        else
        {
            Debug.LogError("ChatSystem: 消息预制件上缺少 ChatMessageUI.cs 脚本!");
        }
    }

    private void ScrollToBottom()
    {
        // 使用协程确保在帧末尾滚动，此时 UI 已更新
        StartCoroutine(ForceScrollDown());
    }

    IEnumerator ForceScrollDown()
    {
        // 等待一帧让 ContentSizeFitter/LayoutGroup 生效
        yield return null;
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // 辅助: 根据 Speaker 获取对应的 Hint GameObject
    private GameObject GetHintForSpeaker(ChatSpeaker speaker)
    {
        switch (speaker)
        {
            case ChatSpeaker.Truth: return hintTruth;
            case ChatSpeaker.Order: return hintBlue;
            case ChatSpeaker.Love: return hintYellow;
            case ChatSpeaker.Peace: return hintRed;
            default:
                Debug.LogWarning($"GetHintForSpeaker: 找不到 {speaker} 对应的 Hint");
                return hintTruth; // 默认返回 Truth
        }
    }

    // 辅助: 根据 Speaker 获取名称
    private string GetNameFromSpeaker(ChatSpeaker speaker)
    {
        switch (speaker)
        {
            case ChatSpeaker.Truth: return "真理部部长";
            case ChatSpeaker.Order: return "秩序部 (精英)"; // 秩序
            case ChatSpeaker.Love: return "友爱部 (民众)"; // 友爱
            case ChatSpeaker.Peace: return "和平部 (军队)"; // 和平
            default: return "???";
        }
    }

    // ## 修复: 此辅助方法现在在正确的位置 ##
    private ChatSpeaker ConvertFactionToSpeaker(FactionType faction)
    {
        switch (faction)
        {
            case FactionType.Truth: return ChatSpeaker.Truth;
            case FactionType.Order: return ChatSpeaker.Order;
            case FactionType.Love: return ChatSpeaker.Love;
            case FactionType.Peace: return ChatSpeaker.Peace;
            default: return ChatSpeaker.Truth; // 默认
        }
    }
}