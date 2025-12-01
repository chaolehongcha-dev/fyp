using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum ChatSpeaker
{
    Truth = 0, // 真理部
    Order = 1, // 秩序部
    Love = 2,  // 友爱部
    Peace = 3, // 和平部
    None = 4   // 空
}

public class ChatSystem : MonoBehaviour
{
    public static ChatSystem Instance { get; private set; }

    [Header("UI 引用")]
    public Text topNameText;
    public Transform contentParent;
    public GameObject chatMessagePrefab;
    public ScrollRect scrollRect;

    [Header("UI 提示 (红点)")]
    public GameObject hintTruth;
    public GameObject hintBlue;
    public GameObject hintYellow;
    public GameObject hintRed;

    private class RuntimeChatMessage
    {
        public string text;
        public bool isPermanent;
        public RuntimeChatMessage(string t, bool p) { text = t; isPermanent = p; }
    }

    private Dictionary<ChatSpeaker, List<RuntimeChatMessage>> messageHistory;
    private ChatSpeaker currentSpeaker = ChatSpeaker.None;

    private GameManager gameManager;
    private bool isFirstBriefingRead = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        messageHistory = new Dictionary<ChatSpeaker, List<RuntimeChatMessage>>();
        foreach (ChatSpeaker speaker in System.Enum.GetValues(typeof(ChatSpeaker)))
        {
            messageHistory[speaker] = new List<RuntimeChatMessage>();
        }

        // 强制重置状态
        currentSpeaker = ChatSpeaker.None;

        // 强制隐藏所有红点
        if (hintTruth) hintTruth.SetActive(false);
        if (hintBlue) hintBlue.SetActive(false);
        if (hintYellow) hintYellow.SetActive(false);
        if (hintRed) hintRed.SetActive(false);
    }

    void Start()
    {
        gameManager = GameManager.Instance;
        SwitchToChat(ChatSpeaker.None);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            SwitchToChat(ChatSpeaker.None);
        }
    }

    public void SwitchToChat(ChatSpeaker speaker)
    {
        currentSpeaker = speaker;

        if (speaker == ChatSpeaker.None)
        {
            if (topNameText) topNameText.text = "系统待机";
            foreach (Transform child in contentParent) Destroy(child.gameObject);
            return;
        }

        if (topNameText) topNameText.text = GetNameFromSpeaker(speaker);

        // 只有在点击进入频道时，才关掉该频道的红点
        GameObject hint = GetHintForSpeaker(speaker);
        if (hint != null) hint.SetActive(false);

        RefreshChatDisplay();

        // 触发剧情检查
        if (speaker == ChatSpeaker.Truth)
        {
            // 只要没读过且在简报阶段，就推进
            if (!isFirstBriefingRead && gameManager != null && gameManager.CurrentState == GameState.CaseBriefing)
            {
                isFirstBriefingRead = true;
                Debug.Log("ChatSystem: 真理部简报已阅，推进游戏阶段...");
                gameManager.EnterStorylinePhase();
            }
        }
    }

    private void RefreshChatDisplay()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        if (messageHistory.ContainsKey(currentSpeaker))
        {
            foreach (RuntimeChatMessage msg in messageHistory[currentSpeaker])
            {
                InstantiateMessagePrefab(msg.text);
            }
        }
        ScrollToBottom();
    }

    private void AddMessage(ChatSpeaker speaker, string message, bool isPermanent)
    {
        if (speaker == ChatSpeaker.None) return;

        if (messageHistory.ContainsKey(speaker))
        {
            messageHistory[speaker].Add(new RuntimeChatMessage(message, isPermanent));
        }

        if (speaker == currentSpeaker)
        {
            InstantiateMessagePrefab(message);
            ScrollToBottom();
        }
        else
        {
            // 不在当前频道，显示红点
            GameObject hint = GetHintForSpeaker(speaker);
            if (hint != null)
            {
                hint.SetActive(true);
            }
        }
    }

    public void ClearTransientMessages()
    {
        foreach (ChatSpeaker speaker in messageHistory.Keys)
        {
            messageHistory[speaker].RemoveAll(msg => !msg.isPermanent);
        }
        if (currentSpeaker != ChatSpeaker.None) RefreshChatDisplay();
    }

    public void ShowBriefing(List<ChatMessage> messages)
    {
        isFirstBriefingRead = false;
        if (messages == null || messages.Count == 0) return;

        Debug.Log($"ChatSystem: 收到简报 {messages.Count} 条");

        foreach (var msg in messages)
        {
            AddMessage(msg.sender, msg.messageContent, false);
        }

        // ## 核心修复: 强制激活真理部红点 ##
        // 在 Build 中 AddMessage 的判断可能因为时序问题失效，这里手动兜底
        if (currentSpeaker != ChatSpeaker.Truth && hintTruth != null)
        {
            hintTruth.SetActive(true);
            Debug.Log("ChatSystem: 强制激活真理部红点");
        }
    }

    public void ShowFactionMessages(List<ChatMessage> messages)
    {
        foreach (var msg in messages)
        {
            AddMessage(msg.sender, msg.messageContent, false);
        }
    }

    public void ShowEvaluationMessages(FactionType faction, List<ChatMessage> messages)
    {
        ChatSpeaker speaker = ConvertFactionToSpeaker(faction);
        foreach (var msg in messages)
        {
            AddMessage(speaker, msg.messageContent, false);
        }
    }

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