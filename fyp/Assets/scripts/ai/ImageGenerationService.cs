using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ####################################################################
// ## AI 结局生成服务 (V3.0 - 恢复生图 API 版)
// ## 1. 恢复 Google Imagen API 调用
// ## 2. 保留“单例覆盖”修复，确保 Build 后正常
// ####################################################################
public class ImageGenerationService : MonoBehaviour
{
    public static ImageGenerationService Instance { get; private set; }

    // ## 请填入你的 API Key ##
    private const string apiKey = "AIzaSyD9TU-JgE9Tcjmkmo3c-lJoA0AktuT_dTY"; // ⚠️ 记得填回你的 Key
    private const string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/imagen-4.0-generate-001:predict?key=";

    [System.Serializable]
    private class ImagenResponse
    {
        public List<Prediction> predictions;
    }
    [System.Serializable]
    private class Prediction
    {
        public string bytesBase64Encoded;
    }

    void Awake()
    {
        // 核心修复：单例覆盖模式 (Singleton Override)
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }

    // 公开方法：生成结局 (图片 + 文字)
    public void GenerateEndingImage(string gameDataJson, Action<Texture2D, List<string>> onComplete)
    {
        StartCoroutine(RequestImageCoroutine(gameDataJson, onComplete));
    }

    // 公开方法：计算结局 ID
    public int CalculateEndingID(string gameDataJson)
    {
        GameEndingData data = JsonUtility.FromJson<GameEndingData>(gameDataJson);
        int opinion = data.totalPublicOpinion;
        int scoreOrder = GetScore(data, "Order");
        int scoreLove = GetScore(data, "Love");
        int scorePeace = GetScore(data, "Peace");
        int maxScore = Mathf.Max(scoreOrder, scoreLove, scorePeace);

        // 1. 氛围层
        int layer1 = 0;
        if (opinion > -1) layer1 = 0;
        else if (opinion == -1) layer1 = 1;
        else layer1 = 2;

        // 2. 统治层
        int layer2 = 0;
        if (opinion >= -1 && maxScore < 1) layer2 = 0;
        else if (maxScore >= 2)
        {
            if (scorePeace == maxScore) layer2 = 1;
            else if (scoreOrder == maxScore) layer2 = 2;
            else if (scoreLove == maxScore) layer2 = 3;
        }
        else layer2 = 4;

        // 3. 冲突层
        int layer3 = 0;
        if ((scorePeace - scoreOrder) > 2) layer3 = 1;
        else if ((scoreOrder - scorePeace) > 2) layer3 = 2;
        else if ((scorePeace - scoreLove) > 2) layer3 = 3;
        else if ((scoreLove - scorePeace) > 2) layer3 = 4;
        else if ((scoreLove - scoreOrder) > 2) layer3 = 5;
        else if ((scoreOrder - scoreLove) > 2) layer3 = 6;
        else layer3 = 0;

        return (layer1 * 35) + (layer2 * 7) + layer3 + 1;
    }

    private IEnumerator RequestImageCoroutine(string gameDataJson, Action<Texture2D, List<string>> onComplete)
    {
        Debug.Log("ImageGenerationService: 开始请求 API 生成结局...");

        GameEndingData data = JsonUtility.FromJson<GameEndingData>(gameDataJson);

        string visualPrompt;
        List<string> narrativeParts;
        ConstructPromptFromRules(data, out visualPrompt, out narrativeParts);

        // 构造请求 JSON
        string jsonRequestData = $@"{{
            ""instances"": [
                {{ ""prompt"": ""{visualPrompt.Replace("\"", "\\\"").Replace("\n", " ")}"" }}
            ],
            ""parameters"": {{
                ""sampleCount"": 1,
                ""aspectRatio"": ""16:9""
            }}
        }}";

        using (UnityWebRequest www = new UnityWebRequest(apiUrl + apiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API Error: " + www.error + "\nBody: " + www.downloadHandler.text);
                // 失败时，返回 null 图片，但保留文字
                onComplete?.Invoke(null, narrativeParts);
            }
            else
            {
                try
                {
                    string jsonResponse = www.downloadHandler.text;
                    ImagenResponse response = JsonUtility.FromJson<ImagenResponse>(jsonResponse);

                    if (response != null && response.predictions != null && response.predictions.Count > 0)
                    {
                        byte[] imageData = Convert.FromBase64String(response.predictions[0].bytesBase64Encoded);
                        Texture2D texture = new Texture2D(2, 2);
                        texture.LoadImage(imageData);
                        Debug.Log("API 生图成功！");
                        onComplete?.Invoke(texture, narrativeParts);
                    }
                    else
                    {
                        Debug.LogWarning("API 返回数据为空");
                        onComplete?.Invoke(null, narrativeParts);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("解析异常: " + e.Message);
                    onComplete?.Invoke(null, narrativeParts);
                }
            }
        }
    }

    private void ConstructPromptFromRules(GameEndingData data, out string visualPrompt, out List<string> narrativeParts)
    {
        StringBuilder visuals = new StringBuilder();
        narrativeParts = new List<string>();

        int opinion = data.totalPublicOpinion;
        int scoreOrder = GetScore(data, "Order");
        int scoreLove = GetScore(data, "Love");
        int scorePeace = GetScore(data, "Peace");
        int maxScore = Mathf.Max(scoreOrder, scoreLove, scorePeace);

        // 0. Base Style
        visuals.Append("Concept art, 1970s retro-futurism style, cold war aesthetic. A frozen post-apocalyptic city centered around a massive, industrial Fusion Energy Tower. Dystopian style, film grain, low saturation colors. Wide shot, 16:9 composition. ");

        // 1. Atmosphere
        string part1 = "";
        if (opinion > -1)
        {
            part1 = "Through the Truth Ministry's efforts, the balance between factions is maintained.";
            visuals.Append("A massive fusion tower glowing with stable, warm yellow incandescent light. ");
        }
        else if (opinion == -1)
        {
            part1 = "The Truth Ministry struggles to maintain the delicate balance between factions.";
            visuals.Append("A fusion tower emitting black smoke and sparks, flickering dim light. ");
        }
        else
        {
            part1 = "Chaotic judgments by the Truth Ministry have caused the original balance to collapse.";
            visuals.Append("A dying energy tower glowing ominous emergency red, city streets frozen over, 1970s cars buried in snow, ice icicles hanging from streetlights. ");
        }
        narrativeParts.Add(part1);

        // 2. Social
        string part2 = "";
        if (opinion >= -1 && maxScore < 1)
        {
            part2 = "Civilization has gained a brief respite.";
            visuals.Append("A balanced city layout, people of different factions mixing in a concrete plaza under the tower, warm streetlights, steam pipes connecting all districts. ");
        }
        else if (maxScore >= 2)
        {
            if (scorePeace == maxScore)
            {
                part2 = "However, military power is gradually spiraling out of control...";
                visuals.Append("Cold War era tanks and soldiers in heavy trench coats and gas masks patrolling, martial law, red banners, concrete roadblocks, searchlights. ");
            }
            else if (scoreOrder == maxScore)
            {
                part2 = "However, the power of the elite is gradually spiraling out of control...";
                visuals.Append("Massive brutalist glass-and-concrete bunkers protecting the upper city, scientists in white coats visible inside, rows of mainframe computers, clean but cold. ");
            }
            else if (scoreLove == maxScore)
            {
                part2 = "However, populist power is gradually spiraling out of control...";
                visuals.Append("Dense shantytown made of corrugated iron and colorful fabric sheets built against the generator, bonfires in oil drums, crowded with civilians. ");
            }
        }
        else
        {
            part2 = "However, the future of civilization remains uncertain...";
            visuals.Append("City divided by barbed wire fences and concrete checkpoints, distinct lighting colors for different sectors, separated by snow. ");
        }
        narrativeParts.Add(part2);

        // 3. Conflict
        string part3 = "";
        if ((scorePeace - scoreOrder) > 2)
        {
            part3 = "Truth is forced into silence at gunpoint.";
            visuals.Append("Soldiers in trench coats burning piles of paper files and microfilm reels in oil drums, ruined archive building in background. ");
        }
        else if ((scoreOrder - scorePeace) > 2)
        {
            part3 = "A soldier's honor is worthless before the ledger.";
            visuals.Append("Industrial style drones patrolling the sky, a military officer in a tattered greatcoat begging on the roadside. ");
        }
        else if ((scorePeace - scoreLove) > 2)
        {
            part3 = "Civilians have become lambs to the slaughter in the face of absolute violence.";
            visuals.Append("Riot police with 70s style helmets and shields using water cannons on civilians, frozen ice sculptures on the street. ");
        }
        else if ((scoreLove - scorePeace) > 2)
        {
            part3 = "Anarchic revelry has reached its climax.";
            visuals.Append("A giant statue of a general being pulled down by cheering civilians, ropes and graffiti, background burning military checkpoint ruins, smoke and sparks. ");
        }
        else if ((scoreLove - scoreOrder) > 2)
        {
            part3 = "Primal fire savagely destroys order.";
            visuals.Append("A mob with molotov cocktails storming a pristine brutalist government building, shattered glass, fire contrasting with cold blue office lights. ");
        }
        else if ((scoreOrder - scoreLove) > 2)
        {
            part3 = "Luxury reigns atop the high tower, leaving a winter of despair for the ragged below.";
            visuals.Append("View from a high office window looking down at frozen, snow-covered slums, silent and dark. ");
        }
        else
        {
            part3 = "The city holds its breath, waiting for what comes next.";
            visuals.Append("A wide shot of the city shrouded in thick freezing fog, silhouettes of people looking up at the tower, atmosphere of mystery. ");
        }
        narrativeParts.Add(part3);

        visualPrompt = visuals.ToString();
    }

    private int GetScore(GameEndingData data, string factionName)
    {
        foreach (var entry in data.factionInfluences) { if (entry.faction == factionName) return entry.influenceScore; }
        return 0;
    }
}